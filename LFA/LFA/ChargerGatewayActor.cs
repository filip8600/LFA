using ChargerMessages;
using LFA.Protocol;
using Proto;
using Proto.Cluster;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using MessageFromCharger = LFA.Protocol.MessageFromCharger;

namespace LFA
{


    /// <summary>
    /// Actor representing 1 charging staion. Can comunicate with a "real" charger using WebSocekt and Central System using a Virtual Actor Grain
    /// </summary>
    public class ChargerGatewayActor : IActor
    {
        private string identity = "uninitializedChargerActor";
        private ChargerGrainClient virtualGrain;
        private WebSocket? websocket;
        private ProtoMessage.PID pid;

        /// <summary>
        /// Switch for handling messages. Runs for each new message in message queue
        /// </summary>
        /// <param name="context">Actor system context. Automaticly supplied by system</param>
        /// <returns></returns>
        public Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;
            switch (msg)
            {
                case Started:
                    break;
                case WebSocketCreated message:
                    Setup(message, context);
                    break;
                case ResendSetup message:
                    resendSetup();
                    break;
                case CommandToChargerMessage command:
                    CommandToCharger(command);
                    break;
                case MessageFromCharger message:
                    SendMessage(message);
                    break;
                case Stopped message:
                    MessageFromCharger newMessage = new(null, Encoding.ASCII.GetBytes("Offline"));
                    SendMessage(newMessage);
                    break;
                case Stopping message:
                    break;
                default:
                    throw new InvalidOperationException("This code is not supposed to be reached. An actor probably received a message it could not process.");
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
            pid = pidDto;
            _ = virtualGrain.NewWebSocketFromCharger(new ChargerActorIdentity { Pid = pidDto, SerialNumber = identity }, CancellationToken.None); ;
            Debug.WriteLine("Virtual Actor Notified of new connection");
        }
        private void resendSetup()
        {
            _ = virtualGrain.NewWebSocketFromCharger(new ChargerActorIdentity { Pid = pid, SerialNumber = identity }, CancellationToken.None); ;
            Debug.WriteLine("Virtual Actor Renotified of connection");
        }
        /// <summary>
        /// Send message from charger to Central System. Startet upon new websocket message arriving to controller
        /// </summary>
        /// <param name="message">Record with buffer and ws meta-data</param>
        private async void SendMessage(MessageFromCharger message)
        {
            string ChargerMessage = Encoding.Default.GetString(message.Buffer);
            ChargerMessages.MessageFromCharger msg = new()
            {
                Msg = ChargerMessage,
                From = identity,
                Pid = pid
            };
            Debug.WriteLine("Message forwarded: " + msg.From + "  " + msg.Msg);
            virtualGrain.ReceiveMsgFromCharger(msg, CancellationToken.None);//.WaitAsync(TimeSpan.FromSeconds(300));

        }
        /// <summary>
        /// Command from CS to charger. Receives command and forwards via Websocket to Charger
        /// </summary>
        /// <param name="request">CommandToChargerMessage (exposed public in ChargerGatewayMessages.proto</param>
        public async void CommandToCharger(LFA.CommandToChargerMessage request)
        {
            Debug.WriteLine("forwarding command to charger");
            byte[] bytes = Encoding.ASCII.GetBytes(request.Payload);
            var succes = false;
            var details = "Command received";
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
