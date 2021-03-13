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
        /// <summary>
        /// Decodes a PES packet from TS packets.
        /// </summary>
        /// <param name="packet">The TS packet to decode a PES packet from.</param>
        /// <returns>A PES packet if one is decoded, otherwise null.</returns>
        public Pes? DecodePesFromTsPacket(TsPacket packet)
        {
            // If the TS packet is the start of a PES, create a new elementary stream packet
            if (packet.PayloadUnitStartIndicator)
            {
                try
                {
                    _elementaryStreamPacket = new Pes(packet);
                }
                catch
                {
                    _elementaryStreamPacket = null;
                }
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

        /// <summary>
        /// Resets the decoder.
        /// </summary>
        public void Reset() => _elementaryStreamPacket = null;
        #endregion
    }
}
