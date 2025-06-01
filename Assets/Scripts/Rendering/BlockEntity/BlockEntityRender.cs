using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class BlockEntityRender : MonoBehaviour
    {
        /// <summary>
        /// BlockEntity type
        /// </summary>
        public BlockEntityType Type { get; private set; } = BlockEntityType.DUMMY_BLOCK_ENTITY_TYPE;

        /// <summary>
        /// BlockEntity location
        /// </summary>
        public BlockLoc Location;
        
        #nullable enable
        
        /// <summary>
        /// BlockEntity NBT tags
        /// </summary>
        public Dictionary<string, object>? BlockEntityTags;
        
        #nullable disable

        /// <summary>
        /// A number made from the block entity's location, used in animations to prevent
        /// several renders of a same type moving synchronisedly, which looks unnatural
        /// </summary>
        protected float pseudoRandomOffset = 0F;

        public void Unload()
        {
            if (this)
            {
                Destroy(gameObject);
            }
        }

        public virtual void Initialize(BlockLoc blockLoc, BlockEntityType blockEntityType, Dictionary<string, object> tags)
        {
            Location = blockLoc;
            Type = blockEntityType;
            BlockEntityTags = tags;
        }

        public virtual void ManagedUpdate(float tickMilSec)
        {
            
        }
    }
}