using Dapper;
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
            var dbConnString = config.GetConnectionString("Postgres")!;

            DefaultTypeMap.MatchNamesWithUnderscores = true;
            services.AddScoped<IPaymentRepository>(
                _ => new PaymentRepository(dbConnString));

            services.AddPaymentProcessorHttpClients(config);

            return services;
        }
    }
}
