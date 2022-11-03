using LFA.ActorSetup;
using LFA.Protocol;
using Microsoft.AspNetCore.Builder;
using Proto;
using LFA.Unit.Test;
using Proto.Cluster;
using ChargerMessages;
using System.Text;
using System.Net.WebSockets;

namespace LFA
{
    public class GatewayActorTest : IDisposable
    {
        private readonly PID uut;
        private readonly ActorSystem actorSystem;
        private readonly ActorSystemClusterHostedService hs;
        private readonly WebSocketReceiveResult message = new(0, new WebSocketMessageType(), true);
        private static int result = 0;
        readonly Props chargerProps;

        public GatewayActorTest()
        {
            actorSystem = ActorSystemConfigurationTest.CreateActorSystem();
            hs = new ActorSystemClusterHostedService(actorSystem);
            _ = hs.StartAsync(CancellationToken.None);
            chargerProps = Props.FromProducer(() => new ChargerGatewayActor());
            uut = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");

        }

        //Connecting
        [Fact]
        public void CsIsNotified()
        {
            Result = 0;
            actorSystem.Root.Send(uut, new WebSocketCreated("123", new WS()));
            Thread.Sleep(50);//Waiting for actor logic, actorsystem traffic and Grain
            Assert.Equal(1, Result);
        }

        [Fact]
        public void DuplicateActorIsCreated()
        {
            Result = 0;
            actorSystem.Root.Send(uut, new WebSocketCreated("123", new WS()));
            var pid = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");
            actorSystem.Root.Send(pid, new WebSocketCreated("123", new WS()));

            Thread.Sleep(70);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(2, Result);
        }
        //Messages
        private static int messageReceivedCount = 0;
        [Fact]
        public async void MessageIsForwardedToGrain()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("123")));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            Thread.Sleep(80);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            //await virtualGrain.StartCharging(CancellationToken.None);
            //Thread.Sleep(80);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(1, MessageReceivedCount);
        }
        private static string? messageReceivedContent;

        public static int Result { get => result; set => result = value; }
        public static int MessageReceivedCount { get => messageReceivedCount; set => messageReceivedCount = value; }
        public static string? MessageReceivedContent { get => messageReceivedContent; set => messageReceivedContent = value; }

        [Fact]
        public void MessageContentIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("123")));
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("123", MessageReceivedContent);
        }
        [Fact]
        public void EmptyMessageIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("")));
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("",MessageReceivedContent);
        }
        [Fact]
        public void LongMessageIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var str = "8792rcyn348975y39478y5cnn0tn03978ctyn93748yctn35ctn89t5y598c7tynt9g54c3gæc546hæc54h5øchcæ6hæ54c5ø hlk65ifhiu34fhc34y t3g3  2 26945 t349823 g3yfg 38f gryfg2 9yr fg8yf g r328yfg2fyugeudfgeruofy3gr f";
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes(str)));
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(str,MessageReceivedContent);
        }
        //Commands
        [Fact]
        public async void CommandIsForwarded()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(1,ws.SendAsyncCalled);
        }
        [Fact]
        public async void CommandIsForwardedCorrect()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
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
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            await virtualGrain.StopCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("", Encoding.Default.GetString(ws.sentData));
        }
        //Kill
        [Fact]
        public void KilledActorCantRespond()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            actorSystem.Root.Poison(uut);
            Assert.ThrowsAsync<NullReferenceException>(() => virtualGrain.StartCharging(CancellationToken.None));
        }
        [Fact]
        public void WorkIsFinishedBeforeKill()
        {
            var ws = new WS();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            for (int i = 0; i < 15; i++)
            {
                actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("123#" + i)));
            }
            actorSystem.Root.Poison(uut); // Also sends message
            Thread.Sleep(300);
            Assert.Equal(16, MessageReceivedCount);
        }

        public void Dispose()
        {
            actorSystem.ShutdownAsync("Test Complete");
            MessageReceivedCount = 0;
            _ = hs.StopAsync(CancellationToken.None);
            GC.SuppressFinalize(this);//Suggested by warning
        }
    }
}