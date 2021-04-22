namespace NetFlanders
{
    public enum ConnectResult
    {
        Connected,
        HostNotFound,
        Timeout,
        Rejected,

        /// <summary>
        /// A socket error occured
        /// </summary>
        Error,
    }
}
