using Proto;
using ChargerMessages;
using Proto.Cluster;

namespace LFA
{
    public class FakeGrain : ChargerGrainBase
    {
        public FakeGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
        {
        }

        public override Task CommandReceived(CommandStatus request)
        {
            throw new NotImplementedException();
        }

        public override async Task NewWebSocketFromCharger(ChargerActorIdentity request)
        {
            GatewayActorTest.result++;
        }

        public override Task ReceiveMsgFromCharger(ChargerMessages.MessageFromCharger request)
        {
            throw new NotImplementedException();
        }

        public override Task StartCharging()
        {
            throw new NotImplementedException();
        }

        public override Task StopCharging()
        {
            throw new NotImplementedException();
        }
    }
}