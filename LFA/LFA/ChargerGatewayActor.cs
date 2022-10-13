using ChargerMessages;
using Proto;
using Proto.Cluster;
using System.Net.WebSockets;
using System.Text;

namespace LFA
{
    //Messages for internal use in Project. External messages in "ChargerGatewayMessages.proto"

    /// <summary>
    /// Message from charger to CS
    /// </summary>
    /// <param name="Message">WebSocket Meta data</param>
    /// <param name="Buffer"> payload</param>
    record MessageFromCharger(WebSocketReceiveResult Message, byte[] Buffer);
    /// <summary>
    /// Message from controller notfying new connection
    /// </summary>
    /// <param name="Identity">Serial number</param>
    /// <param name="Ws">WebSocket Connection for later messages</param>
    record WebSocketCreated(string Identity,WebSocket Ws);
    
    /// <summary>
    /// Actor representing 1 charging staion. Can comunicate with a "real" charger using WebSocekt and Central System using a Virtual Actor Grain
    /// </summary>
    public class ChargerGatewayActor:IActor
    {
        private string identity="uninitializedChargerActor";
        private ChargerGrainClient virtualGrain;
        private WebSocket? websocket;

        /// <summary>
        /// Switch for handling messages. Runs for each new message in message queue
        /// </summary>
        /// <param name="context">Actor system context. Automaticly supplied by system</param>
        /// <returns></returns>
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
        /// <param name="message">Custom type with serial-number and WS-connection</param>
        /// <param name="context">Actor system context</param>
        private void Setup(WebSocketCreated message, IContext context)
        {
            websocket = message.Ws;
            identity = message.Identity;
            virtualGrain = context.System.Cluster().GetChargerGrain(identity: identity);
            ProtoMessage.PID pidDto = new() { Id = context.Self.Id, Address = context.Self.Address };
            _ = virtualGrain.NewWebSocketFromCharger(new ChargerActorIdentity { Pid = pidDto, SerialNumber = identity }, CancellationToken.None); ;
            Console.WriteLine("Virtual Actor Notified of new connection");
        }
        /// <summary>
        /// Send message from charger to Central System. Startet upon new websocket message arriving to controller
        /// </summary>
        /// <param name="message">Record with buffer and ws meta-data</param>
        private async void SendMessage(MessageFromCharger message)
        {
            ChargerMessages.MessageFromCharger msg = new()
            {
                Msg = Encoding.Default.GetString(message.Buffer),
                From = identity
            };
            Console.WriteLine("Message forwarded: " + msg.From + "  " + msg.Msg);
            await virtualGrain.ReceiveMsgFromCharger(msg,CancellationToken.None);


        }
        /// <summary>
        /// Command from CS to charger. Receives command and forwards via Websocket to Charger
        /// </summary>
        /// <param name="request">CommandToChargerMessage (exposed public in ChargerGatewayMessages.proto</param>
        public async void CommandToCharger(LFA.CommandToChargerMessage request)
        {
            Console.WriteLine("forwarding command to charger");
            byte[] bytes = Encoding.ASCII.GetBytes(request.Payload);
            var succes = false;
            var details = "";
            try
            {
                await websocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                succes = true;

            }
            catch (Exception e)
            {
                details = e.Message;
            }
            finally
            {
                await virtualGrain.CommandReceived(new CommandStatus { CommandUid = request.CommandUid, Succeeded = succes, Details = details }, CancellationToken.None);

            }


        }
    }
}
