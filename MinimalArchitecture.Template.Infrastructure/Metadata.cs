using Dapper;
using MinimalArchitecture.Template.Domain.Entities;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Models;
using System.Reflection;
using System.Text.Json.Serialization;
[module: DapperAot]

namespace MinimalArchitecture.Template.Infrastructure
{
    public static class Metadata
    {
        public static Assembly Assembly =>
            typeof(Metadata).Assembly;
    }

    [JsonSerializable(typeof(IEnumerable<Payment>))]
    [JsonSerializable(typeof(PaymentReceivedEvent))]
    [JsonSerializable(typeof(ProcessorHealthModel))]
    internal partial class InfraJsonSerializerContext : JsonSerializerContext
    {
    }
}
