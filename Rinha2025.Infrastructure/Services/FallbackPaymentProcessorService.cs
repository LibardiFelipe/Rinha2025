using Microsoft.Extensions.Logging;
using Rinha2025.Domain.Services;

namespace Rinha2025.Infrastructure.Services
{
    public sealed class FallbackPaymentProcessorService
        : BasePaymentProcessorService, IFallbackPaymentProcessorService
    {
        public FallbackPaymentProcessorService(
            HttpClient httpClient, ILogger<FallbackPaymentProcessorService> logger)
            : base(httpClient, logger)
        {
        }

        protected override string ProcessorName => "fallback";
    }
}
