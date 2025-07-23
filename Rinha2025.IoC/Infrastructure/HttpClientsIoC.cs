using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Rinha2025.Domain.Services;
using Rinha2025.Infrastructure.Configs;
using Rinha2025.Infrastructure.Services;

namespace Rinha2025.IoC.Infrastructure
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
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy);

            services.AddHttpClient<IFallbackPaymentProcessorService, FallbackPaymentProcessorService>(
                client =>
                {
                    client.BaseAddress = new Uri(processorsConfig.Fallback.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(15);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy);

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> RetryPolicy
        {
            get
            {
                var backoffDelay = Backoff.DecorrelatedJitterBackoffV2(
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
                    failureThreshold: 0.25,
                    samplingDuration: TimeSpan.FromSeconds(3),
                    minimumThroughput: 20,
                    durationOfBreak: TimeSpan.FromMilliseconds(4));
    }
}
