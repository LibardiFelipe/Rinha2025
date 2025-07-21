using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalArchitecture.Template.Domain.Repositories;

namespace MinimalArchitecture.Template.Infrastructure.Extensions
{
    public static class DbExtensions
    {
        public static async Task<IHost> TestAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var paymentRepository = scope.ServiceProvider
                .GetRequiredService<IPaymentRepository>();

            for (var i = 0; i < 5; i++)
            {
                await paymentRepository.GetProcessorsSummaryAsync(
                    from: null, to: null);
            }

            return host;
        }

        public static async Task<IHost> PurgePaymentsAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var paymentRepository = scope.ServiceProvider
                .GetRequiredService<IPaymentRepository>();

            try
            {
                await paymentRepository.PurgeAsync();
            }
            catch (Exception) { /* Ignora */ }

            return host;
        }
    }
}
