namespace MinimalArchitecture.Template.Domain.Events
{
    public sealed class PaymentReceivedEvent
    {
        public required Guid CorrelationId { get; init; }
        public required decimal Amount { get; init; }
        public required DateTimeOffset ReceivedAtUtc { get; init; }
        public int IntegrationAttempts { get; private set; } = 0;

        public PaymentReceivedEvent WithIncreasedAttempts()
        {
            IntegrationAttempts++;
            return this;
        }

        public bool IsValid() =>
            CorrelationId != Guid.Empty && Amount > 0M;
    }
}
