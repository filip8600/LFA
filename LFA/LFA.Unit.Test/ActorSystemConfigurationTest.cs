using ChargerMessages;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Testing;
using Proto.Cluster.Partition;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace LFA.Unit.Test;
/// <summary>
/// Configure actorsystem for communication between LFA and CS
/// Prerequisites: Download and run Consul: https://www.consul.io/downloads + ".\consul.exe agent -dev"
/// </summary>
public static class ActorSystemConfigurationTest
{
    public static ActorSystem CreateActorSystem()
    {
        
            // actor system configuration

            var actorSystemConfig = ActorSystemConfig
                .Setup();

            // remote configuration

            var remoteConfig = GrpcNetRemoteConfig
                .BindToLocalhost()
                .WithProtoMessages(new[] { MessagesReflection.Descriptor, ChargerGatewayMessagesReflection.Descriptor });

            // cluster configuration

            var clusterConfig = ClusterConfig
                .Setup(
                    clusterName: "CSSimulatorCluster",
                    clusterProvider: new TestProvider(new TestProviderOptions(), new InMemAgent()),
                    identityLookup: new PartitionIdentityLookup()
                ).WithClusterKind(
                kind: ChargerGrainActor.Kind,
                prop: Props.FromProducer(() =>
            new ChargerGrainActor(
                (context, clusterIdentity) => new FakeGrain(context, clusterIdentity)
            )
        )

    );

        // create the actor system

        return new ActorSystem(actorSystemConfig)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);
       
    }
}
