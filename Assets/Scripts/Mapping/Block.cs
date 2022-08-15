using System;
using MinecraftClient.Mapping.BlockStatePalettes;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Block State
    /// </summary>
    public struct Block
    {
        /// <summary>
        /// Get or set global block ID to Material mapping
        /// The global Palette is a concept introduced with Minecraft 1.13
        /// </summary>
        public static BlockStatePalette Palette { get; set; }

        /// <summary>
        /// Storage for block ID, as ushort for compatibility, performance and lower memory footprint
        /// For Minecraft 1.13 and greater, all 16 bits are used to store block state ID (0-65535)
        /// </summary>
        private ushort stateId;

        /// <summary>
        /// Id of the block state
        /// </summary>
        public int StateId
        {
            get
            {
                return stateId;
            }
            
            set
            {
                if (value > ushort.MaxValue || value < 0)
                    throw new ArgumentOutOfRangeException("value", "Invalid block ID. Accepted range: 0-65535");
                stateId = (ushort)value;
            }
        }

        /// <summary>
        /// Get a block of the specified block state
        /// </summary>
        /// <param name="stateId">block state</param>
        public Block(ushort stateId)
        {
            this.stateId = stateId;
        }

        public BlockState State
        {
            get {
                return Palette.FromId(StateId);
            }
        }

        public ResourceLocation BlockId
        {
            get {
                return Palette.GetBlock(StateId);
            }
        }

        /// <summary>
        /// String representation of the block
        /// </summary>
        public override string ToString()
        {
            return StateId.ToString();
        }

    }
}
