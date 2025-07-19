using MinimalArchitecture.Template.Domain.Models;

namespace MinimalArchitecture.Template.Domain.Services
{
    public interface IPaymentProcessorService
    {
        Task<bool> ProcessAsync(
            Guid correlationId, decimal amount,
            DateTimeOffset requestedAt, CancellationToken cancellationToken = default);

        Task<ProcessorHealthModel> GetHealthAsync(
            CancellationToken cancellationToken = default);
    }

    public interface IDefaultPaymentProcessorService : IPaymentProcessorService;
    public interface IFallbackPaymentProcessorService : IPaymentProcessorService;
}
