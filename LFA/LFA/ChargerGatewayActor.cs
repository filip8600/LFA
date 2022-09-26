using Proto;
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
                    SendMessage(message);
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

        private void SendMessage(MessageFromCharger message)
        {
            Console.WriteLine(Encoding.Default.GetString(message.buffer[0..18]));
        }

        private void ReceiveCommand(CommandToCharger command)
        {
            Console.WriteLine(command.sender + command.command);
        }

    }
}
