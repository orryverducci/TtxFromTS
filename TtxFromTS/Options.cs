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
        public FileInfo? InputFile { get; set; }

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
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets or sets if outputting subtitle pages should be enabled.
        /// </summary>
        /// <value><c>true</c> if subtitles should be outputted, <c>false</c> if not.</value>
        [SwitchArgument("include-subs", false, Description = "Enables the output of subtitle pages")]
        public bool IncludeSubtitles { get; set; }

        /// <summary>
        /// Gets or sets if creating a vbit2 configuration file should be disabled.
        /// </summary>
        /// <value><c>true</c> if a config file should not be created, <c>false</c> if it should.</value>
        [SwitchArgument("no-config", false, Description = "Disabled the creation of a vbit2 config file")]
        public bool DisableConfig { get; set; }

        /// <summary>
        /// Gets or sets the output type.
        /// </summary>
        /// <value>The output type.</value>
        [ValueArgument(typeof(Output.Type), "type", Description = "Sets the output type")]
        public Output.Type OutputType { get; set; }

        /// <summary>
        /// Gets or sets if the output should be continually looped.
        /// </summary>
        /// <value><c>true</c> if the output should be looped, <c>false</c> if not.</value>
        [SwitchArgument("loop", false, Description = "Sets if the output should be continually looped")]
        public bool Loop { get; set; }

        /// <summary>
        /// Gets or sets the web socket port.
        /// </summary>
        /// <value>The port number.</value>
        [ValueArgument(typeof(int), "port", DefaultValue = 80, Description = "The port to be used by the web socket server")]
        public int Port { get; set; }
    }
}
