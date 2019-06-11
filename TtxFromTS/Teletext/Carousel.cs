using System;
using System.Collections.Generic;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// Provides a carousel of teletext pages.
    /// </summary>
    public class Carousel
    {
        #region Properties
        /// <summary>
        /// Gets the page number within the magazine.
        /// </summary>
        /// <value>The page number as a hexidecimal string.</value>
        public string Number { get; set; }

        /// <summary>
        /// Gets the list of teletext pages within the carousel.
        /// </summary>
        /// <value>The list of teletext pages.</value>
        public List<Page> Pages { get; private set; } = new List<Page>();
        #endregion

        #region Methods
        /// <summary>
        /// Adds a teletext page to the carousel.
        /// </summary>
        /// <param name="page">The teletext page to add to the carousel.</param>
        public void AddPage(Page page)
        {
            // Check if a page with the same subcode is already in the list
            Page existingPage = Pages.Find(x => x.Subcode == page.Subcode);
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
        #endregion
    }
}