using System.Net.Sockets;

namespace NetFlanders
{
    internal static class UdpClientExtensions
    {
        public static void SetConnectionResetErrorEnabled(this UdpClient client, bool enabled)
        {
            byte b = enabled ? (byte)1 : (byte)0;
            client.Client.IOControl(IOControlCodes.UDPConnectionReset, new byte[] { b }, null);
        }
    }
}
