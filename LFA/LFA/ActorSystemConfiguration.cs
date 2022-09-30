using ChargerMessages;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace LFA;

public static class ActorSystemConfiguration
{
    public static void AddActorSystem(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton(provider =>
        {
            // actor system configuration

            var actorSystemConfig = ActorSystemConfig
                .Setup();

        // remote configuration

        var remoteConfig = GrpcNetRemoteConfig
            .BindToAllInterfaces(advertisedHost: configuration["ProtoActor:AdvertisedHost"])
            .WithProtoMessages(new[] { MessagesReflection.Descriptor, ChargerGatewayMessagesReflection.Descriptor });

            // cluster configuration

            var clusterConfig = ClusterConfig
                .Setup(
                    clusterName: "CSSimulatorCluster",
                    clusterProvider: new KubernetesProvider(),
                    identityLookup: new PartitionIdentityLookup()
                );

            // create the actor system

            return new ActorSystem(actorSystemConfig)
                .WithServiceProvider(provider)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);
        });
    }
}
