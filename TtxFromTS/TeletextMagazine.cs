using System;

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
            // If packet is a header create a new page
            if (packet.Type == TeletextPacket.PacketType.Header)
            {
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