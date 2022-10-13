using ChargerMessages;
using LFA.Protocol;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Proto;
using Proto.Cluster;
using System;
using System.Diagnostics;
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
                var response = new ObjectResult("Not a websocket request")
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
                return response;
            }
            if (!IsAuthenticated().Result)
            {
                var response = new ObjectResult("No auth Header - or wrong auth")
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return response;
            }
            using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            if (!HttpContext.Request.Headers.TryGetValue("serial-number", out StringValues identity)) identity = "unknown";
            var pid = actorSystem.Root.SpawnPrefix(chargerProps,identity);
            Debug.WriteLine("Connected to socket with serial-number: " + identity);
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
                        actorSystem.Root.Send(pid, new Protocol.MessageFromCharger(receiveResult, buffer));
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
            try
            {
                var result = await actorSystem.Cluster().GetAuthGrain("auth").Authenticate(authMsg, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
                if (result == null || result.Validated == false)
                {
                    Debug.WriteLine("Actor NOT Validated, cutting connection");
                    return false;
                }
                Debug.WriteLine("Actor Validated");
                return true;

            }
            catch (TimeoutException e)
            {
                Debug.WriteLine("Could not connect to auth grain (Timeout)");
                return false;
            }
            catch (Exception e)
            {
                Debug.Print("Could not connect to auth grain");
                return false;
            }
        }
    }
}
