using Rinha2025.Domain.Events;
using Rinha2025.Domain.Models;
using Rinha2025.Domain.Utils;

namespace Rinha2025.Domain.Services
{
    public interface IPaymentProcessorService
    {
        Task<Result<PaymentReceivedEvent>> ProcessAsync(
            PaymentReceivedEvent evt, CancellationToken cancellationToken = default);

        Task<ProcessorHealthModel> GetHealthAsync(
            CancellationToken cancellationToken = default);
    }

    public interface IDefaultPaymentProcessorService : IPaymentProcessorService;
    public interface IFallbackPaymentProcessorService : IPaymentProcessorService;
}
