using System;
using TTRPG.Core.DTOs;
using TTRPG.Shared.Components;
using TTRPG.Shared.Enums;

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
        public static event Action<int, Position, string>? OnEntityMoved; // Added string spriteId

        public static void PublishEntityMoved(int id, Position pos, string spriteId)
        {
            OnEntityMoved?.Invoke(id, pos, spriteId);
        }
        public static event Action<int, string>? OnEntityInspected;
        public static void PublishEntityInspected(int id, string details)
        {
            OnEntityInspected?.Invoke(id, details);
        }
        //Chat Events
        public static event Action<string, string>? OnChatReceived;

        public static void PublishChatReceived(string sender, string message)
        {
            OnChatReceived?.Invoke(sender, message);
        }
        public static event Action<CharacterSheetData>? OnSheetReceived;

        public static void PublishSheetReceived(CharacterSheetData sheet)
        {
            OnSheetReceived?.Invoke(sheet);
        }
    }
}