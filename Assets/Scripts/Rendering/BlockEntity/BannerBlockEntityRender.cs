using System;
using System.Collections.Generic;
using CraftSharp.Event;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class BannerBlockEntityRender : BlockEntityRender
    {
        private static readonly ResourceLocation BANNER_ID = new("banner");
        private static readonly ResourceLocation WALL_BANNER_ID = new("wall_banner");

#nullable enable

        public Transform? faceTransform;

        private float waveTime = 0F;

#nullable disable

        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            if (blockState != BlockState)
            {
                var isWall = blockState.BlockId.Path.EndsWith("wall_banner");

                faceTransform = null;
                ClearBedrockBlockEntityRender();

                var render = BuildBedrockBlockEntityRender(isWall ? WALL_BANNER_ID : BANNER_ID);
                
                render.transform.localScale = Vector3.one * 0.75F;
                render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                faceTransform = render.transform.GetChild(0); // Get 1st child
                
                if (isItemPreview)
                {
                    render.transform.localEulerAngles = new(0F, 180F, 0F);
                }
                else if (blockState.Properties.TryGetValue("rotation", out var rotationVal))
                {
                    float rotationDeg = (short.Parse(rotationVal) + 8) % 16 * 22.5F;
                    render.transform.localEulerAngles = new(0F, rotationDeg, 0F);
                } else if (blockState.Properties.TryGetValue("facing", out var facingVal))
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
                
                SetBedrockBlockEntityRenderTexture(render, "banner/banner_base");
            }
            
            base.UpdateBlockState(blockState, isItemPreview);
        }
        
        public override void ManagedUpdate(float tickMilSec)
        {
            if (faceTransform && Location != null)
            {
                waveTime += Time.deltaTime;
                faceTransform.localEulerAngles = new(0F, 0F, (Mathf.Sin(waveTime) - 1F) * 5F);
            }
        }
    }
}