using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ModbusMqttPublisher.Tests.Common
{
    public static class ServiceCollectionExtension
    {
        public static T AddFakeService<T>(this IServiceCollection services)
            where T : class
        {
            var fakeService = Substitute.For<T>();
            services.AddSingleton(fakeService);
            return fakeService;
        }
    }
}
