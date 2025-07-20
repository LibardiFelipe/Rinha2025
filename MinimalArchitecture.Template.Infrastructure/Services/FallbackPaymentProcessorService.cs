using Microsoft.Extensions.Logging;
using MinimalArchitecture.Template.Domain.Services;

namespace MinimalArchitecture.Template.Infrastructure.Services
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
