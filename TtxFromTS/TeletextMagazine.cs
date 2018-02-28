using System;
using System.Collections.Generic;

namespace TtxFromTS
{
    /// <summary>
    /// Provides a teletext magazine
    /// </summary>
    internal class TeletextMagazine
    {
        /// <summary>
        /// The teletext page currently being decoded.
        /// </summary>
        private TeletextPage _currentPage;

        /// <summary>
        /// Gets the magazine number.
        /// </summary>
        /// <value>The magazine number.</value>
        internal int Number { get; private set; }

        /// <summary>
        /// Gets the list of teletext pages.
        /// </summary>
        /// <value>The list of teletext pages.</value>
        internal List<TeletextCarousel> Pages { get; private set; } = new List<TeletextCarousel>();

        /// <summary>
        /// Gets the total number of pages within the magazine.
        /// </summary>
        /// <value>The total number of pages.</value>
        internal int TotalPages
        {
            get
            {
                return Pages.Count;
            }
        }

        internal byte[][] EnhancementData { get; private set; } = new byte[4][];

        /// <summary>
        /// Gets the page number of the Global Public Object Page for the magazine.
        /// </summary>
        /// <value>The GPOP page number, or 8FF if not defined.</value>
        internal string GlobalObjectPage { get; private set; } = "8FF";

        /// <summary>
        /// Gets a list of page numbers for Public Object Pages for the magazine.
        /// </summary>
        /// <value>The list of POP page numbers.</value>
        internal List<string> ObjectPages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the Global Dynamically Redefinable Character Set pages for the magazine.
        /// </summary>
        /// <value>The list of GDRCS page numbers.</value>
        internal List<string> GDRCSPages { get; private set; } = new List<string>();

        /// <summary>
        /// Gets a list of page numbers for the Dynamically Redefinable Character Set pages for the magazine.
        /// </summary>
        /// <value>The list of DRCS page numbers.</value>
        internal List<string> DRCSPages { get; private set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TtxFromTS.TeletextMagazine"/> class.
        /// </summary>
        /// <param name="number">The magazine number.</param>
        internal TeletextMagazine(int number)
        {
            // Set the magazine number
            Number = number;
        }

        /// <summary>
        /// Adds a teletext packet to the magazine.
        /// </summary>
        /// <param name="packet">The teletext packet to be added.</param>
        internal void AddPacket(TeletextPacket packet)
        {
            // If packet is a header, save current page
            if (packet.Type == TeletextPacket.PacketType.Header)
            {
                SavePage();
                // Create new page
                _currentPage = new TeletextPage { Magazine = Number };
            }
            // If the packet is not a magazine enhancements packet, add it to a page, otherwise process the enhancements
            if (packet.Type != TeletextPacket.PacketType.MagazineEnhancements)
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
        internal void SerialHeaderReceived()
        {
            SavePage();
        }

        /// <summary>
        /// Saves the page currently being decoded.
        /// </summary>
        private void SavePage()
        {
            // Check there is a current page
            if (_currentPage == null)
            {
                return;
            }
            // Check the page number is valid (i.e. not a time filler)
            if (_currentPage.Number != "FF")
            {
                // If the page is a Magazine Organisation Table, decode the page numbers of object and DRCS pages
                if (_currentPage.Number == "FE")
                {
                    DecodeMOT();
                }
                // Check if a carousel with the page number exists
                TeletextCarousel existingCarousel = Pages.Find(x => x.Number == _currentPage.Number);
                // If the carousel exists, add the page to it, otherwise create a carousel and add the page
                if (existingCarousel == null)
                {
                    TeletextCarousel carousel = new TeletextCarousel { Number = _currentPage.Number };
                    carousel.AddPage(_currentPage);
                    Pages.Add(carousel);
                }
                else
                {
                    existingCarousel.AddPage(_currentPage);
                }
            }
            // Clear the page
            _currentPage = null;
        }

        /// <summary>
        /// Decodes magazine enhancement data.
        /// </summary>
        /// <param name="packet">The packet containing enhancement triplets.</param>
        private void DecodeEnhancements(TeletextPacket packet)
        {
            // Get designation code
            int designation = Decode.Hamming84(packet.Data[0]);
            // Check designation code is valid and below 4, otherwise treat as an invalid packet and ignore it
            if (designation == 0xff || designation > 4)
            {
                return;
            }
            // Get the triplets from the packet
            byte[] enhancementTriplets = new byte[packet.Data.Length - 1];
            Buffer.BlockCopy(packet.Data, 1, enhancementTriplets, 0, enhancementTriplets.Length);
            // Store decoded packet
            EnhancementData[designation] = enhancementTriplets;
        }

        /// <summary>
        /// Decodes object and DRCS page numbers from the Magazine Organisation Table.
        /// </summary>
        private void DecodeMOT()
        {
            // Decode object page links
            for (int i = 0; i < 4; i++)
            {
                // Set packet number
                int packetNum = 19 + i;
                if (i > 1)
                {
                    packetNum++;
                }
                // If packet is set, decode links from it
                if (_currentPage.Rows[packetNum] != null)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        // Set link offset
                        int linkOffset = 10 * x;
                        // Decode link page number
                        byte[] linkBytes = new byte[3];
                        Buffer.BlockCopy(_currentPage.Rows[packetNum], linkOffset, linkBytes, 0, 3);
                        string link = DecodePageLink(linkBytes);
                        // If a valid link is decoded and set to a value, add it to the global object link or list of object pages
                        if (link != null)
                        {
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
                }
            }
            // Decode DRCS page links
            for (int i = 0; i < 2; i++)
            {
                // Set packet number
                int packetNum = 21 + (3 * i);
                // If packet is set, decode links from it
                if (_currentPage.Rows[packetNum] != null)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        // Set link offset
                        int linkOffset = 4 * x;
                        // Decode link page number
                        byte[] linkBytes = new byte[3];
                        Buffer.BlockCopy(_currentPage.Rows[packetNum], linkOffset, linkBytes, 0, 3);
                        string link = DecodePageLink(linkBytes);
                        // If a valid link is decoded and set to a value, add it to the list of DRCS pages
                        if (link != null)
                        {
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
            }
        }

        /// <summary>
        /// Decodes a page link from a MOT.
        /// </summary>
        /// <param name="link">The bytes containing the link.</param>
        private string DecodePageLink(byte[] link)
        {
            // Get linked page number
            byte magazine = Decode.Hamming84(link[0]);
            byte pageTens = Decode.Hamming84(link[1]);
            byte pageUnits = Decode.Hamming84(link[2]);
            // If link doesn't have unrecoverable errors, decode it, otherwise return null
            if (magazine != 0xff && pageTens != 0xff & pageUnits != 0xff)
            {
                // Mask X/27/4 flag from magazine number
                magazine = (byte)(magazine & 0x07);
                // Decode and return full page number
                return magazine.ToString() + ((pageTens << 4) | pageUnits).ToString("X2");
            }
            else
            {
                return null;
            }
        }
    }
}