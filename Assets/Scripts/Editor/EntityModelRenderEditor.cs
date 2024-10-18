#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEditor;

using CraftSharp.Molang.Utils;
using CraftSharp.Molang.Runtime.Value;
using CraftSharp.Resource.BedrockEntity;

namespace CraftSharp.Rendering.Editor
{
    [CustomEditor(typeof (BedrockModelEntityRender))]
    public class EntityModelRenderEditor : UnityEditor.Editor
    {
        private static readonly MoPath ANIM_TIME_KEY = new("query.anim_time");
        private int selectedTexture = 0;
        private int selectedAnimation = -1;
        private EntityAnimation? animation = null;

        private Dictionary<MoPath, float> variableTable = new();
        private string animationDescription = string.Empty;

        private readonly static GUILayoutOption[] ANIMATION_DESCRIPTION_LAYOUT_OPTIONS = { GUILayout.Height(270F) };

        public override void OnInspectorGUI()
        {
            var render = (BedrockModelEntityRender) target;

            var texSel = EditorGUILayout.Popup("Texture", selectedTexture, render.TextureNames);
            if (texSel != selectedTexture)
            {
                selectedTexture = texSel;
                render.SetTexture(texSel);
            }

            var animSel = EditorGUILayout.Popup("Animation", selectedAnimation, render.AnimationNames);
            if (animSel != selectedAnimation) // Selection changed
            {
                selectedAnimation = animSel;
                animation = render.SetAnimation(animSel, variableTable.GetValueOrDefault(ANIM_TIME_KEY, 0F));

                if (animation is not null)
                {
                    StringBuilder animationDesc;

                    animationDesc = new($"===========\n* Loop: {animation.Loop}\n* Length: {animation.Length}\n===========\n");
                    animationDesc.AppendLine("* Bones:");

                    HashSet<MoPath> allVariables = new()
                    {
                        // Add query.anim_time anyway
                        ANIM_TIME_KEY
                    };
                    // Then append all variables used in this animation
                    foreach (var pair in animation.BoneAnimations)
                    {
                        pair.Value.Variables.ToList().ForEach(x => allVariables.Add(x));

                        var variables = string.Join(", ", pair.Value.Variables);
                        animationDesc.AppendLine($"   - {pair.Key} ({variables})");
                    }

                    // Update variable values
                    var newTable = new Dictionary<MoPath, float>();
                    foreach (var varName in allVariables)
                    {
                        // Register in inspector
                        newTable.Add(varName, variableTable.GetValueOrDefault(varName, 0F));
                        // Register in entity render's Molang environment
                        render.UpdateMolangValue(varName, MoValue.FromObject(0F));
                    }
                    variableTable = newTable;

                    animationDescription = animationDesc.ToString();
                }
                else
                {
                    animationDescription = "* Selected animation is empty";
                }
            }

            if (selectedAnimation != -1)
            {
                EditorGUILayout.TextArea(
                        $"Selected animation: [{selectedAnimation}] {render.AnimationNames[animSel]}\n\n{animationDescription}",
                        ANIMATION_DESCRIPTION_LAYOUT_OPTIONS);
                
                bool variableUpdated = false;

                EditorGUILayout.LabelField("Variables:");
                foreach (var varName in variableTable.Keys.ToArray())
                {
                    //var newValue = EditorGUILayout.FloatField($"[{varName}]", variableTable[varName]);
                    var newValue = EditorGUILayout.Slider($"[{varName}]", variableTable[varName], -100F, 100F);

                    if (newValue != variableTable[varName])
                    {
                        variableTable[varName] = newValue;
                        render.UpdateMolangValue(varName, MoValue.FromObject(newValue));

                        variableUpdated = true;
                    }
                }

                if (variableUpdated)
                {
                    render.UpdateAnimation(variableTable.GetValueOrDefault(ANIM_TIME_KEY, 0F));
                }
            }
        }
    }
}