using Microsoft.Extensions.Configuration;

namespace Rinha2025.Infrastructure.Configs
{
    public sealed class PaymentProcessorConfig
    {
        public string BaseUrl { get; init; } = string.Empty;
    }

    public sealed class PaymentProcessorsConfig
    {
        public PaymentProcessorsConfig(IConfiguration config) =>
            config.GetRequiredSection("PaymentProcessors").Bind(this);

        public PaymentProcessorConfig Default { get; init; } = new();
        public PaymentProcessorConfig Fallback { get; init; } = new();
    }
}
