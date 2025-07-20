using Microsoft.Extensions.Logging;
using MinimalArchitecture.Template.Domain.Models;
using MinimalArchitecture.Template.Domain.Services;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalArchitecture.Template.Infrastructure.Services
{
    public abstract class BasePaymentProcessorService : IPaymentProcessorService
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger<BasePaymentProcessorService> Logger;

        private static readonly JsonSerializerOptions s_serializerOptions = new()
        {
            TypeInfoResolver = InfraJsonSerializerContext.Default
        };

        protected BasePaymentProcessorService(
            HttpClient httpClient, ILogger<BasePaymentProcessorService> logger)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        public async Task<ProcessorHealthModel> GetHealthAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await HttpClient.GetFromJsonAsync<ProcessorHealthModel>(
                    "/payments/service-health", s_serializerOptions, cancellationToken);

                if (result is not null)
                    return result;

                return ProcessorHealthModel.Failing;
            }
            catch
            {
                return ProcessorHealthModel.Failing;
            }
        }

        public async Task<bool> ProcessAsync(
            Guid correlationId, decimal amount,
            DateTimeOffset requestedAt, CancellationToken cancellationToken = default)
        {
            try
            {
                using var result = await HttpClient.PostAsJsonAsync(
                    "/payments",
                    new
                    {
                        correlationId,
                        amount,
                        requestedAt
                    }, cancellationToken);

                return result.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    [JsonSerializable(typeof(ProcessorHealthModel))]
    internal partial class InfraJsonSerializerContext : JsonSerializerContext
    {
    }
}
