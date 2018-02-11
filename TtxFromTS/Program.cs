using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private static TeletextDecoder _teletextDecoder;
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
                // Setup decoder
                _teletextDecoder = new TeletextDecoder { EnableSubtitles = _options.IncludeSubtitles };
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
                    case TargetInvocationException invocationException: // Short argument used with two dashes
                        OutputError("The values for one or more arguments are invalid");
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
                    foreach (TeletextCarousel carousel in magazine.Pages)
                    {
                        // Output to the console the page being outputted
                        Console.WriteLine($"Outputting P{magazine.Number}{carousel.Number}");
                        // Set output file path
                        string filePath = outputDirectory.FullName + Path.DirectorySeparatorChar + "P" + magazine.Number + carousel.Number + ".tti";
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
                                // Loop through each subpage in order of subcode, writing each one
                                foreach (TeletextPage page in carousel.Pages.OrderBy(x => x.Number).ToList())
                                {
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
                                        streamWriter.WriteLine($"FL,{page.Links[0].Number},{page.Links[1].Number},{page.Links[2].Number},{page.Links[3].Number},{page.Links[4].Number},{page.Links[5].Number}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            OutputWarning($"P{magazine.Number}{carousel.Number}.tti already exists - skipping page");
                        }
                    }
                }
            }
            // If creation of config file is not disabled, create one
            if (!_options.DisableConfig)
            {
                // Retrieve initial page, if set, to get header template from
                TeletextPage initialPage = null;
                if (_teletextDecoder.InitialPage != "8FF")
                {
                    int magazineNumber = int.Parse(_teletextDecoder.InitialPage.Substring(0, 1));
                    TeletextMagazine magazine = _teletextDecoder.Magazine[magazineNumber - 1];
                    TeletextCarousel carousel = magazine.Pages.Find(x => x.Number == _teletextDecoder.InitialPage);
                    if (carousel != null)
                    {
                        initialPage = carousel.Pages.First();
                    }
                }
                // If no initial page has been retrieved, use the first page from magazine 1
                if (initialPage == null)
                {
                    TeletextCarousel carousel = _teletextDecoder.Magazine[0].Pages.First();
                    if (carousel != null)
                    {
                        initialPage = carousel.Pages.First();
                    }
                }
                // Set header template using initial page, or if there isn't one use default
                string headerTemplate;
                if (initialPage != null)
                {
                    headerTemplate = initialPage.Rows[0].Substring(8);
                    // Replace page number in header with number placeholder
                    headerTemplate = headerTemplate.Replace(initialPage.Magazine.ToString() + initialPage.Number, "%%#");
                    // Get header clock
                    string headerClock = headerTemplate.Substring(24);
                    // Check header clock is actually a clock (i.e. doesn't contain text), don't change if it isn't
                    if (headerClock.Any(x => !char.IsLetter(x)))
                    {
                        // Break up clock in to array of segments, split at start of each number sequence
                        string[] clockSegments = Regex.Matches(headerClock, "[0-9]+|[^0-9]+").Cast<Match>().Select(x => x.Value).ToArray();
                        // Set next element to be replaced in clock, 0 for hours, 1 for mins, 2 for secs
                        int clockPosition = 0;
                        // Loop through each segment, creating clock placeholder
                        string clockPlaceholder = string.Empty;
                        for (int i = 0; i < clockSegments.Length; i++)
                        {
                            // If segments is digits, set the placeholder, otherwise use segment text
                            if(clockSegments[i].All(x => char.IsDigit(x)))
                            {
                                // If segment is a valid format set the placeholder, otherwise use segment text
                                if (clockSegments[i].Length == 2 && clockPosition < 3)
                                {
                                    switch (clockPosition)
                                    {
                                        case 0:
                                            clockPlaceholder += "%H";
                                            break;
                                        case 1:
                                            clockPlaceholder += "%M";
                                            break;
                                        case 2:
                                            clockPlaceholder += "%S";
                                            break;
                                    }
                                    clockPosition += 1;
                                }
                                else if (clockSegments[i].Length == 4 && clockPosition == 0)
                                {
                                    clockPlaceholder += "%H%M";
                                    clockPosition += 2;
                                }
                                else
                                {
                                    clockPlaceholder += clockSegments[i];
                                }
                            }
                            else
                            {
                                clockPlaceholder += clockSegments[i];
                            }
                        }
                        // Replace the clock in the header with the placeholder
                        headerTemplate = headerTemplate.Substring(0, 24) + clockPlaceholder;
                    }
                }
                else
                {
                    headerTemplate = "Teletext %%# %%a %e %%b %H:%M:%S";
                }
                // Set config file path
                string configPath = outputDirectory.FullName + Path.DirectorySeparatorChar + "vbit.conf";
                // Check config file doesn't already exist, and output warning if it does
                if (!File.Exists(configPath))
                {
                    // Create file
                    using (StreamWriter streamWriter = new StreamWriter(File.Open(configPath, FileMode.Create), Encoding.ASCII))
                    {
                        // Write header template
                        streamWriter.WriteLine($"header_template={headerTemplate}");
                        // Write initial page
                        streamWriter.WriteLine($"initial_teletext_page={_teletextDecoder.InitialPage}:{_teletextDecoder.InitialSubcode}");
                        // Write row adaptive setting
                        streamWriter.WriteLine("row_adaptive_mode=false");
                        // Write status display
                        streamWriter.WriteLine($"status_display={_teletextDecoder.StatusDisplay}");
                        // Write network identification code
                        streamWriter.Write($"network_identification_code={_teletextDecoder.NetworkID}");
                    }
                }
                else
                {
                    OutputWarning($"Unable to create config file - file already exists");
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