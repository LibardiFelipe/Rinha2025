using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rinha2025.Domain.Orchestrators;

namespace Rinha2025.IoC.Domain
{
    public static class DomainIoC
    {
        public static IServiceCollection SetupDomain(
            this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IPaymentProcessingOrchestrator, PaymentProcessingOrchestrator>();

            return services;
        }
    }
}
