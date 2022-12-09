using ChargerMessages;
using Proto;
using Proto.Cluster;

namespace LFA
{
    public class FakeGrain : ChargerGrainBase
    {
        private PID? currentChargerGateway;

        public FakeGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
        {
        }

        public override Task CommandReceived(CommandStatus request)
        {
            return Task.CompletedTask;
        }

        public override Task NewWebSocketFromCharger(ChargerActorIdentity request)
        {
            GatewayActorTest.Result++;
            currentChargerGateway = new PID
            {
                Address = request.Pid.Address,
                Id = request.Pid.Id
            };
            return Task.CompletedTask;
        }

        public override async Task ReceiveMsgFromCharger(ChargerMessages.MessageFromCharger request)
        {
            GatewayActorTest.MessageReceivedCount++;
            if (request.Msg == null || request.Msg.Length < 1) GatewayActorTest.MessageReceivedContent = "";
            GatewayActorTest.MessageReceivedContent = request.Msg;
        }

        public override Task StartCharging()
        {
            if (currentChargerGateway is not null) Context.Send(currentChargerGateway, new CommandToChargerMessage { Payload = "Turn on, please :)", CommandUid = Guid.NewGuid().ToString() });
            return Task.CompletedTask;
        }

        public override Task StopCharging()
        {
            if (currentChargerGateway is not null) Context.Send(currentChargerGateway, new CommandToChargerMessage { Payload = "", CommandUid = Guid.NewGuid().ToString() });
            return Task.CompletedTask;
        }
    }
}