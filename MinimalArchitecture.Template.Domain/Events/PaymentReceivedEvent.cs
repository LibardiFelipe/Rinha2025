namespace MinimalArchitecture.Template.Domain.Events
{
    public sealed record PaymentReceivedEvent(
        Guid CorrelationId, decimal Amount, DateTimeOffset TimeUtc);
}
