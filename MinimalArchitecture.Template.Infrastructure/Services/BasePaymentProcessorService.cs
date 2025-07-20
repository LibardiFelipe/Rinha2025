using Microsoft.Extensions.Logging;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Models;
using MinimalArchitecture.Template.Domain.Services;
using MinimalArchitecture.Template.Domain.Utils;
using System.Net.Http.Json;

namespace MinimalArchitecture.Template.Infrastructure.Services
{
    public abstract class BasePaymentProcessorService : IPaymentProcessorService
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger<BasePaymentProcessorService> Logger;

        protected BasePaymentProcessorService(
            HttpClient httpClient, ILogger<BasePaymentProcessorService> logger)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        protected abstract string ProcessorName { get; }

        public async Task<ProcessorHealthModel> GetHealthAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await HttpClient.GetFromJsonAsync<ProcessorHealthModel>(
                    "/payments/service-health", cancellationToken);

                if (result is not null)
                    return result;

                return ProcessorHealthModel.Failing;
            }
            catch
            {
                return ProcessorHealthModel.Failing;
            }
        }

        public async Task<Result<PaymentReceivedEvent>> ProcessAsync(
            PaymentReceivedEvent evt, CancellationToken cancellationToken = default)
        {
            try
            {
                using var result = await HttpClient.PostAsJsonAsync(
                    "/payments", evt, cancellationToken);

                if (result.IsSuccessStatusCode)
                {
                    return Result<PaymentReceivedEvent>.Success(
                        evt.UpdateProcessedBy(ProcessorName));
                }

                return Result<PaymentReceivedEvent>.Failure(evt);
            }
            catch
            {
                return Result<PaymentReceivedEvent>.Failure(evt);
            }
        }
    }
}
