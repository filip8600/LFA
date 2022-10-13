using System.Net.WebSockets;

namespace LFA.Protocol
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
    record WebSocketCreated(string Identity, WebSocket Ws);
}
