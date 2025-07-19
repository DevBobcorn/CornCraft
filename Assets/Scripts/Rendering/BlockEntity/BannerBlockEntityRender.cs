using System;
using System.Collections.Generic;
using CraftSharp.Event;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.UI;
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
        private float textureUpdateCooldown = 0F;

        private bool isItem;
        private bool isDirty = false;
        private CommonColors baseColor;

#nullable disable

        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            isItem = isItemPreview;
            
            if (blockState != BlockState)
            {
                var idPath = blockState.BlockId.Path;
                var isWall = idPath.EndsWith("_wall_banner");
                var colorName = isWall ? idPath[..^"_wall_banner".Length] : idPath[..^"_banner".Length];
                
                baseColor = CommonColorsHelper.GetCommonColor(colorName);

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
                
                SetBedrockBlockEntityRenderTexture(render, "banner/banner_base");
            }
            
            base.UpdateBlockState(blockState, isItemPreview);

            if (isItem)
            {
                // Update immediately because data is already present,
                // plus block entity update is not called for items
                UpdateBannerFaceTexture();
            }
            else
            {
                // Schedule an initial update
                isDirty = true;
            }
        }

        private void UpdateBannerFaceTexture()
        {
            var client = CornApp.CurrentClient;
            if (!client || !faceTransform || textureUpdateCooldown > 0F) return;
            
            textureUpdateCooldown = 0.5F;
            isDirty = false;
            
            var patternRecords = new List<BannerPatternRecord>
            {
                // Base pattern is specified by the block/item id
                new(BannerPatternType.BASE_ID, baseColor)
            };

            if (isItem) // Get pattern data from item slot or item entity
            {
                ItemEntityRender itemEntityRender;
                InventoryItemSlot inventoryItemSlot;
                ItemStack itemStack = null;
                
                if (itemEntityRender = GetComponentInParent<ItemEntityRender>())
                {
                    itemStack = itemEntityRender.GetItemStack();
                }
                if (inventoryItemSlot = GetComponentInParent<InventoryItemSlot>())
                {
                    itemStack = inventoryItemSlot.GetItemStack();
                }

                if (itemStack is not null && itemStack.TryGetComponent<BannerPatternsComponent>(
                        StructuredComponentIds.BANNER_PATTERNS_ID, out var bannerPatternsComponent))
                {
                    foreach (var patternData in bannerPatternsComponent.Layers)
                    {
                        // Encoded as enum int (probably as a string)
                        var color = patternData.DyeColor;
                        ResourceLocation patternId;
                        
                        if (patternData.PatternType > 0) // Given as an id
                        {
                            patternId = BannerPatternType.GetIdFromIndex(patternData.PatternType);
                        }
                        else if (patternData.PatternType == 0) // Given as an inline definition
                        {
                            patternId = patternData.AssetId!.Value;
                            var translationKey = patternData.TranslationKey!;
                            var newEntry = new BannerPatternType(patternId, translationKey);
                            BannerPatternPalette.INSTANCE.AddOrUpdateEntry(patternId, newEntry);
                        }
                        else
                        {
                            patternId = ResourceLocation.INVALID;
                            Debug.LogWarning("Unexpected pattern type: " + patternData.PatternType);
                        }
                            
                        patternRecords.Add(new(patternId, color));
                    }
                }
            }
            else // Get pattern data from block entity
            {
                if (BlockEntityNBT is not null)
                {
                    if (BlockEntityNBT.TryGetValue("patterns", out var patterns) && patterns is object[] patternList)
                    {
                        // New format. e.g. "{patterns:{pattern:"right_stripe",color:"white"} }"
                        foreach (Dictionary<string, object> patternData in patternList)
                        {
                            var color = CommonColorsHelper.GetCommonColor((string) patternData["color"]);
                            ResourceLocation patternId;

                            var pattern = patternData["pattern"];
                            if (pattern is string patternStr) // Given as an id
                            {
                                patternId = ResourceLocation.FromString(patternStr);
                            }
                            else if (pattern is Dictionary<string, object> patternDef) // Given as an inline definition
                            {
                                patternId = ResourceLocation.FromString((string) patternDef["asset_id"]);
                                var translationKey = (string) patternDef.GetValueOrDefault("translation_key", string.Empty);
                                var newEntry = new BannerPatternType(patternId, translationKey);
                                BannerPatternPalette.INSTANCE.AddOrUpdateEntry(patternId, newEntry);
                            }
                            else
                            {
                                patternId = ResourceLocation.INVALID;
                                Debug.LogWarning("Unexpected pattern NBT format: " + pattern.GetType().Name);
                            }
                            
                            patternRecords.Add(new(patternId, color));
                        }
                    }
                    else if (BlockEntityNBT.TryGetValue("Patterns", out patterns) && patterns is object[] oldPatternList)
                    {
                        // Old format. e.g. "{Patterns:{Pattern:"rs",Color:0} }"
                        foreach (Dictionary<string, object> patternData in oldPatternList)
                        {
                            // Encoded as enum int (probably as a string)
                            var color = (CommonColors) int.Parse(patternData["Color"].ToString());

                            var patternCode = (string) patternData["Pattern"];
                            var patternId = BannerPatternType.GetIdFromCode(patternCode);
                            
                            patternRecords.Add(new(patternId, color));
                        }
                    }
                    else
                    {
                        //Debug.LogWarning($"Patterns tag not found. Tags: {Json.Object2Json(BlockEntityNBT)}");
                        isDirty = true;
                    }
                }
            }

            var patternSeq = new BannerPatternSequence(patternRecords.ToArray());
            Debug.Log($"Pattern sequence at {Location} {patternSeq}");

            var matManager = client.EntityMaterialManager;
            
            matManager.ApplyBannerTexture(patternSeq, bannerFaceTexture =>
            {
                var faceRenderer = faceTransform.GetComponent<MeshRenderer>();
                
                // Make a copy of the material
                faceRenderer.sharedMaterial = new Material(faceRenderer.sharedMaterial)
                {
                    // And apply generated banner face texture to the material
                    mainTexture = bannerFaceTexture
                };
            });
        }
        
        public override void ManagedUpdate(float tickMilSec)
        {
            if (faceTransform)
            {
                waveTime += Time.deltaTime;
                faceTransform.localEulerAngles = new(0F, 0F, (Mathf.Sin(waveTime) - 1F) * 5F);
                
                textureUpdateCooldown -= Time.deltaTime;
                if (isDirty && textureUpdateCooldown <= 0F)
                {
                    UpdateBannerFaceTexture();
                }
            }
        }
    }
}