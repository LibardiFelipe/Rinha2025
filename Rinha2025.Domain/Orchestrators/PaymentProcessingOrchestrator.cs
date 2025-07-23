using Rinha2025.Domain.Events;
using Rinha2025.Domain.Services;
using Rinha2025.Domain.Utils;

namespace Rinha2025.Domain.Orchestrators
{
    public sealed class PaymentProcessingOrchestrator : IPaymentProcessingOrchestrator
    {
        private readonly IDefaultPaymentProcessorService _defaultProcessor;
        private readonly IFallbackPaymentProcessorService _fallbackProcessor;

        public PaymentProcessingOrchestrator(
            IDefaultPaymentProcessorService defaultProcessor,
            IFallbackPaymentProcessorService fallbackProcessor)
        {
            _defaultProcessor = defaultProcessor;
            _fallbackProcessor = fallbackProcessor;
        }

        public async Task<Result<PaymentReceivedEvent>> ProcessAsync(
            PaymentReceivedEvent evt, CancellationToken cancellationToken = default)
        {
            var result = await _defaultProcessor.ProcessAsync(evt, cancellationToken);
            if (result.IsSuccess)
                return result;

            return await _fallbackProcessor.ProcessAsync(evt, cancellationToken);
        }
    }
}
