using System;
using System.Text;

namespace TtxFromTS
{
    /// <summary>
    /// Provides a teletext page.
    /// </summary>
    internal class TeletextPage
    {
        #region Enumerations
        internal enum CharacterSubset
        {
            English = 0x00,
            German = 0x01,
            SwedishFinishHugarian = 0x02,
            Italian = 0x03,
            French = 0x04,
            PortugeseSpanish = 0x05,
            CzechSlovak = 0x06
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the magazine number for the page.
        /// </summary>
        /// <value>The magazine number.</value>
        internal int Magazine { get; set; }

        /// <summary>
        /// Gets the hex page number within the magazine.
        /// </summary>
        /// <value>The page number.</value>
        internal string Number { get; private set; }

        /// <summary>
        /// Gets the page subcode.
        /// </summary>
        /// <value>The page subcode.</value>
        internal string Subcode { get; private set; }

        /// <summary>
        /// Gets if any previous displays of this page should be erased.
        /// </summary>
        /// <value>True if the page should be erased.</value>
        internal bool ErasePage { get; private set; } = false;

        /// <summary>
        /// Gets if this is a newsflash page.
        /// </summary>
        /// <value>True if the page is a newsflash page.</value>
        internal bool Newsflash { get; private set; }

        /// <summary>
        /// Gets if this is a subtitles page.
        /// </summary>
        /// <value>True if the page is a subtitles page.</value>
        internal bool Subtitles { get; private set; } = false;

        /// <summary>
        /// Gets if the page headers should not be displayed.
        /// </summary>
        /// <value>True if the page header should be hidden.</value>
        internal bool SuppressHeader { get; private set; } = false;

        /// <summary>
        /// Gets if the page has changed since the previous transmission.
        /// </summary>
        /// <value>True if the page is an updated page.</value>
        internal bool Update { get; private set; } = false;

        /// <summary>
        /// Gets if the page has been transmitted out of numerical order.
        /// </summary>
        /// <value>True if the page is has been transmitted out of order.</value>
        internal bool InterruptedSequence { get; private set; } = false;

        /// <summary>
        /// Gets if the page should not be displayed.
        /// </summary>
        /// <value>True if the page should not be displayed.</value>
        internal bool InhibitDisplay { get; private set; } = false;

        /// <summary>
        /// Gets if magazines are being transmitted serially.
        /// </summary>
        /// <value>True if transmitted serially, false if transmitted in parrallel.</value>
        internal bool MagazineSerial { get; private set; } = false;

        /// <summary>
        /// Gets the national option character set to be used for the page.
        /// </summary>
        /// <value>The character set to be used.</value>
        internal CharacterSubset NationalOptionCharacterSubset { get; private set; } = CharacterSubset.English;

        /// <summary>
        /// Gets the rows of the page.
        /// </summary>
        /// <value>The rows of the page.</value>
        internal string[] Rows { get; private set; } = new string[25];

        /// <summary>
        /// Gets the linked pages
        /// </summary>
        /// <value>The linked page numbers and shortcodes.</value>
        internal (string Number, string Subcode)[] Links { get; private set; } = new (string Number, string Subcode)[6];

        /// <summary>
        /// Gets if row 24 should be displayed.
        /// </summary>
        /// <value>True if row 24 should be displayed, false if not.</value>
        internal bool DisplayRow24 { get; private set; } = false;
        #endregion

        #region Packet Methods
        /// <summary>
        /// Adds a teletext packet to the page.
        /// </summary>
        /// <param name="packet">The teletext packet to be added.</param>
        internal void AddPacket(TeletextPacket packet)
        {
            // Choose how to decode the packet based on its type
            switch (packet.Type)
            {
                case TeletextPacket.PacketType.Header:
                    DecodeHeader(packet);
                    break;
                case TeletextPacket.PacketType.PageBody:
                case TeletextPacket.PacketType.Fastext:
                    DecodeRow(packet);
                    break;
                case TeletextPacket.PacketType.LinkedPages:
                    DecodeLinkedPages(packet);
                    break;
            }
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes the header.
        /// </summary>
        /// <param name="packet">The header packet.</param>
        private void DecodeHeader(TeletextPacket packet)
        {
            // Extract page number data from packet
            byte[] pageData = new byte[6];
            Buffer.BlockCopy(packet.Data, 0, pageData, 0, 6);
            // Decode page number and subcode and set them
            (string Number, string Subcode) pageNumber = DecodePageNumber(pageData);
            Number = pageNumber.Number;
            Subcode = pageNumber.Subcode;
            // Set control codes in byte 3 if it doesn't contain errors
            byte controlByte1 = Decode.Hamming84(packet.Data[3]);
            if (controlByte1 != 0xff)
            {
                ErasePage = Convert.ToBoolean((byte)(controlByte1 >> 3));
            }
            // Set control codes in byte 5 if it doesn't contain errors
            byte controlByte2 = Decode.Hamming84(packet.Data[5]);
            if (controlByte2 != 0xff)
            {
                Newsflash = Convert.ToBoolean((byte)((controlByte2 & 0x04) >> 2));
                Subtitles = Convert.ToBoolean((byte)(controlByte2 >> 3));
            }
            // Set control codes in byte 6 if it doesn't contain errors
            byte controlByte3 = Decode.Hamming84(packet.Data[6]);
            if (controlByte3 != 0xff)
            {
                SuppressHeader = Convert.ToBoolean((byte)(controlByte3 & 0x01));
                Update = Convert.ToBoolean((byte)((controlByte3 & 0x02) >> 1));
                InterruptedSequence = Convert.ToBoolean((byte)((controlByte3 & 0x04) >> 2));
                InhibitDisplay = Convert.ToBoolean((byte)((controlByte3 & 0x08) >> 3));
            }
            // Set control codes in byte 7 if it doesn't contain errors
            byte controlByte4 = Decode.Hamming84(packet.Data[7]);
            if (controlByte4 != 0xff)
            {
                MagazineSerial = Convert.ToBoolean((byte)(controlByte4 & 0x01));
                int characterSubset = controlByte4 >> 1;
                // Set character subset if a valid option is given, otherwise leave as default
                if (characterSubset < 7)
                {
                    NationalOptionCharacterSubset = (CharacterSubset)characterSubset;
                }
            }
            // Decode the part of the header to be displayed on row 0
            byte[] headerCharacters = new byte[packet.Data.Length - 8];
            for (int i = 8; i < packet.Data.Length; i++)
            {
                headerCharacters[i - 8] = Decode.OddParity(packet.Data[i]);
                // If character is null (i.e. it had errors), replace with a space
                if (headerCharacters[i - 8] == 0x00)
                {
                    headerCharacters[i - 8] = 0x20;
                }
            }
            Rows[0] = "        " + Encoding.ASCII.GetString(headerCharacters);
        }

        /// <summary>
        /// Decodes a row for display.
        /// </summary>
        /// <param name="packet">The row packet.</param>
        private void DecodeRow(TeletextPacket packet)
        {
            byte[] characters = new byte[packet.Data.Length];
            for (int i = 0; i < packet.Data.Length; i++)
            {
                characters[i] = Decode.OddParity(packet.Data[i]);
                // If character is null (i.e. it had errors), replace with a space
                if (characters[i] == 0x00)
                {
                    characters[i] = 0x20;
                }
            }
            Rows[(int)packet.Number] = Encoding.ASCII.GetString(characters);
        }

        /// <summary>
        /// Decodes the linked pages used for fastext.
        /// </summary>
        /// <param name="packet">The linked pages packet.</param>
        private void DecodeLinkedPages(TeletextPacket packet)
        {
            // Check the designation code is 0, otherwise ignore packet
            if (Decode.Hamming84(packet.Data[0]) == 0)
            {
                // Set the offset for the link bytes, starting at byte 1
                int linkOffset = 1;
                // Retrieve the 6 links
                for (int i = 0; i < 6; i++)
                {
                    // Extract link data from packet
                    byte[] linkData = new byte[6];
                    Buffer.BlockCopy(packet.Data, linkOffset, linkData, 0, 6);
                    // Decode page number and subcode
                    (string Number, string Subcode) pageNumber = DecodePageNumber(linkData);
                    // Get bytes containing magazine number bits
                    byte magazineByte1 = Decode.Hamming84(packet.Data[linkOffset + 3]);
                    byte magazineByte2 = Decode.Hamming84(packet.Data[linkOffset + 5]);
                    // If the magazine bytes don't contain errors, decode the magazine number and add to link page number, otherwise add current magazine
                    if (magazineByte1 != 0xff && magazineByte2 != 0xff)
                    {
                        byte magazineNumber = (byte)(((magazineByte1 >> 3) | ((magazineByte2 & 0x0C) >> 1) ^ Magazine));
                        pageNumber.Number = magazineNumber.ToString("x1") + pageNumber.Number;
                    }
                    else
                    {
                        pageNumber.Number = Magazine + pageNumber.Number;
                    }
                    // Add link to array of links
                    Links[i] = pageNumber;
                    // Increase link offset to the next link data block
                    linkOffset += 6;
                }
                // Check if link control byte has errors, and set if row 24 should be hidden if it hasn't
                byte linkControl = Decode.Hamming84(packet.Data[linkOffset]);
                if (linkControl != 0xff)
                {
                    DisplayRow24 = Convert.ToBoolean((byte)(linkControl >> 3));
                }
            }
        }

        private (string Number, string Subcode) DecodePageNumber(byte[] pageNumberData)
        {
            // Create default (i.e. erroneous and to be ignored) values for number and subcode
            string number = "FF";
            string subcode = "3F7F";
            // Decode page number digits
            byte pageUnits = Decode.Hamming84(pageNumberData[0]);
            byte pageTens = Decode.Hamming84(pageNumberData[1]);
            // If page number digits don't contain errors, set page number
            if (pageUnits != 0xff && pageTens != 0xff)
            {
                number = ((pageTens << 4) | pageUnits).ToString("X2");
            }
            // Decode subcode bytes
            byte subcode1 = Decode.Hamming84(pageNumberData[2]);
            byte subcode2 = Decode.Hamming84(pageNumberData[3]);
            byte subcode3 = Decode.Hamming84(pageNumberData[4]);
            byte subcode4 = Decode.Hamming84(pageNumberData[5]);
            // If subcode bytes don't contain errors and are valid, set the subcode and the included control bits
            if (subcode1 != 0xff && subcode2 != 0xff && subcode3 != 0xff && subcode4 != 0xff)
            {
                byte[] fullSubcode = new byte[] { (byte)(((subcode4 & 0x03) << 4) | subcode3), (byte)(((subcode2 & 0x07) << 4) | subcode1) };
                subcode = BitConverter.ToString(fullSubcode).Replace("-", "");
            }
            // Return values
            return (number, subcode);
        }
        #endregion
    }
}