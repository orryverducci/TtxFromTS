using System;
using System.IO;
using CommandLineParser.Arguments;

namespace TtxFromTS
{
    /// <summary>
    /// Provides option properties for the application
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Gets or sets the input file.
        /// </summary>
        /// <value>The input file.</value>
        [FileArgument('i', "input", Description = "Input TS file to read", Optional = false, FileMustExist = true)]
        public FileInfo InputFile { get; set; }

        /// <summary>
        /// Gets or sets the packet identifier.
        /// </summary>
        /// <value>The packet identifier.</value>
        [ValueArgument(typeof(int), "pid", Description = "Packet identifier of the Teletext service to decode", Optional = false)]
        public int PacketIdentifier { get; set; }
    }
}