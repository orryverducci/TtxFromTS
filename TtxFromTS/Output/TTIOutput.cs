using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TtxFromTS.Teletext;
using Decoder = TtxFromTS.Teletext.Decoder;

namespace TtxFromTS.Output
{
    /// <summary>
    /// Provides output to TTI files.
    /// </summary>
    internal class TTIOutput : IOutput
    {
        #region Enumerations
        enum PageType
        {
            BasicLevel1 = 0,
            DataBroadcasting = 1,
            GlobalObjectDefinition = 2,
            ObjectDefinition = 3,
            GlobalDRCS = 4,
            DRCS = 5,
            MagazineOrganisationTable = 6,
            MagazineInventory = 7,
            BasicTOPTable = 8,
            AdditionalInformationTable = 9,
            MultipageTable = 10,
            MultipageExtension = 11
        }

        enum PageEncoding
        {
            OddParity = 0,
            AllData = 1,
            Hamming2418 = 2,
            Hamming84 = 3,
            HammingWithOddParity = 4,
            FirstByteDesignated = 5
        }
        #endregion

        /// <summary>
        /// The teletext decoder.
        /// </summary>
        private Decoder _teletextDecoder;

        #region Properties
        /// <summary>
        /// Gets the TTI output statistics.
        /// </summary>
        /// <value>A tuple containing the statistic title and its value.</value>
        public (string, string)[] Statistics
        {
            get
            {
                return new (string, string)[]
                {
                    ("Pages decoded", _teletextDecoder.TotalPages.ToString())
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
        internal TTIOutput()
        {
            _teletextDecoder = new Decoder();
        }
        #endregion

        #region Generic Output Methods
        /// <summary>
        /// Provides a teletext packet to the output.
        /// </summary>
        /// <param name="packet">The teletext packet.</param>
        public void AddPacket(Packet packet)
        {
            _teletextDecoder.DecodePacket(packet);
        }

        /// <summary>
        /// Finalise the output, outputting the TTI page files.
        /// </summary>
        public void FinishOutput()
        {
            // If pages have been decoded, output them
            if (_teletextDecoder.TotalPages > 0)
            {
                OutputPages();
            }
        }
        #endregion

        #region TTI Output Methods
        /// <summary>
        /// Outputs the decoded pages to TTI files.
        /// </summary>
        private void OutputPages()
        {
            // Use provided output directory, or get the output directory name from the input filename if not provided
            string directoryName;
            if (Program.Options.OutputPath != null && Program.Options.OutputPath != string.Empty)
            {
                directoryName = Program.Options.OutputPath;
            }
            else
            {
                directoryName = Program.Options.InputFile.Name.Substring(0, Program.Options.InputFile.Name.LastIndexOf('.'));
            }
            // Create the directory to store the pages in
            DirectoryInfo outputDirectory = Directory.CreateDirectory(directoryName);
            // Loop through each magazine to retrieve pages, if any
            foreach (Magazine magazine in _teletextDecoder.Magazine)
            {
                // Check magazine has pages, otherwise ignore magazine
                if (magazine.TotalPages > 0)
                {
                    // Loop through each page and output it
                    foreach (Carousel carousel in magazine.Pages)
                    {
                        // Output to the console the page being outputted
                        Logger.OutputInfo($"Outputting P{magazine.Number}{carousel.Number}");
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
                                streamWriter.WriteLine($"SP,{Program.Options.InputFile}");
                                // Write cycle time
                                streamWriter.WriteLine($"CT,{Program.Options.CycleTime},T");
                                // Loop through each subpage in order of subcode, writing each one
                                foreach (Page page in carousel.Pages.OrderBy(x => x.Subcode).ToList())
                                {
                                    // Set page type and encoding
                                    PageType pageType;
                                    PageEncoding pageEncoding;
                                    if (page.Number == "F0") // TOP BTT
                                    {
                                        pageType = PageType.BasicTOPTable;
                                        pageEncoding = PageEncoding.Hamming84;
                                    }
                                    else if (magazine.MultipageTablePages.Contains(magazine.Number.ToString() + page.Number)) // TOP MPT
                                    {
                                        pageType = PageType.MultipageTable;
                                        pageEncoding = PageEncoding.Hamming84;
                                    }
                                    else if (magazine.AdditionalInformationTablePages.Contains(magazine.Number.ToString() + page.Number)) // TOP AIT
                                    {
                                        pageType = PageType.AdditionalInformationTable;
                                        pageEncoding = PageEncoding.HammingWithOddParity;
                                    }
                                    else if (magazine.MultipageExtensionPages.Contains(magazine.Number.ToString() + page.Number)) // TOP MPT-EX
                                    {
                                        pageType = PageType.MultipageExtension;
                                        pageEncoding = PageEncoding.Hamming84;
                                    }
                                    else if (page.Number == "FE") // MOT
                                    {
                                        pageType = PageType.MagazineOrganisationTable;
                                        pageEncoding = PageEncoding.Hamming84;
                                    }
                                    else if (magazine.GlobalObjectPage == magazine.Number.ToString() + page.Number) // GPOP
                                    {
                                        pageType = PageType.GlobalObjectDefinition;
                                        pageEncoding = PageEncoding.Hamming2418;
                                    }
                                    else if (magazine.ObjectPages.Contains(magazine.Number.ToString() + page.Number)) // POP
                                    {
                                        pageType = PageType.ObjectDefinition;
                                        pageEncoding = PageEncoding.Hamming2418;
                                    }
                                    else if (magazine.GDRCSPages.Contains(magazine.Number.ToString() + page.Number)) // GDRCS
                                    {
                                        pageType = PageType.GlobalDRCS;
                                        pageEncoding = PageEncoding.OddParity;
                                    }
                                    else if (magazine.DRCSPages.Contains(magazine.Number.ToString() + page.Number)) // DRCS
                                    {
                                        pageType = PageType.DRCS;
                                        pageEncoding = PageEncoding.OddParity;
                                    }
                                    else
                                    {
                                        pageType = PageType.BasicLevel1;
                                        pageEncoding = PageEncoding.OddParity;
                                    }
                                    // Write page number
                                    streamWriter.WriteLine($"PN,{magazine.Number}{page.Number}{page.Subcode.Substring(2)}");
                                    // Write subcode
                                    streamWriter.WriteLine($"SC,{page.Subcode}");
                                    // Write page function for pages that aren't standard level 1 pages
                                    if (pageType != PageType.BasicLevel1 || pageEncoding != PageEncoding.OddParity)
                                    {
                                        streamWriter.WriteLine($"PF,{(int)pageType},{(int)pageEncoding}");
                                    }
                                    // Create page status bits
                                    BitArray statusBits = new BitArray(16);
                                    // Set status flag bits
                                    statusBits[2] = page.MagazineSerial; // Magazine serial
                                    statusBits[7] = true; // Transmit page
                                    statusBits[8] = page.Newsflash; // Newsflash
                                    statusBits[9] = page.Subtitles; // Subtitle
                                    statusBits[10] = page.SuppressHeader; // Suppress Header
                                    statusBits[13] = page.InhibitDisplay; // Inhibit Display
                                    // Set status language bits if page is a standard level 1 page or a TOP Additional Information Table
                                    if (pageType == PageType.BasicLevel1 || pageType == PageType.AdditionalInformationTable)
                                    {
                                        statusBits[0] = Convert.ToBoolean(((int)page.NationalOptionCharacterSubset & 0x02) >> 1); // Language (C13)
                                        statusBits[1] = Convert.ToBoolean((int)page.NationalOptionCharacterSubset & 0x01); // Language (C14)
                                        statusBits[15] = Convert.ToBoolean(((int)page.NationalOptionCharacterSubset & 0x03) >> 2); // Language (C12)
                                    }
                                    // Write page status
                                    byte[] statusBytes = new byte[2];
                                    statusBits.CopyTo(statusBytes, 0);
                                    streamWriter.WriteLine($"PS,{BitConverter.ToString(statusBytes).Replace("-", "")}");
                                    // Write header
                                    byte[] headerBytes = new byte[page.Rows[0].Length - 8];
                                    Buffer.BlockCopy(page.Rows[0], 8, headerBytes, 0, headerBytes.Length);
                                    streamWriter.WriteLine($"OL,0,XXXXXXXX{EncodeText(headerBytes)}");
                                    // Write page enhancements, if the page has them and the page is not itself an enhancement page
                                    if (pageType == PageType.BasicLevel1 && page.EnhancementData.Any(x => x != null))
                                    {
                                        // Write each enhancement packet
                                        for (int i = 0; i < page.EnhancementData.Length; i++)
                                        {
                                            if (page.EnhancementData[i] != null)
                                            {
                                                // Write the string
                                                streamWriter.WriteLine($"OL,28,{EncodeEnhancement(i, page.EnhancementData[i])}");
                                            }
                                        }
                                    }
                                    if (page.ReplacementData.Any(x => x != null))
                                    {
                                        // Write each enhancement packet
                                        for (int i = 0; i < page.ReplacementData.Length; i++)
                                        {
                                            if (page.ReplacementData[i] != null)
                                            {
                                                // Write the string
                                                streamWriter.WriteLine($"OL,26,{EncodeEnhancement(i, page.ReplacementData[i])}");
                                            }
                                        }
                                    }
                                    // Loop through each page row
                                    for (int i = 1; i < page.Rows.Length; i++)
                                    {
                                        // Write the row if it contains data, otherwise skip it
                                        if (page.Rows[i] != null)
                                        {
                                            // Write row using correct encoding for the page
                                            switch (pageEncoding)
                                            {
                                                case PageEncoding.Hamming84:
                                                    streamWriter.WriteLine($"OL,{i},{EncodeHammedData(page.Rows[i])}");
                                                    break;
                                                case PageEncoding.Hamming2418:
                                                    streamWriter.WriteLine($"OL,{i},{EncodeEnhancement(Decode.Hamming84(page.Rows[i][0]), page.Rows[i])}");
                                                    break;
                                                case PageEncoding.HammingWithOddParity:
                                                    streamWriter.WriteLine($"OL,{i},{EncodeMixedData(page.Rows[i])}");
                                                    break;
                                                default:
                                                    streamWriter.WriteLine($"OL,{i},{EncodeText(page.Rows[i])}");
                                                    break;
                                            }
                                        }
                                    }
                                    // Write fastext links, if page has them, and service does not have TOP pages
                                    if (page.Links != null && !magazine.Pages.Any(x => x.Number == "F0" ))
                                    {
                                        streamWriter.WriteLine($"FL,{page.Links[0].Number},{page.Links[1].Number},{page.Links[2].Number},{page.Links[3].Number},{page.Links[4].Number},{page.Links[5].Number}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.OutputWarning($"P{magazine.Number}{carousel.Number}.tti already exists - skipping page");
                        }
                    }
                    // If the magazine has enhancements, output them
                    if (magazine.EnhancementData.Any(x => x != null))
                    {
                        // Output to the console that enhancements are being outputted
                        Logger.OutputInfo($"Outputting enhancements for magazine {magazine.Number}");
                        // Set output file path
                        string filePath = outputDirectory.FullName + Path.DirectorySeparatorChar + "P" + magazine.Number + "FF.tti";
                        // Write page number
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
                                streamWriter.WriteLine($"SP,{Program.Options.InputFile}");
                                // Write page number (time filling page is used for magazine enhancements)
                                streamWriter.WriteLine($"PN,{magazine.Number}FF00");
                                // Set subcode
                                streamWriter.WriteLine("SC,0000");
                                // Set page status, set to not transmit so page is not outputted
                                streamWriter.WriteLine("PS,0000");
                                // Write each enhancement packet
                                for (int i = 0; i < magazine.EnhancementData.Length; i++)
                                {
                                    if (magazine.EnhancementData[i] != null)
                                    {
                                        // Write the string
                                        streamWriter.WriteLine($"OL,29,{EncodeEnhancement(i, magazine.EnhancementData[i])}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.OutputWarning($"P{magazine.Number}FF.tti already exists - skipping enhancements");
                        }
                    }
                }
            }
            // If creation of config file is not disabled, create one
            if (!Program.Options.DisableConfig)
            {
                // Retrieve initial page, if set, to get header template from
                Page initialPage = null;
                if (_teletextDecoder.InitialPage != "8FF")
                {
                    int magazineNumber = int.Parse(_teletextDecoder.InitialPage.Substring(0, 1));
                    Magazine magazine = _teletextDecoder.Magazine[magazineNumber - 1];
                    Carousel carousel = magazine.Pages.Find(x => x.Number == _teletextDecoder.InitialPage);
                    if (carousel != null)
                    {
                        initialPage = carousel.Pages.First();
                    }
                }
                // If no initial page has been retrieved, use the first page from magazine 1
                if (initialPage == null)
                {
                    Carousel carousel = _teletextDecoder.Magazine[0].Pages.First();
                    if (carousel != null)
                    {
                        initialPage = carousel.Pages.First();
                    }
                }
                // Set header template using initial page, or if there isn't one use default
                string headerTemplate;
                if (initialPage != null)
                {
                    // Decode the header from the initial page
                    byte[] decodedHeader = new byte[initialPage.Rows[0].Length - 8];
                    for (int i = 8; i < initialPage.Rows[0].Length; i++)
                    {
                        decodedHeader[i - 8] = Decode.OddParity(initialPage.Rows[0][i]);
                    }
                    headerTemplate = Encoding.ASCII.GetString(decodedHeader);
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
                    Logger.OutputWarning($"Unable to create config file - file already exists");
                }
            }
        }

        /// <summary>
        /// Encodes text and control characters to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="text">The text to be encoded.</param>
        private string EncodeText(byte[] text)
        {
            // Initialise output
            StringBuilder outputString = new StringBuilder();
            // Go through each byte
            for (int i = 0; i < text.Length; i++)
            {
                // Decode the character
                byte decodedChar = Decode.OddParity(text[i]);
                // If hex code is 0x20 or greater, output it as is, otherwise add 0x40 and prefix with an escape
                if (decodedChar >= 0x20)
                {
                    outputString.Append(Encoding.ASCII.GetString(new byte[] { decodedChar }));
                }
                else
                {
                    outputString.Append(Encoding.ASCII.GetString(new byte[] { 0x1b }));
                    outputString.Append(Encoding.ASCII.GetString(new byte[] { (byte)(decodedChar + 0x40) }));
                }
            }
            return outputString.ToString();
        }

        /// <summary>
        /// Encodes enhancement packets to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="designation">The designation code for the enhancement data.</param>
        /// <param name="enhancementPacket">The enhancement data to be encoded.</param>
        private string EncodeEnhancement(int designation, byte[] enhancementPacket)
        {
            // Create string to be written
            StringBuilder outputString = new StringBuilder(40);
            // Write designation code
            outputString.Append((char)(designation | 0x40));
            // Set offset for initial triplet
            int tripletOffset;
            if (enhancementPacket.Length == 40)
            {
                tripletOffset = 1;
            }
            else
            {
                tripletOffset = 0;
            }
            // Loop through each triplet and process it
            while (tripletOffset + 2 < enhancementPacket.Length)
            {
                // Get the triplet
                byte[] triplet = new byte[3];
                Buffer.BlockCopy(enhancementPacket, tripletOffset, triplet, 0, 3);
                // Decode the triplet
                byte[] decodedTriplet = Decode.Hamming2418(triplet);
                // If triplet doesn't have unrecoverable errors, encoded it and add it to the packet, otherwise add a blank triplet
                if (decodedTriplet[0] != 0xff)
                {
                    outputString.Append((char)(decodedTriplet[0] | 0x40));
                    outputString.Append((char)(decodedTriplet[1] | 0x40));
                    outputString.Append((char)(decodedTriplet[2] | 0x40));
                }
                else
                {
                    outputString.Append((char)(0x40));
                    outputString.Append((char)(0x40));
                    outputString.Append((char)(0x40));
                }
                // Increase the offset to the next triplet
                tripletOffset += 3;
            }
            // Return string
            return outputString.ToString();
        }

        /// <summary>
        /// Encodes hamming encoded packets to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="dataPacket">The text to be encoded.</param>
        private string EncodeHammedData(byte[] dataPacket)
        {
            // Create string to be written
            StringBuilder outputString = new StringBuilder();
            // Go through each byte
            for (int i = 0; i < dataPacket.Length; i++)
            {
                // Decode the byte
                byte decodedChar = Decode.Hamming84(dataPacket[i]);
                // If the byte doesn't have an unrecoverable error
                outputString.Append((char)(decodedChar | 0x40));
            }
            return outputString.ToString();
        }

        /// <summary>
        /// Encodes packets with both hamming and odd parity (i.e. TOP AIT) to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="dataPacket">The data to be encoded.</param>
        private string EncodeMixedData(byte[] dataPacket)
        {
            // Create string to be written
            StringBuilder outputString = new StringBuilder();
            // Go through each byte
            for (int i = 0; i < dataPacket.Length; i++)
            {
                // Encode byte based on appropraite formatting for the character position
                if (i < 8 || (i > 19 && i < 28))
                {
                    // Decode the byte
                    byte decodedChar = Decode.Hamming84(dataPacket[i]);
                    // If the byte doesn't have an unrecoverable error
                    outputString.Append((char)(decodedChar | 0x40));
                }
                else
                {
                    // Decode the character
                    byte decodedChar = Decode.OddParity(dataPacket[i]);
                    // If hex code is 0x20 or greater, output it as is, otherwise add 0x40 and prefix with an escape
                    if (decodedChar >= 0x20)
                    {
                        outputString.Append(Encoding.ASCII.GetString(new byte[] { decodedChar }));
                    }
                    else
                    {
                        outputString.Append(Encoding.ASCII.GetString(new byte[] { 0x1b }));
                        outputString.Append(Encoding.ASCII.GetString(new byte[] { (byte)(decodedChar + 0x40) }));
                    }
                }
            }
            return outputString.ToString();
        }
        #endregion
    }
}