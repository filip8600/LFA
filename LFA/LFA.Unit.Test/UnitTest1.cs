
using LFA.ActorSetup;
using LFA.Protocol;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using LFA.Unit.Test;
using Proto.Cluster;
using ChargerMessages;
using System.Text;

namespace LFA
{
    public class GatewayActorTest : IClassFixture<ChargerGatewayActor>, IDisposable
    {
        private readonly PID uut;
        private readonly ActorSystem actorSystem;
        private readonly ActorSystemClusterHostedService hs;
        public static int result = 0;
        Props chargerProps;

        public GatewayActorTest()
        {
            var builder = WebApplication.CreateBuilder();
            //builder.SeActorSystemConfigurationTestrvices.AddTestActorSystem();
            actorSystem = ActorSystemConfigurationTest.CreateActorSystem();
            hs = new ActorSystemClusterHostedService(actorSystem);
            _ = hs.StartAsync(CancellationToken.None);
            chargerProps = Props.FromProducer(() => new ChargerGatewayActor());
            uut = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");

        }

        //Connecting
        [Fact]
        public async void CsIsNotified()
        {
            result = 0;
            actorSystem.Root.Send(uut, new WebSocketCreated("123", new WS()));
            Thread.Sleep(50);//Waiting for actor logic, actorsystem traffic and Grain
            Assert.Equal(1, result);
        }

        [Fact]
        public async void DuplicateCreate()
        {
            result = 0;
            actorSystem.Root.Send(uut, new WebSocketCreated("123", new WS()));
            var pid = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");
            actorSystem.Root.Send(pid, new WebSocketCreated("123", new WS()));

            Thread.Sleep(70);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(2, result);
        }
        //Messages
        public static int messageReceivedCount = 0;
        [Fact]
        public async void MessageIsForwarded()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(null, Encoding.Default.GetBytes("123")));

            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(1, messageReceivedCount);
        }
        public static string messageReceivedContent;
        [Fact]
        public async void MessageIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(null, Encoding.Default.GetBytes("123")));

            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(messageReceivedContent, "123");
        }
        [Fact]
        public async void EmptyMessageIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(null, Encoding.Default.GetBytes("")));

            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(messageReceivedContent, "");
        }
        [Fact]
        public async void LongMessageIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var str = "8792rcyn348975y39478y5cnn0tn03978ctyn93748yctn35ctn89t5y598c7tynt9g54c3gæc546hæc54h5øchcæ6hæ54c5ø hlk65ifhiu34fhc34y t3g3  2 26945 t349823 g3yfg 38f gryfg2 9yr fg8yf g r328yfg2fyugeudfgeruofy3gr f";
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(null, Encoding.Default.GetBytes(str)));

            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(messageReceivedContent, str);
        }
        //Commands
        [Fact]
        public async void CommandIsForwarded()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(40);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(ws.SendAsyncCalled, 1);
        }
        [Fact]
        public async void CommandIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("Turn on, please :)", Encoding.Default.GetString(ws.sentData));
        }
        [Fact]
        public async void EmptyCommandIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StopCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("", Encoding.Default.GetString(ws.sentData));
        }
        //Kill
        [Fact]
        public async void KilledActorCantRespond()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            actorSystem.Root.Poison(uut);
            Assert.ThrowsAsync<NullReferenceException>(() => virtualGrain.StartCharging(CancellationToken.None));
        }
        [Fact]
        public async void WorkIsFinishedBeforeKill()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            for (int i = 0; i < 15; i++)
            {
                actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(null, Encoding.Default.GetBytes("123#" + i)));
            }
            actorSystem.Root.Poison(uut);
            Thread.Sleep(300);
            Assert.Equal(15, messageReceivedCount);
        }

        public void Dispose()
        {
            messageReceivedCount = 0;
            hs.StopAsync(CancellationToken.None);
        }
    }
}