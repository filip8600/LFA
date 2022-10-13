
using LFA.ActorSetup;
using LFA.Protocol;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using LFA.Unit.Test;

namespace LFA
{
    public class GatewayActorTest : IClassFixture<ChargerGatewayActor>
    {
        private readonly PID uut;
        private readonly ActorSystem actorSystem;
        public static int result = 0;

        public GatewayActorTest()
        {
            var builder = WebApplication.CreateBuilder();
            //builder.SeActorSystemConfigurationTestrvices.AddTestActorSystem();
            actorSystem = ActorSystemConfigurationTest.CreateActorSystem();
            var hs = new ActorSystemClusterHostedService(actorSystem);
            _ = hs.StartAsync(CancellationToken.None);
            var chargerProps = Props.FromProducer(() => new ChargerGatewayActor());
            uut = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");

        }


        [Fact]
        public async void CsIsNotified()
        {
            result = 0;
            actorSystem.Root.Send(uut, new WebSocketCreated("123", new WS()));
            Thread.Sleep(50);//Waiting for actor logic, actorsystem traffic and Grain
            Assert.Equal(1, result);
        }
    }
}