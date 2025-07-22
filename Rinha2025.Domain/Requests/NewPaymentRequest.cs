using Rinha2025.Domain.Events;

namespace Rinha2025.Domain.Requests
{
    public sealed record NewPaymentRequest(Guid CorrelationId, decimal Amount)
    {
        public PaymentReceivedEvent ToEvent() =>
            new()
            {
                Amount = Amount,
                CorrelationId = CorrelationId,
                RequestedAt = DateTimeOffset.UtcNow
            };
    }
}
