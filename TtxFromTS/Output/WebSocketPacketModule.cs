using System;
using System.Threading.Tasks;
using EmbedIO.WebSockets;

namespace TtxFromTS.Output
{
    /// <summary>
    /// This web server module provides an endpoint which outputs teletext packets via a WebSocket.
    /// </summary>
    public class WebSocketPacketModule : WebSocketModule
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="T:TtxFromTS.Output.WebSocketPacketModule"/> class.
        /// </summary>
        public WebSocketPacketModule(string urlPath) : base(urlPath, true) { }

        /// <summary>
        /// Handles messages received from a client.
        /// </summary>
        /// <param name="context">The WebSocket context.</param>
        /// <param name="rxBuffer">The buffer containing the received data.</param>
        /// <param name="rxResult">The result of the received message.</param>
        /// <returns></returns>
        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult) => Task.CompletedTask;

        /// <summary>
        /// Logs a client connection.
        /// </summary>
        /// <param name="context">The WebSocket context.</param>
        /// <returns>A task responsible for logging.</returns>
        protected override Task OnClientConnectedAsync(IWebSocketContext context) => Task.Run(() => Logger.OutputInfo($"Client Connected: {context.RemoteEndPoint}"));

        /// <summary>
        /// Logs a client connection.
        /// </summary>
        /// <param name="context">The WebSocket context.</param>
        /// <returns>A task responsible for logging.</returns>
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context) => Task.Run(() => Logger.OutputInfo($"Client Disconnected: {context.RemoteEndPoint}"));

        /// <summary>
        /// Sends a teletext packet to all the connect WebSocket clients.
        /// </summary>
        /// <param name="data"></param>
        public void SendPacket(Span<byte> data) => BroadcastAsync(data.ToArray());
    }
}
