using ModbusMqttPublisher.Server.Services.Mqtt;
using MQTTnet;

namespace ModbusMqttPublisher.Server.Infrastructure
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMqtt(this IServiceCollection services)
        {
            services
                .AddSingleton<MqttFactory>()
                .AddTransient<IMqttClientFactory, MqttClientFactory>()
                .AddSingleton<MqttBus>()
                .AddHostedService(sp => sp.GetRequiredService<MqttBus>())
                .AddSingleton<IMqttBus>(sp => sp.GetRequiredService<MqttBus>())
                ;

            return services;
        }
    }
}
