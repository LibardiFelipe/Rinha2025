using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalArchitecture.Template.IoC.Infrastructure
{
    public static class InfraIoC
    {
        public static IServiceCollection SetupInfrastructure(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddPaymentProcessorHttpClients(config);
            services.AddAkkaClusterHosting(config);

            return services;
        }
    }
}
