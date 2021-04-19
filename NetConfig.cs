using System;

namespace NetFlanders
{
    public readonly struct NetConfig
    {
        public readonly TimeSpan Timeout;

        public NetConfig(TimeSpan timeout)
        {
            Timeout = timeout;
        }
    }
}
