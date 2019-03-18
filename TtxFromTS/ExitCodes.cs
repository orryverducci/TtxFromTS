using System;

namespace TtxFromTS
{
    internal enum ExitCodes
    {
        Success = 0,
        InvalidArgs = 1,
        InvalidService = 2,
        InvalidPID = 3,
        TSError = 4
    }
}