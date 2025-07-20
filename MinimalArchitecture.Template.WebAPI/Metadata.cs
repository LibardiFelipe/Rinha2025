using MinimalArchitecture.Template.Domain.Requests;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MinimalArchitecture.Template.WebAPI
{
    public static class Metadata
    {
        public static Assembly Assembly =>
            typeof(Metadata).Assembly;
    }

    [JsonSerializable(typeof(DateTimeOffset?))]
    [JsonSerializable(typeof(NewPaymentRequest))]
    [JsonSerializable(typeof(Dictionary<string, SummaryReadModel>))]
    internal partial class ApiJsonSerializerContext : JsonSerializerContext
    {
    }
}
