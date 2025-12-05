using TTRPG.Shared.Enums;
using TTRPG.Shared.Components; // For Position struct

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

    public class GameStatePacket
    {
        public GameState NewState { get; set; }
    }

    // Client -> Server: "I want to move"
    public class PlayerMovePacket
    {
        public MoveDirection Direction { get; set; }
    }

    // Server -> Client: "Here is where you are now"
    public class EntityPositionPacket
    {
        public int EntityId { get; set; } // Arch Entity ID
        public Position Position { get; set; }
    }
}