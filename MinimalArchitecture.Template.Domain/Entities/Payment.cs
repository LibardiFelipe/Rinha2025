namespace MinimalArchitecture.Template.Domain.Entities
{
    public sealed class Payment
    {
        public Guid CorrelationId { get; init; }
        public decimal Amount { get; init; }
        public string ProcessedBy { get; init; } = string.Empty;
        public DateTime RequestedAtUtc { get; init; }
    }
}
