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
        private readonly FakeWebSocket ws;
        private readonly ActorSystem actorSystem;
        private readonly ActorSystemClusterHostedService hs;
        private readonly WebSocketReceiveResult message = new(0, new WebSocketMessageType(), true);
        private static int result = 0;
        readonly Props chargerProps;
        private static string? messageReceivedContent;
        private ChargerGrainClient virtualGrain;

        public static int Result { get => result; set => result = value; }
        public static int MessageReceivedCount { get => messageReceivedCount; set => messageReceivedCount = value; }
        public static string? MessageReceivedContent { get => messageReceivedContent; set => messageReceivedContent = value; }

        public GatewayActorTest()
        {
            actorSystem = ActorSystemConfigurationTest.CreateActorSystem();
            hs = new ActorSystemClusterHostedService(actorSystem);
            _ = hs.StartAsync(CancellationToken.None);
            chargerProps = Props.FromProducer(() => new ChargerGatewayActor());
            uut = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");
            ws = new FakeWebSocket();
            actorSystem.Root.Send(uut, new WebSocketCreated("123", ws));
            virtualGrain = actorSystem.Cluster().GetChargerGrain("123");

        }

        //Connecting
        [Fact]
        public void CsIsNotified()
        {
            Result = 0;
            Thread.Sleep(50);//Waiting for actor logic, actorsystem traffic and Grain
            Assert.Equal(1, Result);
        }

        [Fact]
        public void DuplicateActorIsCreated()
        {
            //Result = 0;
            var pid = actorSystem.Root.SpawnPrefix(chargerProps, "testActor");
            actorSystem.Root.Send(pid, new WebSocketCreated("123", new FakeWebSocket()));

            Thread.Sleep(70);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(2, Result);
        }
        //Messages
        private static int messageReceivedCount = 0;
        [Fact]
        public async void MessageIsForwardedToGrain()
        {
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("123")));
            var virtualGrain = actorSystem.Cluster().GetChargerGrain("123");
            Thread.Sleep(80);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            //await virtualGrain.StartCharging(CancellationToken.None);
            //Thread.Sleep(80);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(1, MessageReceivedCount);
        }


        [Fact]
        public void MessageContentIsForwardedCorrect()
        {
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("123")));
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("123", MessageReceivedContent);
        }

        [Fact]
        public void EmptyMessageIsForwardedCorrect()
        {
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes("")));
            Thread.Sleep(120);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("",MessageReceivedContent);
        }
        [Fact]
        public void LongMessageIsForwardedCorrect()
        {
            var str = "8792rcyn348975y39478y5cnn0tn03978ctyn93748yctn35ctn89t5y598c7tynt9g54c3gæc546hæc54h5øchcæ6hæ54c5ø hlk65ifhiu34fhc34y t3g3  2 26945 t349823 g3yfg 38f gryfg2 9yr fg8yf g r328yfg2fyugeudfgeruofy3gr f";
            actorSystem.Root.Send(uut, new LFA.Protocol.MessageFromCharger(message, Encoding.Default.GetBytes(str)));
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(str,MessageReceivedContent);
        }
        //Commands
        [Fact]
        public async void CommandIsForwarded()
        {
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal(1,ws.SendAsyncCalled);
        }
        [Fact]
        public async void CommandIsForwardedCorrect()
        {
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            await virtualGrain.StartCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("Turn on, please :)", Encoding.Default.GetString(ws.sentData));
        }
        [Fact]
        public async void EmptyCommandIsForwardedCorrect()
        {
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            await virtualGrain.StopCharging(CancellationToken.None);
            Thread.Sleep(60);//Waiting for actor logic, actorsystem traffic, spawning and Grain
            Assert.Equal("", Encoding.Default.GetString(ws.sentData));
        }
        //Kill
        [Fact]
        public void KilledActorCantRespond()
        {
            actorSystem.Root.Poison(uut);
            Assert.ThrowsAsync<NullReferenceException>(() => virtualGrain.StartCharging(CancellationToken.None));
        }
        [Fact]
        public void WorkIsFinishedBeforeKill()
        {
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