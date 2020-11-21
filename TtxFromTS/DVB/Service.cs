using System;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Represents the information for a service within a transport stream.
    /// </summary>
    public struct Service
    {
        /// <summary>
        /// The service packet ID.
        /// </summary>
        public ushort PID;

        /// <summary>
        /// The service name.
        /// </summary>
        public string Name;
    }
}
