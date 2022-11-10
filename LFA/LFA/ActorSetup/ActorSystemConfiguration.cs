using ChargerMessages;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace LFA.ActorSetup;

public static class ActorSystemConfiguration
{
    public static void AddKubernetesActorSystem(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton(provider =>
        {
            // actor system configuration

            var actorSystemConfig = ActorSystemConfig
                .Setup().WithActorRequestTimeout(TimeSpan.FromMinutes(60)); ;

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
                ).WithGossipRequestTimeout(TimeSpan.FromMinutes(60))
                .WithTimeout(TimeSpan.FromMinutes(60))
                .WithActorSpawnTimeout(TimeSpan.FromMinutes(60))
                .WithActorRequestTimeout(TimeSpan.FromMinutes(60))
                .WithActorActivationTimeout(TimeSpan.FromMinutes(60));

            // create the actor system

            return new ActorSystem(actorSystemConfig)
                .WithServiceProvider(provider)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);
        });
    }
}
