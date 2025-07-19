using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Routing;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Win32;
using MinimalArchitecture.Template.Application.Actors;
using MinimalArchitecture.Template.Domain.Services;
using MinimalArchitecture.Template.Infrastructure.Services;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using static Akka.IO.Tcp;

namespace MinimalArchitecture.Template.WebAPI
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.AddOpenApi();
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions
                    .TypeInfoResolverChain
                    .Insert(0, AppJsonSerializerContext.Default);
            });

            // TODO: Configurar baseUrl e retry
            builder.Services.AddHttpClient<IDefaultPaymentProcessorService, DefaultPaymentProcessorService>();
            builder.Services.AddHttpClient<IFallbackPaymentProcessorService, FallbackPaymentProcessorService>();

            var systemName = "MinimalArchitecture.Template.Akka";
            builder.Services.AddAkka(systemName,
                (configBuilder, provider) =>
            {
                var clusterOptions = new ClusterOptions
                {
                    Roles = ["Web"],
                    SeedNodes = ["...seeds"]
                };

                var remoteOptions = new RemoteOptions
                {
                    PublicHostName = "...hostname",
                    PublicPort = 123,
                    HostName = "0.0.0.0",
                    Port = 123
                };

                configBuilder.WithClustering(clusterOptions)
                    .WithRemoting(remoteOptions)
                    .WithSingleton<HealthMonitorActor>(
                        "healt-monitor", (actorSystem, actorRegistry, dependencyResolver) =>
                        Props.Create<HealthMonitorActor>(
                            dependencyResolver.GetService<IDefaultPaymentProcessorService>(),
                            dependencyResolver.GetService<IFallbackPaymentProcessorService>()),
                        new ClusterSingletonOptions
                        {
                            Role = systemName
                        })
                    .WithActors((actorSystem, actorRegistry, dependencyResolver) =>
                    {
                        var defaultProcessor = dependencyResolver.GetService<IDefaultPaymentProcessorService>();
                        var fallbackProcessor = dependencyResolver.GetService<IFallbackPaymentProcessorService>();

                        var defaultPool = actorSystem.ActorOf(
                            Props.Create<PaymentProcessorActor>(
                                "default", defaultProcessor)
                            .WithRouter(new SmallestMailboxPool(nrOfInstances: 5)), name: "default-pool");

                        var fallbackPool = actorSystem.ActorOf(
                            Props.Create<PaymentProcessorActor>(
                                "fallback", fallbackProcessor)
                            .WithRouter(new SmallestMailboxPool(nrOfInstances: 5)), name: "fallback-pool");

                        var paymentRoutingPool = actorSystem.ActorOf(
                            Props.Create<PaymentRoutingActor>(
                                actorRegistry.Get<HealthMonitorActor>(), defaultPool, fallbackPool)
                            .WithRouter(new SmallestMailboxPool(nrOfInstances: 50)), name: "routing-pool");

                        actorRegistry.Register<PaymentRoutingActor>(paymentRoutingPool);
                    });
            });

            var app = builder.Build();
            app.MapOpenApi();
            app.MapScalarApiReference();

            app.MapGet("/", () => new Todo("Olá mundo!"));

            await app.RunAsync();
        }
    }

    public record Todo(string Title);

    [JsonSerializable(typeof(Todo))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
