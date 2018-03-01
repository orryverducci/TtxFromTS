using System;
using System.Collections.Generic;

namespace TtxFromTS
{
    /// <summary>
    /// Provides a carousel of teletext pages.
    /// </summary>
    internal class TeletextCarousel
    {
        /// <summary>
        /// Gets or sets the hex page number within the magazine.
        /// </summary>
        /// <value>The page number.</value>
        internal string Number { get; set; }

        /// <summary>
        /// Gets the list of teletext pages within the carousel.
        /// </summary>
        /// <value>The list of teletext pages.</value>
        internal List<TeletextPage> Pages { get; private set; } = new List<TeletextPage>();

        /// <summary>
        /// Adds a teletext page to the carousel.
        /// </summary>
        /// <param name="page">The teletext page to add to the carousel.</param>
        internal void AddPage(TeletextPage page)
        {
            // Check if a page with the same subcode is already in the list
            TeletextPage existingPage = Pages.Find(x => x.Subcode == page.Subcode);
            // If the subpage already exists, merge new subpage with existing one, otherwise add to the list of subpages
            if (existingPage != null)
            {
                // If new page has rows and the erase flag set, replace existing page, otherwise merge with the existing page
                if (page.ErasePage && page.UsedRows > 0)
                {
                    Pages.Remove(existingPage);
                    Pages.Add(page);
                }
                else
                {
                    existingPage.MergeUpdate(page);
                }
            }
            else
            {
                // Add the page to the list of pages
                Pages.Add(page);
            }
        }
    }
}