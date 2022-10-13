using Proto;
using Proto.Cluster;
using System.Diagnostics;

namespace LFA.ActorSetup;
/// <summary>
/// Service for use i main - starting and stopping member in actor system
/// </summary>
public class ActorSystemClusterHostedService : IHostedService
{
    private readonly ActorSystem _actorSystem;

    public ActorSystemClusterHostedService(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Debug.WriteLine("Starting a cluster member");

        await _actorSystem
            .Cluster()
            .StartMemberAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Debug.WriteLine("Shutting down a cluster member");

        await _actorSystem
            .Cluster()
            .ShutdownAsync();
    }
}
