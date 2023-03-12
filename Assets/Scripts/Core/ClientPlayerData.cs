#nullable enable
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

namespace MinecraftClient
{
    public class ClientPlayerData
    {
        public Perspective Perspective = 0;
        public GameMode GameMode = 0;

        public Location Location;
        public float? _yaw; // Used for calculation ONLY!!! Doesn't reflect the client yaw
        public float? _pitch; // Used for calculation ONLY!!! Doesn't reflect the client pitch
        public float Yaw;
        public float Pitch;
        public bool Grounded;

        public int SequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)

        public int EntityID;
        public float CurHealth, MaxHealth;
        public int FoodSaturation;
        public int Level;
        public int TotalExperience;
        public Dictionary<int, Container> Inventories = new();
        public byte CurrentSlot = 0;

        public Container? GetInventory(int inventoryID)
        {
            if (Inventories.ContainsKey(inventoryID))
                return Inventories[inventoryID];
            return null;
        }

    }
}