using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
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
                    // If packets are processed, output pages and the number processed, otherwise return an error
                    if (packetsProcessed > 0)
                    {
                        // If pages have been decoded, output them
                        if (_teletextDecoder.TotalPages > 0)
                        {
                            OutputPages();
                        }
                        // Output stats
                        Console.WriteLine($"Total number of packets: {packetsDecoded}");
                        Console.WriteLine($"Packets processed: {packetsProcessed}");
                        Console.WriteLine($"Pages decoded: {_teletextDecoder.TotalPages}");
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
                    case CommandLineArgumentOutOfRangeException rangeException: // Short argument used with two dashes
                        OutputError($"The value for {rangeException.Argument} is outside the valid range");
                        break;
                    default:
                        throw exception;
                }
                // Return failure
                return false;
            }
            // If output directory has been given, check it is valid, output error if it isn't
            if (_options.OutputPath != null && _options.OutputPath != string.Empty)
            {
                try
                {
                    DirectoryInfo outputDirectory = new DirectoryInfo(_options.OutputPath);
                }
                catch (Exception exception)
                {
                    switch (exception)
                    {
                        case ArgumentException argumentException: // Characters are invalid
                            OutputError("The output directory contains invalid characters");
                            break;
                        case PathTooLongException lengthException: // Path is too long
                            OutputError("The output directory path is too long");
                            break;
                        default:
                            throw exception;
                    }
                    // Return failure
                    return false;
                }
            }
            // Return success if arguments are given, otherwise return failure
            return (args.Length > 0) ? true : false;
        }

        /// <summary>
        /// Outputs the decoded pages to TTI files.
        /// </summary>
        private static void OutputPages()
        {
            // Use provided output directory, or get the output directory name from the input filename if not provided
            string directoryName;
            if (_options.OutputPath != null && _options.OutputPath != string.Empty)
            {
                directoryName = _options.OutputPath;
            }
            else
            {
                directoryName = _options.InputFile.Name.Substring(0, _options.InputFile.Name.LastIndexOf('.'));
            }
            // Create the directory to store the pages in
            DirectoryInfo outputDirectory = Directory.CreateDirectory(directoryName);
            // Loop through each magazine to retrieve pages, if any
            foreach (TeletextMagazine magazine in _teletextDecoder.Magazine)
            {
                // Check magazine has pages, otherwise ignore magazine
                if (magazine.TotalPages > 0)
                {
                    // Loop through each page and output it
                    foreach (TeletextPage page in magazine.Pages)
                    {
                        // Output to the console the page being outputted
                        Console.WriteLine($"Outputting P{page.Magazine}{page.Number} subpage {page.Subcode}");
                        // Set output file path
                        string filePath = outputDirectory.FullName + Path.DirectorySeparatorChar + "P" + magazine.Number + page.Number + "-S" + page.Subcode + ".tti";
                        // Check file doesn't already exist, and output warning if it does
                        if (!File.Exists(filePath))
                        {
                            // Create file
                            using (StreamWriter streamWriter = new StreamWriter(File.Open(filePath, FileMode.Create), Encoding.ASCII))
                            {
                                // Write description
                                streamWriter.WriteLine("DE,Exported by TtxFromTS");
                                // Write destination inserter
                                streamWriter.WriteLine("DS,inserter");
                                // Write source file
                                streamWriter.WriteLine($"SP,{_options.InputFile}");
                                // Write cycle time
                                streamWriter.WriteLine($"CT,{_options.CycleTime},T");
                                // Write page number
                                streamWriter.WriteLine($"PN,{magazine.Number}{page.Number}{page.Subcode.Substring(2)}");
                                // Write subcode
                                streamWriter.WriteLine($"SC,{page.Subcode}");
                                // Create page status bits
                                BitArray statusBits = new BitArray(16);
                                // Set status bits
                                statusBits[0] = Convert.ToBoolean(((int)page.NationalOptionCharacterSubset & 0x02) >> 1); // Language (C13)
                                statusBits[1] = Convert.ToBoolean((int)page.NationalOptionCharacterSubset & 0x01); // Language (C14)
                                statusBits[2] = page.MagazineSerial; // Magazine serial
                                statusBits[7] = true; // Transmit page
                                statusBits[8] = page.Newsflash; // Newsflash
                                statusBits[9] = page.Subtitles; // Subtitle
                                statusBits[10] = page.SuppressHeader; // Suppress Header
                                statusBits[13] = page.InhibitDisplay; // Inhibit Display
                                statusBits[15] = Convert.ToBoolean(((int)page.NationalOptionCharacterSubset & 0x03) >> 2); // Language (C12)
                                // Write page status
                                byte[] statusBytes = new byte[2];
                                statusBits.CopyTo(statusBytes, 0);
                                streamWriter.WriteLine($"PS,{BitConverter.ToString(statusBytes).Replace("-", "")}");
                                // Write region code
                                streamWriter.WriteLine("RE,0");
                                // Write header
                                streamWriter.WriteLine($"OL,0,XXXXXXXX{EncodeText(page.Rows[0].Substring(8))}");
                                // Loop through each page row
                                for (int i = 1; i < page.Rows.Length; i++)
                                {
                                    // Write the row if it contains data, otherwise skip it
                                    if (page.Rows[i] != null)
                                    {
                                        streamWriter.WriteLine($"OL,{i},{EncodeText(page.Rows[i])}");
                                    }
                                }
                                // Write fastext links, if page has them
                                if (page.Links != null)
                                {
                                    streamWriter.Write($"FL,{page.Links[0].Number},{page.Links[1].Number},{page.Links[2].Number},{page.Links[3].Number},{page.Links[4].Number},{page.Links[5].Number}");
                                }
                            }
                        }
                        else
                        {
                            OutputWarning($"P{magazine.Number}{page.Number}-S{page.Subcode}.tti already exists - skipping page");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Encodes text and control characters to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="text">The text to be encoded.</param>
        private static string EncodeText(string text)
        {
            // Get bytes from row string
            byte[] inputBytes = Encoding.ASCII.GetBytes(text);
            // Initialise output
            StringBuilder outputString = new StringBuilder();
            // Go through each byte
            for (int i = 0; i < inputBytes.Length; i++)
            {
                // If hex code is 0x20 or greater, output it as is, otherwise add 0x40 and prefix with an escape
                if (inputBytes[i] >= 0x20)
                {
                    outputString.Append(Encoding.ASCII.GetString(new byte[] { inputBytes[i] }));
                }
                else
                {
                    outputString.Append(Encoding.ASCII.GetString(new byte[] { 0x1b }));
                    outputString.Append(Encoding.ASCII.GetString(new byte[] { (byte)(inputBytes[i] + 0x40) }));
                }
            }
            return outputString.ToString();
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

        /// <summary>
        /// Outputs a warning message to the console's standard error output.
        /// </summary>
        /// <param name="warningMessage">The warning message to be displayed.</param>
        private static void OutputWarning(string warningMessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Error.Write("[WARNING] ");
            Console.ResetColor();
            Console.Error.WriteLine(warningMessage);
        }
        #endregion
    }
}