using System;
using Cinegy.TsDecoder.TransportStream;

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
        /// Provides an elementary stream packet to the output.
        /// </summary>
        /// <param name="packet">The elementary stream packet.</param>
        void AddPacket(Pes packet);

        /// <summary>
        /// Finalise the output.
        /// </summary>
        void FinishOutput();
    }
}