using MinimalArchitecture.Template.Domain.Entities;
using MinimalArchitecture.Template.Domain.Events;

namespace MinimalArchitecture.Template.Domain.Repositories
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<PaymentReceivedEvent>> InserBatchAsync(
            IEnumerable<PaymentReceivedEvent> events);

        ValueTask<IEnumerable<Payment>> GetAsync(
            DateTimeOffset? from, DateTimeOffset? to);

        ValueTask PurgeAsync();
    }
}
