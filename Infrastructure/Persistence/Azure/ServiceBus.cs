using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Azure
{
    public class ServiceBus : IServiceBus
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<ServiceBus> _logger;
        public ServiceBus(ServiceBusClient client, IConfiguration config, ILogger<ServiceBus> logger)
        {
            var queueName = config["Azure:ServiceBus:QueueName"];
            _sender = client.CreateSender(queueName);
            _logger = logger;
        }
        public async Task PublishAsync(Guid shipmentId, string blobName, string correlationId)
        {
            var message = new LabelUploadedMessage
            {
                ShipmentId = shipmentId,
                BlobName = blobName,
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow
            };

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(message))
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = message.CorrelationId,
                ContentType = "application/json",
            };

            try
            {
                await _sender.SendMessageAsync(serviceBusMessage);

                _logger.LogInformation(
                    "Label message sent. ShipmentId={ShipmentId}, CorrelationId={CorrelationId}",
                    shipmentId, correlationId);
            }
            catch(Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send Service Bus message. ShipmentId={ShipmentId}, CorrelationId={CorrelationId}",
                    shipmentId, correlationId);
                    
                throw;
            }
        }
    }
}