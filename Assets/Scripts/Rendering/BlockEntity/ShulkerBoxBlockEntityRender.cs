using System;
using System.Collections.Generic;
using System.IO;
using CraftSharp.Event;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ShulkerBlockEntityRender : BlockEntityRender
    {
        private static readonly ResourceLocation SHULKER_ID = new("shulker");

#nullable enable

        private Action<BlockActionEvent>? blockActionCallback;

        public Transform? lidTransform;

        private bool isOpened = false;
        private float openness = 0F;

#nullable disable

        public override void Initialize(BlockLoc? blockLoc, BlockState blockState, BlockEntityType blockEntityType, Dictionary<string, object> tags)
        {
            blockActionCallback = e =>
            {
                if (Location is not null && e.BlockLoc == Location)
                {
                    isOpened = e.ActionParam > 0;
                }
            };
            
            EventManager.Instance.Register(blockActionCallback);

            base.Initialize(blockLoc, blockState, blockEntityType, tags);
        }

        private void OnDestroy()
        {
            if (blockActionCallback is not null)
            {
                EventManager.Instance.Unregister(blockActionCallback);
            }
        }

        public override void UpdateBlockState(BlockState blockState)
        {
            if (blockState != BlockState)
            {
                lidTransform = null;
                ClearBedrockBlockEntityRender();

                // The left part of a double chest doesn't need a visual object, the right part will take care of it
                // Update entity render
                var render = BuildBedrockBlockEntityRender(SHULKER_ID);
                    
                render.transform.localScale = Vector3.one;
                render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                lidTransform = render.transform.GetChild(0).GetChild(0); // Get 1st child
                render.transform.localEulerAngles = new(0F, 0F, 0F);
                
                var headTransform = render.transform.GetChild(0).GetChild(1); // Get 3rd child, and hide it
                headTransform.gameObject.SetActive(false);

                var textureName = blockState.BlockId.Path switch
                {
                    "shulker_box"            => "undyed",
                    "white_shulker_box"      => "white",
                    "orange_shulker_box"     => "orange",
                    "magenta_shulker_box"    => "magenta",
                    "light_blue_shulker_box" => "light_blue",
                    "yellow_shulker_box"     => "yellow",
                    "lime_shulker_box"       => "lime",
                    "pink_shulker_box"       => "pink",
                    "gray_shulker_box"       => "gray",
                    "light_gray_shulker_box" => "silver",
                    "cyan_shulker_box"       => "cyan",
                    "purple_shulker_box"     => "purple",
                    "blue_shulker_box"       => "blue",
                    "brown_shulker_box"      => "brown",
                    "green_shulker_box"      => "green",
                    "red_shulker_box"        => "red",
                    "black_shulker_box"      => "black",
                    _                        => throw new InvalidDataException($"What shulker box is {blockState.BlockId}???")
                };
                    
                render.name += $" {blockState}";

                SetBedrockBlockEntityRenderTexture(render, $"shulker/{textureName}");
            }
            
            base.UpdateBlockState(blockState);
        }
        
        public override void ManagedUpdate(float tickMilSec)
        {
            if (lidTransform && Location != null)
            {
                var client = CornApp.CurrentClient;

                if (!client) // Game is not ready, cancel update
                    return;
                
                if (isOpened) // Should open
                {
                    if (openness < 1F)
                    {
                        openness = Mathf.MoveTowards(openness, 1F, Time.deltaTime * 2F);
                        // Move base upwards a bit to make sure bottom face is visible
                        lidTransform.parent.localPosition = new(0F, openness * 0.015625F, 0F);
                        lidTransform.localPosition = new(0F, openness * 0.5F, 0F);
                        lidTransform.localEulerAngles = new(0F, openness * 360F, 0F);
                    }
                }
                else // Should close
                {
                    if (openness > 0F)
                    {
                        openness = Mathf.MoveTowards(openness, 0F, Time.deltaTime * 2F);
                        // Move base upwards a bit to make sure bottom face is visible
                        lidTransform.parent.localPosition = new(0F, openness * 0.015625F, 0F);
                        lidTransform.localPosition = new(0F, openness * 0.5F, 0F);
                        lidTransform.localEulerAngles = new(0F, openness * 360F, 0F);
                    }
                }
            }
        }
    }
}