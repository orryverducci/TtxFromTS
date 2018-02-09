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
            // If packet is a header, save current page if valid, then create a new page
            if (packet.Type == TeletextPacket.PacketType.Header)
            {
                // Check there is a current page, otherwise skip to creating new page
                if (_currentPage != null)
                {
                    // Check the page number is valid and not an ehancement page
                    if (_currentPage.Number.Substring(0, 1) != "F" && _currentPage.Subcode != "3F7F")
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
                }
                // Create new page
                _currentPage = new TeletextPage { Magazine = Number };
            }
            // If a page is being decoded, add the packet to it
            if (_currentPage != null)
            {
                _currentPage.AddPacket(packet);
            }
        }
    }
}