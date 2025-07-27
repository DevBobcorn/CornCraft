using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

using CraftSharp.Protocol.Message;

namespace CraftSharp.Rendering
{
    public class SignBlockEntityRender : BlockEntityRender
    {
        private static readonly ResourceLocation SIGN_ID = new("sign");
        private static readonly ResourceLocation WALL_SIGN_ID = new("wall_sign");
        
        [SerializeField] private TMP_Text frontTextDisplay;
        [SerializeField] private TMP_Text backTextDisplay;
        [SerializeField] private Transform centerTransform;

        private float textUpdateCooldown = 0F;

        private bool isItem;
        private bool isDirty = false;
        
        private bool frontGlowing = false;
        private CommonColors frontColor = CommonColors.Black;
        private string[] frontLines = { };
        private bool backGlowing = false;
        private CommonColors backColor = CommonColors.Black;
        private string[] backLines = { };

        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            isItem = isItemPreview;
            
            if (blockState != BlockState)
            {
                var variant = blockState.BlockId.Path;
                bool isHanging = false, isWall = false;

                if (variant.EndsWith("_wall_hanging_sign"))
                {
                    variant = variant[..^"_wall_hanging_sign".Length];
                    isHanging = true;
                    isWall = true;
                }
                else if (variant.EndsWith("_hanging_sign"))
                {
                    variant = variant[..^"_hanging_sign".Length];
                    isHanging = true;
                }
                else if (variant.EndsWith("_wall_sign"))
                {
                    variant = variant[..^"_wall_sign".Length];
                    isWall = true;
                }
                else if (variant.EndsWith("_sign")) variant = variant[..^"_sign".Length];
                
                centerTransform = transform.GetChild(0); // Get 1st child
                // Destroy previous block entity render, but preserve the text object
                foreach (Transform t in transform)
                {
                    if (t != centerTransform)
                        Destroy(t.gameObject);
                }

                var render = BuildBedrockBlockEntityRender(isWall ? WALL_SIGN_ID : SIGN_ID);
                
                render.transform.localScale = Vector3.one * 2F / 3F;
                render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                if (centerTransform)
                {
                    if (blockState.Properties.TryGetValue("rotation", out var rotationVal))
                    {
                        float rotationDeg = (short.Parse(rotationVal) + 4) % 16 * 22.5F;
                        centerTransform.localEulerAngles = new(0F, rotationDeg, 0F);
                        centerTransform.localPosition = new(0F, 0.33334F, 0F);
                        
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
                        centerTransform.localPosition = facingVal switch
                        {
                            "north" => new Vector3( 0.4375F, 0F, 0F),
                            "east"  => new Vector3(0F, 0F, -0.4375F),
                            "south" => new Vector3(-0.4375F, 0F, 0F),
                            "west"  => new Vector3(0F, 0F,  0.4375F),
                            _       => new Vector3(0F, 0F, 0F)
                        };
                        
                        render.transform.localEulerAngles = new(0F, rotationDeg + 90, 0F);
                    }   
                }
                
                var entityName = isHanging ? (isWall ? "wall_hanging_sign" : "hanging_sign") : (isWall ? "wall_sign" : "sign");
                SetBedrockBlockEntityRenderTexture(render, $"{entityName}/{variant}");
            }
            
            base.UpdateBlockState(blockState, isItemPreview);

            if (!isItem)
            {
                // Schedule an initial update
                isDirty = true;
            }
        }

        private static string ParseMessage(object message)
        {
            if (message is string s) // Plain text
            {
                return s;
            }
            if (message is Dictionary<string, object> d) // NBT text component
            {
                return ChatParser.ParseText(d);
            }

            return message.ToString();
        }

        private void UpdateSignText()
        {
            var client = CornApp.CurrentClient;
            if (!client || textUpdateCooldown > 0F) return;
            
            textUpdateCooldown = 0.5F;
            isDirty = false;
            
            if (!isItem)
            {
                if (BlockEntityNBT is not null)
                {
                    bool textFound = false;
                    
                    if (BlockEntityNBT.TryGetValue("front_text", out var frontText)
                        && frontText is Dictionary<string, object> frontTextObj)
                    {
                        frontLines = ((object[]) frontTextObj["messages"])
                            .Select(ParseMessage).ToArray();
                        frontColor = frontTextObj.TryGetValue("color", out var fc)
                                     ? CommonColorsHelper.GetCommonColor((string) fc)
                                     : CommonColors.Black;
                        frontGlowing = frontTextObj.TryGetValue("has_glowing_text", out var fg)
                                       && byte.Parse(fg.ToString()) > 0;
                        
                        textFound = true;
                    }
                    
                    if (BlockEntityNBT.TryGetValue("back_text", out var backText)
                        && backText is Dictionary<string, object> backTextObj)
                    {
                        backLines = ((object[]) backTextObj["messages"])
                            .Select(ParseMessage).ToArray();
                        backColor = backTextObj.TryGetValue("color", out var bc)
                                    ? CommonColorsHelper.GetCommonColor((string) bc)
                                    : CommonColors.Black;
                        backGlowing = backTextObj.TryGetValue("has_glowing_text", out var bg)
                                      && byte.Parse(bg.ToString()) > 0;
                        
                        textFound = true;
                    }

                    if (BlockEntityNBT.TryGetValue("Text1", out var text1))
                    {
                        // Text components encoded as json string
                        frontLines = new[]
                        {
                            ChatParser.ParseText((string) text1),
                            ChatParser.ParseText((string) BlockEntityNBT["Text2"]),
                            ChatParser.ParseText((string) BlockEntityNBT["Text3"]),
                            ChatParser.ParseText((string) BlockEntityNBT["Text4"])
                        };
                        frontColor = BlockEntityNBT.TryGetValue("Color", out var c)
                                     ? CommonColorsHelper.GetCommonColor((string) c)
                                     : CommonColors.Black;
                        frontGlowing = BlockEntityNBT.TryGetValue("GlowingText", out var g)
                                       && byte.Parse(g.ToString()) > 0;
                        
                        textFound = true;
                    }

                    if (!textFound) // Come back later
                    {
                        isDirty = true;
                        frontTextDisplay.text = string.Empty;
                        backTextDisplay.text = string.Empty;
                    }
                    else
                    {
                        string frontFull = string.Join("\n", frontLines);
                        frontTextDisplay.text = $"<color=#{ColorConvert.GetHexRGBString(frontColor.GetColor32())}>{frontFull}</color>";
                
                        string backFull = string.Join("\n", backLines);
                        backTextDisplay.text = $"<color=#{ColorConvert.GetHexRGBString(backColor.GetColor32())}>{backFull}</color>";
                    }
                }
            }
        }
        
        public override void ManagedUpdate(float tickMilSec)
        {
            if (centerTransform)
            {
                textUpdateCooldown -= Time.deltaTime;
                if (isDirty && textUpdateCooldown <= 0F)
                {
                    UpdateSignText();
                }
            }
        }
    }
}