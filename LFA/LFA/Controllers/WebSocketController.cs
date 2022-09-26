using CSSimulator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            Console.WriteLine("ny req");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using (WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
                {
                    var pid = actorSystem.Root.Spawn(chargerProps);
                    try
                    {
                        actorSystem.Root.Send(pid, new WebSocketCreated("Alex"));
                        var a=actorSystem.Cluster().GetChargerGrain(identity: "dav");
                        await a.StartCharging(CancellationToken.None);
                        //Recieve messages:
                        WebSocketReceiveResult receiveResult;
                        do
                        {
                            var buffer = new byte[1024 * 4];
                            receiveResult = await webSocket.ReceiveAsync(
                                               new ArraySegment<byte>(buffer), CancellationToken.None);
                            actorSystem.Root.Send(pid, new MessageFromCharger(receiveResult, buffer));
                        } while (!receiveResult.CloseStatus.HasValue);
                    }
                    catch (WebSocketException e)//Handle client disconnect (Without proper close message)
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
