using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

using CraftSharp.Protocol.Message;

namespace CraftSharp.Rendering
{
    public class BaseSignBlockEntityRender : BlockEntityRender
    {
        [SerializeField] protected TMP_Text frontTextDisplay;
        [SerializeField] protected TMP_Text backTextDisplay;
        [SerializeField] protected Transform centerTransform;

        private float textUpdateCooldown = 0F;

        protected bool isItem;
        protected bool isDirty = false;
        
        protected bool frontGlowing = false;
        protected CommonColors frontColor = CommonColors.Black;
        protected string[] frontLines = { };
        protected bool backGlowing = false;
        protected CommonColors backColor = CommonColors.Black;
        protected string[] backLines = { };

        protected static string ParseMessage(object message)
        {
            if (message is string s) // Json text component
            {
                s = ChatParser.ParseText(s);
                return s;
            }
            if (message is Dictionary<string, object> d) // NBT text component
            {
                var ss = ChatParser.ParseText(d);
                return ss;
            }

            var sss = message.ToString();
            return sss;
        }

        public string[] GetLines(bool front)
        {
            return front ? frontLines : backLines;
        }
        
        protected void UpdateSignText()
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