using UnityEngine;

namespace CraftSharp.Rendering
{
    public class SignBlockEntityRender : BlockEntityRender
    {
#nullable enable

        public Transform? centerTransform;

        private float textUpdateCooldown = 0F;

        private bool isItem;
        private bool isDirty = false;

#nullable disable

        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            isItem = isItemPreview;
            
            Debug.Log($"New: {blockState}, Old: {BlockState}");
            
            if (blockState != BlockState)
            {
                centerTransform = transform.GetChild(0); // Get 1st child
                
                Debug.Log($"Center: {centerTransform}");

                if (centerTransform)
                {
                    if (blockState.Properties.TryGetValue("rotation", out var rotationVal))
                    {
                        float rotationDeg = (short.Parse(rotationVal) + 4) % 16 * 22.5F;
                        centerTransform.localEulerAngles = new(0F, rotationDeg, 0F);
                        centerTransform.localPosition = new(0F, 0.375F, 0F);
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
                            "north" => new Vector3( 0.475F, 0.041F, 0F),
                            "east"  => new Vector3(0F, 0.041F, -0.475F),
                            "south" => new Vector3(-0.475F, 0.041F, 0F),
                            "west"  => new Vector3(0F, 0.041F,  0.475F),
                            _       => new Vector3(0F, 0.041F, 0F)
                        };
                    }   
                }
            }
            
            base.UpdateBlockState(blockState, isItemPreview);

            if (!isItem)
            {
                // Schedule an initial update
                isDirty = true;
            }
        }

        private void UpdateSignText()
        {
            var client = CornApp.CurrentClient;
            if (!client || !centerTransform || textUpdateCooldown > 0F) return;
            
            textUpdateCooldown = 0.5F;
            isDirty = false;
            
            if (!isItem)
            {
                if (BlockEntityNBT is not null)
                {
                    /*
                    if (BlockEntityNBT.TryGetValue("Patterns", out patterns) && patterns is object[] oldPatternList)
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
                    */
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