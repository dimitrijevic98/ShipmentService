using System;
using System.Reflection.Metadata;
using Application.Interfaces;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Azure;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerService>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new ServiceBusClient(
        config["Azure:ServiceBus:ConnectionString"]);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    
    var options = new BlobClientOptions
    {
        Retry =
        {
            MaxRetries = 5,
            Mode = RetryMode.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(10)
        }
    };

    return new BlobServiceClient(config["Azure:BlobStorage:ConnectionString"], options);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(typeof(IApplicationDbContext), typeof(ApplicationDbContext));
            
builder.Services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
builder.Services.AddScoped(typeof(IShipmentRepository), typeof(ShipmentRepository));
builder.Services.AddScoped(typeof(IBlobService), typeof(BlobService));

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger());

var host = builder.Build();
host.Run();
