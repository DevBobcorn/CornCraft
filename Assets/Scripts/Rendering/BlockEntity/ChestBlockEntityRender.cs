using System;
using System.Collections.Generic;
using CraftSharp.Event;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ChestBlockEntityRender : BlockEntityRender
    {
        private static readonly ResourceLocation CHEST_ID = new("chest");
        private static readonly ResourceLocation CHEST_LEFT_ID = new("chest_left");
        private static readonly ResourceLocation CHEST_RIGHT_ID = new("chest_right");
        private static readonly ResourceLocation ENDER_CHEST_ID = new("ender_chest");
        private static readonly ResourceLocation TRAPPED_CHEST_ID = new("trapped_chest");

#nullable enable

        private Action<BlockActionEvent>? blockActionCallback;

        public Transform? lidTransform;

        private bool isOpened = false;
        private float openAngle = 0F;

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

        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            if (blockState != BlockState)
            {
                ResourceLocation blockEntityRenderId;
                string entityName;
                
                if (blockState.Properties.TryGetValue("type", out var typeVal)) // Ender chest doesn't have this property
                {
                    switch (typeVal)
                    {
                        case "left":
                            blockEntityRenderId = CHEST_LEFT_ID;
                            entityName = "chest_left";
                            break;
                        case "right":
                            blockEntityRenderId = CHEST_RIGHT_ID;
                            entityName = "chest_right";
                            break;
                        default:
                            blockEntityRenderId = CHEST_ID;
                            entityName = "chest";
                            break;
                    }
                }
                else // Ender chest, etc.
                {
                    blockEntityRenderId = CHEST_ID;
                    entityName = "chest";
                }

                lidTransform = null;
                ClearBedrockBlockEntityRender();

                // Update entity render
                var render = BuildBedrockBlockEntityRender(blockEntityRenderId);
                
                render.transform.localScale = Vector3.one;
                render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                lidTransform = render.transform.GetChild(1); // Get 2nd child

                if (isItemPreview)
                {
                    render.transform.localEulerAngles = new(0F, 180F, 0F);
                }
                else if (blockState.Properties.TryGetValue("facing", out var facingVal))
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
                    GetConnectableChestTextureVariant(blockState.BlockId);
                
                render.name += $" {blockState}";

                SetBedrockBlockEntityRenderTexture(render, $"{entityName}/{textureName}");
            }
            
            base.UpdateBlockState(blockState, isItemPreview);
        }

        private static string GetConnectableChestTextureVariant(ResourceLocation blockId)
        {
            DateTime today = DateTime.Today;

            if (today is { Month: 12, Day: >= 24 and <= 26 }) return "christmas";
            
            return blockId == TRAPPED_CHEST_ID ? "trapped" : "normal";
        }
        
        public override void ManagedUpdate(float tickMilSec)
        {
            if (lidTransform && Location != null)
            {
                if (isOpened) // Should open
                {
                    if (Mathf.DeltaAngle(-90F, openAngle) != 0)
                    {
                        openAngle = Mathf.MoveTowardsAngle(openAngle, -90F, 180F * Time.deltaTime);
                        lidTransform.localEulerAngles = new(0F, 0F, openAngle);
                    }
                }
                else // Should close
                {
                    if (Mathf.DeltaAngle(0F, openAngle) != 0)
                    {
                        openAngle = Mathf.MoveTowardsAngle(openAngle, 0F, 180F * Time.deltaTime);
                        lidTransform.localEulerAngles = new(0F, 0F, openAngle);
                    }
                }
            }
        }
    }
}