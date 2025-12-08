using System;
using System.Collections.Generic; // Required
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Client.Systems;
using TTRPG.Shared;
using TTRPG.Shared.Enums;
using Newtonsoft.Json;

namespace TTRPG.Client.Services
{
    public class ClientNetworkService
    {
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly NetPacketProcessor _packetProcessor;

        public ClientNetworkService()
        {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);
            _packetProcessor = new NetPacketProcessor();

            // 1. Register Position (Existing)
            _packetProcessor.RegisterNestedType<TTRPG.Shared.Components.Position>(
                (writer, pos) => { writer.Put(pos.X); writer.Put(pos.Y); },
                (reader) => new TTRPG.Shared.Components.Position { X = reader.GetInt(), Y = reader.GetInt() }
            );

            // 2. Register Dictionary (NEW FIX)
            // This teaches LiteNetLib how to send the "Stats" list
            _packetProcessor.RegisterNestedType<Dictionary<string, string>>(
                (writer, dict) =>
                {
                    writer.Put(dict.Count);
                    foreach (var kvp in dict)
                    {
                        writer.Put(kvp.Key);
                        writer.Put(kvp.Value);
                    }
                },
                (reader) =>
                {
                    int count = reader.GetInt();
                    var dict = new Dictionary<string, string>();
                    for (int i = 0; i < count; i++)
                    {
                        var key = reader.GetString();
                        var val = reader.GetString();
                        dict[key] = val;
                    }
                    return dict;
                }
            );
            _packetProcessor.RegisterNestedType<TTRPG.Core.DTOs.CharacterSheetData>(
                (writer, sheet) =>
                {
                    string json = JsonConvert.SerializeObject(sheet);
                    writer.Put(json);
                },
                (reader) =>
                {
                    string json = reader.GetString();
                    return JsonConvert.DeserializeObject<TTRPG.Core.DTOs.CharacterSheetData>(json);
                }
            );

            // Subscribe
            _packetProcessor.SubscribeReusable<SheetDataPacket>(OnSheetReceived);

            // 3. Subscriptions
            _packetProcessor.SubscribeReusable<JoinResponsePacket>(OnJoinResponse);
            _packetProcessor.SubscribeReusable<GameStatePacket>(OnGameStateReceived);
            _packetProcessor.SubscribeReusable<EntityPositionPacket>(OnPositionReceived);
            _packetProcessor.SubscribeReusable<ChatMessagePacket>(OnChatReceived);
            _packetProcessor.SubscribeReusable<EntityDetailsPacket>(OnDetailsReceived);

            _listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("[Client] We are connected! Sending Join Request...");
                SendJoinRequest(peer);
            };

            _listener.NetworkReceiveEvent += (peer, reader, deliveryMethod, channel) =>
            {
                _packetProcessor.ReadAllPackets(reader);
            };

            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine($"[Client] Disconnected: {info.Reason}");
            };
        }

        public void Connect(string ip, int port)
        {
            _client.Start();
            _client.Connect(ip, port, "TTRPG_KEY");
        }

        public void Poll()
        {
            _client.PollEvents();
        }

        public void Stop()
        {
            _client.Stop();
        }

        // --- INTERNAL HELPERS ---

        private void SendJoinRequest(NetPeer peer)
        {
            // Create the packet
            var packet = new JoinRequestPacket
            {
                Username = "Traveler", // We can make this dynamic later
                Version = "1.0.0"
            };

            // Serialize and Send
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void OnJoinResponse(JoinResponsePacket packet)
        {
            if (packet.Success)
            {
                EventBus.PublishServerJoined(packet.Message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Client] Rejected: {packet.Message}");
            }
        }
        private void OnGameStateReceived(GameStatePacket packet)
        {
            EventBus.PublishStateChanged(packet.NewState);
        }
        public void SendMove(TTRPG.Shared.Enums.MoveDirection direction)
        {
            var packet = new PlayerMovePacket { Direction = direction };
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);

            // Send ReliableOrdered to ensure moves don't get skipped/out of order
            _client.FirstPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        private void OnPositionReceived(EntityPositionPacket packet)
        {
            EventBus.PublishEntityMoved(packet.EntityId, packet.Position);
        }
        private void OnChatReceived(ChatMessagePacket packet)
        {
            // OLD: System.Diagnostics.Debug.WriteLine(...);

            // NEW: Tell the UI a message arrived
            EventBus.PublishChatReceived(packet.Sender, packet.Message);
        }
        public void SendChat(string text)
        {
            var packet = new ChatMessagePacket { Sender = "Traveler", Message = text };
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            _client.FirstPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        public void InspectEntity(int entityId)
        {
            var packet = new InspectEntityPacket { EntityId = entityId };
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            _client.FirstPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        private void OnDetailsReceived(EntityDetailsPacket packet)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in packet.Stats)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            EventBus.PublishEntityInspected(packet.EntityId, sb.ToString().TrimEnd());
        }
        private void OnSheetReceived(SheetDataPacket packet)
        {
            EventBus.PublishSheetReceived(packet.Sheet);
        }

        public void RequestCharacterSheet()
        {
            var packet = new RequestSheetPacket();
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            _client.FirstPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}