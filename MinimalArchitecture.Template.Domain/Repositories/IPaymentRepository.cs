using MinimalArchitecture.Template.Domain.Events;

namespace MinimalArchitecture.Template.Domain.Repositories
{
    public sealed class SummaryRowReadModel
    {
        public string ProcessedBy { get; init; } = string.Empty;
        public long TotalRequests { get; init; }
        public decimal TotalAmount { get; init; }
    }

    public interface IPaymentRepository
    {
        Task<IEnumerable<PaymentReceivedEvent>> InserBatchAsync(
            IEnumerable<PaymentReceivedEvent> events);

        ValueTask<IEnumerable<SummaryRowReadModel>> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to);

        ValueTask PurgeAsync();
    }
}
