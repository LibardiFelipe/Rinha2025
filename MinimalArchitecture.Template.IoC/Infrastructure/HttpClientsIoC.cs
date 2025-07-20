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
                .AddPolicyHandler(RetryPolicy);

            services.AddHttpClient<IFallbackPaymentProcessorService, FallbackPaymentProcessorService>(
                client =>
                {
                    client.BaseAddress = new Uri(processorsConfig.Fallback.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy);

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
    }
}
