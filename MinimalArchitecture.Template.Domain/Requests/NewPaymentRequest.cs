using MinimalArchitecture.Template.Domain.Events;

namespace MinimalArchitecture.Template.Domain.Requests
{
    public sealed record NewPaymentRequest(Guid CorrelationId, decimal Amount)
    {
        public PaymentReceivedEvent ToEvent() =>
            new()
            {
                Amount = Amount,
                CorrelationId = CorrelationId,
                ReceivedAtUtc = DateTimeOffset.UtcNow
            };
    }
}
