using System;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// Provides a teletext page.
    /// </summary>
    public class Page
    {
        #region Properties
        /// <summary>
        /// Gets the magazine number for the page.
        /// </summary>
        /// <value>The magazine number.</value>
        public int Magazine { get; set; }

        /// <summary>
        /// Gets the page number within the magazine.
        /// </summary>
        /// <value>The page number as a hexidecimal string.</value>
        public string Number { get; private set; } = "8FF";

        /// <summary>
        /// Gets the page subcode.
        /// </summary>
        /// <value>The page subcode as a hexidecimal string.</value>
        public string Subcode { get; private set; } = "F7F3";

        /// <summary>
        /// Gets if any previous displays of this page should be erased.
        /// </summary>
        /// <value><c>true</c> if the page should be erased, <c>false</c> if not.</value>
        public bool ErasePage { get; private set; }

        /// <summary>
        /// Gets if this is a newsflash page.
        /// </summary>
        /// <value><c>true</c> if the page is a newsflash page, <c>false</c> if not.</value>
        public bool Newsflash { get; private set; }

        /// <summary>
        /// Gets if this is a subtitles page.
        /// </summary>
        /// <value><c>true</c> if the page is a subtitles page, <c>false</c> if not.</value>
        public bool Subtitles { get; private set; }

        /// <summary>
        /// Gets if the page header should be hidden from display.
        /// </summary>
        /// <value><c>true</c> if the page header should be hidden, <c>false</c> if not.</value>
        public bool SuppressHeader { get; private set; }

        /// <summary>
        /// Gets if the page has changed since the previous transmission.
        /// </summary>
        /// <value><c>true</c> if an updated page, <c>false</c> if not.</value>
        public bool Update { get; private set; }

        /// <summary>
        /// Gets if the page has been transmitted out of numerical order.
        /// </summary>
        /// <value><c>true</c> if the page has been transmitted out of order, <c>false</c> if not.</value>
        public bool InterruptedSequence { get; private set; }

        /// <summary>
        /// Gets if the page should not be displayed.
        /// </summary>
        /// <value><c>true</c> if the page should not be displayed, <c>false</c> if it should.</value>
        public bool InhibitDisplay { get; private set; }

        /// <summary>
        /// Gets if the magazines in the teletext service is being transmitted serially.
        /// </summary>
        /// <value><c>true</c> if the service magazines are being transmitted serially, <c>false</c> if in parallel.</value>
        public bool MagazineSerial { get; private set; }

        /// <summary>
        /// Gets the national option character set to be used for the page.
        /// </summary>
        /// <value>The character set to be used.</value>
        public CharacterSubset NationalOptionCharacterSubset { get; private set; } = CharacterSubset.English;

        /// <summary>
        /// Gets the rows of the page.
        /// </summary>
        /// <value>The rows of the page.</value>
        public byte[][] Rows { get; private set; } = new byte[26][];

        /// <summary>
        /// Gets the fasttext linked pages.
        /// </summary>
        /// <value>The linked page numbers and subcodes.</value>
        public (string Number, string Subcode)[]? Links { get; private set; }

        /// <summary>
        /// Gets if row 24 should be displayed.
        /// </summary>
        /// <value><c>true</c> if row 24 should be displayed, <c>false</c> if not.</value>
        public bool DisplayRow24 { get; private set; } = false;

        /// <summary>
        /// Gets character replacement and object data (i.e. packet 26) for the page.
        /// </summary>
        /// <value>The rows of enhancement data packets.</value>
        public byte[][] ReplacementData { get; private set; } = new byte[16][];

        /// <summary>
        /// Gets enhancement data (i.e. packet 28) for the page.
        /// </summary>
        /// <value>The rows of enhancement data packets.</value>
        public byte[][] EnhancementData { get; private set; } = new byte[5][];

        /// <summary>
        /// Gets enhancement link data (i.e. packet 27/4 and 27/5) for the page.
        /// </summary>
        /// <value>The rows of enhancement data packets.</value>
        public byte[][] EnhancementLinks { get; private set; } = new byte[2][];

        /// <summary>
        /// Gets the number of rows that contain data.
        /// </summary>
        /// <value>The number of rows with data.</value>
        public int UsedRows
        {
            get
            {
                int usedRows = 0;
                foreach (byte[] row in Rows)
                {
                    if (row != null)
                    {
                        usedRows++;
                    }
                }
                return usedRows;
            }
        }
        #endregion

        #region Page Update Methods
        /// <summary>
        /// Adds a teletext packet to the page.
        /// </summary>
        /// <param name="packet">The teletext packet to be added.</param>
        public void AddPacket(Packet packet)
        {
            // Choose how to decode the packet based on its type
            switch (packet.Type)
            {
                case PacketType.Header:
                    DecodeHeader(packet);
                    break;
                case PacketType.PageBody:
                case PacketType.Fastext:
                case PacketType.TOPCommentary:
                    DecodeRow(packet);
                    break;
                case PacketType.PageReplacements:
                    DecodePageReplacements(packet);
                    break;
                case PacketType.LinkedPages:
                    DecodeLinkedPages(packet);
                    break;
                case PacketType.PageEnhancements:
                    DecodePageEnhancements(packet);
                    break;
            }
        }

        /// <summary>
        /// Merges an updated subpage with this page.
        /// </summary>
        /// <param name="page">The teletext page to be merged.</param>
        public void MergeUpdate(Page page)
        {
            // Update properties with new ones
            Newsflash = page.Newsflash;
            Subtitles = page.Subtitles;
            SuppressHeader = page.SuppressHeader;
            Update = page.Update;
            InhibitDisplay = page.InhibitDisplay;
            InterruptedSequence = page.InterruptedSequence;
            MagazineSerial = page.MagazineSerial;
            NationalOptionCharacterSubset = page.NationalOptionCharacterSubset;
            // Update rows with new ones
            for (int i = 0; i < Rows.Length; i++)
            {
                if (page.Rows[i] != null)
                {
                    Rows[i] = page.Rows[i];
                }
            }
            // Update links with new ones
            Links = page.Links;
            EnhancementLinks = page.EnhancementLinks;
            // Update enhancements with new ones
            for (int i = 0; i < EnhancementData.Length; i++)
            {
                if (page.EnhancementData[i] != null)
                {
                    EnhancementData[i] = page.EnhancementData[i];
                }
            }
            for (int i = 0; i < ReplacementData.Length; i++)
            {
                if (page.ReplacementData[i] != null)
                {
                    ReplacementData[i] = page.ReplacementData[i];
                }
            }
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes the page header (row 0).
        /// </summary>
        /// <param name="packet">The header packet to be decoded.</param>
        private void DecodeHeader(Packet packet)
        {
            // Extract page number data from the packet
            byte[] pageData = new byte[6];
            Buffer.BlockCopy(packet.Data, 0, pageData, 0, 6);
            // Decode the page number and subcode and set them
            (string Number, string Subcode) pageNumber = DecodePageNumber(pageData);
            Number = pageNumber.Number;
            Subcode = pageNumber.Subcode;
            // Set control codes in byte 3 if it doesn't contain errors
            byte controlByte1 = Decode.Hamming84(packet.Data[3]);
            if (controlByte1 != 0xff)
            {
                ErasePage = Convert.ToBoolean(controlByte1 >> 3);
            }
            // Set control codes in byte 5 if it doesn't contain errors
            byte controlByte2 = Decode.Hamming84(packet.Data[5]);
            if (controlByte2 != 0xff)
            {
                Newsflash = Convert.ToBoolean((controlByte2 & 0x04) >> 2);
                Subtitles = Convert.ToBoolean(controlByte2 >> 3);
            }
            // Set control codes in byte 6 if it doesn't contain errors
            byte controlByte3 = Decode.Hamming84(packet.Data[6]);
            if (controlByte3 != 0xff)
            {
                SuppressHeader = Convert.ToBoolean(controlByte3 & 0x01);
                Update = Convert.ToBoolean((controlByte3 & 0x02) >> 1);
                InterruptedSequence = Convert.ToBoolean((controlByte3 & 0x04) >> 2);
                InhibitDisplay = Convert.ToBoolean((controlByte3 & 0x08) >> 3);
            }
            // Set control codes in byte 7 if it doesn't contain errors
            byte controlByte4 = Decode.Hamming84(packet.Data[7]);
            if (controlByte4 != 0xff)
            {
                MagazineSerial = Convert.ToBoolean(controlByte4 & 0x01);
                int characterSubset = (controlByte4 >> 3) | ((controlByte4 & 0x04) >> 1) | ((controlByte4 & 0x02) << 1);
                // Set character subset if a valid option is given, otherwise leave as default
                if (characterSubset < 7)
                {
                    NationalOptionCharacterSubset = (CharacterSubset)characterSubset;
                }
            }
            // Decode the visible part of the header to be displayed on row 0
            byte[] headerCharacters = new byte[packet.Data.Length];
            for (int i = 0; i < packet.Data.Length; i++)
            {
                if (i < 7)
                {
                    headerCharacters[i] = 0x04;
                }
                else
                {
                    headerCharacters[i] = packet.Data[i];
                }
            }
            Rows[0] = headerCharacters;
        }

        /// <summary>
        /// Decodes a row for display.
        /// </summary>
        /// <param name="packet">The row packet to be decoded.</param>
        private void DecodeRow(Packet packet)
        {
            Rows[(int)packet.Number!] = packet.Data;
        }

        /// <summary>
        /// Decodes the linked pages used for fastext.
        /// </summary>
        /// <param name="packet">The linked pages packet to be decoded.</param>
        private void DecodeLinkedPages(Packet packet)
        {
            // Check the designation code is 0, 4 or 5, otherwise ignore packet
            byte designationCode = Decode.Hamming84(packet.Data[0]);
            // Process as fastext page links if designation code 0, or enhancement links if designation 4 or 5
            switch (designationCode)
            {
                case 0:
                    // Initialise links property
                    Links = new (string Number, string Subcode)[6];
                    // Set the offset for the link bytes, starting at byte 1
                    int linkOffset = 1;
                    // Retrieve the 6 links
                    for (int i = 0; i < 6; i++)
                    {
                        // Extract link data from the packet
                        byte[] linkData = new byte[6];
                        Buffer.BlockCopy(packet.Data, linkOffset, linkData, 0, 6);
                        // Decode page number and subcode
                        (string Number, string Subcode) pageNumber = DecodePageNumber(linkData);
                        // Get the bytes containing the magazine number bits
                        byte magazineByte1 = Decode.Hamming84(packet.Data[linkOffset + 3]);
                        byte magazineByte2 = Decode.Hamming84(packet.Data[linkOffset + 5]);
                        // If the magazine bytes don't contain errors, decode the magazine number and add it to the link page number, otherwise add the current magazine
                        if (magazineByte1 != 0xff && magazineByte2 != 0xff)
                        {
                            int rawMagNumber = Magazine < 8 ? Magazine : 0;
                            int magazineNumber = ((magazineByte1 >> 3) | ((magazineByte2 & 0x0C) >> 1)) ^ rawMagNumber;
                            if (magazineNumber == 0)
                            {
                                magazineNumber = 8;
                            }
                            pageNumber.Number = magazineNumber.ToString("X1") + pageNumber.Number;
                        }
                        else
                        {
                            pageNumber.Number = Magazine + pageNumber.Number;
                        }
                        // Add link to array of links
                        Links[i] = pageNumber;
                        // Increase the link offset to the next link data block
                        linkOffset += 6;
                    }
                    // Set if row 24 should be hidden if the link control byte doesn't have errors
                    byte linkControl = Decode.Hamming84(packet.Data[linkOffset]);
                    if (linkControl != 0xff)
                    {
                        DisplayRow24 = Convert.ToBoolean(linkControl >> 3);
                    }
                    break;
                case 4:
                    EnhancementLinks[0] = new byte[packet.Data.Length - 1];
                    Buffer.BlockCopy(packet.Data, 1, EnhancementLinks[0], 0, EnhancementLinks[0].Length);
                    break;
                case 5:
                    EnhancementLinks[1] = new byte[packet.Data.Length - 1];
                    Buffer.BlockCopy(packet.Data, 1, EnhancementLinks[1], 0, EnhancementLinks[1].Length);
                    break;
            }
        }

        /// <summary>
        /// Decodes character replacement enhancement data for the page.
        /// </summary>
        /// <param name="packet">The enhancement packet to be decoded.</param>
        private void DecodePageReplacements(Packet packet)
        {
            // Get designation code
            int designation = Decode.Hamming84(packet.Data[0]);
            // Check the designation code is valid, otherwise treat as an invalid packet and ignore it
            if (designation == 0xff)
            {
                return;
            }
            // Get the triplets from the packet
            byte[] enhancementTriplets = new byte[packet.Data.Length - 1];
            Buffer.BlockCopy(packet.Data, 1, enhancementTriplets, 0, enhancementTriplets.Length);
            // Store the decoded packet
            ReplacementData[designation] = enhancementTriplets;
        }

        /// <summary>
        /// Decodes enhancement data for the page.
        /// </summary>
        /// <param name="packet">The enhancement packet to be decoded.</param>
        private void DecodePageEnhancements(Packet packet)
        {
            // Get designation code
            int designation = Decode.Hamming84(packet.Data[0]);
            // Check the designation code is valid and below 5, otherwise treat as an invalid packet and ignore it
            if (designation == 0xff || designation > 4)
            {
                return;
            }
            // Get the triplets from the packet
            byte[] enhancementTriplets = new byte[packet.Data.Length - 1];
            Buffer.BlockCopy(packet.Data, 1, enhancementTriplets, 0, enhancementTriplets.Length);
            // Store the decoded packet
            EnhancementData[designation] = enhancementTriplets;
        }

        /// <summary>
        /// Decodes a page number from a header or page link.
        /// </summary>
        /// <param name="pageNumberData">The bytes containing the link.</param>
        /// <returns>A tuple containing the page number and the subcode as strings.</returns>
        private (string Number, string Subcode) DecodePageNumber(byte[] pageNumberData)
        {
            // Create default (i.e. erroneous and to be ignored) values for number and subcode
            string number = "FF";
            string subcode = "3F7F";
            // Decode page number bytes
            byte pageUnits = Decode.Hamming84(pageNumberData[0]);
            byte pageTens = Decode.Hamming84(pageNumberData[1]);
            // If page number bytes don't contain errors, set page number
            if (pageUnits != 0xff && pageTens != 0xff)
            {
                number = ((pageTens << 4) | pageUnits).ToString("X2");
            }
            // Decode subcode bytes
            byte subcode1 = Decode.Hamming84(pageNumberData[2]);
            byte subcode2 = Decode.Hamming84(pageNumberData[3]);
            byte subcode3 = Decode.Hamming84(pageNumberData[4]);
            byte subcode4 = Decode.Hamming84(pageNumberData[5]);
            // If subcode bytes don't contain errors, set the subcode
            if (subcode1 != 0xff && subcode2 != 0xff && subcode3 != 0xff && subcode4 != 0xff)
            {
                subcode = (((subcode4 & 0x03) << 12) | (subcode3 << 8) | ((subcode2 & 0x07) << 4) | subcode1).ToString("X4");
            }
            // Return values
            return (number, subcode);
        }
        #endregion
    }
}
