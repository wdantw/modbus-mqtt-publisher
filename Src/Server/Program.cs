using MQTTnet;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Services;
using ModbusMqttPublisher.Server.Services.Configuration;
using ModbusMqttPublisher.Server.Services.Modbus;
using ModbusMqttPublisher.Server.Services.Types;
using NModbus;
using ModbusMqttPublisher.Server.Services.Mqtt;

namespace ModbusMqttPublisher
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

			builder.Configuration
                .AddJsonFile(userConfigFile);

			// Add services to the container.

			builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
            builder.Services.Configure<ModbusDeviceTypes>(builder.Configuration.GetSection(ModbusDeviceTypes.SectionName));
			builder.Services.Configure<ModbusPorts>(builder.Configuration.GetSection(ModbusPorts.SectionName));
			builder.Services.Configure<ModbusModifiers>(builder.Configuration.GetSection(ModbusModifiers.SectionName));

			builder.Services.AddTransient<IQueueFactoryService, QueueFactoryService>();
            builder.Services.AddTransient<ModbusLogger>();
            builder.Services.AddTransient<IConfigurationResolver, ConfigurationResolver>();
			builder.Services.AddTransient<IRegisterValueFactory, RegisterValueFactory>();
            if (IsFakeModbus())
                builder.Services.AddTransient<IModbusClientFactory, FakeFactory>();
            else
				builder.Services.AddTransient<IModbusClientFactory, ModbusClientFactory>();
            builder.Services.AddTransient<IMqttClientFactory, MqttClientFactory>();
			builder.Services.AddSingleton<IModbusFactory>(p => new ModbusFactory(null, true, p.GetRequiredService<ModbusLogger>()));
            builder.Services.AddSingleton<ITopicStateService, TopicStateService>();
            builder.Services.AddSingleton<MqttFactory>();
            builder.Services.AddSingleton<IWriteQueueService, WriteQueueService>();

            builder.Services.AddSingleton<MqttPublisher>();
            builder.Services.AddSingleton<IMqttPublisher>(p => p.GetRequiredService<MqttPublisher>());
            builder.Services.AddHostedService(p => p.GetRequiredService<MqttPublisher>());

            builder.Services.AddSingleton<QueueManagerService>();
            builder.Services.AddSingleton<IQueueManagerService>(p => p.GetRequiredService<QueueManagerService>());
            builder.Services.AddHostedService(p => p.GetRequiredService<QueueManagerService>());
            builder.Services.AddHostedService<MqttConsumer>();
            
            var app = builder.Build();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.UseSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}