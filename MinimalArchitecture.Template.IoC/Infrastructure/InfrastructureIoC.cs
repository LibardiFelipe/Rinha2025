using Microsoft.Extensions.Configuration;
using MinimalArchitecture.Template.Domain.Repositories;
using MinimalArchitecture.Template.Infrastructure.Repositories;
using MinimalArchitecture.Template.IoC.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InfrastructureIoC
    {
        public static IServiceCollection SetupInfrastructure(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddPaymentProcessorHttpClients(config);

            return services;
        }
    }
}
