using Dapper;
using FluentMigrator.Runner;
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

            services.AddMigrator(dbConnString);

            DefaultTypeMap.MatchNamesWithUnderscores = true;
            services.AddScoped<IPaymentRepository>(
                _ => new PaymentRepository(dbConnString));

            services.AddPaymentProcessorHttpClients(config);

            return services;
        }

        private static IServiceCollection AddMigrator(
           this IServiceCollection services, string connectionString)
        {
            services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres15_0()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(MinimalArchitecture.Template.Infrastructure.Metadata.Assembly).For.All())
                .AddLogging(lb => lb.AddFluentMigratorConsole());

            return services;
        }
    }
}
