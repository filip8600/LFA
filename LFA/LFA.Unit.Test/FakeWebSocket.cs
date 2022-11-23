using System.Net.WebSockets;

namespace LFA
{
    public class FakeWebSocket : WebSocket
    {
        public int SendAsyncCalled = 0;
        public ArraySegment<byte> sentData;

        public override WebSocketCloseStatus? CloseStatus => throw new NotImplementedException();

        public override string? CloseStatusDescription => throw new NotImplementedException();

        public override WebSocketState State => throw new NotImplementedException();

        public override string? SubProtocol => throw new NotImplementedException();

        public override void Abort()
        {
            throw new NotImplementedException();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        { 
            SendAsyncCalled++;
            sentData = buffer;
            await Task.Delay(0);//Fix warning about async/await

        }
    }
}