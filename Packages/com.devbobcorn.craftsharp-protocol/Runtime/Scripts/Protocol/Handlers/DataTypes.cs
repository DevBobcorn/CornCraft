#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using UnityEngine;

using CraftSharp.Inventory;

namespace CraftSharp.Protocol.Handlers
{
    /// <summary>
    /// Handle data types encoding / decoding
    /// </summary>
    class DataTypes
    {
        /// <summary>
        /// Protocol version for adjusting data types
        /// </summary>
        private readonly int protocolVersion;

        public bool UseAnonymousNBT => protocolVersion >= ProtocolMinecraft.MC_1_20_2_Version;

        /// <summary>
        /// Initialize a new DataTypes instance
        /// </summary>
        /// <param name="protocol">Protocol version</param>
        public DataTypes(int protocol)
        {
            this.protocolVersion = protocol;
        }

        #region Static data readers

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        public static byte[] ReadData(int offset, Queue<byte> cache)
        {
            byte[] result = new byte[offset];
            for (int i = 0; i < offset; i++)
                result[i] = cache.Dequeue();
            return result;
        }

        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <param name="dest">Storage results</param>
        public static void ReadDataReverse(Queue<byte> cache, Span<byte> dest)
        {
            for (int i = (dest.Length - 1); i >= 0; --i)
                dest[i] = cache.Dequeue();
        }

        /// <summary>
        /// Remove some data from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to drop</param>
        /// <param name="cache">Cache of bytes to drop</param>
        public static void DropData(int offset, Queue<byte> cache)
        {
            while (offset-- > 0)
                cache.Dequeue();
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        public static bool ReadNextBool(Queue<byte> cache)
        {
            return ReadNextByte(cache) != 0x00;
        }

        /// <summary>
        /// Read a single signed byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public static sbyte ReadNextSByte(Queue<byte> cache)
        {
            sbyte result = (sbyte) cache.Dequeue(); // two's complement
            return result;
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public static byte ReadNextByte(Queue<byte> cache)
        {
            byte result = cache.Dequeue();
            return result;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        public static short ReadNextShort(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[2];
            for (int i = (2 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt16(rawValue);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        public static int ReadNextInt(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt32(rawValue);
        }

        /// <summary>
        /// Read a long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public static long ReadNextLong(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToInt64(rawValue);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public static ushort ReadNextUShort(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[2];
            for (int i = (2 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToUInt16(rawValue);
        }

        /// <summary>
        /// Read an unsigned integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public static uint ReadNextUInt(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToUInt32(rawValue);
        }

        /// <summary>
        /// Read an unsigned long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        public static ulong ReadNextULong(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToUInt64(rawValue);
        }

        /// <summary>
        /// Read an "extended short", which is actually an int of some kind, from the cache of bytes.
        /// This is only done with forge.  It looks like it's a normal short, except that if the high
        /// bit is set, it has an extra byte.
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The int</returns>
        public static int ReadNextVarShort(Queue<byte> cache)
        {
            ushort low = ReadNextUShort(cache);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = ReadNextByte(cache);
            }

            return ((high & 0xFF) << 15) | low;
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>
        public static int ReadNextVarIntRAW(SocketWrapper socket)
        {
            int i = 0;
            int j = 0;
            byte b;
            while (true)
            {
                b = socket.ReadDataRAW(1)[0];
                i |= (b & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt is too big");
                if ((b & 0x80) != 128) break;
            }

            return i;
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public static int ReadNextVarInt(Queue<byte> cache)
        {
            int i = 0;
            int j = 0;
            byte b;
            do
            {
                b = cache.Dequeue();
                i |= (b & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt is too big");
            } while ((b & 0x80) == 128);

            return i;
        }

        /// <summary>
        /// Skip a VarInt from a cache of bytes with better performance
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        public static void SkipNextVarInt(Queue<byte> cache)
        {
            while (true)
                if ((ReadNextByte(cache) & 0x80) != 128)
                    break;
        }

        /// <summary>
        /// Read a long from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The long value</returns>
        public static long ReadNextVarLong(Queue<byte> cache)
        {
            int numRead = 0;
            long result = 0;
            byte read;
            do
            {
                read = ReadNextByte(cache);
                long value = (read & 0x7F);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 10)
                {
                    throw new OverflowException("VarLong is too big");
                }
            } while ((read & 0x80) != 0);

            return result;
        }

        /// <summary>
        /// Read a float from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The float value</returns>
        public static float ReadNextFloat(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[4];
            for (int i = (4 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToSingle(rawValue);
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        public static double ReadNextDouble(Queue<byte> cache)
        {
            Span<byte> rawValue = stackalloc byte[8];
            for (int i = (8 - 1); i >= 0; --i) //Endianness
                rawValue[i] = cache.Dequeue();
            return BitConverter.ToDouble(rawValue);
        }

        /// <summary>
        /// Read a string from a cache of bytes and a given length and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public static string ReadNextPString(Queue<byte> cache, int length)
        {
            if (length > 0)
            {
                return Encoding.UTF8.GetString(ReadData(length, cache));
            }
            else return "";
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        public static string ReadNextString(Queue<byte> cache)
        {
            int length = ReadNextVarInt(cache);
            return ReadNextPString(cache, length);
        }

        /// <summary>
        /// Skip a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        public static void SkipNextString(Queue<byte> cache)
        {
            int length = ReadNextVarInt(cache);
            DropData(length, cache);
        }

        /// <summary>
        /// Read a Location encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public static Location ReadNextLocation(Queue<byte> cache)
        {
            ulong locEncoded = ReadNextULong(cache);
            int x, y, z;
            
            // MC 1.14+
            x = (int)(locEncoded >> 38);
            y = (int)(locEncoded & 0xFFF);
            z = (int)(locEncoded << 26 >> 38);

            if (x >= 0x02000000) // 33,554,432
                x -= 0x04000000; // 67,108,864
            if (y >= 0x00000800) //      2,048
                y -= 0x00001000; //      4,096
            if (z >= 0x02000000) // 33,554,432
                z -= 0x04000000; // 67,108,864
            return new Location(x, y, z);
        }

        /// <summary>
        /// Read a BlockLoc encoded as an ulong field and remove it from the cache
        /// </summary>
        /// <returns>The Location value</returns>
        public static BlockLoc ReadNextBlockLoc(Queue<byte> cache)
        {
            ulong locEncoded = ReadNextULong(cache);
            int x, y, z;
            
            // MC 1.14+
            x = (int)(locEncoded >> 38);
            y = (int)(locEncoded & 0xFFF);
            z = (int)(locEncoded << 26 >> 38);

            if (x >= 0x02000000) // 33,554,432
                x -= 0x04000000; // 67,108,864
            if (y >= 0x00000800) //      2,048
                y -= 0x00001000; //      4,096
            if (z >= 0x02000000) // 33,554,432
                z -= 0x04000000; // 67,108,864
            return new BlockLoc(x, y, z);
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>
        public static Guid ReadNextUUID(Queue<byte> cache)
        {
            Span<byte> javaUUID = stackalloc byte[16];
            for (int i = 0; i < 16; ++i)
                javaUUID[i] = cache.Dequeue();
            Guid guid = new(javaUUID);
            if (BitConverter.IsLittleEndian)
                guid = guid.ToLittleEndian();
            return guid;
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache
        /// </summary>
        public static Dictionary<string, object> ReadNextNbt(Queue<byte> cache, bool useAnonymousNbt)
        {
            return ReadNextNbt(cache, true, useAnonymousNbt);
        }

        /// <summary>
        /// Read Named Binary Tag from compressed bytes
        /// </summary>
        public static Dictionary<string, object> ReadNbtFromBytes(byte[] bytes, bool useAnonymousNbt)
        {
            using (var compressedStream = new MemoryStream(bytes))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    using (var memStream = new MemoryStream())
                    {
                        zipStream.CopyTo(memStream);
                        bytes = memStream.ToArray();
                    }
                
            return ReadNextNbt(new(bytes), true, useAnonymousNbt);
        }

        /// <summary>
        /// Read an uncompressed Named Binary Tag blob and remove it from the cache (internal)
        /// </summary>
        private static Dictionary<string, object> ReadNextNbt(Queue<byte> cache, bool root, bool useAnonymousNbt)
        {
            Dictionary<string, object> nbtData = new();

            if (root)
            {
                if (cache.Peek() == 0) // TAG_End
                {
                    cache.Dequeue();
                    return nbtData;
                }

                var nextId = cache.Dequeue();
                if (!useAnonymousNbt)
                {
                    if (nextId is not 10) // TAG_Compound
                        throw new System.IO.InvalidDataException(
                            "Failed to decode NBT: Does not start with TAG_Compound");

                    // NBT root name
                    var rootName = Encoding.ASCII.GetString(ReadData(ReadNextUShort(cache), cache));

                    if (!string.IsNullOrEmpty(rootName))
                        nbtData[""] = rootName;
                }
                // In 1.20.2 The root TAG_Compound doesn't have a name
                // In 1.20.3+ The root can be TAG_Compound or TAG_String
                else
                {
                    if (nextId is not (10 or 8)) // TAG_Compound or TAG_String
                        throw new System.IO.InvalidDataException(
                            "Failed to decode NBT: Does not start with TAG_Compound or TAG_String");

                    // Read TAG_String
                    if (nextId is 8)
                    {
                        var byteArrayLength = ReadNextUShort(cache);
                        var result = Encoding.UTF8.GetString(ReadData(byteArrayLength, cache));

                        return new Dictionary<string, object>()
                        {
                            { "", result }
                        };
                    }
                }
            }

            while (true)
            {
                int fieldType = ReadNextByte(cache);

                if (fieldType == 0) // TAG_End
                    return nbtData;

                int fieldNameLength = ReadNextUShort(cache);
                string fieldName = Encoding.ASCII.GetString(ReadData(fieldNameLength, cache));
                object fieldValue = ReadNbtField(cache, fieldType, useAnonymousNbt);

                // This will override previous tags with the same name
                nbtData[fieldName] = fieldValue;
            }
        }

        /// <summary>
        /// Read a single Named Binary Tag field of the specified type and remove it from the cache
        /// </summary>
        private static object ReadNbtField(Queue<byte> cache, int fieldType, bool useAnonymousNbt)
        {
            switch (fieldType)
            {
                case 1: // TAG_Byte
                    return ReadNextByte(cache);
                case 2: // TAG_Short
                    return ReadNextShort(cache);
                case 3: // TAG_Int
                    return ReadNextInt(cache);
                case 4: // TAG_Long
                    return ReadNextLong(cache);
                case 5: // TAG_Float
                    return ReadNextFloat(cache);
                case 6: // TAG_Double
                    return ReadNextDouble(cache);
                case 7: // TAG_Byte_Array
                    return ReadData(ReadNextInt(cache), cache);
                case 8: // TAG_String
                    return Encoding.UTF8.GetString(ReadData(ReadNextUShort(cache), cache));
                case 9: // TAG_List
                    int listType = ReadNextByte(cache);
                    int listLength = ReadNextInt(cache);
                    object[] listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = ReadNbtField(cache, listType, useAnonymousNbt);
                    return listItems;
                case 10: // TAG_Compound
                    return ReadNextNbt(cache, false, useAnonymousNbt);
                case 11: // TAG_Int_Array
                    listType = 3;
                    listLength = ReadNextInt(cache);
                    listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = ReadNbtField(cache, listType, useAnonymousNbt);
                    return listItems;
                case 12: // TAG_Long_Array
                    listType = 4;
                    listLength = ReadNextInt(cache);
                    listItems = new object[listLength];
                    for (int i = 0; i < listLength; i++)
                        listItems[i] = ReadNbtField(cache, listType, useAnonymousNbt);
                    return listItems;
                default:
                    throw new InvalidDataException("Failed to decode NBT: Unknown field type " + fieldType);
            }
        }

        /// <summary>
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>
        public static byte[] ReadNextByteArray(Queue<byte> cache)
        {
            int len = ReadNextVarInt(cache);
            return ReadData(len, cache);
        }

        /// <summary>
        /// Read a byte array with given length from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <param name="length">Length of the bytes array</param>
        /// <returns>The byte array</returns>
        public static byte[] ReadNextByteArray(Queue<byte> cache, int length)
        {
            return ReadData(length, cache);
        }

        /// <summary>
        /// Read several little endian unsigned short integers at once from a cache of bytes and remove them from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        public static ushort[] ReadNextUShortsLittleEndian(int amount, Queue<byte> cache)
        {
            byte[] rawValues = ReadData(2 * amount, cache);
            ushort[] result = new ushort[amount];
            for (int i = 0; i < amount; i++)
                result[i] = BitConverter.ToUInt16(rawValues, i * 2);
            return result;
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        public static ulong[] ReadNextULongArray(Queue<byte> cache)
        {
            int len = ReadNextVarInt(cache);
            ulong[] result = new ulong[len];
            for (int i = 0; i < len; i++)
                result[i] = ReadNextULong(cache);
            return result;
        }

        #endregion

        #region Complex data readers

        /// <summary>
        /// Read a single item slot from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public ItemStack? ReadNextItemSlot(Queue<byte> cache, ItemPalette itemPalette)
        {
            // MC 1.13.2 and greater
            bool itemPresent = ReadNextBool(cache);
            if (itemPresent)
            {
                int itemID = ReadNextVarInt(cache);
                
                if (itemID == -1)
                    return null;
                
                Item type = itemPalette.GetByNumId(itemID);
                byte itemCount = ReadNextByte(cache);
                Dictionary<string, object> nbt = ReadNextNbt(cache, UseAnonymousNBT);
                return new ItemStack(type, itemCount, nbt);
            }
            else return null;
        }

        /// <summary>
        /// Read entity information from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="entityPalette">Mappings for converting entity type Ids to EntityType</param>
        /// <param name="living">TRUE for living entities (layout differs)</param>
        /// <returns>Entity information</returns>
        public EntityData ReadNextEntity(Queue<byte> cache, EntityTypePalette entityPalette, bool living)
        {
            var entityId = ReadNextVarInt(cache);
            var entityUUID = ReadNextUUID(cache); // MC 1.8+

            EntityType entityType;
            // Entity type data type change from byte to varint after 1.14
            entityType = entityPalette.GetByNumId(ReadNextVarInt(cache));

            var entityX = ReadNextDouble(cache);
            var entityY = ReadNextDouble(cache);
            var entityZ = ReadNextDouble(cache);

            var data = -1;
            byte entityPitch, entityYaw, entityHeadYaw;

            if (living)
            {
                entityYaw = ReadNextByte(cache); // Yaw
                entityPitch = ReadNextByte(cache); // Pitch
                entityHeadYaw = ReadNextByte(cache); // Head Yaw
            }
            else
            {
                entityPitch = ReadNextByte(cache); // Pitch
                entityYaw = ReadNextByte(cache); // Yaw
                entityHeadYaw = entityYaw;

                if (protocolVersion >= ProtocolMinecraft.MC_1_19_Version)
                    entityYaw = ReadNextByte(cache); // Head Yaw

                // Data
                data = protocolVersion >= ProtocolMinecraft.MC_1_19_Version 
                    ? ReadNextVarInt(cache) : ReadNextInt(cache);
            }

            return new EntityData(entityId, entityType, new Location(entityX, entityY, entityZ), entityYaw, entityPitch, entityHeadYaw, data);
        }

        /// <summary>
        /// Read a Entity MetaData and remove it from the cache
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="itemPalette"></param>
        /// <param name="metadataPalette"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public Dictionary<int, object?> ReadNextMetadata(Queue<byte> cache, ItemPalette itemPalette, EntityMetadataPalette metadataPalette)
        {
            Dictionary<int, object?> data = new();
            byte key = ReadNextByte(cache);
            byte terminteValue = (byte) 0xff; // 1.9+

            while (key != terminteValue)
            {
                int typeId = ReadNextVarInt(cache); // 1.9+
                
                
                EntityMetaDataType type;
                try
                {
                    type = metadataPalette.GetDataType(typeId);
                }
                catch (KeyNotFoundException)
                {
                    throw new InvalidDataException("Unknown Metadata Type ID " + typeId + ". Is this up to date for new MC Version?");
                }

                // Value's data type is depended on Type
                object? value = null;

                switch (type)
                {
                    case EntityMetaDataType.Short: // 1.8 only
                        value = ReadNextShort(cache);
                        break;
                    case EntityMetaDataType.Int: // 1.8 only
                        value = ReadNextInt(cache);
                        break;
                    case EntityMetaDataType.Vector3Int: // 1.8 only
                        value = new Vector3Int(
                            ReadNextInt(cache),
                            ReadNextInt(cache),
                            ReadNextInt(cache)
                        );
                        break;
                    case EntityMetaDataType.Byte: // byte
                        value = ReadNextByte(cache);
                        break;
                    case EntityMetaDataType.VarInt: // VarInt
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.VarLong: // Long
                        value = ReadNextVarLong(cache);
                        break;
                    case EntityMetaDataType.Float: // Float
                        value = ReadNextFloat(cache);
                        break;
                    case EntityMetaDataType.String: // String
                        value = ReadNextString(cache);
                        break;
                    case EntityMetaDataType.Chat: // Chat
                        value = ReadNextChat(cache);
                        break;
                    case EntityMetaDataType.OptionalChat: // Optional Chat
                        if (ReadNextBool(cache))
                            value = ReadNextChat(cache);
                        break;
                    case EntityMetaDataType.Slot: // Slot
                        value = ReadNextItemSlot(cache, itemPalette);
                        break;
                    case EntityMetaDataType.Boolean: // Boolean
                        value = ReadNextBool(cache);
                        break;
                    case EntityMetaDataType.Rotation: // Rotation (3x floats)
                        value = new Vector3
                        (
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache)
                        );
                        break;
                    case EntityMetaDataType.Position: // Position
                        value = ReadNextLocation(cache);
                        break;
                    case EntityMetaDataType.OptionalPosition: // Optional Position
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextLocation(cache);
                        }
                        break;
                    case EntityMetaDataType.Direction: // Direction (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.OptionalUuid: // Optional UUID
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextUUID(cache);
                        }
                        break;
                    case EntityMetaDataType.BlockId: // BlockID (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.OptionalBlockId: // Optional BlockID (VarInt)
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.Nbt: // NBT
                        value = ReadNextNbt(cache, UseAnonymousNBT);
                        break;
                    case EntityMetaDataType.Particle: // Particle
                        // Skip data only, not used
                        value = ReadParticleData(cache, itemPalette);
                        break;
                    case EntityMetaDataType.VillagerData: // Villager Data (3x VarInt)
                        value = new Vector3Int
                        (
                            ReadNextVarInt(cache),
                            ReadNextVarInt(cache),
                            ReadNextVarInt(cache)
                        );
                        break;
                    case EntityMetaDataType.OptionalVarInt: // Optional VarInt
                        if (ReadNextBool(cache))
                        {
                            value = ReadNextVarInt(cache);
                        }
                        break;
                    case EntityMetaDataType.Pose: // Pose
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.CatVariant: // Cat Variant
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.FrogVariant: // Frog Variant
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.GlobalPosition: // GlobalPos
                        // Dimension and blockLoc, currently not in use
                        value = new Tuple<string, Location>(ReadNextString(cache), ReadNextLocation(cache));
                        break;
                    case EntityMetaDataType.OptionalGlobalPosition:
                        // FIXME: wiki.vg is bool + string + location
                        //        but minecraft-data is bool + string
                        if (ReadNextBool(cache))
                        {
                            // Dimension and blockLoc, currently not in use
                            value = new Tuple<string, Location>(ReadNextString(cache), ReadNextLocation(cache));
                        }
                        break;
                    case EntityMetaDataType.PaintingVariant: // Painting Variant
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.SnifferState: // Sniffer state
                        value = ReadNextVarInt(cache);
                        break;
                    case EntityMetaDataType.Vector3: // Vector 3f
                        value = new Vector3
                        (
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache)
                        );
                        break;
                    case EntityMetaDataType.Quaternion: // Quaternion
                        value = new Quaternion
                        (
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache),
                            ReadNextFloat(cache)
                        );
                        break;
                }

                data[key] = value;
                key = ReadNextByte(cache);
            }
            return data;
        }

        /// <summary>
        /// Read particle extra data
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="itemPalette"></param>
        public ParticleExtraData ReadParticleData(Queue<byte> cache, ItemPalette itemPalette)
        {
            var particleID = ReadNextVarInt(cache);

            // Reference:
            // 1.19.3 - https://wiki.vg/index.php?title=Data_types&oldid=17986
            // 1.18 - https://wiki.vg/index.php?title=Data_types&oldid=17180
            // 1.17 - https://wiki.vg/index.php?title=Data_types&oldid=16740
            // 1.15 - https://wiki.vg/index.php?title=Data_types&oldid=15338
            // 1.13 - https://wiki.vg/index.php?title=Data_types&oldid=14271

            switch (particleID)
            {
                case 1:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadBlockParticle(cache); // BlockState (minecraft:block)
                    break;
                case 2:
                    // 1.18+
                    if (protocolVersion > ProtocolMinecraft.MC_1_17_1_Version)
                        return ReadBlockParticle(cache); // Block state (minecraft:block before 1.20.6, minecraft:block_marker in 1.20.6+)
                    break;
                case 3:
                    if (protocolVersion is (< ProtocolMinecraft.MC_1_17_Version
                        or > ProtocolMinecraft.MC_1_17_1_Version)
                        and < ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadBlockParticle(cache); // Block State (minecraft:block before 1.18, minecraft:block_marker after 1.18 up to 1.20.6)
                    break;
                case 4:
                    // 1.17 - 1.17.1
                    if (protocolVersion is ProtocolMinecraft.MC_1_17_Version
                        or ProtocolMinecraft.MC_1_17_1_Version)
                        return ReadBlockParticle(cache); // Block state (minecraft:block)
                    break;
                case 13:
                    // 1.20.6+
                    return ReadDustParticle(cache); // minecraft:dust
                case 14:
                    switch (protocolVersion)
                    {
                        // 1.15 - 1.16.5 and 1.18 - 1.20.4
                        case < ProtocolMinecraft.MC_1_17_Version
                            or > ProtocolMinecraft.MC_1_17_1_Version
                            and < ProtocolMinecraft.MC_1_20_6_Version:
                            return ReadDustParticle(cache);
                        // 1.20.6+
                        case >= ProtocolMinecraft.MC_1_20_6_Version:
                            return ReadDustColorTransitionParticle(cache);
                    }
                    break;
                case 15:
                    switch (protocolVersion)
                    {
                        // 1.17 - 1.17.1
                        case ProtocolMinecraft.MC_1_17_Version
                            or ProtocolMinecraft.MC_1_17_1_Version:
                            return ReadDustParticle(cache);
                        // 1.18 - 1.20.4
                        case > ProtocolMinecraft.MC_1_17_1_Version
                            and < ProtocolMinecraft.MC_1_20_6_Version:
                            return ReadDustColorTransitionParticle(cache);
                    }
                    break;
                case 16:
                    // 1.17 - 1.17.1
                    if (protocolVersion is ProtocolMinecraft.MC_1_17_Version
                        or ProtocolMinecraft.MC_1_17_1_Version)
                        return ReadDustColorTransitionParticle(cache);
                    break;
                case 20:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadEntityEffectParticle(cache); // minecraft:entity_effect
                    break;
                case 23:
                    // 1.15 - 1.16.5
                    if (protocolVersion < ProtocolMinecraft.MC_1_17_Version)
                        return ReadBlockParticle(cache); // Block State (minecraft:falling_dust)
                    break;
                case 24:
                    // 1.18 - 1.19.3
                    if (protocolVersion is > ProtocolMinecraft.MC_1_17_1_Version
                        and < ProtocolMinecraft.MC_1_19_3_Version)
                        return ReadBlockParticle(cache); // Block State (minecraft:falling_dust)
                    break;
                case 25:
                    // 1.17 - 1.17.1 and 1.19.3 - 1.20.4
                    if (protocolVersion is ProtocolMinecraft.MC_1_17_Version
                        or ProtocolMinecraft.MC_1_17_1_Version
                        or (>= ProtocolMinecraft.MC_1_19_3_Version and < ProtocolMinecraft.MC_1_20_6_Version))
                        return ReadBlockParticle(cache); // Block State (minecraft:falling_dust)
                    break;
                case 28:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadBlockParticle(cache); // minecraft:falling_dust (BlockState)
                    break;
                case 30:
                    // 1.19.3 - 1.20.4
                    if (protocolVersion is >= ProtocolMinecraft.MC_1_19_3_Version
                        and < ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadSculkChargeParticle(cache); // Roll (minecraft:sculk_charge)
                    break;
                case 32:
                    // 1.15 - 1.16.5
                    if (protocolVersion < ProtocolMinecraft.MC_1_17_Version)
                        return ReadItemParticle(cache, itemPalette); // Item (minecraft:item)
                    break;
                case 35:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadSculkChargeParticle(cache); // minecraft:sculk_charge (Roll)
                    break;
                case 36:
                    switch (protocolVersion)
                    {
                        // 1.17 - 1.17.1
                        case ProtocolMinecraft.MC_1_17_Version
                            or ProtocolMinecraft.MC_1_17_1_Version:
                            return ReadItemParticle(cache, itemPalette); // Item (minecraft:item)
                        // 1.18 - 1.19.2
                        case > ProtocolMinecraft.MC_1_17_1_Version
                            and < ProtocolMinecraft.MC_1_19_3_Version:
                            return ReadVibrationParticleV1(cache, oldFormat: false); // minecraft:vibration
                    }
                    break;
                case 37:
                    // 1.17 - 1.17.1
                    if (protocolVersion is ProtocolMinecraft.MC_1_17_Version
                        or ProtocolMinecraft.MC_1_17_1_Version)
                        return ReadVibrationParticleV1(cache, oldFormat: true); // minecraft:vibration
                    break;
                case 39:
                    // 1.19.3 - 1.20.4
                    if (protocolVersion is >= ProtocolMinecraft.MC_1_19_3_Version and < ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadItemParticle(cache, itemPalette); // Item (minecraft:item)
                    break;
                case 40:
                    // 1.19.3 - 1.20.4
                    if (protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version)
                        return ReadVibrationParticleV2(cache, oldFormat: true); // minecraft:vibration
                    break;
                case 44:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadItemParticle(cache, itemPalette); // minecraft:item (Item)
                    break;
                case 45:
                    // 1.21+
                    if(protocolVersion >= ProtocolMinecraft.MC_1_21_Version)
                        return ReadVibrationParticleV2(cache, oldFormat: false); // minecraft:vibration
                    break;
                case 99:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadShriekParticle(cache); // minecraft:shriek (Delay)
                    break;
                case 105:
                    // 1.20.6+
                    if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
                        return ReadBlockParticle(cache); // minecraft:dust_pillar (BlockState)
                    break;
            }

            return ParticleExtraData.Empty;
        }

        private BlockParticleExtraData ReadBlockParticle(Queue<byte> cache)
        {
            var stateId = ReadNextVarInt(cache);

            return new BlockParticleExtraData(stateId);
        }

        private DustParticleExtraData ReadDustParticle(Queue<byte> cache)
        {
            var r = ReadNextFloat(cache); // Red
            var g = ReadNextFloat(cache); // Green
            var b = ReadNextFloat(cache); // Blue
            var s = ReadNextFloat(cache); // Scale

            return new DustParticleExtraData(new(r, g, b), s);
        }

        private DustColorTransitionParticleExtraData ReadDustColorTransitionParticle(Queue<byte> cache)
        {
            var fr = ReadNextFloat(cache); // From red
            var fg = ReadNextFloat(cache); // From green
            var fb = ReadNextFloat(cache); // From blue
            var s  = ReadNextFloat(cache); // Scale
            var tr = ReadNextFloat(cache); // To red
            var tg = ReadNextFloat(cache); // To green
            var tb = ReadNextFloat(cache); // To Blue

            return new DustColorTransitionParticleExtraData(new(fr, fg, fb), new(tr, tg, tb), s);
        }

        private EntityEffectParticleExtraData ReadEntityEffectParticle(Queue<byte> cache)
        {
            var color = ReadNextInt(cache);

            return new EntityEffectParticleExtraData(color);
        }

        private ItemParticleExtraData ReadItemParticle(Queue<byte> cache, ItemPalette itemPalette)
        {
            var itemStack = ReadNextItemSlot(cache, itemPalette);

            return new ItemParticleExtraData(itemStack);
        }

        private SculkChargeParticleExtraData ReadSculkChargeParticle(Queue<byte> cache)
        {
            var roll = ReadNextFloat(cache);

            return new SculkChargeParticleExtraData(roll);
        }

        private ShriekParticleExtraData ReadShriekParticle(Queue<byte> cache)
        {
            var delay = ReadNextVarInt(cache);

            return new ShriekParticleExtraData(delay);
        }

        /// <summary>
        /// Data for 'minecraft:vibration' particle in 1.17 - 1.19.2
        /// <br/>
        /// Old format for version 1.17 - 1.17.1, new format for version 1.18 - 1.19.2
        /// </summary>
        private VibrationParticleExtraDataV1 ReadVibrationParticleV1(Queue<byte> cache, bool oldFormat)
        {
            Location origin, destination;
            int ticks;

            if (oldFormat) // 1.17 - 1.17.1
            {
                var ox = (float) ReadNextDouble(cache); // Origin X
                var oy = (float) ReadNextDouble(cache); // Origin Y
                var oz = (float) ReadNextDouble(cache); // Origin Z
                var dx = (float) ReadNextDouble(cache); // Destination X
                var dy = (float) ReadNextDouble(cache); // Destination Y
                var dz = (float) ReadNextDouble(cache); // Destination Z

                origin = new(ox, oy, oz);
                destination = new(dx, dy, dz);
                
                ticks = ReadNextInt(cache); // Ticks
            }
            else // 1.18 - 1.19.2
            {
                origin = ReadNextLocation(cache); // Origin (Starting Position)
                destination = ReadNextLocation(cache); // Desitination (Ending Position)
                ticks = ReadNextVarInt(cache); // Ticks
            }
            
            return new VibrationParticleExtraDataV1(origin, destination, ticks);
        }

        /// <summary>
        /// Data for 'minecraft:vibration' particle in 1.19.3+
        /// <br/>
        /// Old format for version 1.19.3 - 1.20.4, new format for version 1.21+
        /// </summary>
        private VibrationParticleExtraDataV2 ReadVibrationParticleV2(Queue<byte> cache, bool oldFormat)
        {
            bool useBlockPos;

            if (oldFormat) // 1.19.3 - 1.20.4
            {
                var positionSourceType = ReadNextString(cache);
                useBlockPos = positionSourceType switch
                {
                    "minecraft:block"  => true,
                    "block"            => true,
                    "minecraft:entity" => false,
                    "entity"           => false,

                    _                  => throw new InvalidDataException($"Unknown position source type: {positionSourceType}")
                };
            }
            else // 1.21+
            {
                var positionSourceType = ReadNextVarInt(cache); // Position Source Type, 0 for 'minecraft:block', 1 for 'minecraft:entity'

                useBlockPos = positionSourceType switch
                {
                    0 => true,
                    1 => false,

                    _ => throw new InvalidDataException($"Unknown position source type: {positionSourceType}")
                };
            }

            if (useBlockPos)
            {
                var loc = ReadNextLocation(cache);
                var ticks = ReadNextVarInt(cache);

                return new VibrationParticleExtraDataV2(loc, ticks);
            }
            else
            {
                var entityId = ReadNextVarInt(cache);
                var eyeHeight = ReadNextFloat(cache);
                var ticks = ReadNextVarInt(cache);

                return new VibrationParticleExtraDataV2(entityId, eyeHeight, ticks);
            }
        }

        /// <summary>
        /// Read a single villager trade from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public VillagerTrade ReadNextTrade(Queue<byte> cache, ItemPalette itemPalette)
        {
            ItemStack inputItem1 = ReadNextItemSlot(cache, itemPalette)!;
            ItemStack outputItem = ReadNextItemSlot(cache, itemPalette)!;

            ItemStack? inputItem2 = null;

            if (protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version)
                inputItem2 = ReadNextItemSlot(cache, itemPalette);
            else
            {
                if (ReadNextBool(cache)) //check if villager has second item
                    inputItem2 = ReadNextItemSlot(cache, itemPalette);
            }

            bool tradeDisabled = ReadNextBool(cache);
            int numberOfTradeUses = ReadNextInt(cache);
            int maximumNumberOfTradeUses = ReadNextInt(cache);
            int xp = ReadNextInt(cache);
            int specialPrice = ReadNextInt(cache);
            float priceMultiplier = ReadNextFloat(cache);
            int demand = ReadNextInt(cache);
            return new VillagerTrade(inputItem1, outputItem, inputItem2, tradeDisabled, numberOfTradeUses,
                maximumNumberOfTradeUses, xp, specialPrice, priceMultiplier, demand);
        }

        public string ReadNextChat(Queue<byte> cache)
        {
            if (protocolVersion >= ProtocolMinecraft.MC_1_20_4_Version)
            {
                // Read as NBT
                var r = ReadNextNbt(cache, UseAnonymousNBT);
                var msg = ChatParser.ParseText(r);
                return msg;
            }
            else
            {
                // Read as String
                var json = ReadNextString(cache);
                return ChatParser.ParseText(json);
            }
        }

        #endregion

        #region Static data getters

        /// <summary>
        /// Easily append several byte arrays
        /// </summary>
        /// <param name="bytes">Bytes to append</param>
        /// <returns>Array containing all the data</returns>
        public static byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> result = new();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        /// <summary>
        /// Convert a byte array to an hexadecimal string representation (for debugging purposes)
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <returns>String representation</returns>
        public static string ByteArrayToString(byte[]? bytes)
        {
            if (bytes == null)
                return "null";
            else
                return BitConverter.ToString(bytes).Replace("-", " ");
        }

        /// <summary>
        /// Build an boolean for sending over the network
        /// </summary>
        /// <param name="paramBool">Boolean to encode</param>
        /// <returns>Byte array for this boolean</returns>
        public static byte[] GetBool(bool paramBool)
        {
            List<byte> bytes = new()
            {
                Convert.ToByte(paramBool)
            };
            return bytes.ToArray();
        }

        /// <summary>
        /// Get byte array representing a short
        /// </summary>
        /// <param name="number">Short to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetShort(short number)
        {
            byte[] theShort = BitConverter.GetBytes(number);
            Array.Reverse(theShort);
            return theShort;
        }

        /// <summary>
        /// Get byte array representing an integer
        /// </summary>
        /// <param name="number">Integer to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetInt(int number)
        {
            byte[] theInt = BitConverter.GetBytes(number);
            Array.Reverse(theInt);
            return theInt;
        }

        /// <summary>
        /// Get byte array representing a long integer
        /// </summary>
        /// <param name="number">Long to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetLong(long number)
        {
            byte[] theLong = BitConverter.GetBytes(number);
            Array.Reverse(theLong);
            return theLong;
        }

        /// <summary>
        /// Get byte array representing an unsigned short
        /// </summary>
        /// <param name="number">Short to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetUShort(ushort number)
        {
            byte[] theShort = BitConverter.GetBytes(number);
            Array.Reverse(theShort);
            return theShort;
        }

        /// <summary>
        /// Get byte array representing an unsigned integer
        /// </summary>
        /// <param name="number">Short to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetUInt(uint number)
        {
            byte[] theInt = BitConverter.GetBytes(number);
            Array.Reverse(theInt);
            return theInt;
        }

        /// <summary>
        /// Get byte array representing an unsigned long integer
        /// </summary>
        /// <param name="number">Long to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetULong(ulong number)
        {
            byte[] theLong = BitConverter.GetBytes(number);
            Array.Reverse(theLong);
            return theLong;
        }

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>
        public static byte[] GetVarInt(int paramInt)
        {
            List<byte> bytes = new();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte)(paramInt & 127 | 128));
                paramInt = (int)(((uint)paramInt) >> 7);
            }

            bytes.Add((byte)paramInt);
            return bytes.ToArray();
        }

        /// <summary>
        /// Get byte array representing a float
        /// </summary>
        /// <param name="number">Floalt to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetFloat(float number)
        {
            byte[] theFloat = BitConverter.GetBytes(number);
            Array.Reverse(theFloat); //Endianness
            return theFloat;
        }

        /// <summary>
        /// Get byte array representing a double
        /// </summary>
        /// <param name="number">Double to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetDouble(double number)
        {
            byte[] theDouble = BitConverter.GetBytes(number);
            Array.Reverse(theDouble); //Endianness
            return theDouble;
        }

        /// <summary>
        /// Get a byte array from the given string for sending over the network.
        /// </summary>
        /// <param name="text">String to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetPString(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        /// <summary>
        /// Get a byte array from the given string for sending over the network, with length information prepended.
        /// </summary>
        /// <param name="text">String to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetString(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return ConcatBytes(GetVarInt(bytes.Length), bytes);
        }

        /// <summary>
        /// Get a byte array representing the given location encoded as an unsigned long
        /// </summary>
        /// <remarks>
        /// A modulo will be applied if the location is outside the following ranges:
        /// X: -33,554,432 to +33,554,431
        /// Y: -2,048 to +2,047
        /// Z: -33,554,432 to +33,554,431
        /// </remarks>
        /// <returns>Location representation as ulong</returns>
        public static byte[] GetLocation(Location location)
        {
            byte[] locationBytes = BitConverter.GetBytes(((((ulong)location.X) & 0x3FFFFFF) << 38) |
                    ((((ulong)location.Z) & 0x3FFFFFF) << 12) |
                    (((ulong)location.Y) & 0xFFF));

            Array.Reverse(locationBytes); //Endianness
            return locationBytes;
        }

        /// <summary>
        /// Get a byte array representing the given BlockLoc encoded as an unsigned long
        /// </summary>
        /// <remarks>
        /// A modulo will be applied if the location is outside the following ranges:
        /// X: -33,554,432 to +33,554,431
        /// Y: -2,048 to +2,047
        /// Z: -33,554,432 to +33,554,431
        /// </remarks>
        /// <returns>Location representation as ulong</returns>
        public static byte[] GetBlockLoc(BlockLoc blockLoc)
        {
            byte[] locationBytes = BitConverter.GetBytes(((((ulong)blockLoc.X) & 0x3FFFFFF) << 38) |
                    ((((ulong)blockLoc.Z) & 0x3FFFFFF) << 12) |
                    (((ulong)blockLoc.Y) & 0xFFF));

            Array.Reverse(locationBytes); //Endianness
            return locationBytes;
        }

        /// <summary>
        /// Get a byte array from the given uuid
        /// </summary>
        /// <param name="uuid">UUID of Player/Entity</param>
        /// <returns>UUID representation</returns>
        public static byte[] GetUUID(Guid UUID)
        {
            return UUID.ToBigEndianBytes();
        }

        /// <summary>
        /// Build an uncompressed Named Binary Tag blob for sending over the network
        /// </summary>
        /// <param name="nbt">Dictionary to encode as Nbt</param>
        /// <returns>Byte array for this NBT tag</returns>
        public static byte[] GetNbt(Dictionary<string, object>? nbt)
        {
            return GetNbt(nbt, true);
        }

        /// <summary>
        /// Build an uncompressed Named Binary Tag blob for sending over the network (internal)
        /// </summary>
        /// <param name="nbt">Dictionary to encode as Nbt</param>
        /// <param name="root">TRUE if starting a new NBT tag, FALSE if processing a nested NBT tag</param>
        /// <returns>Byte array for this NBT tag</returns>
        private static byte[] GetNbt(Dictionary<string, object>? nbt, bool root)
        {
            if (nbt == null || nbt.Count == 0)
                return new byte[] { 0 }; // TAG_End

            List<byte> bytes = new();

            if (root)
            {
                bytes.Add(10); // TAG_Compound

                // NBT root name
                string? rootName = null;

                if (nbt.ContainsKey(""))
                    rootName = nbt[""] as string;

                rootName ??= "";

                bytes.AddRange(GetUShort((ushort)rootName.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(rootName));
            }

            foreach (var item in nbt)
            {
                // Skip NBT root name
                if (item.Key == "" && root)
                    continue;

                byte[] fieldNameLength = GetUShort((ushort)item.Key.Length);
                byte[] fieldName = Encoding.ASCII.GetBytes(item.Key);
                byte[] fieldData = GetNbtField(item.Value, out byte fieldType);
                bytes.Add(fieldType);
                bytes.AddRange(fieldNameLength);
                bytes.AddRange(fieldName);
                bytes.AddRange(fieldData);
            }

            bytes.Add(0); // TAG_End
            return bytes.ToArray();
        }

        /// <summary>
        /// Convert a single object into its NBT representation (internal)
        /// </summary>
        /// <param name="obj">Object to convert</param>
        /// <param name="fieldType">Field type for the passed object</param>
        /// <returns>Binary data for the passed object</returns>
        private static byte[] GetNbtField(object obj, out byte fieldType)
        {
            if (obj is byte)
            {
                fieldType = 1; // TAG_Byte
                return new[] { (byte)obj };
            }
            else if (obj is short)
            {
                fieldType = 2; // TAG_Short
                return GetShort((short)obj);
            }
            else if (obj is int)
            {
                fieldType = 3; // TAG_Int
                return GetInt((int)obj);
            }
            else if (obj is long)
            {
                fieldType = 4; // TAG_Long
                return GetLong((long)obj);
            }
            else if (obj is float)
            {
                fieldType = 5; // TAG_Float
                return GetFloat((float)obj);
            }
            else if (obj is double)
            {
                fieldType = 6; // TAG_Double
                return GetDouble((double)obj);
            }
            else if (obj is byte[])
            {
                fieldType = 7; // TAG_Byte_Array
                return (byte[])obj;
            }
            else if (obj is string)
            {
                fieldType = 8; // TAG_String
                byte[] stringBytes = Encoding.UTF8.GetBytes((string)obj);
                return ConcatBytes(GetUShort((ushort)stringBytes.Length), stringBytes);
            }
            else if (obj is object[])
            {
                fieldType = 9; // TAG_List

                List<object> list = new((object[])obj);
                int arrayLengthTotal = list.Count;

                // Treat empty list as TAG_Byte, length 0
                if (arrayLengthTotal == 0)
                    return ConcatBytes(new[] { (byte)1 }, GetInt(0));

                // Encode first list item, retain its type
                string firstItemTypeString = list[0].GetType().Name;
                byte[] firstItemBytes = GetNbtField(list[0], out byte firstItemType);
                list.RemoveAt(0);

                // Encode further list items, check they have the same type
                List<byte> subsequentItemsBytes = new();
                foreach (object item in list)
                {
                    subsequentItemsBytes.AddRange(GetNbtField(item, out byte subsequentItemType));
                    if (subsequentItemType != firstItemType)
                        throw new InvalidDataException(
                            "GetNbt: Cannot encode object[] list with mixed types: " + firstItemTypeString + ", " +
                            item.GetType().Name + " into NBT!");
                }

                // Build NBT list: type, length, item array
                return ConcatBytes(new[] { firstItemType }, GetInt(arrayLengthTotal), firstItemBytes,
                    subsequentItemsBytes.ToArray());
            }
            else if (obj is Dictionary<string, object>)
            {
                fieldType = 10; // TAG_Compound
                return GetNbt((Dictionary<string, object>)obj, false);
            }
            else if (obj is int[])
            {
                fieldType = 11; // TAG_Int_Array

                int[] srcIntList = (int[])obj;
                List<byte> encIntList = new();
                encIntList.AddRange(GetInt(srcIntList.Length));
                foreach (int item in srcIntList)
                    encIntList.AddRange(GetInt(item));
                return encIntList.ToArray();
            }
            else if (obj is long[])
            {
                fieldType = 12; // TAG_Long_Array

                long[] srcLongList = (long[])obj;
                List<byte> encLongList = new();
                encLongList.AddRange(GetInt(srcLongList.Length));
                foreach (long item in srcLongList)
                    encLongList.AddRange(GetLong(item));
                return encLongList.ToArray();
            }
            else
            {
                throw new InvalidDataException("GetNbt: Cannot encode data type " + obj.GetType().Name + " into NBT!");
            }
        }

        /// <summary>
        /// Get byte array with length information prepended to it
        /// </summary>
        /// <param name="array">Array to process</param>
        /// <returns>Array ready to send</returns>
        public static byte[] GetArray(byte[] array)
        {
            return ConcatBytes(GetVarInt(array.Length), array);
        }

        #endregion

        #region Complex data getters

        /// <summary>
        /// Get a byte array representing the given item as an item slot
        /// </summary>
        /// <param name="item">Item</param>
        /// <param name="itemPalette">Item Palette</param>
        /// <returns>Item slot representation</returns>
        public byte[] GetItemSlot(ItemStack? item, ItemPalette itemPalette)
        {
            List<byte> slotData = new();
            
            // MC 1.13 and greater
            if (item == null || item.IsEmpty)
                slotData.AddRange(GetBool(false)); // No item
            else
            {
                slotData.AddRange(GetBool(true)); // Item is present
                slotData.AddRange(GetVarInt(itemPalette.GetNumIdById(item.ItemType.ItemId)));
                slotData.Add((byte)item.Count);
                slotData.AddRange(GetNbt(item.NBT));
            }

            return slotData.ToArray();
        }

        /// <summary>
        /// Get a byte array representing an array of item slots
        /// </summary>
        /// <param name="items">Items</param>
        /// <param name="itemPalette">Item Palette</param>
        /// <returns>Array of Item slot representations</returns>
        public byte[] GetSlotsArray(Dictionary<int, ItemStack> items, ItemPalette itemPalette)
        {
            byte[] slotsArray = new byte[items.Count];

            foreach (KeyValuePair<int, ItemStack> item in items)
            {
                slotsArray = ConcatBytes(slotsArray, GetShort((short)item.Key), GetItemSlot(item.Value, itemPalette));
            }

            return slotsArray;
        }

        /// <summary>
        /// Get protocol block face from Direction
        /// </summary>
        /// <param name="direction">Direction</param>
        /// <returns>Block face byte enum</returns>
        public byte GetBlockFace(Direction direction)
        {
            return direction switch
            {
                Direction.Down => 0,
                Direction.Up => 1,
                Direction.North => 2,
                Direction.South => 3,
                Direction.West => 4,
                Direction.East => 5,
                _ => throw new NotImplementedException("Unknown direction: " + direction.ToString()),
            };
        }

        /// <summary>
        /// Write LastSeenMessageList
        /// </summary>
        /// <param name="msgList">Message.LastSeenMessageList</param>
        /// <param name="isOnlineMode">Whether the server is in online mode</param>
        /// <returns>Message.LastSeenMessageList Packet Data</returns>
        public byte[] GetLastSeenMessageList(Message.LastSeenMessageList msgList, bool isOnlineMode)
        {
            if (!isOnlineMode)
                return GetVarInt(0); // Message list size
            else
            {
                List<byte> fields = new();
                fields.AddRange(GetVarInt(msgList.entries.Length)); // Message list size
                foreach (Message.LastSeenMessageList.AcknowledgedMessage entry in msgList.entries)
                {
                    fields.AddRange(entry.profileId.ToBigEndianBytes()); // UUID
                    fields.AddRange(GetVarInt(entry.signature.Length)); // Signature length
                    fields.AddRange(entry.signature); // Signature data
                }

                return fields.ToArray();
            }
        }

        /// <summary>
        /// Write LastSeenMessageList.Acknowledgment
        /// </summary>
        /// <param name="ack">Acknowledgment</param>
        /// <param name="isOnlineMode">Whether the server is in online mode</param>
        /// <returns>Acknowledgment Packet Data</returns>
        public byte[] GetAcknowledgment(Message.LastSeenMessageList.Acknowledgment ack, bool isOnlineMode)
        {
            List<byte> fields = new();
            fields.AddRange(GetLastSeenMessageList(ack.lastSeen, isOnlineMode));
            if (!isOnlineMode || ack.lastReceived == null)
                fields.AddRange(GetBool(false)); // Has last received message
            else
            {
                fields.AddRange(GetBool(true));
                fields.AddRange(ack.lastReceived.profileId.ToBigEndianBytes()); // Has last received message
                fields.AddRange(GetVarInt(ack.lastReceived.signature.Length)); // Last received message signature length
                fields.AddRange(ack.lastReceived.signature); // Last received message signature data
            }

            return fields.ToArray();
        }

        #endregion
    }
}