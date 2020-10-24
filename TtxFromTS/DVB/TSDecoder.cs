using System;
using System.Collections.Generic;
using Cinegy.TsDecoder.TransportStream;
using TtxFromTS.Teletext;

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

        #region Elemtary Stream Decoding Methods
        /// <summary>
        /// Decodes elementary stream packets from the data provided.
        /// </summary>
        /// <param name="data">The data to be decoded.</param>
        /// <returns>A list of elementary stream packets.</returns>
        public List<Pes> DecodePackets(byte[] data)
        {
            // Create a list of elementary stream packets to return
            List<Pes> pesPackets = new List<Pes>();
            // Get packets from the data
            TsPacket[] tsPackets = _packetFactory.GetTsPacketsFromData(data);
            // For each TS packet decoded process it, add the decoded PES packets to the list to return and increase the packet counters if the packet is from the specified packet ID
            if (tsPackets != null)
            {
                foreach (TsPacket packet in tsPackets)
                {
                    PacketsReceived++;
                    if (PacketID == -1 || packet.Pid == PacketID)
                    {
                        PacketsDecoded++;
                        Pes? processedPacket = ProcessTSPacket(packet);
                        if (processedPacket != null)
                        {
                            pesPackets.Add(processedPacket);
                            _elementaryStreamPacket = null;
                        }
                    }
                }
            }
            // Return the decoded elementary stream packets
            return pesPackets;
        }

        /// <summary>
        /// Processes TS packets and retrieves elementary stream packets from them.
        /// </summary>
        /// <param name="packet">The packet to be processed.</param>
        /// <returns>An elementary stream packets if one has finished decoding, or null.</returns>
        private Pes? ProcessTSPacket(TsPacket packet)
        {
            // Check packet is free from errors
            if (packet.TransportErrorIndicator)
            {
                if (!_errorWarning)
                {
                    Logger.OutputWarning("The transport stream contains errors which will be ignored, possibly as a result of poor reception");
                    _errorWarning = true;
                }
                return null;
            }
            // Check packet is not encrypted
            if (packet.ScramblingControl != 0)
            {
                if (!_encryptedWarning)
                {
                    Logger.OutputWarning("The specified packet ID contains encrypted packets which will be ignored");
                    _encryptedWarning = true;
                }
                return null;
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
                    return _elementaryStreamPacket;
                }
            }
            // Return null if no other response has been returned
            return null;
        }
        #endregion

        #region Data Decoding Methods
        /// <summary>
        /// Decodes teletext packets from the complete elementary stream packet.
        /// </summary>
        /// <param name="elementaryStreamPacket">The elementary stream packet to decode teletext packets from.</param>
        /// <param name="decodeSubtitles">True if teletext subtitles packets should be decoded, false if not.</param>
        /// <returns>A list of teletext packets.</returns>
        public static List<Packet> DecodeTeletextPacket(Pes elementaryStreamPacket, bool decodeSubtitles)
        {
            // Create a list of teletext packets to return
            List<Packet> packets = new List<Packet>();
            // Check the PES is a private stream packet
            if (elementaryStreamPacket.StreamId != (byte)PesStreamTypes.PrivateStream1)
            {
                return packets;
            }
            // Set offset in bytes for teletext packet data
            int teletextPacketOffset;
            if (elementaryStreamPacket.OptionalPesHeader.MarkerBits == 2) // If optional PES header is present
            {
                // If optional header is present, teletext data starts after 9 bytes plus header bytes
                teletextPacketOffset = 9 + elementaryStreamPacket.OptionalPesHeader.PesHeaderLength;
            }
            else
            {
                // If no optional header is present, teletext data starts after 6 bytes
                teletextPacketOffset = 6;
            }
            // Check the data identifier is within the range for EBU teletext
            if (elementaryStreamPacket.Data[teletextPacketOffset] < 0x10 || elementaryStreamPacket.Data[teletextPacketOffset] > 0x1F)
            {
                return packets;
            }
            // Increase offset by 1 to the start of the first teletext data unit
            teletextPacketOffset++;
            // Loop through each teletext data unit within the PES
            while (teletextPacketOffset < elementaryStreamPacket.PesPacketLength)
            {
                // Get length of data unit
                int dataUnitLength = elementaryStreamPacket.Data[teletextPacketOffset + 1];
                // Check data unit contains non-subtitle teletext data, or contains subtitles teletext data if subtitles are enabled, otherwise ignore
                if (elementaryStreamPacket.Data[teletextPacketOffset] == 0x02 || (decodeSubtitles && elementaryStreamPacket.Data[teletextPacketOffset] == 0x03))
                {
                    // Create array of bytes to contain teletext packet data
                    byte[] teletextData = new byte[dataUnitLength];
                    // Copy teletext packet data to the array
                    Buffer.BlockCopy(elementaryStreamPacket.Data, teletextPacketOffset + 2, teletextData, 0, dataUnitLength);
                    // Reverse the bits in the bytes, required as teletext is transmitted as little endian whereas computers are generally big endian
                    for (int i = 0; i < teletextData.Length; i++)
                    {
                        teletextData[i] = Decode.Reverse(teletextData[i]);
                    }
                    // Create a new teletext packet from the bytes of data and add it to the list
                    packets.Add(new Packet(teletextData));
                    
                }
                // Increase offset to the next data unit
                teletextPacketOffset += (dataUnitLength + 2);
            }
            // Return the list of teletext packets
            return packets;
        }
        #endregion
    }
}
