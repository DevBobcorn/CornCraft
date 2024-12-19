#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CraftSharp.Protocol.Handlers
{
    /// <summary>
    /// Terrain Decoding handler for MC 1.15+
    /// </summary>
    class ProtocolTerrain
    {
        private readonly int protocolVersion;
        private readonly DataTypes dataTypes;
        private readonly IMinecraftComHandler handler;

        /// <summary>
        /// Initialize a new Terrain Decoder
        /// </summary>
        /// <param name="protocolVersion">Minecraft Protocol Version</param>
        /// <param name="dataTypes">Minecraft Protocol Data Types</param>
        public ProtocolTerrain(int protocolVersion, DataTypes dataTypes, IMinecraftComHandler handler)
        {
            this.protocolVersion = protocolVersion;
            this.dataTypes = dataTypes;
            this.handler = handler;
        }

        /// <summary>
        /// Reading the "Block states" field: consists of 4096 entries, representing all the blocks in the chunk section.
        /// See https://wiki.vg/Chunk_Format#Data_structure
        /// </summary>
        /// <param name="chunk">Blocks will store in this chunk</param>
        /// <param name="cache">Cache for reading data</param>
        private Chunk? ReadBlockStatesField(Queue<byte> cache)
        {
            // read Block states (Type: Paletted Container)
            byte bitsPerEntry = DataTypes.ReadNextByte(cache);

            // 1.18(1.18.1) add a palette named "Single valued" to replace the vertical strip bitmask in the old
            if (bitsPerEntry == 0 && protocolVersion >= ProtocolMinecraft.MC_1_18_1_Version)
            {
                // Palettes: Single valued - 1.18(1.18.1) and above
                ushort blockId = (ushort)DataTypes.ReadNextVarInt(cache);

                DataTypes.SkipNextVarInt(cache); // Data Array Length will be zero

                // Empty chunks will not be stored
                if (blockId == 0)
                    return null;
                
                Chunk chunk = new();
                for (int blockY = 0; blockY < Chunk.SIZE; blockY++)
                    for (int blockZ = 0; blockZ < Chunk.SIZE; blockZ++)
                        for (int blockX = 0; blockX < Chunk.SIZE; blockX++)
                            chunk.SetWithoutCheck(blockX, blockY, blockZ, new(blockId));

                return chunk;
            }
            else
            {
                // Palettes: Indirect or Direct
                bool usePalette = (bitsPerEntry <= 8);

                // Indirect Mode: For block states with bits per entry <= 4, 4 bits are used to represent a block.
                if (bitsPerEntry < 4) bitsPerEntry = 4;

                //int entryPerLong = 64 / bitsPerEntry; // entryPerLong = sizeof(long) / bitsPerEntry

                // Direct Mode: Bit mask covering bitsPerEntry bits
                // EG, if bitsPerEntry = 5, valueMask = 00011111 in binary
                uint valueMask = (uint)((1 << bitsPerEntry) - 1);

                int paletteLength = 0; // Assume zero when length is absent
                if (usePalette) paletteLength = DataTypes.ReadNextVarInt(cache);

                Span<uint> palette = paletteLength < 256 ? stackalloc uint[paletteLength] : new uint[paletteLength];
                for (int i = 0; i < paletteLength; i++)
                    palette[i] = (uint)DataTypes.ReadNextVarInt(cache);

                //// Block IDs are packed in the array of 64-bits integers
                DataTypes.SkipNextVarInt(cache); // Entry length
                Span<byte> entryDataByte = stackalloc byte[8];
                Span<long> entryDataLong = MemoryMarshal.Cast<byte, long>(entryDataByte); // Faster than MemoryMarshal.Read<long>

                Chunk chunk = new();
                int startOffset = 64; // Read the first data immediately
                for (int blockY = 0; blockY < Chunk.SIZE; blockY++)
                    for (int blockZ = 0; blockZ < Chunk.SIZE; blockZ++)
                        for (int blockX = 0; blockX < Chunk.SIZE; blockX++)
                        {
                            // Calculate location of next block ID inside the array of Longs
                            if ((startOffset += bitsPerEntry) > (64 - bitsPerEntry))
                            {
                                // In MC 1.16+, padding is applied to prevent overlapping between Longs:
                                // [     LONG INTEGER     ][     LONG INTEGER     ]
                                // [Block][Block][Block]XXX[Block][Block][Block]XXX

                                // When overlapping, move forward to the beginning of the next Long
                                startOffset = 0;
                                DataTypes.ReadDataReverse(cache, entryDataByte); // read long
                            }

                            uint blockId = (uint)(entryDataLong[0] >> startOffset) & valueMask;

                            // Map small IDs to actual larger block IDs
                            if (usePalette)
                            {
                                if (paletteLength <= blockId)
                                {
                                    int blockNumber = (blockY * Chunk.SIZE + blockZ) * Chunk.SIZE + blockX;
                                    throw new IndexOutOfRangeException(String.Format("Block ID {0} is outside Palette range 0-{1}! (bitsPerBlock: {2}, blockNumber: {3})",
                                        blockId,
                                        paletteLength - 1,
                                        bitsPerEntry,
                                        blockNumber));
                                }

                                blockId = palette[(int)blockId];
                            }

                            // NOTICE: In the future a single ushort may not store the entire block id;
                            // the Block class may need to change if block state IDs go beyond 65535
                            Block block = new((ushort)blockId);

                            // We have our block, save the block into the chunk
                            chunk.SetWithoutCheck(blockX, blockY, blockZ, block);
                        }
                
                return chunk;
            }
        }

        /// <summary>
        /// Reading the "Biomes" field: consists of 64 entries, representing all the biomes in the chunk section.
        /// See https://wiki.vg/Chunk_Format#Data_structure
        /// </summary>
        private void ReadBiomesField(int chunkY, short[] biomes, Queue<byte> cache)
        {
            // Vertical offset of a 'cell' which is 4*4*4
            int cellYOffset = chunkY << 2;

            byte bitsPerEntry = DataTypes.ReadNextByte(cache); // Bits Per Entry

            // Direct Mode: Bit mask covering bitsPerEntry bits
            // EG, if bitsPerEntry = 5, valueMask = 00011111 in binary
            uint valueMask = (uint)((1 << bitsPerEntry) - 1);

            if (bitsPerEntry == 0) // Single valued
            {
                short biomeId = (short) DataTypes.ReadNextVarInt(cache); // Value
                DataTypes.SkipNextVarInt(cache); // Data Array Length
                // Data Array must be empty

                // Fill the whole section with this biome
                Array.Fill(biomes, biomeId, cellYOffset << 4, 64);
            }
            else // Indirect
            {
                if (bitsPerEntry <= 3) // For biomes the given value is always used, and will be <= 3
                {
                    int paletteLength = DataTypes.ReadNextVarInt(cache); // Palette Length

                    Span<uint> palette = paletteLength < 256 ? stackalloc uint[paletteLength] : new uint[paletteLength];
                    for (int i = 0; i < paletteLength; i++)
                        palette[i] = (uint)DataTypes.ReadNextVarInt(cache); // Palette

                    //// Biome IDs are packed in the array of 64-bits integers
                    int dataArrayLength = DataTypes.ReadNextVarInt(cache); // Data Array Length

                    //DataTypes.DropData(dataArrayLength * 8, cache); // Data Array
                    //UnityEngine.Debug.Log($"Biome data length: {dataArrayLength}");

                    Span<byte> entryDataByte = stackalloc byte[8];
                    Span<long> entryDataLong = MemoryMarshal.Cast<byte, long>(entryDataByte); // Faster than MemoryMarshal.Read<long>

                    int startOffset = 64; // Read the first data immediately
                    for (int cellY = 0; cellY < 4; cellY++)
                        for (int cellZ = 0; cellZ < 4; cellZ++)
                            for (int cellX = 0; cellX < 4; cellX++) // Each 'cell' here means a 4*4*4 area
                            {
                                // Calculate location of next block ID inside the array of Longs
                                if ((startOffset += bitsPerEntry) > (64 - bitsPerEntry))
                                {
                                    // In MC 1.16+, padding is applied to prevent overlapping between Longs:
                                    // [     LONG INTEGER     ][     LONG INTEGER     ]
                                    // [Biome][Biome][Biome]XXX[Biome][Biome][Biome]XXX

                                    // When overlapping, move forward to the beginning of the next Long
                                    startOffset = 0;
                                    DataTypes.ReadDataReverse(cache, entryDataByte); // read long
                                }

                                uint biomeId = (uint)(entryDataLong[0] >> startOffset) & valueMask;

                                // Map small IDs to actual larger biome IDs
                                if (paletteLength <= biomeId)
                                {
                                    int cellIndex = (cellY * 4 + cellZ) * 4 + cellX;
                                    throw new IndexOutOfRangeException(String.Format("Biome ID {0} is outside Palette range 0-{1}! (bitsPerEntry: {2}, cellIndex: {3})",
                                        biomeId,
                                        paletteLength - 1,
                                        bitsPerEntry,
                                        cellIndex));
                                }

                                biomeId = palette[(int)biomeId];

                                // Set it in biome array
                                biomes[((cellY + cellYOffset) << 4) | (cellZ << 2) | cellX] = (short) biomeId;
                                
                            }
                    
                }
                else
                    UnityEngine.Debug.LogWarning($"Bits per biome entry not valid: {bitsPerEntry}");

            }

        }

        /// <summary>
        /// Process chunk column data from the server and (un)load the chunk from the Minecraft world - 1.17 and above
        /// </summary>
        /// <param name="chunkX">Chunk X location</param>
        /// <param name="chunkZ">Chunk Z location</param>
        /// <param name="verticalStripBitmask">Chunk mask for reading data, store in bitset, used in 1.17 and 1.17.1</param>
        /// <param name="cache">Cache for reading chunk data</param>
        /// <returns>true if successfully loaded</returns>
        public bool ProcessChunkColumnData17(int chunkX, int chunkZ, ulong[]? verticalStripBitmask, Queue<byte> cache)
        {
            var chunksManager = handler.GetChunkRenderManager();

            // Biome data of this whole chunk column
            short[]? biomes = null;

            int chunkColumnSize = (World.GetDimensionType().height + Chunk.SIZE - 1) / Chunk.SIZE; // Round up
            int chunkMask = 0;

            int dataSize;

            if (protocolVersion >= ProtocolMinecraft.MC_1_18_1_Version) // 1.18, 1.18.1 and above
            {
                dataSize = DataTypes.ReadNextVarInt(cache); // Size

                // Prepare an empty array and do nothing else here
                biomes = new short[64 * chunkColumnSize];
            }
            else // 1.17 and 1.17.1, read biome data right here
            {
                int biomesLength = DataTypes.ReadNextVarInt(cache); // Biomes length
                biomes = new short[biomesLength];

                // Read all biome data at once before other chunk data
                for (int i = 0; i < biomesLength; i++)
                    biomes[i] = (short) DataTypes.ReadNextVarInt(cache); // Biomes
                
                dataSize = DataTypes.ReadNextVarInt(cache); // Size
            }

            //var aaa = DataTypes.ReadData(dataSize, cache);

            int totalSize = cache.Count;

            // 1.17 and above chunk format
            // Unloading chunks is handled by a separate packet
            for (int chunkY = 0; chunkY < chunkColumnSize; chunkY++)
            {
                // 1.18 and above always contains all chunk section in data
                // 1.17 and 1.17.1 need vertical strip bitmask to know if the chunk section is included
                if ((protocolVersion >= ProtocolMinecraft.MC_1_18_1_Version) ||
                    ((verticalStripBitmask![chunkY / 64] & (1UL << (chunkY % 64))) != 0))
                {
                    // Non-air block count inside chunk section, for lighting purposes
                    int blockCount = DataTypes.ReadNextShort(cache);
                    
                    // Read Block states (Type: Paletted Container)
                    var chunk = ReadBlockStatesField(cache);
                    
                    if (chunk is not null) // Chunk not empty(air)
                        chunkMask |= 1 << chunkY;

                    // We have our chunk, save the chunk into the world
                    chunksManager.StoreChunk(chunkX, chunkY, chunkZ, chunkColumnSize, chunk);
                }

                // Read Biomes (Type: Paletted Container) - 1.18(1.18.1) and above
                if (protocolVersion >= ProtocolMinecraft.MC_1_18_1_Version)
                    ReadBiomesField(chunkY, biomes!, cache);
            }

            if (chunkMask == 0) // The whole chunk column is empty (chunks around main island in the end, for example)
            {
                chunksManager.CreateEmptyChunkColumn(chunkX, chunkZ, chunkColumnSize);
            }

            int consumedSize = totalSize - cache.Count;
            int error = dataSize - consumedSize;

            //UnityEngine.Debug.Log($"Data size: {dataSize} Consumed size: {consumedSize} Bytes left: {cache.Count} Error: {error}");

            if (error > 0) // Error correction
                DataTypes.ReadData(error, cache);

            // Read block entity data
            int blockEntityCount = DataTypes.ReadNextVarInt(cache);
            if (blockEntityCount > 0)
            {
                for (int i = 0; i < blockEntityCount; i++) {
                    var packedXZ = DataTypes.ReadNextByte(cache);
                    var y = DataTypes.ReadNextShort(cache);
                    var ttt = DataTypes.ReadNextVarInt(cache);
                    var tag = DataTypes.ReadNextNbt(cache, dataTypes.UseAnonymousNBT);
                    int x = (chunkX << 4) + (packedXZ >> 4);
                    int z = (chunkZ << 4) + (packedXZ & 15);
                    // Output block entity data
                    var blockLoc = new BlockLoc(x, y, z);

                    var type = BlockEntityTypePalette.INSTANCE.GetByNumId(ttt);
                    //UnityEngine.Debug.Log($"Chunk17 [{blockLoc}] {Json.Object2Json(tag)}");
                    Loom.QueueOnMainThread(() => {
                        chunksManager.AddBlockEntityRender(blockLoc, type, tag);
                    });
                }
            }
            
            // Parse lighting data
            var skyLight   = new byte[4096 * (chunkColumnSize + 2)];
            var blockLight = new byte[4096 * (chunkColumnSize + 2)];

            ReadChunkColumnLightData17(ref skyLight, ref blockLight, cache);

            // All data in packet should be parsed now, with nothing left

            // Set the column's chunk mask and load state
            var c = chunksManager.GetChunkColumn(chunkX, chunkZ);
            if (c is not null)
            {
                if (biomes!.Length == c.ColumnSize * 64)
                    c.SetBiomeIds(biomes);
                else if (biomes.Length > 0)
                    UnityEngine.Debug.Log($"Unexpected biome length: {biomes.Length}, should be {c.ColumnSize * 64}");
                
                c.SetLights(skyLight, blockLight);
                c.InitializeBlockLightCache();

                c!.FullyLoaded = true;
            }
            return true;
        }

        /// <summary>
        /// Process chunk column data from the server and (un)load the chunk from the Minecraft world - 1.16
        /// </summary>
        /// <param name="chunkX">Chunk X location</param>
        /// <param name="chunkZ">Chunk Z location</param>
        /// <param name="chunkMask">Chunk mask for reading data</param>
        /// <param name="chunksContinuous">Are the chunk continuous</param>
        /// <param name="cache">Cache for reading chunk data</param>
        /// <returns>true if successfully loaded</returns>
        public bool ProcessChunkColumnData16(int chunkX, int chunkZ, ushort chunkMask, bool chunksContinuous, Queue<byte> cache)
        {
            var chunksManager = handler.GetChunkRenderManager();

            int biomesLength = 0;
                                
            if (chunksContinuous)
                biomesLength = DataTypes.ReadNextVarInt(cache); // Biomes length - 1.16.2 and above
            
            short[] biomes = new short[biomesLength];

            if (chunksContinuous) // 1.15 and above
            {
                for (int i = 0; i < biomesLength; i++) // Biomes - 1.16.2 and above
                    biomes[i] = (short) DataTypes.ReadNextVarInt(cache);
            }

            int dataSize = DataTypes.ReadNextVarInt(cache);

            const int chunkColumnSize = 16;

            if (chunkMask == 0) // The whole chunk column is empty (chunks around main island in the end, for example)
            {
                chunksManager.CreateEmptyChunkColumn(chunkX, chunkZ, chunkColumnSize);
            }
            else
            {
                // 1.9 and above chunk format
                // Unloading chunks is handled by a separate packet
                for (int chunkY = 0; chunkY < chunkColumnSize; chunkY++)
                {
                    if ((chunkMask & (1 << chunkY)) != 0)
                    {
                        // 1.14 and above Non-air block count inside chunk section, for lighting purposes
                        DataTypes.ReadNextShort(cache);

                        byte bitsPerBlock = DataTypes.ReadNextByte(cache);
                        bool usePalette = (bitsPerBlock <= 8);

                        // Vanilla Minecraft will use at least 4 bits per block
                        if (bitsPerBlock < 4)
                            bitsPerBlock = 4;

                        // MC 1.9 to 1.12 will set palette length field to 0 when palette
                        // is not used, MC 1.13+ does not send the field at all in this case
                        int paletteLength = 0; // Assume zero when length is absent
                        if (usePalette)
                            paletteLength = DataTypes.ReadNextVarInt(cache);

                        int[] palette = new int[paletteLength];
                        for (int i = 0; i < paletteLength; i++)
                            palette[i] = DataTypes.ReadNextVarInt(cache);

                        // Bit mask covering bitsPerBlock bits
                        // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                        uint valueMask = (uint)((1 << bitsPerBlock) - 1);

                        // Block IDs are packed in the array of 64-bits integers
                        ulong[] dataArray = DataTypes.ReadNextULongArray(cache);

                        Chunk chunk = new();

                        if (dataArray.Length > 0)
                        {
                            int longIndex = 0;
                            int startOffset = 0 - bitsPerBlock;

                            for (int blockY = 0; blockY < Chunk.SIZE; blockY++)
                            {
                                for (int blockZ = 0; blockZ < Chunk.SIZE; blockZ++)
                                {
                                    for (int blockX = 0; blockX < Chunk.SIZE; blockX++)
                                    {
                                        // NOTICE: In the future a single ushort may not store the entire block id;
                                        // the Block class may need to change if block state IDs go beyond 65535
                                        ushort blockId;

                                        // Calculate location of next block ID inside the array of Longs
                                        startOffset += bitsPerBlock;
                                        bool overlap = false;

                                        if ((startOffset + bitsPerBlock) > 64)
                                        {
                                            // In MC 1.16+, padding is applied to prevent overlapping between Longs:
                                            // [      LONG INTEGER      ][      LONG INTEGER      ]
                                            // [Block][Block][Block]XXXXX[Block][Block][Block]XXXXX

                                            // When overlapping, move forward to the beginning of the next Long
                                            startOffset = 0;
                                            longIndex++;
                                        }

                                        // Extract Block ID
                                        if (overlap)
                                        {
                                            int endOffset = 64 - startOffset;
                                            blockId = (ushort)((dataArray[longIndex] >> startOffset | dataArray[longIndex + 1] << endOffset) & valueMask);
                                        }
                                        else
                                            blockId = (ushort)((dataArray[longIndex] >> startOffset) & valueMask);
                                        
                                        // Map small IDs to actual larger block IDs
                                        if (usePalette)
                                        {
                                            if (paletteLength <= blockId)
                                            {
                                                int blockNumber = (blockY * Chunk.SIZE + blockZ) * Chunk.SIZE + blockX;
                                                throw new IndexOutOfRangeException(String.Format("Block ID {0} is outside Palette range 0-{1}! (bitsPerBlock: {2}, blockNumber: {3})",
                                                    blockId,
                                                    paletteLength - 1,
                                                    bitsPerBlock,
                                                    blockNumber));
                                            }

                                            blockId = (ushort)palette[blockId];
                                        }

                                        // We have our block, save the block into the chunk
                                        chunk[blockX, blockY, blockZ] = new Block(blockId);

                                    }
                                }
                            }
                        }

                        // We have our chunk, save the chunk into the world
                        chunksManager.StoreChunk(chunkX, chunkY, chunkZ, chunkColumnSize, chunk);
                    }
                }
            }

            // Read block entity data
            int blockEntityCount = DataTypes.ReadNextVarInt(cache);
            if (blockEntityCount > 0)
            {
                for (int i = 0; i < blockEntityCount; i++) {
                    var tag = DataTypes.ReadNextNbt(cache, dataTypes.UseAnonymousNBT);
                    // Output block entity data
                    var blockLoc = new BlockLoc((int) tag["x"], (int) tag["y"], (int) tag["z"]);
                    var typeId = ResourceLocation.FromString((string) tag["id"]);
                    var type = BlockEntityTypePalette.INSTANCE.GetById(typeId);
                    //UnityEngine.Debug.Log($"Chunk16 [{blockLoc}] {Json.Object2Json(tag)}");
                    Loom.QueueOnMainThread(() => {
                        chunksManager.AddBlockEntityRender(blockLoc, type, tag);
                    });
                }
            }

            // All data in packet should be parsed now, with nothing left

            // Set the column's chunk mask and load state
            var c = chunksManager.GetChunkColumn(chunkX, chunkZ);
            if (c is not null) // Receive and store biome and light data, these should be present even for empty chunk columns
            {
                if (biomes.Length == c.ColumnSize * 64)
                    c.SetBiomeIds(biomes);
                else if (biomes.Length > 0)
                    UnityEngine.Debug.Log($"Unexpected biome length: {biomes.Length}, should be {c.ColumnSize * 64}");

                // Check if light data for this chunk column is present
                int2 chunkKey = new(chunkX, chunkZ);

                if (chunksManager.GetLightingCache().TryRemove(chunkKey, out Queue<byte>? lightData))
                {
                    // Parse lighting data
                    var skyLight   = new byte[4096 * (chunkColumnSize + 2)];
                    var blockLight = new byte[4096 * (chunkColumnSize + 2)];

                    ReadChunkColumnLightData16(ref skyLight, ref blockLight, lightData);

                    c.SetLights(skyLight, blockLight);
                    c.InitializeBlockLightCache();

                    //UnityEngine.Debug.Log($"Lighting up chunk column [{chunkX}, {chunkZ}]");
                }

                c!.FullyLoaded = true;
            }
            return true;
        }

        /// <summary>
        /// Read chunk column light data from the server - 1.17 and above.
        /// The returned indices is from one section below bottom to one section above top
        /// </summary>
        /// <returns>Indicies of chunk sections whose light data is included</returns>
        public int[] ReadChunkColumnLightData17(ref byte[] skyLight, ref byte[] blockLight, Queue<byte> cache)
        {
            var trustEdges = DataTypes.ReadNextBool(cache);

            // Sky Light Mask
            var skyLightMask = DataTypes.ReadNextULongArray(cache);

            // Block Light Mask
            var blockLightMask = DataTypes.ReadNextULongArray(cache);

            // Empty Sky Light Mask
            var emptySkyLightMask = DataTypes.ReadNextULongArray(cache);

            // Empty Block Light Mask
            var emptyBlockLightMask = DataTypes.ReadNextULongArray(cache);

            int ptr = 0, ulLen = sizeof(ulong) << 3;

            // Sky Light Arrays
            int skyLightArrayCount = DataTypes.ReadNextVarInt(cache);
            var skyLightIndices = new int[skyLightArrayCount];

            for (int li = 0;li < skyLightMask.Length;li++)
                for (int bit = 0;bit < ulLen;bit++)
                {
                    if ((skyLightMask[li] & (1UL << bit)) != 0)
                        skyLightIndices[ptr++] = li * ulLen + bit;
                }
            
            if (ptr != skyLightArrayCount)
                UnityEngine.Debug.Log($"Sky light data mismatch with data: {skyLightArrayCount} {ptr} {skyLightMask[0]}");

            for (int i = 0;i < skyLightArrayCount;i++)
            {
                var skyLightArray = DataTypes.ReadNextByteArray(cache);
                var chunkBlockY = skyLightIndices[i] * 16;

                ReadLightArray(skyLightArray, chunkBlockY, ref skyLight);
            }

            if (protocolVersion >= ProtocolMinecraft.MC_1_20_Version)
            {
                // TODO: Fix and implement
                return new int[0];
            }
            
            // Block Light Arrays
            int blockLightArrayCount = DataTypes.ReadNextVarInt(cache);
            var blockLightIndices = new int[blockLightArrayCount];

            ptr = 0;

            for (int li = 0;li < blockLightMask.Length;li++)
                for (int bit = 0;bit < ulLen;bit++)
                {
                    if ((blockLightMask[li] & (1UL << bit)) != 0)
                        blockLightIndices[ptr++] = li * ulLen + bit;
                }

            for (int i = 0;i < blockLightArrayCount;i++)
            {
                var blockLightArray = DataTypes.ReadNextByteArray(cache);
                var chunkBlockY = blockLightIndices[i] * 16;

                ReadLightArray(blockLightArray, chunkBlockY, ref blockLight);
            }

            return skyLightIndices.Union(blockLightIndices).Distinct().ToArray();
        }

        /// <summary>
        /// Read chunk column light data from the server - 1.16.
        /// The returned indices is from one section below bottom to one section above top
        /// </summary>
        /// <returns>Indicies of chunk sections whose light data is included</returns>
        public int[] ReadChunkColumnLightData16(ref byte[] skyLight, ref byte[] blockLight, Queue<byte> cache)
        {
            var trustEdges = DataTypes.ReadNextBool(cache);
            
            // Sky Light Mask
            var skyLightMask = DataTypes.ReadNextVarInt(cache);

            // Block Light Mask
            var blockLightMask = DataTypes.ReadNextVarInt(cache);

            // Empty Sky Light Mask
            var emptySkyLightMask = DataTypes.ReadNextVarInt(cache);

            // Empty Block Light Mask
            var emptyBlockLightMask = DataTypes.ReadNextVarInt(cache);

            var updatedSections = new HashSet<int>();

            // Sky light arrays
            for (int i = 0;i < 18;i++) //  // From one chunk below bottom to one chunk above top, 18 chunks in a column
            {
                if ((skyLightMask & (1 << i)) == 0)
                    continue; // Skip
                
                updatedSections.Add(i);

                var skyLightArray = DataTypes.ReadNextByteArray(cache);
                var chunkBlockY = i * 16;

                ReadLightArray(skyLightArray, chunkBlockY, ref skyLight);
            }
            
            // Block light arrays
            for (int i = 0;i < 18;i++) // From one chunk below bottom to one chunk above top, 18 chunks in a column
            {
                if ((blockLightMask & (1 << i)) == 0)
                    continue; // Skip
                
                updatedSections.Add(i);

                var blockLightArray = DataTypes.ReadNextByteArray(cache);
                var chunkBlockY = i * 16;

                ReadLightArray(blockLightArray, chunkBlockY, ref blockLight);
            }

            return updatedSections.ToArray();
        }

        private void ReadLightArray(byte[] srcArray, int chunkBlockY, ref byte[] light)
        {
            // 3 bits for x, 4 bits for z
            for (int halfX = 0;halfX < 8;halfX++)
                for (int z = 0;z < 16;z++)
                    for (int y = 0;y < 16;y++)
                    {
                        int srcIndex = (y << 7) + (z << 3) + halfX;
                        int dstIndex = ((y + chunkBlockY) << 8) + (z << 4) + (halfX << 1);

                        // Low bits => even x indices
                        light[dstIndex] = (byte) (srcArray[srcIndex] & 0xF);

                        // High bits => odd x indices
                        light[dstIndex + 1] = (byte) (srcArray[srcIndex] >> 4);
                    }
        }

        /// <summary>
        /// Process chunk column light data from the server
        /// </summary>
        public void ProcessChunkLightData(int chunkX, int chunkZ, Queue<byte> cache)
        {
            var chunksManager = handler.GetChunkRenderManager();
            var chunkColumn = chunksManager.GetChunkColumn(chunkX, chunkZ);

            if (chunkColumn is null)
            {
                // Save light data for later use (when the chunk data is ready)
                chunksManager.GetLightingCache().AddOrUpdate(new(chunkX, chunkZ), (_) => cache, (_, _) => cache);
            }
            else
            {
                int chunkColumnSize = (World.GetDimensionType().height + Chunk.SIZE - 1) / Chunk.SIZE; // Round up

                var skyLight   = new byte[4096 * (chunkColumnSize + 2)];
                var blockLight = new byte[4096 * (chunkColumnSize + 2)];

                int[] updatedSections;
                
                if (protocolVersion >= ProtocolMinecraft.MC_1_17_Version)
                    updatedSections = ReadChunkColumnLightData17(ref skyLight, ref blockLight, cache);
                else
                    updatedSections = ReadChunkColumnLightData16(ref skyLight, ref blockLight, cache);
                
                chunkColumn.SetLights(skyLight, blockLight);

                // TODO: Figure out when these will be triggered and when they will not
                Loom.QueueOnMainThread(() => {
                    for (int i = 0; i < updatedSections.Length; i++)
                    {
                        int chunkY = updatedSections[i] - 1;
                        chunksManager.QueueChunkBuildAfterLightUpdate(chunkX, chunkY, chunkZ);
                    }
                });
            }
        }
    }
}
