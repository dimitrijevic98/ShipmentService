using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Azure;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {   
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped(typeof(IApplicationDbContext), typeof(ApplicationDbContext));
            
            services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
            services.AddScoped(typeof(IShipmentRepository), typeof(ShipmentRepository));
            services.AddScoped(typeof(IShipmentEventRepository), typeof(ShipmentEventRepository));
            services.AddSingleton(typeof(IServiceBus), typeof(ServiceBus));
            services.AddSingleton(typeof(IBlobService), typeof(BlobService));

            services.AddSingleton(sc =>
            {
                var configuration = sc.GetRequiredService<IConfiguration>();
                var connectionString = configuration["Azure:ServiceBus:ConnectionString"];
                return new ServiceBusClient(connectionString);
            });

            // services.AddSingleton(sc =>
            // {
            //     var configuration = sc.GetRequiredService<IConfiguration>();
            //     var connectionString = configuration["Azure:BlobStorage:ConnectionString"];
            //     return new BlobServiceClient(connectionString);
            // });
            services.AddSingleton(sp =>
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

            
            return services;
        }

    }
}