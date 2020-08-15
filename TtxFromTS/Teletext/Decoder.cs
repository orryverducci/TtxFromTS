using System;
using System.Text;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// Provides a teletext decoder, to decode received packets in to full pages.
    /// </summary>
    public class Decoder
    {
        #region Properties
        /// <summary>
        /// Gets the teletext magazines.
        /// </summary>
        /// <value>An array of magazines.</value>
        public Magazine[] Magazine { get; private set; } = new Magazine[8];

        /// <summary>
        /// Gets if the teletext data is multiplexed with video.
        /// </summary>
        /// <value><c>true</c> if the data is multiplexed with video, <c>false</c> if full frame.</value>
        public bool Multiplexed { get; private set; } = true;

        /// <summary>
        /// Gets the status display (usually the channel name).
        /// </summary>
        /// <value>The status display.</value>
        public string StatusDisplay { get; private set; }

        /// <summary>
        /// Gets the initial page to display.
        /// </summary>
        /// <value>The initial page number as a hexidecimal string. Returns 8FF when no particular page is specified.</value>
        public string InitialPage { get; private set; } = "8FF";

        /// <summary>
        /// Gets the initial subcode to display.
        /// </summary>
        /// <value>The initial subcode as a hexidecimal string. Returns 3F7F when no particular subcode is specified.</value>
        public string InitialSubcode { get; private set; } = "3F7F";

        /// <summary>
        /// Gets the network identification code.
        /// </summary>
        /// <value>The network identification code as a hexidecimal string.</value>
        public string NetworkID { get; private set; }

        /// <summary>
        /// Gets the total number of pages, including subpages, within the teletext service.
        /// </summary>
        /// <value>The total number of pages.</value>
        public int TotalPages
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
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="T:TtxFromTS.Teletext.Decoder"/> class.
        /// </summary>
        public Decoder()
        {
            // Create the magazines
            for (int i = 0; i < 8; i++)
            {
                Magazine[i] = new Magazine(i + 1);
            }
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes a teletext packet.
        /// </summary>
        /// <param name="packet">The teletext packet to be decoded.</param>
        public void DecodePacket(Packet packet)
        {
            // Check packet is free from errors, otherwise ignore it
            if (packet.DecodingError || packet.Magazine == null || packet.Number == null)
            {
                return;
            }
            // Decode the packet if the packet type requires it, or add it the packet's magazine
            switch (packet.Type)
            {
                case PacketType.Header:
                    DecodeHeader(packet);
                    goto default;
                case PacketType.BroadcastServiceData:
                    DecodeBroadcastServiceData(packet);
                    break;
                default:
                    Magazine[(int)packet.Magazine - 1].AddPacket(packet);
                    break;
            }
        }

        /// <summary>
        /// Decodes from a header packet if the service is being transmitted serially, and signals a header to the magazines if it is.
        /// </summary>
        /// <param name="packet">The teletext packet to be decoded.</param>
        private void DecodeHeader(Packet packet)
        {
            // Get the serial flag
            bool magazineSerial = false;
            byte controlByte = Decode.Hamming84(packet.Data[7]);
            if (controlByte != 0xff)
            {
                magazineSerial = Convert.ToBoolean((byte)(controlByte & 0x01));
            }
            // If the flag is set, loop through each magazine and signal to them that a header has been received
            if (magazineSerial)
            {
                for (int i = 0; i < 8; i++)
                {
                    // Only signal a header has been received if the magazine is not the one for this header
                    if (i + 1 != packet.Magazine)
                    {
                        Magazine[i].SerialHeaderReceived();
                    }
                }
            }
        }

        /// <summary>
        /// Decodes information from a broadcast services data packet.
        /// </summary>
        /// <param name="packet">The teletext packet to be decoded.</param>
        private void DecodeBroadcastServiceData(Packet packet)
        {
            // Get the designation byte
            byte designationByte = Decode.Hamming84(packet.Data[0]);
            // Check the designation byte does not have unrecoverable errors, and if it does ignore the packet
            if (designationByte == 0xff)
            {
                return;
            }
            // Decode the designation code and multiplex status
            bool multiplexed = Convert.ToBoolean(designationByte & 0x01);
            int designation = designationByte >> 1;
            // Check the designation code is 0 (format 1) or 1 (format 2), otherwise ignore the packet
            if (designation > 1)
            {
                return;
            }
            // Set the multiplexed status
            Multiplexed = !multiplexed;
            // If the packet is format 1, decode the network identification code
            if (designation == 0)
            {
                byte[] networkID = { packet.Data[7], packet.Data[8] };
                NetworkID = BitConverter.ToString(networkID).Replace("-", "");
            }
            // Get the status display
            byte[] statusCharacters = new byte[packet.Data.Length - 20];
            for (int i = 20; i < packet.Data.Length; i++)
            {
                statusCharacters[i - 20] = Decode.OddParity(packet.Data[i]);
            }
            StatusDisplay = Encoding.ASCII.GetString(statusCharacters);
            // Decode the digits for the inital page number
            byte pageUnits = Decode.Hamming84(packet.Data[1]);
            byte pageTens = Decode.Hamming84(packet.Data[2]);
            // Decode the bytes for the initial subcode
            byte subcode1 = Decode.Hamming84(packet.Data[3]);
            byte subcode2 = Decode.Hamming84(packet.Data[4]);
            byte subcode3 = Decode.Hamming84(packet.Data[5]);
            byte subcode4 = Decode.Hamming84(packet.Data[6]);
            // If the subcode bytes containing the magazine number contains errors, stop processing
            if (subcode2 == 0xff || subcode4 == 0xff)
            {
                return;
            }
            // If the page number digits don't contain errors, set the initial page number
            if (pageUnits != 0xff && pageTens != 0xff)
            {
                // Get the magazine number, changing 0 to 8
                byte magazineNumber = (byte)((subcode2 >> 3) | ((subcode4 & 0x0C) >> 1));
                if (magazineNumber == 0)
                {
                    magazineNumber = 8;
                }
                // Set the initial page number
                InitialPage = magazineNumber.ToString("X1") + ((pageTens << 4) | pageUnits).ToString("X2");
            }
            // If the remaining subcode bytes don't contain any errors, set the subcode
            if (subcode1 != 0xff && subcode3 != 0xff)
            {
                byte[] fullSubcode = { (byte)(((subcode4 & 0x03) << 4) | subcode3), (byte)(((subcode2 & 0x07) << 4) | subcode1) };
                InitialSubcode = BitConverter.ToString(fullSubcode).Replace("-", "");
            }
        }
        #endregion
    }
}
