using MinimalArchitecture.Template.Domain.Models;

namespace MinimalArchitecture.Template.Domain.Messages
{
    public sealed record HealthUpdatedEvent(
        ProcessorHealthModel DefaultHealth, ProcessorHealthModel FallbackHealth);
}
