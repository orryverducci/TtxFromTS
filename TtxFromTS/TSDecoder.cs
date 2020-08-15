using System;
using Cinegy.TsDecoder.TransportStream;
using TtxFromTS.Teletext;

namespace TtxFromTS
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
        TsPacketFactory _packetFactory = new TsPacketFactory();

        /// <summary>
        /// Buffer for an elementary stream packet.
        /// </summary>
        private Pes? _elementaryStreamPacket;

        /// <summary>
        /// Indicates if a PES error warning has been output.
        /// </summary>
        private bool _errorWarning;

        /// <summary>
        /// Indicates if an encrypted warning has been output.
        /// </summary>
        private bool _encryptedWarning;

        /// <summary>
        /// Indicates if a warning for non-teletext packets has been output.
        /// </summary>
        private bool _invalidPacketWarning;
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

        /// <summary>
        /// Gets and sets if subtitle pages should be decoded.
        /// </summary>
        /// <value><c>true</c> if subtitles should be decoded, <c>false</c> if not.</value>
        public bool EnableSubtitles { get; set; } = false;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a teletext packet has been successfully decoded from the specified Packet ID.
        /// </summary>
        public EventHandler<Packet>? PacketDecoded;
        #endregion

        #region Methods
        /// <summary>
        /// Decodes TS packets from the data provided.
        /// </summary>
        /// <param name="data">The data to be decoded.</param>
        public void DecodeData(byte[] data)
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
        /// <param name="packet">The packet to be processed.</param>
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
                    DecodeTeletextPackets();
                    _elementaryStreamPacket = null;
                }
            }
        }

        /// <summary>
        /// Decodes teletext packets from the complete elementary stream packet.
        /// </summary>
        private void DecodeTeletextPackets()
        {
            // Check the PES is a private stream packet
            if (_elementaryStreamPacket!.StreamId != (byte)PesStreamTypes.PrivateStream1)
            {
                if (!_invalidPacketWarning)
                {
                    Logger.OutputWarning("Packets for the specified packet ID do not contain a teletext service and will be ignored");
                    _invalidPacketWarning = true;
                }
                return;
            }
            // Set offset in bytes for teletext packet data
            int teletextPacketOffset;
            if (_elementaryStreamPacket.OptionalPesHeader.MarkerBits == 2) // If optional PES header is present
            {
                // If optional header is present, teletext data starts after 9 bytes plus header bytes
                teletextPacketOffset = 9 + _elementaryStreamPacket.OptionalPesHeader.PesHeaderLength;
            }
            else
            {
                // If no optional header is present, teletext data starts after 6 bytes
                teletextPacketOffset = 6;
            }
            // Check the data identifier is within the range for EBU teletext
            if (_elementaryStreamPacket.Data[teletextPacketOffset] < 0x10 || _elementaryStreamPacket.Data[teletextPacketOffset] > 0x1F)
            {
                if (!_invalidPacketWarning)
                {
                    Logger.OutputWarning("Packets for the specified packet ID do not contain a teletext service and will be ignored");
                    _invalidPacketWarning = true;
                }
                return;
            }
            // Increase offset by 1 to the start of the first teletext data unit
            teletextPacketOffset++;
            // Loop through each teletext data unit within the PES
            while (teletextPacketOffset < _elementaryStreamPacket.PesPacketLength)
            {
                // Get length of data unit
                int dataUnitLength = _elementaryStreamPacket.Data[teletextPacketOffset + 1];
                // Check data unit contains non-subtitle teletext data, or contains subtitles teletext data if subtitles are enabled, otherwise ignore
                if (_elementaryStreamPacket.Data[teletextPacketOffset] == 0x02 || (EnableSubtitles && _elementaryStreamPacket.Data[teletextPacketOffset] == 0x03))
                {
                    // Create array of bytes to contain teletext packet data
                    byte[] teletextData = new byte[dataUnitLength];
                    // Copy teletext packet data to the array
                    Buffer.BlockCopy(_elementaryStreamPacket.Data, teletextPacketOffset + 2, teletextData, 0, dataUnitLength);
                    // Reverse the bits in the bytes, required as teletext is transmitted as little endian whereas computers are generally big endian
                    for (int i = 0; i < teletextData.Length; i++)
                    {
                        teletextData[i] = Decode.Reverse(teletextData[i]);
                    }
                    // Create packet from bytes
                    Packet packet = new Packet(teletextData);
                    // Fire packet decoded event
                    PacketDecoded?.Invoke(this, packet);
                }
                // Increase offset to the next data unit
                teletextPacketOffset += (dataUnitLength + 2);
            }
        }
        #endregion
    }
}
