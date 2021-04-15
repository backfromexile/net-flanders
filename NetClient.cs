using System.Threading.Tasks;

namespace NetFlanders
{
    public sealed class NetClient : NetSocket
    {
        private NetPeer? _serverPeer;

        public NetClient() : base(0)
        {
        }

        public async Task<bool> ConnectAsync(string host, int port)
        {
            base
        }

        public void Send(byte[] data, byte channelId)
        {
            var writer = new NetDataWriter();
            writer.PutRaw(data);

            _serverPeer.Send(writer, channelId);
        }
    }
}