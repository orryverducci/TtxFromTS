using System;
using System.IO;
using System.Reflection;
using Cinegy.TsDecoder.TransportStream;
using CommandLineParser.Exceptions;

namespace TtxFromTS
{
    /// <summary>
    /// Provides the main functionality for the application
    /// </summary>
    internal class Program
    {
        #region Private Fields
        /// <summary>
        /// The application options.
        /// </summary>
        private static Options _options = new Options();
        /// <summary>
        /// The teletext decoder.
        /// </summary>
        private static TeletextDecoder _teletextDecoder = new TeletextDecoder();
        #endregion

        #region Main Application Methods
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        internal static void Main(string[] args)
        {
            // Parse command line arguments, and run application if successful
            if (ParseArguments(args))
            {
                // Open the input file
                using (FileStream fileStream = _options.InputFile.OpenRead())
                {
                    // Setup transport stream decoder
                    TsPacketFactory packetFactory = new TsPacketFactory();
                    // Setup count of packets processed and buffer for packet data
                    int packetsDecoded = 0;
                    int packetsProcessed = 0;
                    byte[] data = new byte[1316];
                    // Read the file in a loop until the end of the file
                    while (fileStream.Read(data, 0, 1316) > 0)
                    {
                        // Retrieve transport stream packets from the data
                        TsPacket[] packets = packetFactory.GetTsPacketsFromData(data);
                        // If packets are returned, process them
                        if (packets != null)
                        {
                            foreach (TsPacket packet in packets)
                            {
                                // Increase count of packets decoded
                                packetsDecoded++;
                                // If packet is from the wanted identifier, pass it to the decoder and increase count of packets processed
                                if (packet.Pid == _options.PacketIdentifier)
                                {
                                    try
                                    {
                                        _teletextDecoder.AddPacket(packet);
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        OutputError("The provided packet identifier is not a teletext service");
                                        return;
                                    }
                                    packetsProcessed++;
                                }
                            }
                        }
                    }
                    // If packets are processed, output the number processed, otherwise return an error
                    if (packetsProcessed > 0)
                    {
                        Console.WriteLine($"Total number of packets: {packetsDecoded}");
                        Console.WriteLine($"Packets processed: {packetsProcessed}");
                    }
                    else if (packetsDecoded > 0)
                    {
                        OutputError("Invalid packet identifier provided");
                    }
                    else
                    {
                        OutputError("Unable to process transport stream - please check it is a valid TS file");
                    }
                }
            }
        }

        /// <summary>
        /// Parses and validates the command line arguments.
        /// </summary>
        /// <returns><c>true</c> if arguments was successfully parsed, <c>false</c> otherwise.</returns>
        /// <param name="args">The command line arguments</param>
        private static bool ParseArguments(string[] args)
        {
            // Create parser
            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser();
            // Set help header
            parser.ShowUsageHeader = $"TtxFromTS {Assembly.GetEntryAssembly().GetName().Version.ToString(2)}{Environment.NewLine}";
            // Show help information if no arguments are provided
            parser.ShowUsageOnEmptyCommandline = true;
            // Parse command line arguments into the options class
            parser.ExtractArgumentAttributes(_options);
            try
            {
                parser.ParseCommandLine(args);
                // Return success if arguments are given, otherwise return failure
                return (args.Length > 0) ? true : false;
            }
            catch (Exception exception)
            {
                // Output error messages for argument exceptions, otherwise rethrow exception
                switch (exception)
                {
                    case FileNotFoundException notFoundException: // Input file could not be found
                        OutputError("Input file could not be found");
                        break;
                    case MandatoryArgumentNotSetException notSetException: // Required argument not provided
                        OutputError($"The {notSetException.Argument} argument is required");
                        break;
                    case UnknownArgumentException unknownException: // Not a valid argument
                        OutputError($"{unknownException.Argument} is not a valid argument");
                        break;
                    case CommandLineFormatException formatException: // Short argument used with two dashes
                        OutputError("Short arguments must be prefixed with a single '-' character");
                        break;
                    default:
                        throw exception;
                }
                // Return failure
                return false;
            }
        }
        #endregion

        #region Console Output Methods
        /// <summary>
        /// Outputs an error message to the console's standard error output.
        /// </summary>
        /// <param name="errorMessage">The error message to be displayed.</param>
        private static void OutputError(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write("[ERROR] ");
            Console.ResetColor();
            Console.Error.WriteLine(errorMessage);
        }
        #endregion
    }
}