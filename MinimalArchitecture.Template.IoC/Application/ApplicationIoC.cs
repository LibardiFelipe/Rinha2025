using Microsoft.Extensions.Configuration;
using MinimalArchitecture.Template.IoC.Application;

namespace Microsoft.Extensions.DependencyInjection
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
