using MQTTnet;
using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Services;
using MudbusMqttPublisher.Server.Services.Configuration;
using MudbusMqttPublisher.Server.Services.Types;
using NModbus;

namespace MudbusMqttPublisher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
            builder.Services.Configure<ModbusDeviceTypes>(builder.Configuration.GetSection(ModbusDeviceTypes.SectionName));
            builder.Services.Configure<ModbusPorts>(builder.Configuration.GetSection(ModbusPorts.SectionName));

			builder.Services.AddTransient<IQueueFactoryService, QueueFactoryService>();
            builder.Services.AddTransient<ModbusLogger>();
            builder.Services.AddTransient<IConfigurationResolver, ConfigurationResolver>();
			builder.Services.AddTransient<IRegisterValueFactory, RegisterValueFactory>();
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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }

            app.UseBlazorFrameworkFiles();
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