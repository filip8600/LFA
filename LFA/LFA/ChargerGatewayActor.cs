using Proto;
using System.Net.WebSockets;

namespace LFA
{
    record CommandToCharger(string command, string sender);
    record MessageFromCharger(WebSocketReceiveResult message, byte[] buffer);
    record WebSocketCreated(string message);

    public class ChargerGatewayActor : IActor
    {
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
        }

        private void SendMessage(MessageFromCharger message)
        {
            Console.WriteLine(message.buffer);
        }

        private void ReceiveCommand(CommandToCharger command)
        {
            Console.WriteLine(command.sender + command.command);
        }

    }
}
