namespace NetFlanders
{
    internal static class IOControlCodes
    {
        private const uint In = 0x80000000;
        private const uint Vendor = 0x18000000;

        public const int UDPConnectionReset = unchecked((int)(In | Vendor | 12)); //This is not contained in the IOControlCode enum for some reason, that's why it is here
    }
}
