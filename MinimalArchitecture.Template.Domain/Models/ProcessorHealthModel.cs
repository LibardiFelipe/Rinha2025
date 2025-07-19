using System.Text.Json.Serialization;

namespace MinimalArchitecture.Template.Domain.Models
{
    public sealed class ProcessorHealthModel
    {
        [JsonPropertyName("failing")]
        public required bool IsFailing { get; init; }

        [JsonPropertyName("minResponseTime")]
        public int MinResponseTime { get; init; }

        public static ProcessorHealthModel Failing => new()
        {
            IsFailing = true,
            MinResponseTime = 0
        };
    }
}
