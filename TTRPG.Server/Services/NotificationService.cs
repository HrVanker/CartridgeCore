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
            // 1. COMMAND INTERCEPTION
            if (message.StartsWith("/"))
            {
                HandleCommand(senderPeer, message);
                return; // Do not broadcast commands to chat
            }
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
        private void HandleCommand(NetPeer senderPeer, string message)
        {
            // Parse: "/tp Zone_B" -> ["/tp", "Zone_B"]
            var parts = message.Split(' ');
            var command = parts[0].ToLower();

            var entity = _network.GetEntityForPeer(senderPeer);
            if (entity == Arch.Core.Entity.Null) return;

            switch (command)
            {
                case "/tp":
                case "/teleport":
                    if (parts.Length > 1)
                    {
                        string targetZone = parts[1];
                        TeleportEntity(entity, targetZone);
                    }
                    else
                    {
                        // Send private system message back (Optional improvement)
                        System.Console.WriteLine($"[Command] Usage: /tp Zone_ID");
                    }
                    break;

                case "/regroup":
                    // DM Tool: Move everyone to MY zone
                    RegroupParty(entity);
                    break;

                default:
                    System.Console.WriteLine($"[Command] Unknown command: {command}");
                    break;
            }
        }

        private void TeleportEntity(Arch.Core.Entity entity, string zoneId)
        {
            // We need to access the World to set the component.
            // Since we passed 'World' in the constructor, we can use it.
            if (_world.Has<Zone>(entity) && _world.Has<Position>(entity))
            {
                // 1. Update Logic
                _world.Set(entity, new Zone { Id = zoneId });
                ref var pos = ref _world.Get<Position>(entity);

                // Reset position to center of new zone so they don't spawn in a wall
                pos.X = 0;
                pos.Y = 0;

                // 2. Notify Network (Relevancy update)
                // We need to tell the GameLoop/Network that a forced move happened.
                // For Phase 5, let's just log it. The next 'Heartbeat' from GameLoop 
                // will pick up the new Zone and sync it to clients automatically!
                System.Console.WriteLine($"[DM Tool] Teleported Entity {entity.Id} to {zoneId}");

                BroadcastSystemMessage($"Player moved to {zoneId}");
            }
        }

        private void RegroupParty(Arch.Core.Entity dmEntity)
        {
            if (!_world.Has<Zone>(dmEntity)) return;

            var targetZone = _world.Get<Zone>(dmEntity).Id;
            BroadcastSystemMessage($"DM is regrouping the party to {targetZone}...");

            // Query ALL entities with a Zone component
            var query = new Arch.Core.QueryDescription().WithAll<Zone, Position>();
            _world.Query(in query, (Arch.Core.Entity e, ref Zone z, ref Position p) =>
            {
                z.Id = targetZone;
                p.X = 0;
                p.Y = 0;
            });
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