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
    public class Program
    {
        #region Private Fields
        /// <summary>
        /// The chosen application output.
        /// </summary>
        private static IOutput _output;

        /// <summary>
        /// The transport stream decoder.
        /// </summary>
        private static TSDecoder _tsDecoder;
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
            // Setup TS decoder
            _tsDecoder = new TSDecoder
            {
                PacketID = Options.PacketIdentifier,
                EnableSubtitles = Options.IncludeSubtitles
            };
            _tsDecoder.PacketDecoded += (sender, packet) => _output.AddPacket(packet);
            // Process the file
            ProcessFile();
            // Finish the output and log stats
            _output.FinishOutput();
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

        /// <summary>
        /// Opens the provided input file and retrieves packets of data from it.
        /// </summary>
        private static void ProcessFile()
        {
            // Open the input file and read it until the end of the file, or in a loop if enabled
            using (FileStream fileStream = Options.InputFile.OpenRead())
            {
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
                        _tsDecoder.DecodeData(data);
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
                        if (_tsDecoder.PacketsReceived == 0)
                        {
                            Logger.OutputError("Unable to process transport stream - please check it is a valid TS file");
                            Environment.Exit((int)ExitCodes.TSError);
                        }
                        if (_tsDecoder.PacketsDecoded == 0)
                        {
                            Logger.OutputError("Invalid packet identifier provided");
                            Environment.Exit((int)ExitCodes.InvalidPID);
                        }
                        // If looping reset the file back to the start, otherwise finish processing
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
