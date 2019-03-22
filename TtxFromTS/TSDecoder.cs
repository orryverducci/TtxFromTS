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
        /// <summary>
        /// A processor which generates TS packets from raw data.
        /// </summary>
        TsPacketFactory _packetFactory = new TsPacketFactory();

        /// <summary>
        /// Buffer for an elementary stream packet.
        /// </summary>
        private Pes _elementaryStreamPacket;

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
        /// Occurs when a teletext packet has been successfully decoded from the specified Packet ID.
        /// </summary>
        internal EventHandler<Pes> PacketDecoded;
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
            // For each packet decoded, process it and increase the packet counters if the packet is from the specified packet ID
            foreach (TsPacket packet in packets)
            {
                PacketsReceived++;
                if (PacketID == -1 || packet.Pid == PacketID)
                {
                    PacketsDecoded++;
                    ProcessPacket(packet);
                }
            }
        }

        /// <summary>
        /// Processes TS packets and retrieves elementary stream packets from them.
        /// </summary>
        /// <param name="data">The packet to be processed.</param>
        private void ProcessPacket(TsPacket packet)
        {
            // Check packet is free from errors
            if (packet.TransportErrorIndicator)
            {
                if (!_errorWarning)
                {
                    Logger.OutputWarning("The transport stream contains errors which will be ignored, possibly as a result of poor reception");
                    _errorWarning = true;
                }
                return;
            }
            // Check packet is not encrypted
            if (packet.ScramblingControl != 0)
            {
                if (!_encryptedWarning)
                {
                    Logger.OutputWarning("The specified packet ID contains encrypted packets which will be ignored");
                    _encryptedWarning = true;
                }
                return;
            }
            // If the TS packet is the start of a PES, create a new elementary stream packet, otherwise add to an existing one
            if (packet.PayloadUnitStartIndicator)
            {
                _elementaryStreamPacket = new Pes(packet);
            }
            else
            {
                // Only add the packet if not starting midway through a PES
                if (_elementaryStreamPacket != null)
                {
                    _elementaryStreamPacket.Add(packet);
                }
            }
            // If the TS packet completes a PES, decode it and fire the packet decoded event
            if (_elementaryStreamPacket != null && _elementaryStreamPacket.HasAllBytes())
            {
                _elementaryStreamPacket.Decode();
                PacketDecoded?.Invoke(this, _elementaryStreamPacket);
                _elementaryStreamPacket = null;
            }
        }
        #endregion
    }
}