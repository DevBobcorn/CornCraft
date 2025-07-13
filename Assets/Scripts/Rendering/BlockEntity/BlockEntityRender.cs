using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Resource.BedrockEntity;

namespace CraftSharp.Rendering
{
    public class BlockEntityRender : MonoBehaviour
    {
        protected static readonly Vector3 BEDROCK_BLOCK_ENTITY_OFFSET = new(0F, -0.5F, 0F);
        
        /// <summary>
        /// BlockEntity type
        /// </summary>
        public BlockEntityType Type { get; private set; } = BlockEntityType.DUMMY_BLOCK_ENTITY_TYPE;

        /// <summary>
        /// BlockEntity location
        /// </summary>
        public BlockLoc? Location;
        
        /// <summary>
        /// BlockState at BlockEntity location
        /// </summary>
        protected BlockState BlockState;
        
        #nullable enable
        
        /// <summary>
        /// BlockEntity NBT tags
        /// </summary>
        public Dictionary<string, object>? BlockEntityNBT;
        
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

        public virtual void Initialize(BlockLoc? blockLoc, BlockState blockState, BlockEntityType blockEntityType, Dictionary<string, object> tags)
        {
            Location = blockLoc;
            Type = blockEntityType;
            BlockEntityNBT = tags;
            
            UpdateBlockState(blockState, blockLoc is null);
        }

        public virtual void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            BlockState = blockState;
        }

        protected void ClearBedrockBlockEntityRender()
        {
            // Clear children if present
            foreach (Transform t in transform)
            {
                Destroy(t.gameObject);
            }
        }

        protected BedrockModelEntityRender BuildBedrockBlockEntityRender(ResourceLocation blockEntityModelId)
        {
            var entityResManager = BedrockEntityResourceManager.Instance;
            
            var client = CornApp.CurrentClient;
            if (!client) return null;

            var entityMaterialManager = client.EntityMaterialManager;

            if (entityResManager.EntityRenderDefinitions.TryGetValue(blockEntityModelId, out var blockEntityDef))
            {
                var visualObj = new GameObject("Visual")
                {
                    transform =
                    {
                        parent = transform,
                        localPosition = Vector3.zero
                    }
                };
                
                var blockEntityRender = visualObj.AddComponent<BedrockModelEntityRender>();
                try
                {
                    blockEntityRender.SetDefinitionData(blockEntityDef);
                    blockEntityRender.BuildEntityModel(entityResManager, entityMaterialManager);
                    
                    return blockEntityRender;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"An exception occurred building model for block entity {blockEntityModelId}: {e}");
                }
            }

            return null;
        }

        protected bool SetBedrockBlockEntityRenderTexture(BedrockModelEntityRender render, string textureName)
        {
            for (int i = 0; i < render.TextureNames.Length; i++)
            {
                if (render.TextureNames[i] == textureName)
                {
                    render.SetTexture(i);
                    return true;
                }
            }
            return false;
        }

        public virtual void ManagedUpdate(float tickMilSec)
        {
            
        }
    }
}