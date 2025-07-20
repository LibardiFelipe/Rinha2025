using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Repositories;

namespace MinimalArchitecture.Template.Infrastructure.Repositories
{
    public sealed class PaymentRepository : IPaymentRepository
    {
        public Task<IEnumerable<PaymentReceivedEvent>> InserBatchAsync(IEnumerable<PaymentReceivedEvent> payments)
        {
            throw new NotImplementedException();
        }
    }
}
