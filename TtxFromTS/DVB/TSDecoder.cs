using System;
using System.Collections.Generic;
using Cinegy.TsDecoder.TransportStream;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Decodes packets of data from a MPEG transport stream.
    /// </summary>
    public class TSDecoder
    {
        #region Private Fields
        /// <summary>
        /// A processor which generates TS packets from raw data.
        /// </summary>
        private readonly TsPacketFactory _packetFactory = new TsPacketFactory();

        /// <summary>
        /// Indicates if a PES error warning has been output.
        /// </summary>
        private bool _errorWarning;

        /// <summary>
        /// Indicates if an encrypted warning has been output.
        /// </summary>
        private bool _encryptedWarning;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the packet identifier to be decoded. If set to -1 (the default) all packets are decoded.
        /// </summary>
        /// <value>The packet identifier.</value>
        public int PacketID { set; get; } = -1;

        /// <summary>
        /// Gets the number of TS packets that have been received from the data provided.
        /// </summary>
        /// <value>The packets received.</value>
        public int PacketsReceived { private set; get; }

        /// <summary>
        /// Gets the number of TS packets from the specified Packet ID required that have been successfully decoded.
        /// </summary>
        /// <value>The packets decoded.</value>
        public int PacketsDecoded { private set; get; }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes transport stream packets from the data provided.
        /// </summary>
        /// <param name="data">The data to be decoded.</param>
        /// <returns>A list of transport stream packets.</returns>
        public List<TsPacket> DecodePackets(byte[] data)
        {
            // Create a list of TS packets to return
            List<TsPacket> tsPackets = new List<TsPacket>();
            // Get packets from the data
            TsPacket[]? decodedTsPackets = _packetFactory.GetTsPacketsFromData(data);
            // Return empty list if no packets were decoded
            if (decodedTsPackets == null)
            {
                return tsPackets;
            }
            // For each TS packet decoded increase the packet counters and add it to the list of packets to be returned if the packet is from the specified packet ID
            foreach (TsPacket packet in decodedTsPackets)
            {
                // Increase the received packet counter
                PacketsReceived++;
                // If the packet is free from errors and from the right packet ID, add it to the list of packets to be returned
                if (packet.TransportErrorIndicator)
                {
                    if (!_errorWarning)
                    {
                        Logger.OutputWarning("The transport stream contains errors which will be ignored, possibly as a result of poor reception");
                        _errorWarning = true;
                    }
                }
                else if (PacketID == -1 || packet.Pid == PacketID)
                {
                    // Add the packet if it is not encrypted, otherwise output warning
                    if (packet.ScramblingControl == 0)
                    {
                        PacketsDecoded++;
                        tsPackets.Add(packet);
                    }
                    else if (!_encryptedWarning)
                    {
                        Logger.OutputWarning("The specified packet ID contains encrypted packets which will be ignored");
                        _encryptedWarning = true;
                    }
                }
            }
            // Return the decoded elementary stream packets
            return tsPackets;
        }
        #endregion
    }
}
