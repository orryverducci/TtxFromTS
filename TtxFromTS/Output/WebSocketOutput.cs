using System;
using EmbedIO;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides output to a WebSocket.
    /// </summary>
    public class WebSocketOutput : PacketStreamOutput, IOutput
    {
        #region Private Fields
        /// <summary>
        /// The web server.
        /// </summary>
        private readonly WebServer _server;

        /// <summary>
        /// The module serving the WebSocket endpont.
        /// </summary>
        private readonly WebSocketPacketModule _webSocketModule;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the web socket output statistics.
        /// </summary>
        /// <value>A tuple containing the statistic title and its value.</value>
        public (string, string)[] Statistics => new (string, string)[0];

        /// <summary>
        /// Gets if output looping is supported for this output.
        /// </summary>
        /// <value><c>true</c> if output looping is supported, <c>false</c> if not.</value>
        public bool LoopSupported { get; } = true;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="T:TtxFromTS.Output.WebSocketOutput"/> class.
        /// </summary>
        public WebSocketOutput()
        {
            _webSocketModule = new WebSocketPacketModule("/");
            _server = new WebServer(Program.Options.Port).WithLocalSessionManager().WithModule(_webSocketModule);
            Logger.OutputInfo($"Server is now listening for connections on port {Program.Options.Port}");
        }
        #endregion

        #region Generic Output Methods
        /// <summary>
        /// Sends teletext packet data from the stream to the output.
        /// </summary>
        /// <param name="packetData">The bytes of data to be output as a span.</param>
        protected override void OutputPacket(Span<byte> packetData) => _webSocketModule.SendPacket(packetData);

        /// <summary>
        /// Finalise the output, stopping the server.
        /// </summary>
        public void FinishOutput() => _server.Dispose();
        #endregion 
    }
}
