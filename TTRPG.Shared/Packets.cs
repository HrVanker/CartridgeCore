namespace TTRPG.Shared
{
    // Sent by Client -> Server immediately after connecting
    public class JoinRequestPacket
    {
        public string Username { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
    }

    // Sent by Server -> Client to confirm entry
    public class JoinResponsePacket
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}