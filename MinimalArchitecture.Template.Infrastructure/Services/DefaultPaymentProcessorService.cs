using Microsoft.Extensions.Logging;
using MinimalArchitecture.Template.Domain.Services;

namespace MinimalArchitecture.Template.Infrastructure.Services
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
