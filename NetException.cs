using System;
using System.Runtime.Serialization;

namespace NetFlanders
{
    [Serializable]
    internal class NetException : Exception
    {
        public NetException()
        {
        }

        public NetException(string message) : base(message)
        {
        }

        public NetException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NetException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}