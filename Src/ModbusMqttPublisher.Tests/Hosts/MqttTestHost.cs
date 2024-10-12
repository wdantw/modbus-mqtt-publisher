using Microsoft.Extensions.DependencyInjection;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Infrastructure;
using ModbusMqttPublisher.Server.Services.Mqtt;
using System;

namespace ModbusMqttPublisher.Tests.Hosts
{
    public class MqttTestHost : BaseTestHost
    {
        public IMqttClientFactory MqttClientFactory => Host.Services.GetRequiredService<IMqttClientFactory>();


        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddMqtt();
            services.Configure<MqttOptions>(opt =>
            {
                opt.TcpAddress = "localhost";
                opt.AutoReconnectDelay = TimeSpan.FromMilliseconds(100);
                opt.ConnectionCheckInterval = TimeSpan.FromMilliseconds(100);
            });
        }
    }
}
