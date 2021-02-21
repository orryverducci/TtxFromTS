using System;
using System.Collections;
using System.Collections.Generic;
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
    public class TTIOutput : IOutput
    {
        #region Enumerations
        private enum PageType
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

        private enum PageEncoding
        {
            OddParity = 0,
            AllData = 1,
            Hamming2418 = 2,
            Hamming84 = 3,
            HammingWithOddParity = 4,
            FirstByteDesignated = 5
        }
        #endregion

        #region Private Fields
        /// <summary>
        /// The teletext decoder.
        /// </summary>
        private readonly Decoder _teletextDecoder;
        #endregion

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
        /// <summary>
        /// Initialises a new instance of the <see cref="T:TtxFromTS.Output.TTIOutput"/> class.
        /// </summary>
        public TTIOutput() => _teletextDecoder = new Decoder();
        #endregion

        #region Generic Output Methods
        /// <summary>
        /// Provides a teletext packet to the output.
        /// </summary>
        /// <param name="packet">The teletext packet.</param>
        public void AddPacket(Packet packet) => _teletextDecoder.DecodePacket(packet);

        /// <summary>
        /// Finalise the output, outputting the decoded pages to TTI files.
        /// </summary>
        public void FinishOutput()
        {
            // If no pages have been decoded finish execution
            if (_teletextDecoder.TotalPages == 0)
            {
                return;
            }
            // Use provided output directory, or get the output directory name from the input filename if not provided
            string directoryName = string.IsNullOrEmpty(Program.Options.OutputPath) ? Program.Options.InputFile!.Name.Substring(0, Program.Options.InputFile.Name.LastIndexOf('.')) : Program.Options.OutputPath;
            // Create the directory to store the pages in
            DirectoryInfo outputDirectory = Directory.CreateDirectory(directoryName);
            // Loop through each magazine to retrieve pages
            foreach (Magazine magazine in _teletextDecoder.Magazine)
            {
                // Check the magazine has pages, otherwise ignore it and skip to the next magazine
                if (magazine.TotalPages == 0)
                {
                    continue;
                }
                // Loop through each carousel and output it as a TTI file
                foreach (Carousel carousel in magazine.Pages)
                {
                    // Log the carousel page number being output
                    Logger.OutputInfo($"Outputting P{magazine.Number}{carousel.Number}");
                    // Output a TTI file for the carousel
                    OutputTTIFile(outputDirectory, magazine.Number + carousel.Number, CarouselData(magazine, carousel));
                }
                // If the magazine has enhancements, output them to a TTI file
                if (magazine.EnhancementData.Any(x => x != null))
                {
                    // Log that enhancements are being output
                    Logger.OutputInfo($"Outputting enhancements for magazine {magazine.Number}");
                    OutputTTIFile(outputDirectory, magazine.Number + "FF", MagazineEnhancementData(magazine));
                }
            }
            // Create a vbit2 configuration file if it hasn't been disabled
            if (!Program.Options.DisableConfig)
            {
                OutputVbitConfig(outputDirectory);
            }
        }
        #endregion

        #region File Output Methods
        /// <summary>
        /// Outputs a TTI file.
        /// </summary>
        /// <param name="outputDirectory">The directory the TTI file should be output to.</param>
        /// <param name="pageNumber">The page number the TTI file is for.</param>
        /// <param name="payload">The payload of encoded data to be output within the TTI file.</param>
        private void OutputTTIFile(DirectoryInfo outputDirectory, string pageNumber, List<string> payload)
        {
            // Set output file path
            string filePath = $"{outputDirectory.FullName}{Path.DirectorySeparatorChar}P{pageNumber}.tti";
            // If the file already exists, output a warning and skip it
            if (File.Exists(filePath))
            {
                Logger.OutputWarning($"P{pageNumber}.tti already exists - skipping");
                return;
            }
            // Write the TTI file
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
                // Write the provided payload
                foreach (string line in payload)
                {
                    streamWriter.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Outputs a vbit configuration file.
        /// </summary>
        /// <param name="outputDirectory">The directory the configuration file should be output to.</param>
        private void OutputVbitConfig(DirectoryInfo outputDirectory)
        {
            // Retrieve the initial page, if set, otherwise use the first page of the first magazine (usually P100)
            Page? initialPage = null;
            if (_teletextDecoder.InitialPage != "8FF")
            {
                int magazineNumber = int.Parse(_teletextDecoder.InitialPage.Substring(0, 1));
                Magazine magazine = _teletextDecoder.Magazine[magazineNumber - 1];
                Carousel? carousel = magazine.Pages.Find(x => x.Number == _teletextDecoder.InitialPage);
                if (carousel != null)
                {
                    initialPage = carousel.Pages.First();
                }
            }
            if (initialPage == null)
            {
                Carousel carousel = _teletextDecoder.Magazine[0].Pages.First();
                if (carousel != null)
                {
                    initialPage = carousel.Pages.First();
                }
            }
            // Set the page header template from the header in the initial page, or if there isn't one use the default
            StringBuilder headerTemplateBuilder = new StringBuilder();
            string headerTemplate;
            if (initialPage != null)
            {
                // Decode the header from the initial page
                for (int i = 8; i < initialPage.Rows[0].Length; i++)
                {
                    // Decode the character, and if the hex code for the character is 0x20 or greater output it as is, otherwise add 0x40 and prefix it with an escape (0x1b)
                    byte decodedChar = Decode.OddParity(initialPage.Rows[0][i]);
                    if (decodedChar >= 0x20)
                    {
                        headerTemplateBuilder.Append(Encoding.ASCII.GetString(new byte[] { decodedChar }));
                    }
                    else
                    {
                        headerTemplateBuilder.Append(Encoding.ASCII.GetString(new byte[] { 0x1b }));
                        headerTemplateBuilder.Append(Encoding.ASCII.GetString(new byte[] { (byte)(decodedChar + 0x40) }));
                    }
                }
                headerTemplate = headerTemplateBuilder.ToString();
                // Replace the page number in header with the number placeholder
                headerTemplate = headerTemplate.Replace(initialPage.Magazine.ToString() + initialPage.Number, "%%#");
                // Get the header clock
                string headerClock = headerTemplate.Substring(headerTemplate.Length - 8);
                // Check the header clock is actually a clock (i.e. it doesn't contain text), and process it if it is
                if (headerClock.All(x => !char.IsLetter(x)))
                {
                    // Break up clock in to array of segments, split at start of each number sequence
                    string[] clockSegments = Regex.Matches(headerClock, "[0-9]+|[^0-9]+").Select(x => x.Value).ToArray();
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
                    headerTemplate = headerTemplate.Substring(0, headerTemplate.Length - 8) + clockPlaceholder;
                }
            }
            else
            {
                headerTemplate = "Teletext %%# %%a %e %%b %H:%M:%S";
            }
            // Set the config file location
            string filePath = $"{outputDirectory.FullName}{Path.DirectorySeparatorChar}vbit.conf";
            // If the file already exists, output a warning and skip it
            if (File.Exists(filePath))
            {
                Logger.OutputWarning($"Unable to create config file - file already exists");
                return;
            }
            // Write the config file
            using (StreamWriter streamWriter = new StreamWriter(File.Open(filePath, FileMode.Create), Encoding.ASCII))
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
                streamWriter.WriteLine($"network_identification_code={_teletextDecoder.NetworkID}");
            }
        }
        #endregion

        #region Encoding Methods
        /// <summary>
        /// Provides the data from a carousel as lines to be inserted in to a TTI file.
        /// </summary>
        /// <returns>A list of strings containing data to be written to the TTI file.</returns>
        /// <param name="magazine">The magazine the carousel belongs to.</param>
        /// <param name="carousel">The carousel to output.</param>
        private List<string> CarouselData(Magazine magazine, Carousel carousel)
        {
            // Create payload data to return
            List<string> payload = new List<string>();
            // Loop through each subpage in order of subcode, writing each one sequentially
            foreach (Page page in carousel.Pages.OrderBy(x => x.Subcode).ToList())
            {
                // Set page type and encoding
                PageType pageType;
                PageEncoding pageEncoding;
                string pageNumber = magazine.Number.ToString() + page.Number;
                if (page.Number == "F0") // TOP BTT
                {
                    pageType = PageType.BasicTOPTable;
                    pageEncoding = PageEncoding.Hamming84;
                }
                else if (magazine.MultipageTablePages.Contains(pageNumber)) // TOP MPT
                {
                    pageType = PageType.MultipageTable;
                    pageEncoding = PageEncoding.Hamming84;
                }
                else if (magazine.AdditionalInformationTablePages.Contains(pageNumber)) // TOP AIT
                {
                    pageType = PageType.AdditionalInformationTable;
                    pageEncoding = PageEncoding.HammingWithOddParity;
                }
                else if (magazine.MultipageExtensionPages.Contains(pageNumber)) // TOP MPT-EX
                {
                    pageType = PageType.MultipageExtension;
                    pageEncoding = PageEncoding.Hamming84;
                }
                else if (page.Number == "FE") // MOT
                {
                    pageType = PageType.MagazineOrganisationTable;
                    pageEncoding = PageEncoding.Hamming84;
                }
                else if (magazine.GlobalObjectPage == pageNumber) // GPOP
                {
                    pageType = PageType.GlobalObjectDefinition;
                    pageEncoding = PageEncoding.Hamming2418;
                }
                else if (magazine.ObjectPages.Contains(pageNumber)) // POP
                {
                    pageType = PageType.ObjectDefinition;
                    pageEncoding = PageEncoding.Hamming2418;
                }
                else if (magazine.GDRCSPages.Contains(pageNumber)) // GDRCS
                {
                    pageType = PageType.GlobalDRCS;
                    pageEncoding = PageEncoding.OddParity;
                }
                else if (magazine.DRCSPages.Contains(pageNumber)) // DRCS
                {
                    pageType = PageType.DRCS;
                    pageEncoding = PageEncoding.OddParity;
                }
                else
                {
                    pageType = PageType.BasicLevel1;
                    pageEncoding = PageEncoding.OddParity;
                }
                // Add page number to payload
                payload.Add($"PN,{magazine.Number}{page.Number}{page.Subcode.Substring(2)}");
                // Add subcode to payload
                payload.Add($"SC,{page.Subcode}");
                // Add the page function to the payload for pages that aren't standard level 1 pages
                if (pageType != PageType.BasicLevel1 || pageEncoding != PageEncoding.OddParity)
                {
                    payload.Add($"PF,{(int)pageType},{(int)pageEncoding}");
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
                // Add the page status to the payload
                byte[] statusBytes = new byte[2];
                statusBits.CopyTo(statusBytes, 0);
                payload.Add($"PS,{BitConverter.ToString(statusBytes).Replace("-", "")}");
                // Add the header to the payload
                payload.Add($"OL,0,XXXXXXXX{EncodeText(page.Rows[0].AsSpan().Slice(8).ToArray())}");
                // Add page enhancements to the payload, if the page has them and the page is not itself an enhancement page
                if (pageType == PageType.BasicLevel1 && page.EnhancementData.Any(x => x != null))
                {
                    for (int i = 0; i < page.EnhancementData.Length; i++)
                    {
                        if (page.EnhancementData[i] != null)
                        {
                            // Write the string
                            payload.Add($"OL,28,{EncodeEnhancement(i, page.EnhancementData[i])}");
                        }
                    }
                }
                if (page.ReplacementData.Any(x => x != null))
                {
                    for (int i = 0; i < page.ReplacementData.Length; i++)
                    {
                        if (page.ReplacementData[i] != null)
                        {
                            // Write the string
                            payload.Add($"OL,26,{EncodeEnhancement(i, page.ReplacementData[i])}");
                        }
                    }
                }
                // Add page links to the payload using an OL row if the page has them and any of them specify a subcode
                bool linksSpecifySubcode = page.Links != null ? page.Links.Any(x => x.Subcode != "3F7F") : false;
                if (linksSpecifySubcode)
                {
                    payload.Add($"OL,27,{EncodeFastextLinks(page.Links!, page.Magazine, page.DisplayRow24)}");
                }
                // Add enhancement links to the payload if the page has them
                for (int i = 0; i < 2; i++)
                {
                    if (page.EnhancementLinks[i] != null)
                    {
                        payload.Add($"OL,27,{EncodeEnhancement(4 + i, page.EnhancementLinks[i])}");
                    }
                }
                // Loop through each row in the page and add the row to the payload if it contains data using the correct encoding for the page
                for (int i = 1; i < page.Rows.Length; i++)
                {
                    if (page.Rows[i] != null)
                    {
                        switch (pageEncoding)
                        {
                            case PageEncoding.Hamming84:
                                payload.Add($"OL,{i},{EncodeHammedData(page.Rows[i])}");
                                break;
                            case PageEncoding.Hamming2418:
                                payload.Add($"OL,{i},{EncodeEnhancement(Decode.Hamming84(page.Rows[i][0]), page.Rows[i])}");
                                break;
                            case PageEncoding.HammingWithOddParity:
                                payload.Add($"OL,{i},{EncodeMixedData(page.Rows[i])}");
                                break;
                            default:
                                payload.Add($"OL,{i},{EncodeText(page.Rows[i])}");
                                break;
                        }
                    }
                }
                // Add fastext page links to the payload using an FL if page has them and they haven't already been added
                if (page.Links != null && !linksSpecifySubcode)
                {
                    payload.Add($"FL,{page.Links[0].Number},{page.Links[1].Number},{page.Links[2].Number},{page.Links[3].Number},{page.Links[4].Number},{page.Links[5].Number}");
                }
            }
            // Return the payload
            return payload;
        }

        /// <summary>
        /// Provides magazine enhancement data as lines to be inserted in to a TTI file.
        /// </summary>
        /// <returns>A list of strings containing data to be written to the TTI file.</returns>
        /// <param name="magazine">The magazine to output enhancement data from.</param>
        private List<string> MagazineEnhancementData(Magazine magazine)
        {
            // Create payload data to return
            List<string> payload = new List<string>();
            // Add the page number for the time filling page to the payload, which is used for magazine enhancements
            payload.Add($"PN,{magazine.Number}FF00");
            // Add the subcode to the payload
            payload.Add("SC,0000");
            // Add a page status to the payload in which all the bits are false, indicating not to transmit the page
            payload.Add("PS,0000");
            // Add each magazine enhancement packet to the payload if it contains data
            for (int i = 0; i < magazine.EnhancementData.Length; i++)
            {
                if (magazine.EnhancementData[i] != null)
                {
                    // Write the string
                    payload.Add($"OL,29,{EncodeEnhancement(i, magazine.EnhancementData[i])}");
                }
            }
            // Return the payload
            return payload;
        }

        /// <summary>
        /// Encodes text and control characters to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="text">The text to be encoded.</param>
        private string EncodeText(byte[] text)
        {
            // Create the string to be returned
            StringBuilder outputString = new StringBuilder();
            // Loop through each byte and process it
            for (int i = 0; i < text.Length; i++)
            {
                // Decode the character, and if the hex code for the character is 0x20 or greater output it as is, otherwise add 0x40 and prefix it with an escape (0x1b)
                byte decodedChar = Decode.OddParity(text[i]);
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
            // Return the encoded string
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
            // Create the string to be returned
            StringBuilder outputString = new StringBuilder(40);
            // Output the designation code
            outputString.Append((char)(designation | 0x40));
            // Set the offset for the initial triplet
            int tripletOffset = enhancementPacket.Length == 40 ? 1 : 0;
            // Loop through each triplet and process it
            while (tripletOffset + 2 < enhancementPacket.Length)
            {
                // Get the triplet
                byte[] triplet = enhancementPacket.AsSpan().Slice(tripletOffset, 3).ToArray();
                // Decode the triplet
                byte[] decodedTriplet = Decode.Hamming2418(triplet);
                // If the triplet doesn't have unrecoverable errors encoded it and output, otherwise output a blank triplet
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
            // Return the encoded string
            return outputString.ToString();
        }

        /// <summary>
        /// Encodes hamming encoded packets to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="dataPacket">The text to be encoded.</param>
        private string EncodeHammedData(byte[] dataPacket)
        {
            // Create the string to be returned
            StringBuilder outputString = new StringBuilder();
            // Loop through each byte, decode it and output it encoded for TTI
            for (int i = 0; i < dataPacket.Length; i++)
            {
                byte decodedChar = Decode.Hamming84(dataPacket[i]);
                outputString.Append((char)(decodedChar | 0x40));
            }
            // Return the encoded string
            return outputString.ToString();
        }

        /// <summary>
        /// Encodes packets with both hamming and odd parity (i.e. TOP AIT) to a format valid for TTI files.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="dataPacket">The data to be encoded.</param>
        private string EncodeMixedData(byte[] dataPacket)
        {
            // Create the string to be returned
            StringBuilder outputString = new StringBuilder();
            // Loop through each byte and process it
            for (int i = 0; i < dataPacket.Length; i++)
            {
                // Encode the byte based on appropraite encoding for the character position (hamming encoded in positions before 8 and between 19 and 28)
                if (i < 8 || (i > 19 && i < 28))
                {
                    // Decode the byte and output it encoded for TTI
                    byte decodedChar = Decode.Hamming84(dataPacket[i]);
                    outputString.Append((char)(decodedChar | 0x40));
                }
                else
                {
                    // Decode the character, and if the hex code for the character is 0x20 or greater output it as is, otherwise add 0x40 and prefix it with an escape (0x1b)
                    byte decodedChar = Decode.OddParity(dataPacket[i]);
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
            // Return the encoded string
            return outputString.ToString();
        }

        /// <summary>
        /// Encodes fastext page links as hamming encoded packets.
        /// </summary>
        /// <returns>The encoded string.</returns>
        /// <param name="links">The linked page numbers and subcodes.</param>
        /// <param name="magazine">The current page magazine number.</param>
        /// <param name="displayRow24"><c>true</c> row 24 should be displayed, <c>false</c> otherwise.</param>
        private string EncodeFastextLinks((string Number, string Subcode)[] links, int magazine, bool displayRow42)
        {
            // Create the string to be returned
            StringBuilder outputString = new StringBuilder();
            // Output the designation code
            outputString.Append((char)0x40);
            // Loop through each link and output its bytes
            for (int i = 0; i < 6; i++)
            {
                byte magazineByte = (byte)((magazine < 8 ? magazine : 0) ^ Convert.ToByte(links[i].Number[0].ToString(), 16));
                outputString.Append((char)(Convert.ToByte(links[i].Number[2].ToString(), 16) | 0x40));
                outputString.Append((char)(Convert.ToByte(links[i].Number[1].ToString(), 16) | 0x40));
                outputString.Append((char)(Convert.ToByte(links[i].Subcode[3].ToString(), 16) | 0x40));
                outputString.Append((char)((byte)(Convert.ToByte(links[i].Subcode[2].ToString(), 16) | ((magazineByte & 0x04) << 1) | 0x40)));
                outputString.Append((char)(Convert.ToByte(links[i].Subcode[1].ToString(), 16) | 0x40));
                outputString.Append((char)((byte)(Convert.ToByte(links[i].Subcode[0].ToString(), 16) | ((magazineByte & 0x03) << 2) | 0x40)));
            }
            // Output the link control byte
            outputString.Append((char)(displayRow42 ? 0x48 : 0x40));
            // Output a blank CRC (this should be replaced by the transmitting application)
            outputString.Append(Encoding.ASCII.GetString(new byte[] { 0x40, 0x40 }));
            // Return the encoded string
            return outputString.ToString();
        }
        #endregion
    }
}
