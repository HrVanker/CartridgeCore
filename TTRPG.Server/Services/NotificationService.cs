using Arch.Core;
using LiteNetLib;
using TTRPG.Shared;
using TTRPG.Shared.Components;

namespace TTRPG.Server.Services
{
    public class NotificationService
    {
        private readonly ServerNetworkService _network;
        private readonly ServerConfig _config;
        private readonly World _world;

        public NotificationService(ServerNetworkService network, World world)
        {
            _network = network;
            _world = world;

            // For Phase 4, we hardcode this. 
            // Phase 5 (Launcher) would load this from a file.
            _config = new ServerConfig { IsCasualMode = true };
        }

        public void ToggleMode()
        {
            _config.IsCasualMode = !_config.IsCasualMode;
            var modeName = _config.IsCasualMode ? "CASUAL (Global Chat)" : "HARDCORE (Proximity Only)";

            // FIX: Print to Server Console
            System.Console.WriteLine($"[Config] {modeName}");

            BroadcastSystemMessage($"Server mode changed to: {modeName}");
        }

        public void SendChatMessage(NetPeer senderPeer, string message)
        {
            // 1. Identification
            string senderName = $"Player {senderPeer.Id}";
            var senderEntity = _network.GetEntityForPeer(senderPeer);

            // 2. Logic Selection
            if (_config.IsCasualMode)
            {
                // CASUAL: Broadcast to everyone regardless of location
                BroadcastGlobal(senderName, message);
            }
            else
            {
                // HARDCORE: Only send to people in the same Zone
                if (_world.Has<Zone>(senderEntity))
                {
                    var zone = _world.Get<Zone>(senderEntity);
                    BroadcastLocal(senderName, message, zone.Id);
                }
            }
        }
        public void BroadcastZoneEvent(string zoneId, string message)
        {
            // Prefix the message so we know where it came from
            string formattedMessage = $"[{zoneId}] {message}";

            if (_config.IsCasualMode)
            {
                // CASUAL: Tell everyone ("Player X started combat in Zone A!")
                BroadcastGlobal("[SYSTEM]", formattedMessage);
            }
            else
            {
                // HARDCORE: Only tell people inside that zone
                BroadcastLocal("[SYSTEM]", formattedMessage, zoneId);
            }
        }

        private void BroadcastGlobal(string sender, string message)
        {
            var packet = new ChatMessagePacket { Sender = sender, Message = message };
            _network.BroadcastPacket(packet);
        }

        private void BroadcastLocal(string sender, string message, string zoneId)
        {
            var packet = new ChatMessagePacket { Sender = sender, Message = message };
            _network.BroadcastToZone(zoneId, packet);
        }

        // Helper for System Alerts (e.g. "Combat Started")
        public void BroadcastSystemMessage(string message)
        {
            var packet = new ChatMessagePacket { Sender = "[SYSTEM]", Message = message };
            _network.BroadcastPacket(packet);
        }
    }
}