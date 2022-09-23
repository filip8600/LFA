using LFA;
using Proto;
using System.Net;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);
//app.UseAuthorization();



var system = new ActorSystem();
var ChargerProps = Props.FromProducer(() => new ChargerGatewayActor());

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ab")
    {
        await context.Response.WriteAsync("Hello");
    }
    await next();
});


    app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
            {
                var pid = system.Root.Spawn(ChargerProps);
                try
                {
                    system.Root.Send(pid, new WebSocketCreated("Alex"));

                    //Recieve messages:
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        var buffer = new byte[1024 * 4];
                        receiveResult = await webSocket.ReceiveAsync(
                                           new ArraySegment<byte>(buffer), CancellationToken.None);
                        system.Root.Send(pid, new MessageFromCharger(receiveResult,buffer));
                    } while (!receiveResult.CloseStatus.HasValue) ;
                }
                catch (WebSocketException e)//Handle client disconnect (Without proper close message)
                {
                    webSocket.Abort();
                    webSocket.Dispose();
                    
                }
                finally
                {
                    //luk actor
                    system.Root.Poison(pid);
                }
            }
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Sorry, only WebSocket accepted");

        }
    }
    else
    {
        await next();
    }

});


app.Run();

