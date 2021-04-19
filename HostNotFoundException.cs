using System;

namespace NetFlanders
{
    [Serializable]
    internal class HostNotFoundException : Exception
    {
        public HostNotFoundException(string host) : base($"Unable to resolve host '{host}'")
        {
        }
    }
}