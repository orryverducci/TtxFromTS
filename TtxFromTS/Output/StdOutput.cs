using System;
using System.IO;
using TtxFromTS.Teletext;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides output to standard output.
    /// </summary>
    public class StdOutput : IOutput
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
        public StdOutput() => _stdOutStream = Console.OpenStandardOutput();
        #endregion

        #region Generic Output Methods
        /// <summary>
        /// Provides a teletext packet to the output.
        /// </summary>
        /// <param name="packet">The teletext packet.</param>
        public void AddPacket(Packet packet)
        {
            // Write the teletext packet to standard out
            _stdOutStream.Write(packet.FullPacketData, 2, packet.FullPacketData.Length - 2);
        }

        /// <summary>
        /// Finalise the output, closing the standard out stream.
        /// </summary>
        public void FinishOutput() => _stdOutStream.Dispose();
        #endregion
    }
}
