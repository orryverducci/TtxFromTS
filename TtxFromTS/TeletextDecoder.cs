using System;
using System.Text;
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
        internal TeletextMagazine[] Magazine { get; private set; } = new TeletextMagazine[8];

        /// <summary>
        /// Gets and sets if subtitle pages should be decoded.
        /// </summary>
        /// <value><c>true</c> if subtitles should be decoded, <c>false</c> if not.</value>
        internal bool EnableSubtitles { get; set; } = false;

        /// <summary>
        /// Gets if the teletext data is multiplexed with video.
        /// </summary>
        /// <value><c>true</c> if the data is multiplexed with video, <c>false</c> if full frame.</value>
        internal bool Multiplexed { get; private set; } = true;

        /// <summary>
        /// Gets the status display (usually the channel name).
        /// </summary>
        /// <value>The status display.</value>
        internal string StatusDisplay { get; private set; }

        /// <summary>
        /// Gets the initial page to display.
        /// </summary>
        /// <value>The initial page number. Returns 8FF when no particular page is specified.</value>
        internal string InitialPage { get; private set; } = "8FF";

        /// <summary>
        /// Gets the initial subcode to display.
        /// </summary>
        /// <value>The initial subcode number. Returns 3F7F when no particular subcode is specified.</value>
        internal string InitialSubcode { get; private set; } = "3F7F";

        /// <summary>
        /// Gets the network identification code.
        /// </summary>
        /// <value>The initial subcode number. Returns 3F7F when no particular subcode is specified.</value>
        internal string NetworkID { get; private set; }

        /// <summary>
        /// Gets the total pages, including subpages, within the teletext service.
        /// </summary>
        /// <value>The total number of pages.</value>
        internal int TotalPages
        {
            get
            {
                int count = 0;
                foreach (var magazine in Magazine)
                {
                    count += magazine.TotalPages;
                }
                return count;
            }
        }

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
                // Check data unit contains non-subtitle teletext data, or contains subtitles teletext data if subtitles are enabled, otherwise ignore
                if (_elementaryStreamPacket.Data[teletextPacketOffset] == 0x02 || (EnableSubtitles && _elementaryStreamPacket.Data[teletextPacketOffset] == 0x03))
                {
                    // Create array of bytes to contain teletext packet data
                    byte[] teletextData = new byte[dataUnitLength];
                    // Copy teletext packet data to array
                    Buffer.BlockCopy(_elementaryStreamPacket.Data, teletextPacketOffset + 2, teletextData, 0, dataUnitLength);
                    // Create teletext packet from data
                    TeletextPacket teletextPacket = new TeletextPacket(teletextData);
                    // Check packet is free from errors, and if it is add it to its magazine, or decode broadcast services data
                    if (!teletextPacket.DecodingError && teletextPacket.Magazine != null)
                    {
                        if (teletextPacket.Type != TeletextPacket.PacketType.BroadcastServiceData)
                        {
                            Magazine[(int)teletextPacket.Magazine - 1].AddPacket(teletextPacket);
                        }
                        else
                        {
                            DecodeBroadcastServiceData(teletextPacket);
                        }
                    }
                }
                // Increase offset to the next data unit
                teletextPacketOffset += (dataUnitLength + 2);
            }
        }

        /// <summary>
        /// Decodes information from a broadcast services data packet.
        /// </summary>
        /// <param name="packet">The teletext packet to decode from.</param>
        private void DecodeBroadcastServiceData(TeletextPacket packet)
        {
            // Get designation byte
            byte designationByte = Decode.Hamming84(packet.Data[0]);
            // Check the designation byte does not have unrecoverable errors, otherwise ignore
            if (designationByte == 0xff)
            {
                return;
            }
            // Decode designation code and multiplex status
            bool multiplexed = Convert.ToBoolean(designationByte & 0x01);
            int designation = designationByte >> 1;
            // Check designation is 0 (format 1) or 1 (format 2), otherwise ignore
            if (designation > 1)
            {
                return;
            }
            // Set multiplexed status
            Multiplexed = !multiplexed;
            // Decode inital page number digits
            byte pageUnits = Decode.Hamming84(packet.Data[1]);
            byte pageTens = Decode.Hamming84(packet.Data[2]);
            // Decode initial subcode bytes
            byte subcode1 = Decode.Hamming84(packet.Data[3]);
            byte subcode2 = Decode.Hamming84(packet.Data[4]);
            byte subcode3 = Decode.Hamming84(packet.Data[5]);
            byte subcode4 = Decode.Hamming84(packet.Data[6]);
            // If subcode bytes don't contain errors, set the subcode
            if (subcode1 != 0xff && subcode2 != 0xff && subcode3 != 0xff && subcode4 != 0xff)
            {
                byte[] fullSubcode = new byte[] { (byte)(((subcode4 & 0x03) << 4) | subcode3), (byte)(((subcode2 & 0x07) << 4) | subcode1) };
                InitialSubcode = BitConverter.ToString(fullSubcode).Replace("-", "");
            }
            // If the magazine number bytes don't contain errors, decode the magazine number and set the inital page number
            if (subcode2 != 0xff && subcode4 != 0xff)
            {
                byte magazineNumber = (byte)((subcode2 >> 3) | ((subcode4 & 0x0C) >> 1));
                // If page number digits don't contain errors, set page number
                if (pageUnits != 0xff && pageTens != 0xff)
                {
                    // Get magazine string, changing 0 to 8
                    string magazineString;
                    if (magazineNumber != 0)
                    {
                        magazineString = magazineNumber.ToString("X1");
                    }
                    else
                    {
                        magazineString = "8";
                    }
                    // Set initial page number
                    InitialPage = magazineString + ((pageTens << 4) | pageUnits).ToString("X2");
                }
            }
            // If format one, decode the network identification code
            if (designation == 0)
            {
                byte[] networkID = new byte[] { packet.Data[7], packet.Data[8] };
                NetworkID = BitConverter.ToString(networkID).Replace("-", "");
            }
            // Get status display
            byte[] statusCharacters = new byte[packet.Data.Length - 20];
            for (int i = 20; i < packet.Data.Length; i++)
            {
                statusCharacters[i - 20] = Decode.OddParity(packet.Data[i]);
            }
            StatusDisplay = Encoding.ASCII.GetString(statusCharacters);
        }
    }
}