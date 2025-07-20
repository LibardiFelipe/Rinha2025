namespace MinimalArchitecture.Template.Domain.Events
{
    /* TODO: Saporra tá complexa d+ pra um evento... */
    public sealed class PaymentReceivedEvent
    {
        public required Guid CorrelationId { get; init; }
        public required decimal Amount { get; init; }
        public required DateTimeOffset RequestedAt { get; init; }
        public string? ProcessedBy { get; private set; }
        public int IntegrationAttempts { get; private set; } = 0;
        public int ProcessingAttempts { get; private set; } = 0;

        public PaymentReceivedEvent UpdateProcessedBy(string processorName)
        {
            ProcessedBy = processorName;
            return this;
        }

        public PaymentReceivedEvent IncrementIntegrationAttemps()
        {
            IntegrationAttempts++;
            return this;
        }

        public PaymentReceivedEvent IncrementProcessingAttemps()
        {
            ProcessingAttempts++;
            return this;
        }

        public bool IsValid() =>
            CorrelationId != Guid.Empty && Amount > 0M;
    }
}
