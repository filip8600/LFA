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
                    Setup(word);
                    break;
                case CommandToCharger command:
                    ReceiveCommand(command);
                    break;
                case MessageFromCharger message:
                    SendMessage(message, context);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        private void Setup(WebSocketCreated word)
        {
            //Forbind til virtuel actor
        }

        private async void SendMessage(MessageFromCharger message, IContext context)
        {
            Console.WriteLine(Encoding.Default.GetString(message.buffer[0..18]));
            var grain = context.System.Cluster().GetChargerGrain(identity: "klaus");
            CSSimulator.MessageFromCharger msg=new CSSimulator.MessageFromCharger();
            msg.Msg = Encoding.Default.GetString(message.buffer[0..18]);
            msg.From = "kurt";
            await grain.ReceiveMsgFromCharger(msg,CancellationToken.None);
        }

        private void ReceiveCommand(CommandToCharger command)
        {
            Console.WriteLine(command.sender + command.command);
        }

    }
}
