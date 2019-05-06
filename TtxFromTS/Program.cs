using System;
using System.IO;
using System.Reflection;
using CommandLineParser.Exceptions;
using TtxFromTS.Output;

namespace TtxFromTS
{
    /// <summary>
    /// Provides the main functionality for the application
    /// </summary>
    internal class Program
    {
        #region Properties
        /// <summary>
        /// The application options.
        /// </summary>
        /// <value>The options.</value>
        internal static Options Options { get; private set; } = new Options();
        #endregion

        #region Main Application Methods
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        internal static int Main(string[] args)
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
            IOutput output;
            switch (Options.OutputType)
            {
                case Output.Type.StdOut:
                    output = new StdOutput();
                    break;
                default:
                    output = new TTIOutput();
                    break;
            }
            // Setup TS decoder
            TSDecoder tsDecoder = new TSDecoder
            {
                PacketID = Options.PacketIdentifier,
                EnableSubtitles = Options.IncludeSubtitles
            };
            tsDecoder.PacketDecoded += (sender, packet) => output.AddPacket(packet);
            // Open the input file and read it in a loop until the end of the file
            using (FileStream fileStream = Options.InputFile.OpenRead())
            {
                byte[] data = new byte[1316];
                int loggedPercentage = 0;
                while (fileStream.Read(data, 0, 1316) > 0)
                {
                    tsDecoder.DecodeData(data);
                    double currentPercentage = (double)fileStream.Position / (double)fileStream.Length * 100;
                    if (currentPercentage > loggedPercentage + 10)
                    {
                        loggedPercentage = (int)currentPercentage;
                        Logger.OutputInfo($"Reading TS file: {loggedPercentage}%");
                    }
                }
                if (loggedPercentage < 100)
                {
                    Logger.OutputInfo("Reading TS file: 100%");
                }
                // If packets are processed, finish the output and log stats, otherwise return an error
                if (tsDecoder.PacketsDecoded > 0)
                {
                    output.FinishOutput();
                    Logger.OutputStats(tsDecoder.PacketsReceived, tsDecoder.PacketsDecoded, output.Statistics);
                    return (int)ExitCodes.Success;
                }
                else if (tsDecoder.PacketsReceived > 0)
                {
                    Logger.OutputError("Invalid packet identifier provided");
                    return (int)ExitCodes.InvalidPID;
                }
                else
                {
                    Logger.OutputError("Unable to process transport stream - please check it is a valid TS file");
                    return (int)ExitCodes.TSError;
                }
            }
        }

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
            // If an output directory has been given, check it is valid, logging an error if it isn't
            if (!string.IsNullOrEmpty(Options.OutputPath))
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