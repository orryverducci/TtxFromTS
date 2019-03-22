using System;
using System.Reflection;

namespace TtxFromTS
{
    /// <summary>
    /// Provides console logging functionality.
    /// </summary>
    internal static class Logger
    {
        /// <summary>
        /// Outputs an introductory header message to the console's standard error output.
        /// </summary>
        /// <param name="errorMessage">The error message to be displayed.</param>
        internal static void OutputHeader()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.Error.Write($" TtxFromTS {Assembly.GetEntryAssembly().GetName().Version.ToString(2)} ");
            Console.ResetColor();
            Console.Error.WriteLine();
            Console.Error.WriteLine();
        }

        /// <summary>
        /// Outputs an info message to the console's standard error output.
        /// </summary>
        /// <param name="infoMessage">The info message to be displayed.</param>
        internal static void OutputInfo(string infoMessage)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Error.Write("[INFO] ");
            Console.ResetColor();
            Console.Error.WriteLine(infoMessage);
        }

        /// <summary>
        /// Outputs an error message to the console's standard error output.
        /// </summary>
        /// <param name="errorMessage">The error message to be displayed.</param>
        internal static void OutputError(string errorMessage)
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
        internal static void OutputWarning(string warningMessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Error.Write("[WARNING] ");
            Console.ResetColor();
            Console.Error.WriteLine(warningMessage);
        }

        /// <summary>
        /// Outputs decoding stats to the console's standard error output.
        /// </summary>
        /// <param name="packetsReceived">The number of packets received.</param>
        /// <param name="packetsProcessed">The number of packets processed.</param>
        /// <param name="pagesDecoded">The number of pages decoded.</param>
        internal static void OutputStats(int packetsReceived, int packetsProcessed, int pagesDecoded)
        {
            Console.Error.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.WriteLine("[STATISTICS]");
            Console.ResetColor();
            Console.Error.WriteLine($"Total number of packets: {packetsReceived}");
            Console.Error.WriteLine($"Packets processed: {packetsProcessed}");
            Console.Error.WriteLine($"Pages decoded: {pagesDecoded}");
        }
    }
}