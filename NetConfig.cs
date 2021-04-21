using System;

namespace NetFlanders
{
    public struct NetConfig
    {
        /// <summary>
        /// If a peer receives no packets within this time frame, the connection will be closed
        /// </summary>
        public TimeSpan Timeout;

        /// <summary>
        /// Time to hold received data packets before processing them (useful for interpolation)
        /// </summary>
        public TimeSpan PacketBufferTime;
    }
}
