using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimalArchitecture.Template.Domain.Repositories;
using System.Text;

namespace MinimalArchitecture.Template.Infrastructure.Extensions
{
    public static class DbExtensions
    {
        public static Task MigrateAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var runner = scope.ServiceProvider
                .GetRequiredService<IMigrationRunner>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILogger<MigrationRunner>>();

            try
            {
                if (runner.HasMigrationsToApplyUp())
                {
                    logger?.LogInformation("Aplicando novas migrations...");
                    runner.MigrateUp();
                }
            }
            catch (Exception)
            {
                /* Ignora */
            }

            return Task.CompletedTask;
        }

        public static async Task<IHost> PurgePaymentsAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var paymentRepository = scope.ServiceProvider
                .GetRequiredService<IPaymentRepository>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILogger<MigrationRunner>>();

            try
            {
                logger.LogInformation("Limpando pagamentos anteriores...");
                await paymentRepository.PurgeAsync();
            }
            catch (Exception)
            {
                /* Ignora */
            }
            
            return host;
        }
    }
}
