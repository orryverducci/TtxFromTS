using System;
using System.IO;
using TtxFromTS.Teletext;

namespace TtxFromTS.Output
{
    public class T42Output : IOutput
    {
        #region Private Fields
        /// <summary>
        /// The binary file writer.
        /// </summary>
        private BinaryWriter _binaryWriter;

        /// <summary>
        /// Count of teletext packets decoded.
        /// </summary>
        private int _packetCount;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the T42 output statistics.
        /// </summary>
        /// <value>A tuple containing the statistic title and its value.</value>
        public (string, string)[] Statistics
        {
            get
            {
                return new (string, string)[]
                {
                    ("Teletext packets written", _packetCount.ToString())
                };
            }
        }

        /// <summary>
        /// Gets if output looping is supported for this output.
        /// </summary>
        /// <value><c>true</c> if output looping is supported, <c>false</c> if not.</value>
        public bool LoopSupported { get; } = false;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="T:TtxFromTS.Output.T42Output"/> class.
        /// </summary>
        public T42Output()
        {
            // Set output file path
            string filePath = String.IsNullOrEmpty(Program.Options.OutputPath) ? Path.ChangeExtension(Program.Options.InputFile!.FullName, "t42") : Program.Options.OutputPath;
            // If the file already exists throw an exception
            if (File.Exists(filePath))
            {
                throw new Exception($"{filePath} already exists");
            }
            // Setup the T42 file stream writer
            _binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create));
        }
        #endregion

        #region Generic Output Methods
        /// <summary>
        /// Provides a teletext packet to the stream.
        /// </summary>
        /// <param name="packet">The teletext packet.</param>
        public void AddPacket(Packet packet)
        {
            // Write the packet without the synchronisation sequence to the file
            _binaryWriter.Write(packet.FullPacketData.AsSpan().Slice(2, 42));
            // Increase the count of teletext packets decoded
            _packetCount++;
        }

        /// <summary>
        /// Finalise the output, closing the file writer.
        /// </summary>
        public void FinishOutput() => _binaryWriter.Dispose();
        #endregion
    }
}
