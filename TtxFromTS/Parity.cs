using System;
using System.Collections;

namespace TtxFromTS
{
    internal static class Parity
    {
        /// <summary>
        /// Checks an odd parity encoded bit for errors, and returns the original value if there isn't any
        /// </summary>
        /// <returns>The original parity byte if there is no errors, or 0x00 if there is.</returns>
        /// <param name="encodedByte">Encoded byte.</param>
        internal static byte OddParity(byte encodedByte)
        {
            // Convert byte to an array of bits
            BitArray bits = new BitArray(encodedByte);
            // Count the number of 1 bits
            int bitCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (bits[i])
                {
                    bitCount++;
                }
            }
            // If the number of 1 bits is odd, return the original value byte, otherwise return 0x00
            if (bitCount % 2 != 0)
            {
                return (byte)(encodedByte & 0x7f);
            }
            else
            {
                return 0x00;
            }
        }
    }
}