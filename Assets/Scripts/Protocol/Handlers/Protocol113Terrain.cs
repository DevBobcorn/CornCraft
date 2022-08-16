using System;
using System.Collections.Generic;

using MinecraftClient.Event;
using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Terrain Decoding handler for Protocol18
    /// </summary>
    class Protocol113Terrain
    {
        private int protocolversion;
        private DataTypes dataTypes;
        private IMinecraftComHandler handler;

        /// <summary>
        /// Initialize a new Terrain Decoder
        /// </summary>
        /// <param name="protocolVersion">Minecraft Protocol Version</param>
        /// <param name="dataTypes">Minecraft Protocol Data Types</param>
        public Protocol113Terrain(int protocolVersion, DataTypes dataTypes, IMinecraftComHandler handler)
        {
            this.protocolversion = protocolVersion;
            this.dataTypes = dataTypes;
            this.handler = handler;
        }

        /// <summary>
        /// Process chunk column data from the server and (un)load the chunk from the Minecraft world
        /// </summary>
        /// <param name="chunkX">Chunk X location</param>
        /// <param name="chunkZ">Chunk Z location</param>
        /// <param name="chunkMask">Chunk mask for reading data</param>
        /// <param name="chunksContinuous">Are the chunk continuous</param>
        /// <param name="currentDimension">Current dimension type (0 = overworld)</param>
        /// <param name="cache">Cache for reading chunk data</param>
        public void ProcessChunkColumnData(int chunkX, int chunkZ, ushort chunkMask, bool chunksContinuous, int currentDimension, Queue<byte> cache)
        {
            // 1.9 and above chunk format
            // Unloading chunks is handled by a separate packet
            for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
            {
                if ((chunkMask & (1 << chunkY)) != 0)
                {
                    // 1.14 and above Non-air block count inside chunk section, for lighting purposes
                    if (protocolversion >= Protocol113Handler.MC_1_14_Version)
                        dataTypes.ReadNextShort(cache);

                    byte bitsPerBlock = dataTypes.ReadNextByte(cache);
                    bool usePalette = (bitsPerBlock <= 8);

                    // Vanilla Minecraft will use at least 4 bits per block
                    if (bitsPerBlock < 4)
                        bitsPerBlock = 4;

                    // MC 1.9 to 1.12 will set palette length field to 0 when palette
                    // is not used, MC 1.13+ does not send the field at all in this case
                    int paletteLength = 0; // Assume zero when length is absent
                    if (usePalette || protocolversion < Protocol113Handler.MC_1_13_Version)
                        paletteLength = dataTypes.ReadNextVarInt(cache);

                    int[] palette = new int[paletteLength];
                    for (int i = 0; i < paletteLength; i++)
                    {
                        palette[i] = dataTypes.ReadNextVarInt(cache);
                    }

                    // Bit mask covering bitsPerBlock bits
                    // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                    uint valueMask = (uint)((1 << bitsPerBlock) - 1);

                    // Block IDs are packed in the array of 64-bits integers
                    ulong[] dataArray = dataTypes.ReadNextULongArray(cache);

                    Chunk chunk = new Chunk(null);

                    if (dataArray.Length > 0)
                    {
                        int longIndex = 0;
                        int startOffset = 0 - bitsPerBlock;

                        for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                        {
                            for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                            {
                                for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                {
                                    // NOTICE: In the future a single ushort may not store the entire block id;
                                    // the Block class may need to change if block state IDs go beyond 65535
                                    ushort blockId;

                                    // Calculate location of next block ID inside the array of Longs
                                    startOffset += bitsPerBlock;
                                    bool overlap = false;

                                    if ((startOffset + bitsPerBlock) > 64)
                                    {
                                        if (protocolversion >= Protocol113Handler.MC_1_16_Version)
                                        {
                                            // In MC 1.16+, padding is applied to prevent overlapping between Longs:
                                            // [      LONG INTEGER      ][      LONG INTEGER      ]
                                            // [Block][Block][Block]XXXXX[Block][Block][Block]XXXXX

                                            // When overlapping, move forward to the beginning of the next Long
                                            startOffset = 0;
                                            longIndex++;
                                        }
                                        else
                                        {
                                            // In MC 1.15 and lower, block IDs can overlap between Longs:
                                            // [      LONG INTEGER      ][      LONG INTEGER      ]
                                            // [Block][Block][Block][Blo  ck][Block][Block][Block][

                                            // Detect when we reached the next Long or switch to overlap mode
                                            if (startOffset >= 64)
                                            {
                                                startOffset -= 64;
                                                longIndex++;
                                            }
                                            else overlap = true;
                                        }
                                    }

                                    // Extract Block ID
                                    if (overlap)
                                    {
                                        int endOffset = 64 - startOffset;
                                        blockId = (ushort)((dataArray[longIndex] >> startOffset | dataArray[longIndex + 1] << endOffset) & valueMask);
                                    }
                                    else
                                    {
                                        blockId = (ushort)((dataArray[longIndex] >> startOffset) & valueMask);
                                    }

                                    // Map small IDs to actual larger block IDs
                                    if (usePalette)
                                    {
                                        if (paletteLength <= blockId)
                                        {
                                            int blockNumber = (blockY * Chunk.SizeZ + blockZ) * Chunk.SizeX + blockX;
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

                    //We have our chunk, save the chunk into the world
                    handler.InvokeOnNetReadThread(() =>
                    {
                        if (handler.GetWorld()[chunkX, chunkZ] == null)
                            handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn(handler.GetWorld());
                        // Set the column's chunk mask
                        handler.GetWorld()[chunkX, chunkZ].ChunkMask = chunkMask;
                        chunk.SetParent(handler.GetWorld());
                        handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;
                    });

                    //Pre-1.14 Lighting data
                    if (protocolversion < Protocol113Handler.MC_1_14_Version)
                    {
                        //Skip block light
                        dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                        //Skip sky light
                        if (currentDimension == 0)
                            // Sky light is not sent in the nether or the end
                            dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                    }
                }
                else
                {
                    // This chunk is empty (filled with air)
                    handler.GetWorld()[chunkX, chunkZ][chunkY] = null;
                }
            }

            // Broadcast event to update world render
            Loom.QueueOnMainThread(() => {
                    EventManager.Instance.Broadcast<ReceiveChunkColumnEvent>(new ReceiveChunkColumnEvent(chunkX, chunkZ));
                }
            );

            // Don't worry about skipping remaining data since there is no useful data afterwards in 1.9
            // (plus, it would require parsing the tile entity lists' NBT)
        }

        
    }
}
