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

        /// <summary>
        /// Gets or sets the cycle time.
        /// </summary>
        /// <value>The cycle time in seconds.</value>
        [BoundedValueArgument(typeof(int), "cycle", Description = "Sets the cycle time to be used between each subpage", DefaultValue = 10, MinValue = 1)]
        public int CycleTime { get; set; }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        /// <value>The output path.</value>
        [ValueArgument(typeof(string), 'o', "output", Description = "Sets the output directory")]
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets if outputting subtitle pages should be enabled.
        /// </summary>
        /// <value><c>true</c> if subtitles should be outputted, <c>false</c> if not.</value>
        [SwitchArgument("include-subs", false, Description = "Enables the output of subtitle pages")]
        public bool IncludeSubtitles { get; set; }
    }
}