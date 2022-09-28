using CSSimulator;
using Proto;
using Proto.Cluster;
using System.Net.WebSockets;
using System.Text;

namespace LFA
{
    record CommandToCharger(byte[] command, string sender);
    record MessageFromCharger(WebSocketReceiveResult Message, byte[] buffer);
    record WebSocketCreated(string message,WebSocket ws);

    public class ChargerGatewayActor : IActor
    {
        private string identity="uninitializedChargerActor";
        private ChargerGrainClient virtualGrain;
        private WebSocket websocket;
        //private readonly ActorSystem actorSystem;
        //public ChargerGatewayActor(ActorSystem actorSystem)
        //{
        //    this.actorSystem = actorSystem;
        //}

        public Task ReceiveAsync(IContext context)
        {
            var msg =context.Message;
            switch (msg)
            {
                case Started:
                    break;
                case WebSocketCreated word:
                    Setup(word, context);
                    break;
                case CommandToCharger command:
                    ReceiveCommandAsync(command);
                    break;
                case MessageFromCharger message:
                    SendMessage(message);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        private void Setup(WebSocketCreated word, IContext context)
        {
            identity = word.message;
            virtualGrain = context.System.Cluster().GetChargerGrain(identity: identity);
            //virtualGrain.NewWebSocketFromCharger(new ChargerActorIdentity { Pid = context.Self,SerialNumber=identity });
        }

        private async void SendMessage(MessageFromCharger message)
        {
            CSSimulator.MessageFromCharger msg=new CSSimulator.MessageFromCharger();
            msg.Msg = Encoding.Default.GetString(message.buffer[0..18]);
            msg.From = identity;
            Console.WriteLine("Message forwarded: " + msg.From + "  " + msg.Msg);
            
            await virtualGrain.ReceiveMsgFromCharger(msg,CancellationToken.None);

        }

        private async Task ReceiveCommandAsync(CommandToCharger command)
        {
            Console.WriteLine("forwarding command to charger");
            await websocket.SendAsync(command.command, WebSocketMessageType.Text, true, CancellationToken.None);
            //Todo: return OK to grain
        }

    }
}
