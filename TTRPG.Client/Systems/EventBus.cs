using System;
using TTRPG.Shared.Enums;
using TTRPG.Shared.Components;

namespace TTRPG.Client.Systems
{
    public static class EventBus
    {
        // Event: Fired when the Client successfully joins the server
        // Payload: The welcome message from the server
        public static event Action<string>? OnServerJoined;

        // Method to safely invoke the event
        public static void PublishServerJoined(string message)
        {
            OnServerJoined?.Invoke(message);
        }

        public static event Action<GameState>? OnGameStateChanged;

        public static void PublishStateChanged(GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }
        public static event Action<int, Position>? OnEntityMoved;

        public static void PublishEntityMoved(int entityId, Position pos)
        {
            OnEntityMoved?.Invoke(entityId, pos);
        }
        public static event Action<int, string>? OnEntityInspected;
        public static void PublishEntityInspected(int id, string details)
        {
            OnEntityInspected?.Invoke(id, details);
        }
    }
}