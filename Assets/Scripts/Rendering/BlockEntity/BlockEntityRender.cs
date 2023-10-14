#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class BlockEntityRender : MonoBehaviour
    {
        /// <summary>
        /// BlockEntity type
        /// </summary>
        public BlockEntityType? Type;

        /// <summary>
        /// BlockEntity location
        /// </summary>
        public BlockLoc Location;
        
        /// <summary>
        /// BlockEntity NBT tags
        /// </summary>
        public Dictionary<string, object>? BlockEntityTags;

        /// <summary>
        /// A number made from the block entity's location, used in animations to prevent
        /// several renders of a same type moving synchronisedly, which looks unnatural
        /// </summary>
        protected float pseudoRandomOffset = 0F;

        public void Unload()
        {
            Destroy(this.gameObject);
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