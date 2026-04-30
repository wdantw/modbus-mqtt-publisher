using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Services;
using ModbusMqttPublisher.Server.Services.Configuration;
using ModbusMqttPublisher.Server.Services.Modbus;
using MQTTnet;
using MQTTnet.DependencyInjection;
using OpenTelemetry.Metrics;

namespace ModbusMqttPublisher.Server
{
    public class Program
    {
        private static bool IsFakeModbus()
        {
            var isFakeModbusEnv = Environment.GetEnvironmentVariable("MODBUS_MQTT_PUBLISHER_FAKE_MODBUS");

            if (string.IsNullOrWhiteSpace(isFakeModbusEnv))
                return false;

            isFakeModbusEnv = isFakeModbusEnv.ToLower();

            return isFakeModbusEnv == "true";
        }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var userConfigFile = Environment.GetEnvironmentVariable("MODBUS_MQTT_PUBLISHER_USERCONFIG");

            if (!string.IsNullOrWhiteSpace(userConfigFile))
                builder.Configuration.AddJsonFile(userConfigFile);

            // Add services to the container.

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<PublisherMqttOptions>(builder.Configuration.GetSection(PublisherMqttOptions.SectionName));
            builder.Services.Configure<ModbusDeviceTypes>(builder.Configuration.GetSection(ModbusDeviceTypes.SectionName));
            builder.Services.Configure<ModbusPorts>(builder.Configuration.GetSection(ModbusPorts.SectionName));
            builder.Services.Configure<ModbusModifiers>(builder.Configuration.GetSection(ModbusModifiers.SectionName));

            builder.Services.AddTransient<IQueueFactoryService, QueueFactoryService>();
            builder.Services.AddTransient<IConfigurationResolver, ConfigurationResolver>();
            if (IsFakeModbus())
                builder.Services.AddTransient<IModbusClientFactory, FakeFactory>();
            else
                builder.Services.AddTransient<IModbusClientFactory, ModbusClientFactory>();

            builder.Services.ConfigureMqtt(builder.Configuration);
            builder.Services.AddMqtt();
            builder.Services.RegisterMqttConsumerSingleton<MqttConsumer>(null);
            builder.Services.BuildMqttConsumerFilter<MqttConsumer, IOptions<PublisherMqttOptions>>((b, o) => b
                .WithTopic(MqttPath.CombineTopicPath(o.Value.BaseTopicPath, MqttTopicFilterComparer.MultiLevelWildcard.ToString()))
                .WithAtLeastOnceQoS());

            builder.Services.AddSingleton<IWriteQueueService, WriteQueueService>();

            builder.Services.AddSingleton<QueueManagerService>();
            builder.Services.AddSingleton<IQueueManagerService>(p => p.GetRequiredService<QueueManagerService>());
            builder.Services.AddHostedService(p => p.GetRequiredService<QueueManagerService>());
            builder.Services.AddTransient<IModbusSerialPortFactory, ModbusSerialPortFactory>();

            // телеметрия

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrix => metrix
                    .AddMeter("ModbusMqttPublisher.*")
                    .AddPrometheusExporter());

            // приложение

            var app = builder.Build();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapRazorPages();
            app.MapControllers();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            app.MapFallbackToFile("index.html");

            app.UseSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}