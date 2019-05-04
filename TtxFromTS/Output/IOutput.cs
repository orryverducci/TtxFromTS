using System;
using TtxFromTS.Teletext;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides an interface to teletext data outputs.
    /// </summary>
    internal interface IOutput
    {
        /// <summary>
        /// Gets the output specific statistics.
        /// </summary>
        /// <value>A tuple containing the statistic title and its value.</value>
        (string, string)[] Statistics { get; }

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