using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rinha2025.Domain.Repositories;
using Rinha2025.Infrastructure.Repositories;

namespace Rinha2025.IoC.Infrastructure
{
    public static class InfrastructureIoC
    {
        public static IServiceCollection SetupInfrastructure(
            this IServiceCollection services, IConfiguration config)
        {
            //var dbConnString = config.GetConnectionString("Postgres")!;

            DefaultTypeMap.MatchNamesWithUnderscores = true;
            services.AddSingleton<IPaymentRepository>(
                _ => new InMemoryPaymentRepository());

            services.AddPaymentProcessorHttpClients(config);

            return services;
        }
    }
}
