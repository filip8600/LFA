using ChargerMessages;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Cluster.Partition;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace LFA.ActorSetup;
/// <summary>
/// Configure actorsystem for communication between LFA and CS
/// Prerequisites: Download and run Consul: https://www.consul.io/downloads + ".\consul.exe agent -dev"
/// </summary>
public static class ActorSystemConfigurationConsul
{
    public static void AddConsulActorSystem(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(provider =>
        {
            // actor system configuration

            var actorSystemConfig = ActorSystemConfig
                .Setup().WithActorRequestTimeout(TimeSpan.FromMinutes(60)); ;

            // remote configuration

            var remoteConfig = GrpcNetRemoteConfig
                .BindToLocalhost()
                .WithProtoMessages(new[] { MessagesReflection.Descriptor, ChargerGatewayMessagesReflection.Descriptor });

            // cluster configuration

            var clusterConfig = ClusterConfig
                .Setup(
                    clusterName: "CSSimulatorCluster",
                    clusterProvider: new ConsulProvider(new ConsulProviderConfig()),
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
