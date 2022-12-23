#nullable enable
using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents an entity evolving into a Minecraft world
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// ID of the entity on the Minecraft server
        /// </summary>
        public int ID;

        /// <summary>
        /// UUID of the entity if it is a player.
        /// </summary>
        public Guid UUID;

        /// <summary>
        /// Nickname of the entity if it is a player.
        /// </summary>
        public string? Name;
        
        /// <summary>
        /// CustomName of the entity.
        /// </summary>
        public string? CustomNameJson;
        
        /// <summary>
        /// IsCustomNameVisible of the entity.
        /// </summary>
        public bool IsCustomNameVisible;

        /// <summary>
        /// CustomName of the entity.
        /// </summary>
        public string? CustomName;
        
        /// <summary>
        /// Latency of the entity if it is a player.
        /// </summary>
        public int Latency;

        /// <summary>
        /// Entity type
        /// </summary>
        public EntityType Type;

        /// <summary>
        /// Entity location in the Minecraft world
        /// </summary>
        public Location Location;

        /// <summary>
        /// Entity yaw
        /// </summary>
        public float Yaw = 0;

        /// <summary>
        /// Entity head yaw
        /// </summary>
        public float HeadYaw = 0;

        /// <summary>
        /// Entity head pitch
        /// </summary>
        public float Pitch = 0;

        /// <summary>
        /// Used in Item Frame, Falling Block and Fishing Float.
        /// See https://wiki.vg/Object_Data for details.
        /// </summary>
        /// <remarks>Untested</remarks>
        public int ObjectData = -1;

        /// <summary>
        /// Health of the entity
        /// </summary>
        public float Health;

        /// <summary>
        /// Max health of the entity
        /// </summary>
        public float MaxHealth;
        
        /// <summary>
        /// Item of the entity if ItemFrame or Item
        /// </summary>
        public ItemStack Item;
        
        /// <summary>
        /// Entity pose in the Minecraft world
        /// </summary>
        public EntityPose Pose;
        
        /// <summary>
        /// Entity metadata
        /// </summary>
        public Dictionary<int, object?>? Metadata;

        /// <summary>
        /// Entity equipment
        /// </summary>
        public Dictionary<int, ItemStack> Equipment;

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, EntityType type, Location location)
        {
            this.ID = ID;
            this.Type = type;
            this.Location = location;
            this.Health = 1F;
            this.MaxHealth = 1F;
            this.Equipment = new Dictionary<int, ItemStack>();
            this.Item = new ItemStack(Mapping.Item.AIR_ITEM, 0, null);
        }

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, EntityType type, Location location, byte yaw, byte pitch, byte headYaw, int objectData)
        {
            this.ID = ID;
            this.Type = type;
            this.Location = location;
            this.Health = 1F;
            this.MaxHealth = 1F;
            this.Equipment = new Dictionary<int, ItemStack>();
            this.Item = new ItemStack(Mapping.Item.AIR_ITEM, 0, null);
            this.Yaw = AngleConvert.MC2Unity(yaw);
            this.Pitch = AngleConvert.MC2Unity(pitch);
            this.HeadYaw = AngleConvert.MC2Unity(headYaw);
            this.ObjectData = objectData;
        }

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type, location, name and UUID
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        /// <param name="uuid">Player uuid</param>
        /// <param name="name">Player name</param>
        public Entity(int ID, EntityType type, Location location, Guid uuid, string? name)
        {
            this.ID = ID;
            this.Type = type;
            this.Location = location;
            this.UUID = uuid;
            this.Name = name;
            this.Health = 1F;
            this.MaxHealth = 1F;
            this.Equipment = new Dictionary<int, ItemStack>();
            this.Item = new ItemStack(Mapping.Item.AIR_ITEM, 0, null);
        }

        public override string ToString()
        {
            return "Entity " + ID + " (" + Type.ToString() + ")";
        }
    }
}
