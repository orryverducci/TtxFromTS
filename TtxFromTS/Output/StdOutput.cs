using System;
using System.IO;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides output to standard output.
    /// </summary>
    public class StdOutput : PacketStreamOutput, IOutput
    {
        #region Private Fields
        /// <summary>
        /// The stream to standard output.
        /// </summary>
        private readonly Stream _stdOutStream;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the standard output statistics.
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
        /// Initialises a new instance of the <see cref="T:TtxFromTS.Output.StdOutput"/> class.
        /// </summary>
        public StdOutput() => _stdOutStream = Console.OpenStandardOutput();
        #endregion

        #region Generic Output Methods
        /// <summary>
        /// Sends teletext packet data from the stream to the output.
        /// </summary>
        /// <param name="packetData">The bytes of data to be output as a span.</param>
        protected override void OutputPacket(Span<byte> packetData) => _stdOutStream.Write(packetData);

        /// <summary>
        /// Finalise the output, closing the standard out stream.
        /// </summary>
        public void FinishOutput() => _stdOutStream.Dispose();
        #endregion
    }
}
