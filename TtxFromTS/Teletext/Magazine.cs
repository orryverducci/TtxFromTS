using System;
using System.Collections.Generic;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// Provides a teletext magazine
    /// </summary>
    public class Magazine
    {
        #region Private Fields
        /// <summary>
        /// The teletext page currently being decoded.
        /// </summary>
        private Page? _currentPage;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the magazine number.
        /// </summary>
        /// <value>The magazine number.</value>
        public int Number { get; private set; }

        /// <summary>
        /// Gets the list of teletext page carousels.
        /// </summary>
        /// <value>The list of teletext page carousels.</value>
        public List<Carousel> Pages { get; private set; } = new List<Carousel>();

        /// <summary>
        /// Gets the total number of pages within the magazine.
        /// </summary>
        /// <value>The total number of pages.</value>
        public int TotalPages
        {
            get => Pages.Count;
        }

        /// <summary>
        /// Gets the enhancement data (i.e. packet 29) for the magazine.
        /// </summary>
        /// <value>The rows of enhancement data packets.</value>
        public byte[][] EnhancementData { get; private set; } = new byte[4][];

        /// <summary>
        /// Gets the page number of the Global Public Object Page for the magazine.
        /// </summary>
        /// <value>The GPOP page number, or 8FF if not defined.</value>
        public string GlobalObjectPage { get; private set; } = "8FF";

        /// <summary>
        /// Gets a list of page numbers for the Public Object Pages for the magazine.
        /// </summary>
        /// <value>The list of POP page numbers.</value>
        public List<string> ObjectPages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the Global Dynamically Redefinable Character Set pages for the magazine.
        /// </summary>
        /// <value>The list of GDRCS page numbers.</value>
        public List<string> GDRCSPages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the Dynamically Redefinable Character Set pages for the magazine.
        /// </summary>
        /// <value>The list of DRCS page numbers.</value>
        public List<string> DRCSPages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the TOP Multipage Table pages for the magazine.
        /// </summary>
        /// <value>The list of MPT page numbers.</value>
        public List<string> MultipageTablePages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the TOP Multipage Table Extension pages for the magazine.
        /// </summary>
        /// <value>The list of MPT-EX page numbers.</value>
        public List<string> MultipageExtensionPages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the TOP Additional Information Table pages for the magazine.
        /// </summary>
        /// <value>The list of AIT page numbers.</value>
        public List<string> AdditionalInformationTablePages { get; private set; } = new List<string>();
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="T:TtxFromTS.Teletext.Magazine"/> class.
        /// </summary>
        /// <param name="number">The magazine number.</param>
        public Magazine(int number)
        {
            // Set the magazine number
            Number = number;
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Adds a teletext packet to the magazine.
        /// </summary>
        /// <param name="packet">The teletext packet to be added.</param>
        public void AddPacket(Packet packet)
        {
            // If the packet is a header, save the page currently being decoded and create a new one
            if (packet.Type == PacketType.Header)
            {
                SavePage();
                _currentPage = new Page
                {
                    Magazine = Number
                };
            }
            // If the packet is not a magazine enhancements packet, add it to the page currently being decoded, otherwise process the enhancements
            if (packet.Type != PacketType.MagazineEnhancements)
            {
                // If a page is being decoded, add the packet to it
                if (_currentPage != null)
                {
                    _currentPage.AddPacket(packet);
                }
            }
            else
            {
                DecodeEnhancements(packet);
            }
        }

        /// <summary>
        /// Called when a serial header is received from any magazine. Saves the page currently being decoded.
        /// </summary>
        public void SerialHeaderReceived() => SavePage();

        /// <summary>
        /// Saves the page currently being decoded.
        /// </summary>
        private void SavePage()
        {
            // Check there is a current page and that the page number is valid (i.e. not a time filler)
            if (_currentPage == null || _currentPage.Number == "FF")
            {
                return;
            }
            // If the page is a Magazine Organisation Table, decode the page numbers of object and DRCS pages
            if (_currentPage.Number == "FE")
            {
                DecodeMOT();
            }
            // If the page is a Basic TOP Table, decode the page numbers of additional TOP pages
            if (_currentPage.Number == "F0")
            {
                DecodeTOP();
            }
            // Check if a carousel with the page number exists
            Carousel? existingCarousel = Pages.Find(x => x.Number == _currentPage.Number);
            // If the carousel exists, add the page to it, otherwise create a carousel and add the page
            if (existingCarousel == null)
            {
                Carousel carousel = new Carousel { Number = _currentPage.Number };
                carousel.AddPage(_currentPage);
                Pages.Add(carousel);
            }
            else
            {
                existingCarousel.AddPage(_currentPage);
            }
            // Clear the page
            _currentPage = null;
        }

        /// <summary>
        /// Decodes enhancement data for the magazine.
        /// </summary>
        /// <param name="packet">The enhancement packet to be decoded.</param>
        private void DecodeEnhancements(Packet packet)
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
        /// Decodes object and DRCS page numbers from the Magazine Organisation Table.
        /// </summary>
        private void DecodeMOT()
        {
            // Loop through and decode each packet containing object page links
            for (int i = 0; i < 4; i++)
            {
                // Set the packet number to decode
                int packetNum = 19 + i;
                if (i > 1)
                {
                    packetNum++;
                }
                // Check the packet has been set, otherwise jump to the next one
                if (_currentPage!.Rows[packetNum] == null)
                {
                    continue;
                }
                // Loop through and decode each page link within the packet
                for (int x = 0; x < 4; x++)
                {
                    // Set link offset
                    int linkOffset = 10 * x;
                    // Decode the link page number
                    byte[] linkBytes = new byte[3];
                    Buffer.BlockCopy(_currentPage.Rows[packetNum], linkOffset, linkBytes, 0, 3);
                    string? link = DecodePageLink(linkBytes);
                    // Check a valid link has been decoded and set to a value, otherwise jump to the next link
                    if (link == null)
                    {
                        continue;
                    }
                    // Add the link to the global object link or list of object pages
                    if (link.Substring(1) != "FF" && linkOffset == 0)
                    {
                        GlobalObjectPage = link;
                    }
                    else if (link.Substring(1) != "FF")
                    {
                        ObjectPages.Add(link);
                    }
                }
            }
            // Loop through and decode each packet containing DRCS page links
            for (int i = 0; i < 2; i++)
            {
                // Set the packet number to decode
                int packetNum = 21 + (3 * i);
                // Check the packet has been set, otherwise jump to the next one
                if (_currentPage!.Rows[packetNum] == null)
                {
                    continue;
                }
                // Loop through and decode each page link within the packet
                for (int x = 0; x < 8; x++)
                {
                    // Set link offset
                    int linkOffset = 4 * x;
                    // Decode the link page number
                    byte[] linkBytes = new byte[3];
                    Buffer.BlockCopy(_currentPage.Rows[packetNum], linkOffset, linkBytes, 0, 3);
                    string? link = DecodePageLink(linkBytes);
                    // Check a valid link has been decoded and set to a value, otherwise jump to the next link
                    if (link == null)
                    {
                        continue;
                    }
                    // Add the link to the list of DRCS pages
                    if (link.Substring(1) != "FF" && linkOffset == 0)
                    {
                        GDRCSPages.Add(link);
                    }
                    else if (link.Substring(1) != "FF")
                    {
                        DRCSPages.Add(link);
                    }
                }
            }
        }

        /// <summary>
        /// Decodes additional TOP page numbers from the Basic TOP Table.
        /// </summary>
        private void DecodeTOP()
        {
            // Loop through and decode each packet containing a page linking table
            for (int i = 0; i < 2; i++)
            {
                // Set packet number
                int packetNum = 21 + i;
                // Check the packet has been set, otherwise jump to the next one
                if (_currentPage!.Rows[packetNum] == null)
                {
                    continue;
                }
                // Loop through and decode each page link within the packet
                for (int x = 0; x < 5; x++)
                {
                    // Set link offset
                    int linkOffset = 8 * x;
                    // Decode link page number
                    byte[] linkBytes = new byte[3];
                    Buffer.BlockCopy(_currentPage.Rows[packetNum], linkOffset, linkBytes, 0, 3);
                    string? link = DecodePageLink(linkBytes);
                    // Check a valid link has been decoded and set to a value, otherwise jump to the next link
                    if (link == null)
                    {
                        continue;
                    }
                    // Decode page type and add it to the appropriate list
                    int type = Decode.Hamming84(_currentPage.Rows[packetNum][linkOffset + 7]);
                    switch (type)
                    {
                        case 1:
                            MultipageTablePages.Add(link);
                            break;
                        case 2:
                            AdditionalInformationTablePages.Add(link);
                            break;
                        case 3:
                            MultipageExtensionPages.Add(link);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Decodes a page number from a MOT page link.
        /// </summary>
        /// <param name="link">The bytes containing the link.</param>
        private string? DecodePageLink(byte[] link)
        {
            // Decode magazine and page number bytes
            byte magazine = Decode.Hamming84(link[0]);
            byte pageTens = Decode.Hamming84(link[1]);
            byte pageUnits = Decode.Hamming84(link[2]);
            // If the page number bytes don't contain errors, set the page number and return it
            if (magazine != 0xff && pageTens != 0xff & pageUnits != 0xff)
            {
                // Mask X/27/4 flag from magazine number
                magazine = (byte)(magazine & 0x07);
                // Decode and return full page number
                return magazine.ToString() + ((pageTens << 4) | pageUnits).ToString("X2");
            }
            // If the page number bytes do contain errors, return null
            return null;
        }
        #endregion
    }
}
