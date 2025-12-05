using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib;         // needed for DeliveryMethod
using TTRPG.Shared;
using TTRPG.Shared.Components;
using TTRPG.Shared.Enums;

namespace TTRPG.Server.Services
{
    public class GameLoopService
    {
        private readonly ServerNetworkService _network;
        // REMOVED: private readonly NetPacketProcessor _packetProcessor; <-- Caused the bug!

        private World _world;

        public GameState CurrentState { get; private set; } = GameState.Exploration;
        private float _stateTimer = 0f;
        private const float TOGGLE_TIME = 10.0f;

        public GameLoopService(ServerNetworkService network, World world)
        {
            _network = network;
            _world = world;
            // REMOVED: _packetProcessor = new NetPacketProcessor(); 

            _network.OnPlayerInput += HandlePlayerMove;
        }

        public void Update(float deltaTime)
        {
            _stateTimer += deltaTime;

            if (_stateTimer >= TOGGLE_TIME)
            {
                _stateTimer = 0f;
                ToggleState();
            }
        }

        private void ToggleState()
        {
            CurrentState = (CurrentState == GameState.Exploration)
                ? GameState.Combat
                : GameState.Exploration;

            Console.WriteLine($"[GameLoop] State Changed to: {CurrentState}");
            BroadcastState();
        }

        private void BroadcastState()
        {
            var packet = new GameStatePacket { NewState = CurrentState };

            // FIXED: Use the NetworkService to serialize and send
            _network.BroadcastPacket(packet);
        }

        private void HandlePlayerMove(Entity entity, MoveDirection direction)
        {
            if (CurrentState != GameState.Exploration)
            {
                Console.WriteLine("[GameLoop] Move rejected: In Combat.");
                return;
            }

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

                BroadcastPosition(entity.Id, pos);
            }
        }

        private void BroadcastPosition(int entityId, Position pos)
        {
            var packet = new EntityPositionPacket { EntityId = entityId, Position = pos };

            // FIXED: Use the NetworkService to serialize and send
            _network.BroadcastPacket(packet);
        }
    }
}