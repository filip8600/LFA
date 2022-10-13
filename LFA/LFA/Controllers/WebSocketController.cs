using ChargerMessages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Proto;
using Proto.Cluster;
using System.Net;
using System.Net.WebSockets;

namespace LFA.Controllers
{
    [Route("")]
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private readonly ActorSystem actorSystem;
        private readonly Props chargerProps;


        public WebSocketController(ActorSystem actorSystem)
        {
            this.actorSystem = actorSystem;
            chargerProps = Props.FromProducer(() => new ChargerGatewayActor());

        }

        [HttpGet("/ws")]
        public async Task<IActionResult> GetAsync()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                var response = new ObjectResult("Not a websocket request");
                response.StatusCode = StatusCodes.Status400BadRequest;
                return response;
            }
            if (!IsAuthenticated().Result)
            {
                var response = new ObjectResult("No auth Header - or wrong auth");
                response.StatusCode = StatusCodes.Status401Unauthorized;
                return response;
            }
            using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            if (!HttpContext.Request.Headers.TryGetValue("serial-number", out StringValues identity)) identity = "unknown";
            var pid = actorSystem.Root.SpawnPrefix(chargerProps,identity);
            Console.WriteLine("Connected to socket with serial-number: " + identity);
            actorSystem.Root.Send(pid, new WebSocketCreated(identity, webSocket));
            await ReceiveMessagesLoop(webSocket, pid);
            return new EmptyResult();
        }

        private async Task ReceiveMessagesLoop(WebSocket webSocket, PID pid)
        {
            try
            {
                WebSocketReceiveResult receiveResult;
                do
                {
                    var buffer = new byte[1024 * 4];
                    receiveResult = await webSocket.ReceiveAsync(
                                       new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (!receiveResult.CloseStatus.HasValue)
                    {
                        actorSystem.Root.Send(pid, new MessageFromCharger(receiveResult, buffer));
                    }
                } while (!receiveResult.CloseStatus.HasValue);
            }
            catch (WebSocketException)//Handle client disconnect (Without proper close message)
            {
                webSocket.Abort();
                webSocket.Dispose();

            }
            finally
            {
                //luk actor
                actorSystem.Root.Poison(pid);
            }
        }

        private async Task<bool> IsAuthenticated()
        {
            if (!HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues auth))
            {
                return false;
            }
            string[] base64Strings = auth.ToString().Split(" ");
            if (base64Strings.Length != 2) return false;
            AuthenticationMessage authMsg = new()
            {
                Credentials = base64Strings[1] // removes "Basic" from authorization
            };
            var result= await actorSystem.Cluster().GetAuthGrain("auth").Authenticate(authMsg, CancellationToken.None);
            if (result == null || result.Validated == false)
            {
                Console.WriteLine("Actor NOT Validated, cutting connection");
                return false;
            }
            Console.WriteLine("Actor Validated");
            return true;
        }
    }
}
