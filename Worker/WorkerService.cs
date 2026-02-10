using System.Text.Json;
using Application.Interfaces;
using Azure;
using Azure.Messaging.ServiceBus;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Azure;

namespace Worker;

public class WorkerService : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(ServiceBusClient busClient, IConfiguration config,
        IServiceScopeFactory scopeFactory,ILogger<WorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;

        var queueName = config["Azure:ServiceBus:QueueName"];

        _processor = busClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // normal shutdown
        }

        await _processor.StopProcessingAsync();
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        var body = message.Body.ToString();

        var payload = JsonSerializer.Deserialize<LabelUploadedMessage>(body);

        if (payload == null)
        {
            await args.DeadLetterMessageAsync(message, "InvalidPayload");
            _logger.LogError("Invalid payload received. Dead-lettering message.");
            return;
        }

        _logger.LogInformation("Processing message. DeliveryCount: {DeliveryCount}, CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
            message.DeliveryCount, payload.CorrelationId, payload.ShipmentId);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

        var shipment = await unitOfWork.Shipments.GetShipmentByIdAsync(payload.ShipmentId);

        if (shipment == null)
        {
            _logger.LogError("Shipment not found. Dead-lettering. CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                payload.CorrelationId, payload.ShipmentId);
            await args.DeadLetterMessageAsync(args.Message, "Shipment not found");
            return;
        }

        try
        {
            if (shipment.ShipmentEvents.Any(e => e.EventCode == "LABEL_PROCESSED"))
            {
                _logger.LogInformation("Message already processed. Completing. CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                    payload.CorrelationId, payload.ShipmentId);
                await args.CompleteMessageAsync(message);
                return;
            }

            _logger.LogInformation("Downloading blob {BlobName}. CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                payload.BlobName, payload.CorrelationId, payload.ShipmentId);

            using var stream = await blobService.DownloadAsync(payload.BlobName);

            if (stream.ReadByte() == -1)
            {
                _logger.LogWarning("Label file is empty. BlobName: {BlobName}, CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                    payload.BlobName, payload.CorrelationId, payload.ShipmentId);

                shipment.ShipmentEvents.Add(new ShipmentEvent
                {
                    ShipmentId = shipment.Id,
                    EventCode = "FAILED",
                    EventTime = DateTime.UtcNow,
                    Payload = $"Empty label file. BlobName: {payload.BlobName}",
                    CorrelationId = payload.CorrelationId
                });

                shipment.State = ShipmentState.Failed;
                shipment.UpdatedAt = DateTime.UtcNow;

                await unitOfWork.SaveChangesAsync(args.CancellationToken);

                await args.DeadLetterMessageAsync(args.Message, 
                "EmptyLabelBlob", $"Label blob {payload.BlobName} is empty for Shipment ID {payload.ShipmentId}");

                return;
            }

            _logger.LogInformation("Processing label...");

            await Task.Delay(2000);

            shipment.ShipmentEvents.Add(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventCode = "LABEL_PROCESSED",
                EventTime = DateTime.UtcNow,
                CorrelationId = payload.CorrelationId
            });

            shipment.State = ShipmentState.LabelProcessed;
            shipment.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync(args.CancellationToken);

            await args.CompleteMessageAsync(message);

            _logger.LogInformation("Shipment processed successfully! CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                payload.CorrelationId, payload.ShipmentId);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Blob operation failed. Status: {Status}, ErrorCode: {ErrorCode}, BlobName: {BlobName}, CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                ex.Status, ex.ErrorCode, payload.BlobName, payload.CorrelationId, payload.ShipmentId);

            var reason = $"Blob error. Status: {ex.Status}, ErrorCode: {ex.ErrorCode}, BlobName: {payload.BlobName}";

            shipment.ShipmentEvents.Add(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventCode = "FAILED",
                EventTime = DateTime.UtcNow,
                Payload = reason,
                CorrelationId = payload.CorrelationId
            });

            shipment.State = ShipmentState.Failed;
            shipment.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync(args.CancellationToken);

            await args.DeadLetterMessageAsync(message, "BlobOperationFailed", reason);

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected processing error. CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                payload.CorrelationId, payload.ShipmentId);
            
            if (message.DeliveryCount >= 5)
            {
                _logger.LogError("Max retries reached. Dead-lettering. CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                    payload.CorrelationId, payload.ShipmentId);

                shipment.ShipmentEvents.Add(new ShipmentEvent
                {
                    ShipmentId = shipment.Id,
                    EventCode = "FAILED",
                    EventTime = DateTime.UtcNow,
                    Payload = $"Unexpected error: {ex.Message}",
                    CorrelationId = payload.CorrelationId
                });

                shipment.State = ShipmentState.Failed;
                shipment.UpdatedAt = DateTime.UtcNow;

                await unitOfWork.SaveChangesAsync(args.CancellationToken);

                await args.DeadLetterMessageAsync(message, "UnexpectedError");
                return;
            }

            _logger.LogWarning("Retrying message Attempt: {Attempt}, CorrelationId: {CorrelationId}, ShipmentId: {ShipmentId}",
                message.DeliveryCount, payload.CorrelationId, payload.ShipmentId);

            await args.AbandonMessageAsync(message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "ServiceBus error: {ErrorSource}", args.ErrorSource);

        return Task.CompletedTask;
    }

}
