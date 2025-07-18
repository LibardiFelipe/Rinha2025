using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

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

            builder.Services.AddAkka("MinimalArchitecture.Template", (configBuilder, provider) =>
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
                    .WithRemoting(remoteOptions);
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
