using CSSimulator;
using Microsoft.AspNetCore.Http;
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
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using (WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
                {
                    var pid = actorSystem.Root.Spawn(chargerProps);
                    StringValues identity;
                    if (!HttpContext.Request.Headers.TryGetValue("serial-number", out identity)){
                        identity = "unknown";
                    }
                    Console.WriteLine("Connected to socket with serial-number: "+ identity);
                    actorSystem.Root.Send(pid, new WebSocketCreated(identity,webSocket));

                    try
                    {
                        //Recieve messages:
                        WebSocketReceiveResult receiveResult;
                        do
                        {//Todo: brug pipes i stedet for buffer
                            var buffer = new byte[1024 * 4];
                            receiveResult = await webSocket.ReceiveAsync(
                                               new ArraySegment<byte>(buffer), CancellationToken.None);
                            actorSystem.Root.Send(pid, new MessageFromCharger(receiveResult, buffer));
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
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await HttpContext.Response.WriteAsync("Sorry, only WebSocket accepted");

            }

            return new EmptyResult();
        }
    }
}
