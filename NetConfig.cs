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

        /// <summary>
        /// Sliding Ping Window
        /// </summary>
        public TimeSpan PingWindow;

        /// <summary>
        /// Constant time added to the resend logic
        /// </summary>
        public TimeSpan ResendTime;

        /// <summary>
        /// Number of attempts to resend a reliable packet before the connection will be dropped
        /// </summary>
        public int ResendAttempts;
    }
}
