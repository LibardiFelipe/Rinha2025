using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalArchitecture.Template.Application.Actors;
using MinimalArchitecture.Template.Application.Configs;
using MinimalArchitecture.Template.Domain.Services;

namespace MinimalArchitecture.Template.IoC.Application
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
                    const int poolSize = 16;
                    const int routingPoolSize = 8;

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

                            var defaultPool = actorSystem.ActorOf(
                                Props.Create<PaymentProcessorActor>(serviceProvider, defaultProcessor)
                                    .WithRouter(new SmallestMailboxPool(poolSize)), name: "default-pool");

                            var fallbackPool = actorSystem.ActorOf(
                                Props.Create<PaymentProcessorActor>(serviceProvider, fallbackProcessor)
                                    .WithRouter(new SmallestMailboxPool(poolSize)), name: "fallback-pool");

                            var healthMonitor = actorRegistry.Get<HealthMonitorActor>();
                            var paymentRoutingPool = actorSystem.ActorOf(
                                Props.Create<PaymentRoutingActor>(healthMonitor, defaultPool, fallbackPool)
                                    .WithRouter(new SmallestMailboxPool(routingPoolSize)), name: "routing-pool");

                            actorRegistry.Register<PaymentRoutingActor>(paymentRoutingPool);
                        })
                        .WithWebHealthCheck(provider);
                });

            return services;
        }
    }
}
