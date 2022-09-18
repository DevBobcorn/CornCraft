#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Mathematics;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft World
    /// </summary>
    public class World
    {
        /// <summary>
        /// The chunks contained into the Minecraft world
        /// </summary>
        private ConcurrentDictionary<int2, ChunkColumn> chunks = new();
        
        /// <summary>
        /// The dimension info of the world
        /// </summary>
        private static Dimension curDimension = new();

        private static Dictionary<string, Dimension> dimensionList = new();

        /// <summary>
        /// Chunk data parsing progress
        /// </summary>
        public int chunkCnt = 0;
        public int chunkLoadNotCompleted = 0;

        /// <summary>
        /// Read, set or unload the specified chunk column
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <returns>chunk at the given location</returns>
        public ChunkColumn? this[int chunkX, int chunkZ]
        {
            get
            {
                chunks.TryGetValue(new(chunkX, chunkZ), out ChunkColumn? chunkColumn);
                return chunkColumn;
            }
            set
            {
                int2 chunkCoord = new(chunkX, chunkZ);
                if (value is null)
                    chunks.TryRemove(chunkCoord, out _);
                else
                    chunks.AddOrUpdate(chunkCoord, value, (_, _) => value);
            }
        }

        // Chunk column data is sent one whole column per time,
        // a whole air chunk is represent by null
        public bool isChunkColumnReady(int chunkX, int chunkZ)
        {
            if (chunks.TryGetValue(new(chunkX, chunkZ), out ChunkColumn? chunkColumn))
                return (chunkColumn is not null && chunkColumn.FullyLoaded);
            return false;
        }

        /// <summary>
        /// Storage of all dimensional data - 1.19.1 and above
        /// </summary>
        /// <param name="registryCodec">Registry Codec nbt data</param>
        public static void StoreDimensionList(Dictionary<string, object> registryCodec)
        {
            var dimensionListNbt = (object[])(((Dictionary<string, object>)registryCodec["minecraft:dimension_type"])["value"]);
            foreach (Dictionary<string, object> dimensionNbt in dimensionListNbt)
            {
                string dimensionName = (string)dimensionNbt["name"];
                Dictionary<string, object> dimensionType = (Dictionary<string, object>)dimensionNbt["element"];
                StoreOneDimension(dimensionName, dimensionType);
            }
        }

        /// <summary>
        /// Store one dimension - Directly used in 1.16.2 to 1.18.2
        /// </summary>
        /// <param name="dimensionName">Dimension name</param>
        /// <param name="dimensionType">Dimension Type nbt data</param>
        public static void StoreOneDimension(string dimensionName, Dictionary<string, object> dimensionType)
        {
            if (dimensionList.ContainsKey(dimensionName))
                dimensionList.Remove(dimensionName);
            dimensionList.Add(dimensionName, new Dimension(dimensionName, dimensionType));
        }

        /// <summary>
        /// Set current dimension - 1.16 and above
        /// </summary>
        /// <param name="name">	The name of the dimension type</param>
        /// <param name="nbt">The dimension type (NBT Tag Compound)</param>
        public static void SetDimension(string name)
        {
            curDimension = dimensionList[name]; // Should not fail
        }

        /// <summary>
        /// Get current dimension
        /// </summary>
        /// <returns>Current dimension</returns>
        public static Dimension GetDimension()
        {
            return curDimension;
        }

        /// <summary>
        /// Set chunk column at the specified location
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkY">ChunkColumn Y</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <param name="chunkColumnSize">ChunkColumn size</param>
        /// <param name="chunk">Chunk data</param>
        public void StoreChunk(int chunkX, int chunkY, int chunkZ, int chunkColumnSize, Chunk? chunk)
        {
            ChunkColumn chunkColumn = chunks.GetOrAdd(new(chunkX, chunkZ), (_) => new(this, chunkColumnSize));
            chunkColumn[chunkY] = chunk;
        }

        /// <summary>
        /// Get chunk column at the specified location
        /// </summary>
        /// <param name="location">Location to retrieve chunk column</param>
        /// <returns>The chunk column</returns>
        public ChunkColumn? GetChunkColumn(Location location)
        {
            return this[location.ChunkX, location.ChunkZ];
        }

        public ChunkColumn? GetChunkColumn(int chunkX, int chunkZ)
        {
            return this[chunkX, chunkZ];
        }

        private static readonly Block AIR_INSTANCE = new Block(0);

        /// <summary>
        /// Get block at the specified location
        /// </summary>
        /// <param name="location">Location to retrieve block from</param>
        /// <returns>Block at specified location or Air if the location is not loaded</returns>
        public Block GetBlock(Location location)
        {
            var column = GetChunkColumn(location);
            if (column != null)
            {
                var chunk = column.GetChunk(location);
                if (chunk != null)
                    return chunk.GetBlock(location);
            }
            return AIR_INSTANCE; // Air
        }

        public int GetCullFlags(Location location, Block self, Chunk.BlockCheck check)
        {
            int cullFlags = 0;
            if (check(self, GetBlock(location.Up())))
                cullFlags |= (1 << 0);
            if (check(self, GetBlock(location.Down())))
                cullFlags |= (1 << 1);
            if (check(self, GetBlock(location.South())))
                cullFlags |= (1 << 2);
            if (check(self, GetBlock(location.North())))
                cullFlags |= (1 << 3);
            if (check(self, GetBlock(location.East())))
                cullFlags |= (1 << 4);
            if (check(self, GetBlock(location.West())))
                cullFlags |= (1 << 5);
            return cullFlags;
        }

        /// <summary>
        /// Look for a block around the specified location
        /// </summary>
        /// <param name="from">Start location</param>
        /// <param name="block">Block type</param>
        /// <param name="radius">Search radius - larger is slower: O^3 complexity</param>
        /// <returns>Block matching the specified block type</returns>
        public List<Location> FindBlock(Location from, ResourceLocation targetBlock, int radius)
        {
            return FindBlock(from, targetBlock, radius, radius, radius);
        }

        /// <summary>
        /// Look for a block around the specified location
        /// </summary>
        /// <param name="from">Start location</param>
        /// <param name="targetBlock">Block type</param>
        /// <param name="radiusx">Search radius on the X axis</param>
        /// <param name="radiusy">Search radius on the Y axis</param>
        /// <param name="radiusz">Search radius on the Z axis</param>
        /// <returns>Block matching the specified block type</returns>
        public List<Location> FindBlock(Location from, ResourceLocation targetBlock, int radiusx, int radiusy, int radiusz)
        {
            Location minPoint = new Location(from.X - radiusx, from.Y - radiusy, from.Z - radiusz);
            Location maxPoint = new Location(from.X + radiusx, from.Y + radiusy, from.Z + radiusz);
            List<Location> list = new List<Location> { };
            for (double x = minPoint.X; x <= maxPoint.X; x++)
            {
                for (double y = minPoint.Y; y <= maxPoint.Y; y++)
                {
                    for (double z = minPoint.Z; z <= maxPoint.Z; z++)
                    {
                        Location doneloc = new Location(x, y, z);
                        Block doneblock = GetBlock(doneloc);
                        ResourceLocation blockId = doneblock.BlockId;
                        if (blockId == targetBlock)
                        {
                            list.Add(doneloc);
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Set block at the specified location
        /// </summary>
        /// <param name="location">Location to set block to</param>
        /// <param name="block">Block to set</param>
        public void SetBlock(Location location, Block block)
        {
            var column = this[location.ChunkX, location.ChunkZ];
            if (column != null && column.ColumnSize >= location.ChunkY)
            {
                var chunk = column.GetChunk(location);
                if (chunk == null)
                    column[location.ChunkY] = chunk = new Chunk(this);
                chunk[location.ChunkBlockX, location.ChunkBlockY, location.ChunkBlockZ] = block;
            }

        }

        /// <summary>
        /// Clear all terrain data from the world
        /// </summary>
        public void Clear()
        {
            chunks = new();
            chunkCnt = chunkLoadNotCompleted = 0;
        }

        /// <summary>
        /// Get the location of block of the entity is looking
        /// </summary>
        /// <param name="location">Location of the entity</param>
        /// <param name="yaw">Yaw of the entity</param>
        /// <param name="pitch">Pitch of the entity</param>
        /// <returns>Location of the block or empty Location if no block was found</returns>
        public Location GetLookingBlockLocation(Location location, double yaw, double pitch)
        {
            double rotX = (Math.PI / 180) * yaw;
            double rotY = (Math.PI / 180) * pitch;
            double x = -Math.Cos(rotY) * Math.Sin(rotX);
            double y = -Math.Sin(rotY);
            double z = Math.Cos(rotY) * Math.Cos(rotX);
            Location vector = new Location(x, y, z);
            for (int i = 0; i < 5; i++)
            {
                Location newVector = vector * i;
                Location blockLocation = location.EyesLocation() + new Location(newVector.X, newVector.Y, newVector.Z);
                blockLocation.X = Math.Floor(blockLocation.X);
                blockLocation.Y = Math.Floor(blockLocation.Y);
                blockLocation.Z = Math.Floor(blockLocation.Z);
                Block b = GetBlock(blockLocation);
                if (b.BlockId != BlockState.AIR_ID)
                {
                    return blockLocation;
                }
            }
            return new Location();
        }

    }
}
