using System;
using Cinegy.TsDecoder.TransportStream;

namespace TtxFromTS
{
    /// <summary>
    /// Provides a teletext decoder, to decode received packets in to full pages.
    /// </summary>
    internal class TeletextDecoder
    {
        /// <summary>
        /// Buffer for an elementary stream packet.
        /// </summary>
        private Pes _elementaryStreamPacket;

        /// <summary>
        /// Gets the teletext magazines.
        /// </summary>
        /// <value>The magazine.</value>
        internal TeletextMagazine[] Magazine { get; private set; } = new TeletextMagazine[7];

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TtxFromTS.TeletextDecoder"/> class.
        /// </summary>
        internal TeletextDecoder()
        {
            // Create the magazines to decode packets in to
            for (int i = 0; i < 8; i++)
            {
                Magazine[i] = new TeletextMagazine(i + 1);
            }
        }

        /// <summary>
        /// Adds a transport stream packet to the teletext decoder.
        /// </summary>
        /// <param name="packet">The transport stream packet to be decoded.</param>
        internal void AddPacket(TsPacket packet)
        {
            // Check packet is free from errors and not encrypted, otherwise ignore
            if (packet.TransportErrorIndicator || packet.ScramblingControl != 0)
            {
                return;
            }
            // If the TS packet is the start of a PES, create new elementary stream, otherwise add to existing one
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
            // If the TS packet completes a PES, try to decode it
            if (_elementaryStreamPacket != null && _elementaryStreamPacket.HasAllBytes())
            {
                DecodeTeletextPackets();
            }
        }

        /// <summary>
        /// Decode the current elementary stram packet.
        /// </summary>
        private void DecodeTeletextPackets()
        {
            // Decode the PES stream
            _elementaryStreamPacket.Decode();
            // Check the PES is a private stream packet, otherwise throw exception
            if (_elementaryStreamPacket.StreamId != (byte)PesStreamTypes.PrivateStream1)
            {
                throw new InvalidOperationException("The packet is not a private stream packet as used for teletext.");
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
            // Check the data identifier is within the range for EBU teletext, otherwise throw exception
            if (_elementaryStreamPacket.Data[teletextPacketOffset] < 0x10 || _elementaryStreamPacket.Data[teletextPacketOffset] > 0x1F)
            {
                throw new InvalidOperationException("The packet is not a teletext service.");
            }
            // Increase offset by 1 to the start of the first teletext data unit
            teletextPacketOffset++;
            // Loop through each teletext data unit within the PES
            while (teletextPacketOffset < _elementaryStreamPacket.PesPacketLength)
            {
                // Get length of data unit
                int dataUnitLength = _elementaryStreamPacket.Data[teletextPacketOffset + 1];
                // Check data unit contains non-subtitle teletext data and process it, otherwise ignore
                if (_elementaryStreamPacket.Data[teletextPacketOffset] == 0x02)
                {
                    // Create array of bytes to contain teletext packet data
                    byte[] teletextData = new byte[dataUnitLength];
                    // Copy teletext packet data to array
                    Buffer.BlockCopy(_elementaryStreamPacket.Data, teletextPacketOffset + 2, teletextData, 0, dataUnitLength);
                    // Create teletext packet from data
                    TeletextPacket teletextPacket = new TeletextPacket(teletextData);
                    // Check packet is free from errors, and if it is add it to its magazine
                    if (!teletextPacket.DecodingError && teletextPacket.Magazine != null)
                    {
                        Magazine[(int)teletextPacket.Magazine].AddPacket(teletextPacket);
                    }
                }
                // Increase offset to the next data unit
                teletextPacketOffset += (dataUnitLength + 2);
            }
        }
    }
}