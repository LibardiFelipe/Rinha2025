namespace Rinha2025.Domain.Events
{
    public sealed record PaymentFailedEvent(
        PaymentReceivedEvent Event, int Attempts);
}
