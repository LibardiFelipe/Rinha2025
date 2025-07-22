using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rinha2025.IoC.Application
{
    public static class ApplicationIoC
    {
        public static IServiceCollection SetupApplication(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddAkkaClusterHosting(config);

            return services;
        }
    }
}
