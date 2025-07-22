using Microsoft.Extensions.Logging;
using Rinha2025.Domain.Services;

namespace Rinha2025.Infrastructure.Services
{
    public sealed class DefaultPaymentProcessorService
        : BasePaymentProcessorService, IDefaultPaymentProcessorService
    {
        public DefaultPaymentProcessorService(
            HttpClient httpClient, ILogger<DefaultPaymentProcessorService> logger)
            : base(httpClient, logger)
        {
        }

        protected override string ProcessorName => "default";
    }
}
