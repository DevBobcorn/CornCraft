#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.Tests
{
    public class DummyPacketBuffer
    {
        private readonly Queue<byte> cache = new();

        public DummyPacketBuffer()
        {

        }

        public void WriteData(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                cache.Enqueue(buffer[i]);
            }

            //Console.WriteLine($"{buffer.Length} byte(s) written. Current length: {cache.Count} byte(s)");
        }

        public void WriteBool(bool paramBool)
        {
            WriteData(DataTypes.GetBool(paramBool));
        }

        public void WriteByte(byte b)
        {
            cache.Enqueue(b);

            //Console.WriteLine($"1 byte(s) written. Current length: {cache.Count} byte(s)");
        }

        public void WriteShort(short number)
        {
            WriteData(DataTypes.GetShort(number));
        }

        public void WriteInt(int number)
        {
            WriteData(DataTypes.GetInt(number));
        }

        public void WriteLong(long number)
        {
            WriteData(DataTypes.GetLong(number));
        }

        public void WriteUShort(ushort number)
        {
            WriteData(DataTypes.GetUShort(number));
        }

        public void WriteUInt(uint number)
        {
            WriteData(DataTypes.GetUInt(number));
        }

        public void WriteULong(ulong number)
        {
            WriteData(DataTypes.GetULong(number));
        }

        public void WriteVarInt(int paramInt)
        {
            WriteData(DataTypes.GetVarInt(paramInt));
        }

        public void WriteFloat(float number)
        {
            WriteData(DataTypes.GetFloat(number));
        }

        public void WriteDouble(float number)
        {
            WriteData(DataTypes.GetDouble(number));
        }

        public void WriteString(string paramString)
        {
            var stringBytes = Encoding.UTF8.GetBytes(paramString);

            // Write string byte count as varint
            WriteData(DataTypes.GetVarInt(stringBytes.Length));

            WriteData(stringBytes);
        }

        public void WritePString(string paramString, int length)
        {
            var stringBytes = Encoding.UTF8.GetBytes(paramString);

            if (stringBytes.Length > length)
            {
                throw new InvalidDataException($"Encoded string {paramString} is longer given fixed size {length}");
            }
            else if (stringBytes.Length < length)
            {
                Array.Resize(ref stringBytes, length);
            }

            WriteData(stringBytes);
        }

        public void WriteLocation(Location location)
        {
            WriteData(DataTypes.GetLocation(location));
        }

        public void WriteBlockLoc(BlockLoc blockLoc)
        {
            WriteData(DataTypes.GetBlockLoc(blockLoc));
        }

        public void WriteUUID(Guid guid)
        {
            WriteData(DataTypes.GetUUID(guid));
        }

        public void WriteNbt(Dictionary<string, object>? nbt)
        {
            WriteData(DataTypes.GetNbt(nbt));
        }

        public byte[] GetBufferBytes()
        {
            return cache.ToArray();
        }

        public Queue<byte> GetBufferByteQueueClone()
        {
            return new Queue<byte>(cache);
        }
    }
}
