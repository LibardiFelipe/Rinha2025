namespace MinimalArchitecture.Template.Domain.Events
{
    public sealed record PaymentFailedEvent(
        PaymentReceivedEvent Event, int Attempts);
}
