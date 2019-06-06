using System;
using TtxFromTS.Teletext;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides an interface to teletext data outputs.
    /// </summary>
    public interface IOutput
    {
        /// <summary>
        /// Gets the output specific statistics.
        /// </summary>
        /// <value>A tuple containing the statistic title and its value.</value>
        (string, string)[] Statistics { get; }

        /// <summary>
        /// Gets if output looping is supported for this output.
        /// </summary>
        /// <value><c>true</c> if output looping is supported, <c>false</c> if not.</value>
        bool LoopSupported { get; }

        /// <summary>
        /// Provides a teletext packet to the output.
        /// </summary>
        /// <param name="packet">The teletext packet.</param>
        void AddPacket(Packet packet);

        /// <summary>
        /// Finalise the output.
        /// </summary>
        void FinishOutput();
    }
}