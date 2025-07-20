using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using MinimalArchitecture.Template.Application.Actors;
using MinimalArchitecture.Template.Domain.Requests;
using MinimalArchitecture.Template.IoC.Infrastructure;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

namespace MinimalArchitecture.Template.WebAPI
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            builder.Services.AddOpenApi();
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions
                    .TypeInfoResolverChain
                    .Insert(0, ApiJsonSerializerContext.Default);
            });

            builder.Services.SetupInfrastructure(config);

            var app = builder.Build();
            app.MapOpenApi();
            app.MapScalarApiReference();

            app.MapPost("/payments", (
                [FromBody] NewPaymentRequest request,
                [FromServices] IRequiredActor<PaymentRoutingActor> routing) =>
            {
                var evt = request.ToEvent();
                if (!evt.IsValid())
                    return Results.BadRequest();

                routing.ActorRef.Tell(evt);
                return Results.Accepted();
            });

            await app.RunAsync();
        }
    }

    [JsonSerializable(typeof(NewPaymentRequest))]
    internal partial class ApiJsonSerializerContext : JsonSerializerContext
    {
    }
}
