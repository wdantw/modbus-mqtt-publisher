using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MudbusMqttPublisher.Server.Contracts;
using MudbusMqttPublisher.Server.Services;
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

            builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(nameof(MqttOptions)));

            builder.Services.AddTransient<ISettingsService, SettingsService>();
            builder.Services.AddTransient<IQueueManagerService, QueueManagerService>();
            builder.Services.AddTransient<IQueueFactoryService, QueueFactoryService>();
            builder.Services.AddTransient<IQueueRepository, QueueRepository>();
            builder.Services.AddSingleton<IModbusFactory, ModbusFactory>();
            builder.Services.AddSingleton<ITopicStateService, TopicStateService>();
            builder.Services.AddSingleton<MqttFactory>();
            builder.Services.AddSingleton<MqttPublisher>();
            builder.Services.AddTransient<IMqttPublisher>(p => p.GetRequiredService<MqttPublisher>());

            builder.Services.AddHostedService(p => p.GetRequiredService<MqttPublisher>());
            builder.Services.AddHostedService<MainWorker>();

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