using System;
using System.Collections.Generic;
using Cinegy.TsDecoder.TransportStream;
using TtxFromTS.Teletext;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Decoding methods for elementary streams.
    /// </summary>
    public static class ElementaryDecode
    {
        /// <summary>
        /// Decodes teletext packets from the complete elementary stream packet.
        /// </summary>
        /// <param name="elementaryStreamPacket">The elementary stream packet to decode teletext packets from.</param>
        /// <param name="decodeSubtitles">True if teletext subtitles packets should be decoded, false if not.</param>
        /// <returns>A list of teletext packets, or null if the elementary stream packet is not for a teletext service.</returns>
        public static List<Packet>? DecodeTeletextPacket(Pes elementaryStreamPacket, bool decodeSubtitles)
        {
            // Check the PES is a private stream packet
            if (elementaryStreamPacket.StreamId != (byte)PesStreamTypes.PrivateStream1)
            {
                return null;
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
                return null;
            }
            // Increase offset by 1 to the start of the first teletext data unit
            teletextPacketOffset++;
            // Create a list of teletext packets to return
            List<Packet> packets = new List<Packet>();
            // Loop through each teletext data unit within the PES
            while (teletextPacketOffset < elementaryStreamPacket.Data.Length)
            {
                // Get length of data unit
                int dataUnitLength = elementaryStreamPacket.Data[teletextPacketOffset + 1];
                // Check the data unit length doesn't exceed the PES length, and exit the loop if it does (assumed it is corrupted)
                if (dataUnitLength > elementaryStreamPacket.Data.Length - teletextPacketOffset + 2)
                {
                    Logger.OutputWarning("Skipping data unit with invalid length");
                    break;
                }
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
    }
}
