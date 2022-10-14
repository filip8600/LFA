    using Proto;
using ChargerMessages;
using Proto.Cluster;

namespace LFA
{
    public class FakeGrain : ChargerGrainBase
    {
        private PID currentChargerGateway;
        private string identity;

        public FakeGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
        {
        }

        public override async Task CommandReceived(CommandStatus request)
        {
            
        }

        public override async Task NewWebSocketFromCharger(ChargerActorIdentity request)
        {
            GatewayActorTest.result++;
            currentChargerGateway = new PID
            {
                Address = request.Pid.Address,
                Id = request.Pid.Id
            };
            //currentChargerGateway =request.Pid;
            identity = request.SerialNumber;
        }

        public override async Task ReceiveMsgFromCharger(ChargerMessages.MessageFromCharger request)
        {
            GatewayActorTest.messageReceivedCount++;
            GatewayActorTest.messageReceivedContent = request.Msg;
        }

        public override async Task StartCharging()
        {
            Context.Send(currentChargerGateway, new CommandToChargerMessage { Payload = "Turn on, please :)", CommandUid = Guid.NewGuid().ToString() });

        }

        public override async Task StopCharging()
        {
            Context.Send(currentChargerGateway, new CommandToChargerMessage { Payload = "", CommandUid = Guid.NewGuid().ToString() });
        }
    }
}