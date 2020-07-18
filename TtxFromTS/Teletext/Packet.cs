using System;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// Provides an individual teletext packet.
    /// </summary>
    public class Packet
    {
        #region Properties
        /// <summary>
        /// Gets the framing code for the teletext packet.
        /// </summary>
        /// <value>The framing code.</value>
        public byte FramingCode { get; private set; }

        /// <summary>
        /// Gets the magazine number the packet corresponds to.
        /// </summary>
        /// <value>The magazine number.</value>
        public int? Magazine { get; private set; }

        /// <summary>
        /// Gets the packet number.
        /// </summary>
        /// <value>The packet number.</value>
        public int? Number { get; private set; }

        /// <summary>
        /// Gets the data contained within the packet.
        /// </summary>
        /// <value>The packet data.</value>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets if the packet has been determined to contain unrecoverable errors.
        /// </summary>
        /// <value>True if there is an error, false if there isn't.</value>
        public bool DecodingError { get; private set; } = false;

        /// <summary>
        /// Gets the type of packet.
        /// </summary>
        /// <value>The packet type.</value>
        public PacketType Type { get; private set; } = PacketType.Unspecified;

        /// <summary>
        /// Gets the full packet data with framing code, magazine and row.
        /// </summary>
        /// <value>The full packet data.</value>
        public byte[] FullPacketData { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="T:TtxFromTS.TeletextPacket"/> class.
        /// </summary>
        /// <param name="packetData">The teletext packet data to be decoded.</param>
        public Packet(byte[] packetData)
        {
            // Store the original full packet data
            FullPacketData = packetData;
            // Retrieve the framing code
            FramingCode = packetData[1];
            // Check the framing code is valid, otherwise mark the packet as containing errors
            DecodingError = FramingCode != 0x27;
            // Retrieve and decode the magazine number
            byte address1 = Decode.Hamming84(packetData[2]);
            Magazine = address1 & 0x07;
            // Check the magazine number is valid, otherwise mark the packet as containing errors, and change 0 to 8
            if (Magazine > 7)
            {
                Magazine = null;
                DecodingError = true;
            }
            else if (Magazine == 0)
            {
                Magazine = 8;
            }
            // Retrieve and decode the packet number
            byte address2 = Decode.Hamming84(packetData[3]);
            Number = (address1 >> 3) | (address2 << 1);
            // Set the packet type from the packet number, or if it is not a valid number mark the packet as containing errors
            switch (Number)
            {
                case 0:
                    Type = PacketType.Header;
                    break;
                case int packetNumber when packetNumber <= 23:
                    Type = PacketType.PageBody;
                    break;
                case 24:
                    Type = PacketType.Fastext;
                    break;
                case 25:
                    Type = PacketType.TOPCommentary;
                    break;
                case 26:
                    Type = PacketType.PageReplacements;
                    break;
                case 27:
                    Type = PacketType.LinkedPages;
                    break;
                case 28:
                    Type = PacketType.PageEnhancements;
                    break;
                case 29:
                    Type = PacketType.MagazineEnhancements;
                    break;
                case int packetNumber when packetNumber == 30 && Magazine == 8:
                    Type = PacketType.BroadcastServiceData;
                    break;
                default:
                    Number = null;
                    DecodingError = true;
                    break;
            }
            // Retrieve packet data
            Data = new byte[packetData.Length - 4];
            Buffer.BlockCopy(packetData, 4, Data, 0, packetData.Length - 4);
        }
        #endregion
    }
}
