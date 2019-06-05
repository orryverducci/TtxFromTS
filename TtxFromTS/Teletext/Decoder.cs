using System;
using System.Text;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// Provides a teletext decoder, to decode received packets in to full pages.
    /// </summary>
    internal class Decoder
    {
        /// <summary>
        /// Gets the teletext magazines.
        /// </summary>
        /// <value>The magazine.</value>
        internal Magazine[] Magazine { get; private set; } = new Magazine[8];

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
        internal Decoder()
        {
            // Create the magazines to decode packets in to
            for (int i = 0; i < 8; i++)
            {
                Magazine[i] = new Magazine(i + 1);
            }
        }

        /// <summary>
        /// Decodes a teletext packet.
        /// </summary>
        /// <param name="packet">The packet to be decoded.</param>
        internal void DecodePacket(Packet packet)
        {
            // Check packet is free from errors, and if it is add it to its magazine, or decode broadcast services data
            if (!packet.DecodingError && packet.Magazine != null)
            {
                if (packet.Type != PacketType.BroadcastServiceData)
                {
                    // If packet is a header, check if it is in serial mode, and if it is signal to the other magazines that a serial header has been received
                    if (packet.Type == PacketType.Header)
                    {
                        // Get serial flag
                        bool magazineSerial = false;
                        byte controlByte = Decode.Hamming84(packet.Data[7]);
                        if (controlByte != 0xff)
                        {
                            magazineSerial = Convert.ToBoolean((byte)(controlByte & 0x01));
                        }
                        // If flag is set, loop through each magazine
                        if (magazineSerial)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                // Only signal header if magazine is not the one for this header
                                if (i + 1 != packet.Magazine)
                                {
                                    Magazine[i].SerialHeaderReceived();
                                }
                            }
                        }
                    }
                    // Add packet to its magazine
                    Magazine[(int)packet.Magazine - 1].AddPacket(packet);
                }
                else
                {
                    DecodeBroadcastServiceData(packet);
                }
            }
        }

        /// <summary>
        /// Decodes information from a broadcast services data packet.
        /// </summary>
        /// <param name="packet">The teletext packet to decode from.</param>
        private void DecodeBroadcastServiceData(Packet packet)
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