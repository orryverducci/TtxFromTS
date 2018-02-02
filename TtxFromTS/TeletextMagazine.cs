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
        internal List<TeletextPage> Pages { get; private set; } = new List<TeletextPage>();

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
                    if (_currentPage.Number.Substring(1, 1) != "F" && _currentPage.Subcode != "3F7F")
                    {
                        // Check if a page with the same number is already in the list
                        TeletextPage existingPage = Pages.Find(x => (x.Number == _currentPage.Number) && (x.Subcode == _currentPage.Subcode));
                        // If an existing page doesn't exist, add the current page to list of pages
                        if (existingPage != null)
                        {
                            Pages.Add(_currentPage);
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