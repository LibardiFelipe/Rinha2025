using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rinha2025.Application.Actors;
using Rinha2025.Application.Configs;
using Rinha2025.Application.Messages;
using Rinha2025.Domain.Services;

namespace Rinha2025.IoC.Application
{
    internal static class AkkaIoC
    {
        public static IServiceCollection AddAkkaClusterHosting(
            this IServiceCollection services, IConfiguration config)
        {
            var akkaConfig = new AkkaConfig(config);
            services.WithAkkaHealthCheck(HealthCheckType.Default | HealthCheckType.Cluster);
            services.AddAkka(akkaConfig.SystemName,
                (configBuilder, provider) =>
                {
                    configBuilder
                        .WithClustering(akkaConfig.ClusterOptions)
                        .WithRemoting(akkaConfig.RemoteOptions)
                        .WithSingleton<HealthMonitorActor>(
                            "health-monitor", (actorSystem, actorRegistry, dependencyResolver) =>
                                Props.Create<HealthMonitorActor>(
                                    dependencyResolver.GetService<IDefaultPaymentProcessorService>(),
                                    dependencyResolver.GetService<IFallbackPaymentProcessorService>()),
                                    new ClusterSingletonOptions { Role = akkaConfig.SystemName })
                        .WithActors((actorSystem, actorRegistry, dependencyResolver) =>
                        {
                            var defaultProcessor = dependencyResolver.GetService<IDefaultPaymentProcessorService>();
                            var fallbackProcessor = dependencyResolver.GetService<IFallbackPaymentProcessorService>();
                            var serviceProvider = dependencyResolver.GetService<IServiceProvider>();

                            var healthMonitor = actorRegistry.Get<HealthMonitorActor>();
                            var routingPool = actorSystem.ActorOf(
                                Props.Create<PaymentRoutingActor>(healthMonitor, ActorRefs.Nobody, ActorRefs.Nobody)
                                    .WithRouter(new SmallestMailboxPool(8)), name: "routing-pool");

                            var defaultPool = actorSystem.ActorOf(
                                Props.Create<PaymentProcessorActor>(routingPool, serviceProvider, defaultProcessor)
                                    .WithRouter(new SmallestMailboxPool(6)), name: "default-pool");

                            var fallbackPool = actorSystem.ActorOf(
                                Props.Create<PaymentProcessorActor>(routingPool, serviceProvider, fallbackProcessor)
                                    .WithRouter(new SmallestMailboxPool(6)), name: "fallback-pool");

                            routingPool.Tell(new ProcessorsMessage(defaultPool, fallbackPool));

                            actorRegistry.Register<PaymentRoutingActor>(routingPool);
                        })
                        .WithWebHealthCheck(provider);
                });

            return services;
        }
    }
}
