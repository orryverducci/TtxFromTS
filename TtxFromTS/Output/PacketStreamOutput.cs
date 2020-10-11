using System;
using TtxFromTS.Teletext;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides a constant stream of teletext packets, intended to be consumed by child classes which would provide this stream as an output.
    /// </summary>
    public abstract class PacketStreamOutput
    {
        #region Private Fields
        /// <summary>
        /// A blank teletext packet to be used to output an empty (silent) line.
        /// </summary>
        private readonly byte[] _blankPacket = new byte[42];

        /// <summary>
        /// The field of the last packet output.
        /// </summary>
        private bool _lastField = false;

        /// <summary>
        /// The line number of the last packet output.
        /// </summary>
        private byte _lastLine = 0;
        #endregion

        #region Methods
        /// <summary>
        /// Provides a teletext packet to the stream.
        /// </summary>
        /// <param name="packet">The teletext packet.</param>
        public void AddPacket(Packet packet)
        {
            // Get the packet line number as an offset from the first teletext line (line  7)
            byte lineNumber = (byte)(packet.LineNumber - 6);
            // If the packet is on a different field to the last one output, output a blank packet on the remaining lines on the field
            if (packet.Field != _lastField || lineNumber <= _lastLine)
            {
                while (_lastLine < 16)
                {
                    OutputPacket(_blankPacket.AsSpan());
                    _lastLine++;
                }
            }
            // If the last line output was line 16, loop back to the start and set the field to the one in the packet
            if (_lastLine == 16)
            {
                _lastField = packet.Field;
                _lastLine = 0;
            }
            // If there's a gap between packet's line number and the last line output, output blank packets on the lines between them
            while (lineNumber != _lastLine + 1)
            {
                OutputPacket(_blankPacket.AsSpan());
                _lastLine++;
            }
            // Write the teletext packet to standard out and update the last line output
            OutputPacket(packet.FullPacketData.AsSpan().Slice(2));
            _lastLine = packet.LineNumber;
        }

        /// <summary>
        /// Sends teletext packet data from the stream to the output.
        /// </summary>
        /// <param name="packetData">The bytes of data to be output as a span.</param>
        protected abstract void OutputPacket(Span<byte> packetData);
        #endregion
    }
}
