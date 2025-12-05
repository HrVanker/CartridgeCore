using System;
using System.Diagnostics;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared;
using TTRPG.Shared.Components;
using TTRPG.Shared.Enums;

namespace TTRPG.Server.Services
{
    public class GameLoopService
    {
        private readonly ServerNetworkService _network;
        private readonly NetPacketProcessor _packetProcessor;
        private World _world;

        // State Management
        public GameState CurrentState { get; private set; } = GameState.Exploration;

        // Timer for testing (Toggle state every 5 seconds)
        private float _stateTimer = 0f;
        private const float TOGGLE_TIME = 10.0f;

        public GameLoopService(ServerNetworkService network, World world)
        {
            _network = network;
            _world = world;
            _packetProcessor = new NetPacketProcessor();

            // SUBSCRIBE TO INPUT
            _network.OnPlayerInput += HandlePlayerMove;
        }

        public void Update(float deltaTime) // deltaTime in seconds
        {
            // 1. Update Timer
            _stateTimer += deltaTime;

            // 2. Check for State Transition (Test Logic)
            if (_stateTimer >= TOGGLE_TIME)
            {
                _stateTimer = 0f;
                ToggleState();
            }
        }

        private void HandlePlayerMove(Entity entity, MoveDirection direction)
        {
            // 1. Validation: Can only move in Exploration Mode?
            if (CurrentState != GameState.Exploration)
            {
                Console.WriteLine("[GameLoop] Move rejected: In Combat.");
                return;
            }

            // 2. Logic: Update Position Component
            // We use 'ref' to modify the struct directly in memory (High Performance)
            if (_world.Has<Position>(entity))
            {
                ref var pos = ref _world.Get<Position>(entity);

                switch (direction)
                {
                    case MoveDirection.Up: pos.Y -= 1; break;
                    case MoveDirection.Down: pos.Y += 1; break;
                    case MoveDirection.Left: pos.X -= 1; break;
                    case MoveDirection.Right: pos.X += 1; break;
                }

                Console.WriteLine($"[GameLoop] Entity {entity.Id} moved to {pos.X}, {pos.Y}");

                // 3. Broadcast New Position
                BroadcastPosition(entity.Id, pos);
            }
        }

        private void BroadcastPosition(int entityId, Position pos)
        {
            var packet = new EntityPositionPacket { EntityId = entityId, Position = pos };
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            _network.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void ToggleState()
        {
            // Switch Enum
            CurrentState = (CurrentState == GameState.Exploration)
                ? GameState.Combat
                : GameState.Exploration;

            Console.WriteLine($"[GameLoop] State Changed to: {CurrentState}");
            BroadcastState();
        }

        private void BroadcastState()
        {
            // Create Packet
            var packet = new GameStatePacket { NewState = CurrentState };

            // Serialize
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);

            // Send to ALL connected players
            _network.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}