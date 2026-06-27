using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBanking.Infrastructure.Messaging.Consumers
{
    public static class MassTransitConfig
    {
        public static void AddMassTransitServices(this IServiceCollection services, IConfiguration configuration)
        {
            var rabbitHost = configuration["RabbitMq:Host"];

            services.AddMassTransit(x =>
            {
                x.AddConsumer<UserCreatedConsumer>();

                if (!string.IsNullOrEmpty(rabbitHost))
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(rabbitHost, "/", h =>
                        {
                            h.Username(configuration["RabbitMq:Username"] ?? "guest");
                            h.Password(configuration["RabbitMq:Password"] ?? "guest");
                        });

                        cfg.ReceiveEndpoint("user-created-dlq", e => { });

                        cfg.ReceiveEndpoint("user-created-queue", e =>
                        {
                            e.ConfigureConsumer<UserCreatedConsumer>(context);
                            e.UseMessageRetry(r => r.Interval(30, TimeSpan.FromSeconds(10)));
                            cfg.UseDelayedMessageScheduler();
                        });
                    });
                }
                else
                {
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ReceiveEndpoint("user-created-queue", e =>
                        {
                            e.ConfigureConsumer<UserCreatedConsumer>(context);
                        });
                    });
                }
            });
        }
    }
}
