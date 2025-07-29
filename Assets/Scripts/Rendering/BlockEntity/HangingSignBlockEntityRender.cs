using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

using CraftSharp.Protocol.Message;

namespace CraftSharp.Rendering
{
    public class HangingSignBlockEntityRender : BaseSignBlockEntityRender
    {
        private static readonly ResourceLocation HANGING_SIGN_ID = new("hanging_sign");
        private static readonly ResourceLocation WALL_HANGING_SIGN_ID = new("wall_hanging_sign");
        
        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            isItem = isItemPreview;
            
            if (blockState != BlockState)
            {
                var variant = blockState.BlockId.Path;
                bool isWall = variant.EndsWith("_wall_hanging_sign");

                variant = isWall ? variant[..^"_wall_hanging_sign".Length] : variant[..^"_hanging_sign".Length];
                
                centerTransform = transform.GetChild(0); // Get 1st child
                // Destroy previous block entity render, but preserve the text object
                foreach (Transform t in transform)
                {
                    if (t != centerTransform)
                        Destroy(t.gameObject);
                }

                var render = BuildBedrockBlockEntityRender(isWall ? WALL_HANGING_SIGN_ID : HANGING_SIGN_ID);
                
                render.transform.localScale = Vector3.one;
                render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                if (!isWall)
                {
                    bool attached = blockState.Properties.TryGetValue("attached", out var attachedVal) &&
                                    attachedVal == "true";
                    
                    var chainAttached = render.transform.GetChild(1); // Get 2nd child
                    var chainParallel = render.transform.GetChild(2); // Get 3rd child
                    
                    chainAttached.gameObject.SetActive(attached);
                    chainParallel.gameObject.SetActive(!attached);
                }
                
                centerTransform.localPosition = new(0F, -0.1875F, 0F);

                if (centerTransform)
                {
                    if (blockState.Properties.TryGetValue("rotation", out var rotationVal))
                    {
                        float rotationDeg = (short.Parse(rotationVal) + 4) % 16 * 22.5F;
                        centerTransform.localEulerAngles = new(0F, rotationDeg, 0F);
                        render.transform.localEulerAngles = new(0F, rotationDeg + 90, 0F);
                    }
                    else if (blockState.Properties.TryGetValue("facing", out var facingVal))
                    {
                        int rotationDeg = facingVal switch
                        {
                            "north" => 270,
                            "east"  => 0,
                            "south" => 90,
                            "west"  => 180,
                            _       => 0
                        };
                        centerTransform.localEulerAngles = new(0F, rotationDeg, 0F);
                        render.transform.localEulerAngles = new(0F, rotationDeg + 90, 0F);
                    }   
                }
                
                var entityName = isWall ? "wall_hanging_sign" : "hanging_sign";
                SetBedrockBlockEntityRenderTexture(render, $"{entityName}/{variant}");
            }
            
            base.UpdateBlockState(blockState, isItemPreview);

            if (!isItem)
            {
                // Schedule an initial update
                isDirty = true;
            }
        }
    }
}