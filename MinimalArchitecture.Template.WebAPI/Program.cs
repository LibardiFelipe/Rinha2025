using Akka.Actor;
using Akka.HealthCheck.Hosting.Web;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using MinimalArchitecture.Template.Application.Actors;
using MinimalArchitecture.Template.Domain.Repositories;
using MinimalArchitecture.Template.Domain.Requests;
using MinimalArchitecture.Template.Infrastructure.Extensions;
using Scalar.AspNetCore;

namespace MinimalArchitecture.Template.WebAPI
{
    public sealed record SummaryReadModel(
        long TotalRequests, decimal TotalAmount);

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

            builder.Services.AddHealthChecks();
            builder.Services.SetupApplication(config);
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

            app.MapGet("/payments-summary", async (
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                var payments = await paymentRepository.GetAsync(from, to);

                var summary = payments
                    .GroupBy(p => p.ProcessedBy)
                    .ToDictionary(
                        grouping => grouping.Key,
                        grouping => new SummaryReadModel(
                            TotalRequests: grouping.Count(),
                            TotalAmount: grouping.Sum(payment => payment.Amount)
                        ));

                return Results.Ok(summary);
            });

            app.MapPost("/purge-payments", async (
                [FromServices] IPaymentRepository paymentRepository) =>
            {
                await paymentRepository.PurgeAsync();
                return Results.Ok();
            });

            app.MapHealthChecks("/healthz");
            app.MapAkkaHealthCheckRoutes();

            await app.MigrateAsync();
            await app.PurgePaymentsAsync();
            await app.RunAsync();
        }
    }
}
