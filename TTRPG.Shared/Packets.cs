using TTRPG.Shared.Components; // For Position struct
using TTRPG.Shared.Enums;
using TTRPG.Core.DTOs;

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
    public class ChatMessagePacket
    {
        public string Sender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
    // Client -> Server: "What is this thing?"
    public class InspectEntityPacket
    {
        public int EntityId { get; set; }
    }

    // Server -> Client: "Here are the stats."
    public class EntityDetailsPacket
    {
        public int EntityId { get; set; }

        // OLD: public string Details { get; set; }
        // NEW: Structured Data
        public Dictionary<string, string> Stats { get; set; } = new Dictionary<string, string>();
    }
    // Client -> Server: "Open my character sheet"
    public class RequestSheetPacket
    {
    }

    // Server -> Client: "Here is your data"
    public class SheetDataPacket
    {
        public CharacterSheetData Sheet { get; set; } = new CharacterSheetData();
    }
}