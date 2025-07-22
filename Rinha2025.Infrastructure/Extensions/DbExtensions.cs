using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rinha2025.Domain.Repositories;

namespace Rinha2025.Infrastructure.Extensions
{
    public static class DbExtensions
    {
        public static async Task<IHost> TestAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var paymentRepository = scope.ServiceProvider
                .GetRequiredService<IPaymentRepository>();

            await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
            {
                await paymentRepository.GetProcessorsSummaryAsync(
                    from: null, to: null);
            }));

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
