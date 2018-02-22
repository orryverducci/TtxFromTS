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
            // Check the page number is valid and not an ehancement page
            if (_currentPage.Number != "FE" && _currentPage.Number != "FF")
            {
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
            // Set offset for initial triplet
            int tripletOffset = 1;
            // Create array to store decoded packet data in
            byte[] decodedPacket = new byte[39];
            // Loop through each triplet and process it
            while (tripletOffset + 2 < packet.Data.Length)
            {
                // Get the triplet
                byte[] triplet = new byte[3];
                Buffer.BlockCopy(packet.Data, tripletOffset, triplet, 0, 3);
                // Decode the triplet
                byte[] decodedTriplet = Decode.Hamming2418(triplet);
                // If triplet has unrecoverable errors, set it to a blank triplet
                if (decodedTriplet[0] == 0xff)
                {
                    decodedTriplet[0] = 0x00;
                    decodedTriplet[1] = 0x00;
                    decodedTriplet[2] = 0x00;
                }
                // Copy the decoded triplet to the full decoded packet data
                Buffer.BlockCopy(decodedTriplet, 0, decodedPacket, tripletOffset - 1, 3);
                // Increase the offset to the next triplet
                tripletOffset += 3;
            }
            // Store decoded packet
            EnhancementData[designation] = decodedPacket;
        }
    }
}