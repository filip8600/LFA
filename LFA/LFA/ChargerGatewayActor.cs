using CSSimulator;
using Proto;
using Proto.Cluster;
using System.Net.WebSockets;
using System.Text;

namespace LFA
{
    record CommandToCharger(string command, string sender);
    record MessageFromCharger(WebSocketReceiveResult message, byte[] buffer);
    record WebSocketCreated(string message);

    public class ChargerGatewayActor : IActor
    {
        private string identity="uninitializedChargerActor";
        private ChargerGrainClient virtualGrain;
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
                    ReceiveCommand(command);
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
            //Todo: Send hello message
        }

        private async void SendMessage(MessageFromCharger message)
        {
            CSSimulator.MessageFromCharger msg=new CSSimulator.MessageFromCharger();
            msg.Msg = Encoding.Default.GetString(message.buffer[0..18]);
            msg.From = identity;
            Console.WriteLine("Message forwarded: " + msg.From + "  " + msg.Msg);
            
            await virtualGrain.ReceiveMsgFromCharger(msg,CancellationToken.None);

        }

        private void ReceiveCommand(CommandToCharger command)
        {
            Console.WriteLine(command.sender + command.command);
            //Todo: Send message via WebSocket
        }

    }
}
