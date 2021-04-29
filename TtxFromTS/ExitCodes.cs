using System;

namespace TtxFromTS
{
    /// <summary>
    /// Exit codes used by the application.
    /// </summary>
    public enum ExitCodes
    {
        Success = 0,
        InvalidArgs = 1,
        InvalidService = 2,
        InvalidPID = 3,
        TSError = 4,
        Unspecified = 5,
        InvalidServiceID = 6,
        TeletextPIDNotFound = 7
    }
}
