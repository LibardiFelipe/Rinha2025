using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Models;
using MinimalArchitecture.Template.Domain.Utils;

namespace MinimalArchitecture.Template.Domain.Services
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
