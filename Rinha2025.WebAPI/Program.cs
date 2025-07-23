using Akka.Actor;
using Akka.HealthCheck.Hosting.Web;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Rinha2025.Application.Actors;
using Rinha2025.Domain.Repositories;
using Rinha2025.Domain.Requests;
using Rinha2025.Infrastructure.Extensions;
using Rinha2025.IoC.Application;
using Rinha2025.IoC.Domain;
using Rinha2025.IoC.Infrastructure;
using Scalar.AspNetCore;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rinha2025.WebAPI
{
    public class Program
    {
        public sealed class SummaryReadModel
        {
            [JsonPropertyName("totalRequests")]
            public long TotalRequests { get; init; }
            [JsonPropertyName("totalAmount")]
            public decimal TotalAmount { get; init; }
        }

        public sealed class SummaryResponseModel
        {
            [JsonPropertyName("default")]
            public required SummaryReadModel Default { get; init; }
            [JsonPropertyName("fallback")]
            public required SummaryReadModel Fallback { get; init; }
        }

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);
            var config = builder.Configuration;

            builder.Services.AddOpenApi();
            builder.Services.AddHealthChecks();
            builder.Services.SetupDomain(config);
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

            app.MapGet("/internal/payments-summary", async (
                [FromServices] ILogger<Program> logger,
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                logger.LogCritical(
                    "Received at internal endpoint: From {From}, To {To}", from, to);

                var payments = await paymentRepository.GetProcessorsSummaryAsync(from, to);
                var defaultPayment = payments.FirstOrDefault(p => p.ProcessedBy == "default")
                    ?? new SummaryRowReadModel();
                var fallbackPayment = payments.FirstOrDefault(p => p.ProcessedBy == "fallback")
                    ?? new SummaryRowReadModel();

                return Results.Ok(new SummaryResponseModel
                {
                    Default = new SummaryReadModel
                    {
                        TotalAmount = defaultPayment.TotalAmount,
                        TotalRequests = defaultPayment.TotalRequests
                    },
                    Fallback = new SummaryReadModel
                    {
                        TotalAmount = fallbackPayment.TotalAmount,
                        TotalRequests = fallbackPayment.TotalRequests
                    }
                });
            });

            app.MapGet("/payments-summary", async (
                [FromServices] ILogger<Program> logger,
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] IConfiguration configuration,
                [FromServices] IPaymentRepository paymentRepository,
                [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to) =>
            {
                var client = httpClientFactory.CreateClient("internal");
                var baseUrl = configuration.GetConnectionString("OtherInstance");

                var queryParams = new List<string>();
                if (from.HasValue)
                    queryParams.Add($"from={WebUtility.UrlEncode(from.Value.ToString("O"))}");
                if (to.HasValue)
                    queryParams.Add($"to={WebUtility.UrlEncode(to.Value.ToString("O"))}");

                var url = $"{baseUrl}/internal/payments-summary{(queryParams.Count > 0 ? "?"
                    + string.Join("&", queryParams) : "")}";

                logger.LogCritical("Calling internal URL {Url}", url);
                var response = await client.GetFromJsonAsync<SummaryResponseModel>(url);
                logger.LogCritical("Return from internal URL {Retorno}", JsonSerializer.Serialize(response));

                if (response is null)
                    return Results.Ok(new SummaryResponseModel {
                        Default = new SummaryReadModel
                        {
                            TotalAmount = 0,
                            TotalRequests = 0
                        },
                        Fallback = new SummaryReadModel
                        {
                            TotalAmount = 0,
                            TotalRequests = 0
                        },
                    });

                var localPayments = await paymentRepository.GetProcessorsSummaryAsync(from, to);
                var defaultLocal = localPayments.FirstOrDefault(p => p.ProcessedBy == "default")
                    ?? new SummaryRowReadModel();
                var fallbackLocal = localPayments.FirstOrDefault(p => p.ProcessedBy == "fallback")
                    ?? new SummaryRowReadModel();

                var defaultTotal = new SummaryReadModel
                {
                    TotalRequests = defaultLocal.TotalRequests + (response?.Default?.TotalRequests ?? 0),
                    TotalAmount = defaultLocal.TotalAmount + (response?.Default?.TotalAmount ?? 0M)
                };

                var fallbackTotal = new SummaryReadModel
                {
                    TotalRequests = fallbackLocal.TotalRequests + (response?.Fallback?.TotalRequests ?? 0),
                    TotalAmount = fallbackLocal.TotalAmount + (response?.Fallback?.TotalAmount ?? 0M)
                };

                return Results.Ok(new SummaryResponseModel
                {
                    Default = defaultTotal,
                    Fallback = fallbackTotal,
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

            await app.RunAsync();
        }
    }
}
