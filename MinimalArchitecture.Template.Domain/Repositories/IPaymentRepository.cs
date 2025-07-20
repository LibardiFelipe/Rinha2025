using MinimalArchitecture.Template.Domain.Events;

namespace MinimalArchitecture.Template.Domain.Repositories
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<PaymentReceivedEvent>> InserBatchAsync(
            IEnumerable<PaymentReceivedEvent> payments);
    }
}
