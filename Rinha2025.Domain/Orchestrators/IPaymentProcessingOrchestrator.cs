using Rinha2025.Domain.Events;
using Rinha2025.Domain.Utils;

namespace Rinha2025.Domain.Orchestrators
{
    public interface IPaymentProcessingOrchestrator
    {
        Task<Result<PaymentReceivedEvent>> ProcessAsync(
            PaymentReceivedEvent evt, CancellationToken cancellationToken = default);
    }
}
