using Akka.Actor;
using Akka.HealthCheck.Hosting.Web;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using MinimalArchitecture.Template.Application.Actors;
using MinimalArchitecture.Template.Domain.Repositories;
using MinimalArchitecture.Template.Domain.Requests;
using MinimalArchitecture.Template.Infrastructure.Extensions;
using Scalar.AspNetCore;

namespace MinimalArchitecture.Template.WebAPI
{
    public static class Program
    {
        public sealed record SummaryReadModel(
            long TotalRequests, decimal TotalAmount);

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            builder.Services.AddOpenApi();
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
                var payments = await paymentRepository.GetProcessorsSummaryAsync(from, to);

                var defaultPayment = payments
                    .FirstOrDefault(p => p.ProcessedBy == "default") ?? new SummaryRowReadModel();
                var fabllbackPayment = payments
                    .FirstOrDefault(p => p.ProcessedBy == "fallback") ?? new SummaryRowReadModel();

                return Results.Ok(new
                {
                    Default = new SummaryReadModel(
                        defaultPayment.TotalRequests, defaultPayment.TotalAmount),
                    Fallback = new SummaryReadModel(
                        fabllbackPayment.TotalRequests, fabllbackPayment.TotalAmount)
                });
            });

            app.MapPost("/purge-payments", async (
                [FromServices] IPaymentRepository paymentRepository) =>
            {
                await paymentRepository.PurgeAsync();
                return Results.Ok();
            });

            app.MapHealthChecks("/healthz");
            app.MapAkkaHealthCheckRoutes();

            if (app.Environment.IsDevelopment())
                await app.PurgePaymentsAsync();

            await app.TestAsync();
            await app.RunAsync();
        }
    }
}
