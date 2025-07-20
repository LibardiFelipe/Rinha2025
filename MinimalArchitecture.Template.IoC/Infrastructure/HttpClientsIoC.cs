using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalArchitecture.Template.Domain.Services;
using MinimalArchitecture.Template.Infrastructure.Configs;
using MinimalArchitecture.Template.Infrastructure.Services;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

namespace MinimalArchitecture.Template.IoC.Infrastructure
{
    internal static class HttpClientsIoC
    {
        public static IServiceCollection AddPaymentProcessorHttpClients(
            this IServiceCollection services, IConfiguration config)
        {
            var processorsConfig = new PaymentProcessorsConfig(config);

            services.AddHttpClient<IDefaultPaymentProcessorService, DefaultPaymentProcessorService>(
                client =>
                {
                    client.BaseAddress = new Uri(processorsConfig.Default.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(15);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy)
                .AddPolicyHandler(CircuitBreakerPolicy);

            services.AddHttpClient<IFallbackPaymentProcessorService, FallbackPaymentProcessorService>(
                client =>
                {
                    client.BaseAddress = new Uri(processorsConfig.Fallback.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy)
                .AddPolicyHandler(CircuitBreakerPolicy);

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> RetryPolicy
        {
            get
            {
                var backoffDelay = Backoff
                    .DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromSeconds(1),
                    retryCount: 3);

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                    .WaitAndRetryAsync(backoffDelay);
            }
        }

        private static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy => HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.2, // 20% de falha
                    samplingDuration: TimeSpan.FromSeconds(3), // Em 3 segundos
                    minimumThroughput: 20,
                    durationOfBreak: TimeSpan.FromSeconds(5)); // Fecha o circuito por 5s
    }
}
