namespace RKW.Network
{
    public enum NetworkConnectionStatus
    {
        Connected,
        Failed,
        TimedOut,
        Cancelled
    }

    public readonly struct NetworkConnectionResult
    {
        public NetworkConnectionResult(NetworkConnectionStatus status, string reason)
        {
            Status = status;
            Reason = reason ?? string.Empty;
        }

        public NetworkConnectionStatus Status { get; }

        public string Reason { get; }

        public bool IsSuccess => Status == NetworkConnectionStatus.Connected;
    }
}
