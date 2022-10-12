using ChargerMessages;
using Proto;
using Proto.Cluster;
using System.Net.WebSockets;
using System.Text;

namespace LFA
{
    //Messages for internal use in Project. External messages in "ChargerGatewayMessages.proto"
    record MessageFromCharger(WebSocketReceiveResult Message, byte[] Buffer);
    record WebSocketCreated(string Identity,WebSocket Ws);

    public class ChargerGatewayActor:IActor
    {
        private string identity="uninitializedChargerActor";
        private ChargerGrainClient? virtualGrain;
        private WebSocket? websocket;


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
                case CommandToChargerMessage command:
                    CommandToCharger(command);
                    break;
                case MessageFromCharger message:
                    SendMessage(message);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Prepare Actor with necesary information. Called by HttpController upon established WS Connection
        /// </summary>
        /// <param name="word">Custom type with serial-number and WS-connection</param>
        /// <param name="context">Actor system context</param>
        private void Setup(WebSocketCreated message, IContext context)
        {
            websocket = message.Ws;
            identity = message.Identity;
            virtualGrain = context.System.Cluster().GetChargerGrain(identity: identity);
            PID pidDto = new() { Id = context.Self.Id, Address = context.Self.Address };
            _ = virtualGrain.NewWebSocketFromCharger(new ChargerActorIdentity { Pid = pidDto, SerialNumber = identity }, CancellationToken.None); ;
            Console.WriteLine("Virtual Actor Notified of new connection");
        }

        private async void SendMessage(MessageFromCharger message)
        {
            ChargerMessages.MessageFromCharger msg =new();
            msg.Msg = Encoding.Default.GetString(message.Buffer);
            msg.From = identity;
            Console.WriteLine("Message forwarded: " + msg.From + "  " + msg.Msg);
            await virtualGrain.ReceiveMsgFromCharger(msg,CancellationToken.None);


        }

        public async void CommandToCharger(LFA.CommandToChargerMessage request)
        {
            Console.WriteLine("forwarding command to charger");
            byte[] bytes = Encoding.ASCII.GetBytes(request.Payload);
            await websocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            //Todo: return OK to grain
        }
    }
}
