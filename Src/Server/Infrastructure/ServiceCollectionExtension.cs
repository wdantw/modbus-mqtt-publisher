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
                .AddTransient<IMqttClientFactory, MqttClientFactory>();

            return services;
        }
    }
}
