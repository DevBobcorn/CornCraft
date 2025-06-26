using System;
using System.Collections.Generic;
using CraftSharp.Event;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ChestBlockEntityRender : BlockEntityRender
    {
        private static readonly ResourceLocation CHEST_ID = new("chest");
        private static readonly ResourceLocation LARGE_CHEST_ID = new("large_chest");
        private static readonly ResourceLocation ENDER_CHEST_ID = new("ender_chest");
        private static readonly ResourceLocation TRAPPED_CHEST_ID = new("trapped_chest");

#nullable enable

        private Action<InventoryOpenEvent>? inventoryOpenCallback;
        private Action<InventoryCloseEvent>? inventoryCloseCallback;

        // Used when this chest is open by this client
        private int openedInventoryId = -1;
        // Count of players using this chest
        public int openCount = 0;

        public Transform? lidTransform;

#nullable disable

        public override void Initialize(BlockLoc blockLoc, BlockState blockState, BlockEntityType blockEntityType, Dictionary<string, object> tags)
        {
            inventoryOpenCallback = e =>
            {
                if (e.BlockLoc == Location)
                {
                    openedInventoryId = e.InventoryId;

                    openCount++;
                    Debug.Log($"Open chest at {Location}: {e.InventoryId}, Open count: {openCount}");
                }
            };

            inventoryCloseCallback = e =>
            {
                if (openedInventoryId == e.InventoryId)
                {
                    openCount--;
                    Debug.Log($"Close chest at {Location}: {e.InventoryId}, Close count: {openCount}");
                }
                openedInventoryId = -1;
            };
            
            EventManager.Instance.Register(inventoryOpenCallback);
            EventManager.Instance.Register(inventoryCloseCallback);

            base.Initialize(blockLoc, blockState, blockEntityType, tags);
        }

        private void OnDestroy()
        {
            if (inventoryOpenCallback is not null)
            {
                EventManager.Instance.Unregister(inventoryOpenCallback);
            }

            if (inventoryCloseCallback is not null)
            {
                EventManager.Instance.Unregister(inventoryCloseCallback);
            }
        }

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

                lidTransform = null;
                ClearBedrockBlockEntityRender();

                // The left part of a double chest doesn't need a visual object, the right part will take care of it
                if (isNotDoubleLeft)
                {
                    // Update entity render
                    var render = BuildBedrockBlockEntityRender(isDouble ? LARGE_CHEST_ID : CHEST_ID);
                    
                    render.transform.localScale = Vector3.one;
                    render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                    lidTransform = render.transform.GetChild(1); // Get 2nd child
                    
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
        
        public override void ManagedUpdate(float tickMilSec)
        {
            if (lidTransform)
            {
                var curAngle = lidTransform.localEulerAngles.z;
                
                if (openCount > 0) // Should open
                {
                    if (Mathf.DeltaAngle(-90F, curAngle) != 0)
                    {
                        curAngle = Mathf.MoveTowardsAngle(curAngle, -90F, 180F * Time.deltaTime);
                    }
                    lidTransform.localEulerAngles = new(0F, 0F, curAngle);
                }
                else // Should close
                {
                    if (Mathf.DeltaAngle(0F, curAngle) != 0)
                    {
                        curAngle = Mathf.MoveTowardsAngle(curAngle, 0F, 180F * Time.deltaTime);
                    }
                    lidTransform.localEulerAngles = new(0F, 0F, curAngle);
                }
            }
        }
    }
}