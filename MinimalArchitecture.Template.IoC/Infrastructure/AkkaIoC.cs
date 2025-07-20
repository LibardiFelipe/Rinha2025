using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalArchitecture.Template.Application.Actors;
using MinimalArchitecture.Template.Application.Configs;
using MinimalArchitecture.Template.Domain.Services;

namespace MinimalArchitecture.Template.IoC.Infrastructure
{
    internal static class AkkaIoC
    {
        public static IServiceCollection AddAkkaClusterHosting(
            this IServiceCollection services, IConfiguration config)
        {
            var akkaConfig = new AkkaConfig(config);
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

                            var defaultPool = actorSystem.ActorOf(
                                Props.Create<PaymentProcessorActor>("default", defaultProcessor)
                                    .WithRouter(new SmallestMailboxPool(nrOfInstances: 5)), name: "default-pool");

                            var fallbackPool = actorSystem.ActorOf(
                                Props.Create<PaymentProcessorActor>("fallback", fallbackProcessor)
                                    .WithRouter(new SmallestMailboxPool(nrOfInstances: 5)), name: "fallback-pool");

                            var healthMonitor = actorRegistry.Get<HealthMonitorActor>();
                            var paymentRoutingPool = actorSystem.ActorOf(
                                Props.Create<PaymentRoutingActor>(healthMonitor, defaultPool, fallbackPool)
                                    .WithRouter(new SmallestMailboxPool(nrOfInstances: 50)), name: "routing-pool");

                            actorRegistry.Register<PaymentRoutingActor>(paymentRoutingPool);
                        });
                });

            return services;
        }
    }
}
