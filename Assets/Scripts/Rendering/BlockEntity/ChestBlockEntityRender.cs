using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ChestBlockEntityRender : BlockEntityRender
    {
        private static readonly ResourceLocation CHEST_ID = new("chest");
        private static readonly ResourceLocation LARGE_CHEST_ID = new("large_chest");
        private static readonly ResourceLocation ENDER_CHEST_ID = new("ender_chest");
        private static readonly ResourceLocation TRAPPED_CHEST_ID = new("trapped_chest");
        
        public override void UpdateBlockState(BlockState blockState)
        {
            if (blockState != BlockState)
            {
                var isDouble = false;
                var isNotDoubleLeft = true;
                
                if (blockState.Properties.TryGetValue("type", out var typeVal)) // Ender chest doesn't have this property
                {
                    isDouble = typeVal is "left" or "right";
                    isNotDoubleLeft = typeVal != "left";
                }
                
                ClearBedrockBlockEntityRender();

                // The left part of a double chest doesn't need a visual object, the right part will take care of it
                if (isNotDoubleLeft)
                {
                    // Update entity render
                    var render = BuildBedrockBlockEntityRender(isDouble ? LARGE_CHEST_ID : CHEST_ID);
                    
                    render.transform.localScale = Vector3.one;
                    render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;
                    
                    if (blockState.Properties.TryGetValue("facing", out var facingVal))
                    {
                        int rotationDeg = facingVal switch
                        {
                            "north" => 0,
                            "east"  => 90,
                            "south" => 180,
                            "west"  => 270,
                            _       => 0
                        };
                        render.transform.localEulerAngles = new(0F, rotationDeg, 0F);
                    }

                    var textureName = blockState.BlockId == ENDER_CHEST_ID ? "ender" :
                        blockState.BlockId == TRAPPED_CHEST_ID ? "trapped" : "normal";
                    
                    render.name += $" {blockState}";

                    var entityName = isDouble ? "large_chest" : "chest";
                    SetBedrockBlockEntityRenderTexture(render, $"{entityName}/{textureName}");
                }
            }
            
            base.UpdateBlockState(blockState);
        }
    }
}