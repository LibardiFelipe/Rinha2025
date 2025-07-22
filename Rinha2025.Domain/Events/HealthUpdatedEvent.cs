using Rinha2025.Domain.Models;

namespace Rinha2025.Domain.Events
{
    public sealed record HealthUpdatedEvent(
        ProcessorHealthModel DefaultHealth, ProcessorHealthModel FallbackHealth);
}
