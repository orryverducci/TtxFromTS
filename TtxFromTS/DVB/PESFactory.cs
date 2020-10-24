using System;
using Cinegy.TsDecoder.TransportStream;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Generates elementary stream (PES) packets from TS packets.
    /// </summary>
    public class PESFactory
    {
        #region Private Fields
        /// <summary>
        /// Buffer for an elementary stream packet.
        /// </summary>
        private Pes? _elementaryStreamPacket;
        #endregion

        #region Methods
        public Pes? DecodePesFromTsPacket(TsPacket packet)
        {
            // If the TS packet is the start of a PES, create a new elementary stream packet
            if (packet.PayloadUnitStartIndicator)
            {
                _elementaryStreamPacket = new Pes(packet);
            }
            // If we have an elementary stream packet, add the packet and decode the PES if it is complete
            if (_elementaryStreamPacket != null)
            {
                _elementaryStreamPacket.Add(packet);
                if (_elementaryStreamPacket.HasAllBytes())
                {
                    _elementaryStreamPacket.Decode();
                    return _elementaryStreamPacket;
                }
            }
            // Return null if no other response has been returned
            return null;
        }
        #endregion
    }
}
