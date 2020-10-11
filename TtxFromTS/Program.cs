using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cinegy.TsDecoder.TransportStream;
using CommandLineParser.Exceptions;
using TtxFromTS.Output;
using TtxFromTS.Teletext;

namespace TtxFromTS
{
    /// <summary>
    /// Provides the main functionality for the application
    /// </summary>
    public class Program
    {
        #region Private Fields
        /// <summary>
        /// The chosen application output.
        /// </summary>
        private static IOutput? _output;

        /// <summary>
        /// The transport stream decoder.
        /// </summary>
        private static TSDecoder _tsDecoder = new TSDecoder();

        /// <summary>
        /// Indicates if a warning for non-teletext packets has been output.
        /// </summary>
        private static bool _invalidPacketWarning = false;
        #endregion

        #region Properties
        /// <summary>
        /// The application options.
        /// </summary>
        /// <value>The options.</value>
        public static Options Options { get; private set; } = new Options();
        #endregion

        #region Main Application Methods
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static int Main(string[] args)
        {
            // Output application header
            Logger.OutputHeader();
            // Catch any unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrap;
            // Parse command line arguments, and exit if invalid
            if (!ParseArguments(args))
            {
                return (int)ExitCodes.InvalidArgs;
            }
            // Setup output
            SetupOutput();
            // Open the file and process it
            using (FileStream fileStream = Options.InputFile!.OpenRead())
            {
                DecodeTeletext(fileStream);
            }
            // Finish the output and log stats
            _output!.FinishOutput();
            Logger.OutputStats(_tsDecoder.PacketsReceived, _tsDecoder.PacketsDecoded, _output.Statistics);
            return (int)ExitCodes.Success;
        }

        /// <summary>
        /// Sets up the chosen output type.
        /// </summary>
        private static void SetupOutput()
        {
            // Set output based on chosen type
            switch (Options.OutputType)
            {
                case Output.Type.StdOut:
                    _output = new StdOutput();
                    break;
                case Output.Type.WebSocket:
                    _output = new WebSocketOutput();
                    break;
                case Output.Type.T42:
                    _output = new T42Output();
                    break;
                default:
                    _output = new TTIOutput();
                    break;
            }
            // Log if looping is enabled, or if looping is enabled but the output doesn't support it log a warning and disable it
            if (Options.Loop && _output.LoopSupported)
            {
                Logger.OutputInfo("Looping output is enabled");
            }
            else if (Options.Loop)
            {
                Logger.OutputWarning("Looping has been enabled but it is not supported by the selected output type");
                Options.Loop = false;
            }
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Opens the provided input file and retrieves packets of data from it.
        /// </summary>
        private static void DecodeTeletext(FileStream fileStream)
        {
            // Set the packet ID to be decoded
            _tsDecoder.PacketID = Options.PacketIdentifier;
            // Initialise data buffer and processing state
            byte[] data = new byte[1316];
            int loggedPercentage = 0;
            bool finished = false;
            // Keep processing until finished
            while (!finished)
            {
                // Decode data if there's more bytes available, otherwise finish or loop
                if (fileStream.Read(data, 0, 1316) > 0)
                {
                    // Decode TS packets from the data
                    List<Pes> tsPackets = _tsDecoder!.DecodePackets(data);
                    // If TS packets have been returned, process teletext packets from them and output them
                    foreach (Pes tsPacket in tsPackets)
                    {
                        List<Packet> teletextPackets = TSDecoder.DecodeTeletextPacket(tsPacket, Options.IncludeSubtitles);
                        if (teletextPackets.Count > 0)
                        {
                            foreach (Packet teletextPacket in teletextPackets)
                            {
                                _output!.AddPacket(teletextPacket);
                            }
                        }
                        else if (!_invalidPacketWarning)
                        {
                            Logger.OutputWarning("The specified packet ID contains packets without a teletext service which will be ignored");
                            _invalidPacketWarning = true;
                        }
                    }
                    // If not set to loop, log the progress in increments of 10%
                    if (!Options.Loop)
                    {
                        double currentPercentage = (double)fileStream.Position / (double)fileStream.Length * 100;
                        if (currentPercentage >= loggedPercentage + 10)
                        {
                            loggedPercentage = (int)currentPercentage;
                            Logger.OutputInfo($"Reading TS file: {loggedPercentage}%");
                        }
                    }
                }
                else
                {
                    // If no packets were successfully decoded, exit with the appropriate error
                    if (_tsDecoder!.PacketsReceived == 0)
                    {
                        Logger.OutputError("Unable to process transport stream - please check it is a valid TS file");
                        Environment.Exit((int)ExitCodes.TSError);
                    }
                    if (_tsDecoder.PacketsDecoded == 0)
                    {
                        Logger.OutputError("Invalid packet identifier provided");
                        Environment.Exit((int)ExitCodes.InvalidPID);
                    }
                    // If looping is enabled reset the file back to the start, otherwise finish processing
                    if (Options.Loop)
                    {
                        fileStream.Position = 0;
                    }
                    else
                    {
                        finished = true;
                    }
                }
            }
        }
        #endregion

        #region Support Methods
        /// <summary>
        /// Parses and validates the command line arguments.
        /// </summary>
        /// <returns><c>true</c> if the arguments were successfully parsed, <c>false</c> otherwise.</returns>
        /// <param name="args">The command line arguments.</param>
        private static bool ParseArguments(string[] args)
        {
            // Create parser set to show help information if no arguments are provided
            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser
            {
                ShowUsageOnEmptyCommandline = true
            };
            // Parse command line arguments into the application options, catching any exceptions
            parser.ExtractArgumentAttributes(Options);
            try
            {
                parser.ParseCommandLine(args);
            }
            catch (Exception exception)
            {
                // Output error messages for argument exceptions, otherwise rethrow exception
                switch (exception)
                {
                    case FileNotFoundException notFoundException: // Input file could not be found
                        Logger.OutputError("Input file could not be found");
                        break;
                    case MandatoryArgumentNotSetException notSetException: // Required argument not provided
                        Logger.OutputError($"The {notSetException.Argument} argument is required");
                        break;
                    case UnknownArgumentException unknownException: // Not a valid argument
                        Logger.OutputError($"{unknownException.Argument} is not a valid argument");
                        break;
                    case CommandLineFormatException formatException: // Short argument used with two dashes
                        Logger.OutputError("Short arguments must be prefixed with a single '-' character");
                        break;
                    case CommandLineArgumentOutOfRangeException rangeException: // Short argument used with two dashes
                        Logger.OutputError($"The value for {rangeException.Argument} is outside the valid range");
                        break;
                    case TargetInvocationException invocationException: // Invalid value parsed to argument
                    case ArgumentException argException:
                        Logger.OutputError("The values for one or more arguments are invalid");
                        break;
                    default:
                        throw exception;
                }
                // Return failure
                return false;
            }
            // If an output directory has been given and TTI output is being used check it is valid, logging an error if it isn't
            if (Options.OutputType == Output.Type.TTI && !string.IsNullOrEmpty(Options.OutputPath))
            {
                try
                {
                    DirectoryInfo outputDirectory = new DirectoryInfo(Options.OutputPath);
                }
                catch (Exception exception)
                {
                    switch (exception)
                    {
                        case ArgumentException argumentException: // Characters are invalid
                            Logger.OutputError("The output directory contains invalid characters");
                            break;
                        case PathTooLongException lengthException: // Path is too long
                            Logger.OutputError("The output directory path is too long");
                            break;
                        default:
                            throw exception;
                    }
                    // Return failure
                    return false;
                }
            }
            // Return success if arguments are given, otherwise return failure
            return args.Length > 0;
        }

        /// <summary>
        /// Writes an unhandled exception to the console and exits the application.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private static void UnhandledExceptionTrap(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.OutputError($"An unexpected error occurred: {e.ExceptionObject.ToString()}");
            Environment.Exit((int)ExitCodes.Unspecified);
        }
        #endregion
    }
}
