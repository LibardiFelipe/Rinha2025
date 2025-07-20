using Akka.Cluster.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;

namespace MinimalArchitecture.Template.Application.Configs
{
    public sealed class AkkaConfig
    {
        public AkkaConfig(IConfiguration config)
        {
            config.GetRequiredSection("Akka")
                .Bind(this);
        }

        public string SystemName { get; init; } = string.Empty;
        public ClusterOptions ClusterOptions { get; init; } = new();
        public RemoteOptions RemoteOptions { get; init; } = new();
    }
}
