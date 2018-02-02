using System;

namespace TtxFromTS
{
    /// <summary>
    /// Provides an individual teletext packet.
    /// </summary>
    internal class TeletextPacket
    {
        internal enum PacketType
        {
            Header,
            PageBody,
            Fastext,
            LinkedPages,
            Unspecified
        }

        /// <summary>
        /// Gets the framing code for the teletext packet.
        /// </summary>
        /// <value>The packet data.</value>
        internal byte FramingCode { get; private set; }

        /// <summary>
        /// Gets the magazine number the packet corresponds to.
        /// </summary>
        /// <value>The magazine number.</value>
        internal int? Magazine { get; private set; }

        /// <summary>
        /// Gets the packet number.
        /// </summary>
        /// <value>The packet number.</value>
        internal int? Number { get; private set; }

        /// <summary>
        /// Gets the data contained within the packet.
        /// </summary>
        /// <value>The packet data.</value>
        internal byte[] Data { get; private set; }

        /// <summary>
        /// Gets if the packet has been determined to contain unrecoverable errors.
        /// </summary>
        /// <value>True if there is an error, false if there isn't.</value>
        internal bool DecodingError { get; private set; } = false;

        /// <summary>
        /// Gets the type of packet
        /// </summary>
        /// <value>The packet type.</value>
        internal PacketType Type { get; private set; } = PacketType.Unspecified;

        /// <summary>
        /// Initialises a new instance of the <see cref="T:TtxFromTS.TeletextPacket"/> class.
        /// </summary>
        /// <param name="packetData">The teletext packet data to be decoded.</param>
        internal TeletextPacket(byte[] packetData)
        {
            // Retrieve framing code
            FramingCode = packetData[1];
            // Check the framing code is valid, otherwise mark packet as containing errors
            if (FramingCode != 0xe4)
            {
                DecodingError = true;
            }
            // Retrieve and decode magazine number
            Magazine = Hamming.Decode84(packetData[2]);
            // Check magazine number is valid, otherwise mark packet as containing errors, and change 0 to 8
            if (Magazine > 7)
            {
                Magazine = null;
                DecodingError = true;
            }
            else if (Magazine == 0)
            {
                Magazine = 8;
            }
            // Retrieve and decode packet number
            Number = Hamming.Decode84(packetData[3]);
            // Check the packet number is valid, otherwise mark packet as containing errors, and set the packet type
            if (Number == 0)
            {
                Type = PacketType.Header;
            }
            else if (Number <= 23)
            {
                Type = PacketType.PageBody;
            }
            else if (Number == 24)
            {
                Type = PacketType.Fastext;
            }
            else if (Number == 27)
            {
                Type = PacketType.LinkedPages;
            }
            if (Number > 31)
            {
                Number = null;
                DecodingError = true;
            }
            // Retrieve packet data
            Data = new byte[packetData.Length - 4];
            Buffer.BlockCopy(packetData, 4, Data, 0, packetData.Length - 4);
        }
    }
}