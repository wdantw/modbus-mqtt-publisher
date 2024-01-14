using MudbusMqttPublisher.Server.Services;

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
            builder.Services.AddHostedService<MainWorker>();
            builder.Services.AddTransient<ISettingsService, SettingsService>();
            builder.Services.AddTransient<IQueueManagerService, QueueManagerService>();
            builder.Services.AddTransient<IQueueFactoryService, QueueFactoryService>();
            

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