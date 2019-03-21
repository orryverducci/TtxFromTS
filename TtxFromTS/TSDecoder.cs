using System;
using Cinegy.TsDecoder.TransportStream;

namespace TtxFromTS
{
    /// <summary>
    /// Decodes packets of data from a MPEG transport stream.
    /// </summary>
    internal class TSDecoder
    {
        #region Private Fields
        // Setup transport stream decoder
        TsPacketFactory _packetFactory = new TsPacketFactory();
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the packet identifier to be decoded. If set to -1 (the default) all packets are decoded.
        /// </summary>
        /// <value>The packet identifier.</value>
        internal int PacketID { set; get; } = -1;

        /// <summary>
        /// Gets the number of TS packets that have been received from the data provided.
        /// </summary>
        /// <value>The packets received.</value>
        internal int PacketsReceived { private set; get; }

        /// <summary>
        /// Gets the number of TS packets from the specified Packet ID required that have been successfully decoded.
        /// </summary>
        /// <value>The packets decoded.</value>
        internal int PacketsDecoded { private set; get; }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a TS packet has been successfully decoded from the specified Packet ID.
        /// </summary>
        internal EventHandler<TsPacket> PacketDecoded;
        #endregion

        #region Methods
        /// <summary>
        /// Decodes TS packets from the data provided.
        /// </summary>
        /// <param name="data">The data to be decoded.</param>
        internal void DecodeData(byte[] data)
        {
            // Get packets from the data
            TsPacket[] packets = _packetFactory.GetTsPacketsFromData(data);
            // If no packets were decoded, return the function
            if (packets == null)
            {
                return;
            }
            // For each packet decoded, increase the packet counters and fire the decoded event if the packet is from the specified packet ID
            foreach (TsPacket packet in packets)
            {
                PacketsReceived++;
                if (PacketID == -1 || packet.Pid == PacketID)
                {
                    PacketsDecoded++;
                    PacketDecoded?.Invoke(this, packet);
                }
            }
        }
        #endregion
    }
}