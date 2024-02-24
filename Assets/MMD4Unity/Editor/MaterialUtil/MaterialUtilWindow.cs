using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MMD
{
    public class MaterialUtilWindow : EditorWindow
    {
        private Color DiffuseHigh = new Color32(170, 170, 170, 255);
        private Color DiffuseDark = new Color32(166, 153, 150, 255);

        private readonly Dictionary<FernMaterialCategory, List<Material>> TargetMaterials = new();

        private GameObject target = null;
        private string targetPrefabPath = string.Empty;
        private int currentCategoryMask = 1; // Only category 0 is selected by default

        // KKS Panel Variables
        private bool usingKKSPanel = false;
        private int selectedBakedType = 0;

        [MenuItem("MMD for Unity/Fern Material Util")]
        static void Init()
        {
            var window = GetWindow<MaterialUtilWindow>(false, "Fern Material Util");
            window.Show();
        }

        private List<Material> GetSelectedMaterials()
        {
            return TargetMaterials
                // Find in selected material categories
                .Where(pair => ( currentCategoryMask & ( 1 << (int) pair.Key ) ) != 0 )
                .SelectMany(pair => pair.Value).ToList();
        }

        /// <summary>
        /// This is called when the selected object for edit is changed
        /// </summary>
        /// <param name="newTarget">The new selected object</param>
        void HandleTargetChange(GameObject newTarget)
        {
            target = newTarget;

            if (target == null)
            {
                foreach (var list in TargetMaterials.Values)
                {
                    list.Clear();
                }
            }
            else
            {
                // Update materials for the target
                var renderers = newTarget.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x => (Renderer) x)
                        .Union( newTarget.GetComponentsInChildren<MeshRenderer>().Select(x => (Renderer) x) );
                // Select all materials which uses FernRP shaders
                var materials = renderers.SelectMany(x => x.sharedMaterials).Distinct()
                        .Where( x => x.shader.name.StartsWith(FERN_NPR_SHADER_PREFIX) ).ToArray();

                // Initialize material dictionary
                foreach (FernMaterialCategory type in Enum.GetValues(typeof (FernMaterialCategory)))
                {
                    if (TargetMaterials.ContainsKey(type))
                    {
                        TargetMaterials[type].Clear();
                    }
                    else
                    {
                        TargetMaterials.Add(type, new());
                    }
                }

                if (materials.Length > 0) // Make sure it isn't empty
                {
                    // Populate material dictionary
                    foreach (var material in materials)
                    {
                        var type = FernMaterialTypeUtil.GuessMaterialType(material.name);

                        TargetMaterials[type].Add(material);
                    }
                }

                targetPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(newTarget) ?? "";
                if (targetPrefabPath != "")
                {
                    targetPrefabPath = targetPrefabPath[..targetPrefabPath.LastIndexOf('/')];
                }
            }
        }

        void DetectTargetChange()
        {
            var newTarget = Selection.activeObject as GameObject;

            if (newTarget != target)
            {
                HandleTargetChange(newTarget);

                Repaint();
            }
        }

        void OnFocus()
        {
            DetectTargetChange();
        }

        void OnSelectionChange()
        {
            DetectTargetChange();
        }

        private const string FERN_NPR_SHADER_PREFIX = "FernRender/URP/FERNNPR";

        private readonly Dictionary<string, string> FERN_DIFFUSE_KEYWORDS = new()
                {
                    ["_CELLSHADING"]     = "Cel ",
                    ["_RAMPSHADING"]     = "Ramp",
                    ["_CELLBANDSHADING"] = "CelBand",
                    ["_LAMBERTIAN"]      = "Lambert",
                    ["_SDFFACE"]         = "SDF Face"
                };
        
        private readonly Dictionary<string, string> FERN_SPECULAR_KEYWORDS = new()
                {
                    ["_GGX"]          = "PBR-GGX",
                    ["_STYLIZED"]     = "Ramp",
                    ["_BLINNPHONG"]   = "Blinn-Phong",
                    ["_KAJIYAHAIR"]   = "Anisotropy",
                    ["_ANGLERING"]    = "AngleRing"
                };

        private string GetKeywordType(Dictionary<string, string> dictionary, Material material)
        {
            foreach (var keyword in material.shaderKeywords)
            {
                if (dictionary.ContainsKey(keyword))
                {
                    return dictionary[keyword];
                }
            }

            return "None";
        }

        private static readonly Color CATEGORY_NAME_COLOR = Color.cyan;
        private static readonly Color SELECTED_NAME_COLOR = Color.white * 1.5F;
        private static readonly Color SELECTED_SLOT_COLOR = Color.white * 1.8F; // Man, it really works!

        private void DrawMaterialList(FernMaterialCategory type, List<Material> materials, bool selected, bool drawHeader)
        {
            var oldColor = GUI.color;

            if (!drawHeader)
            {
                GUILayout.BeginHorizontal();
                    if (selected) GUI.color = SELECTED_NAME_COLOR;
                    EditorGUILayout.LabelField($"> {type}", EditorStyles.boldLabel);
                    if (selected) GUI.color = oldColor;
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                    if (selected) GUI.color = SELECTED_NAME_COLOR;
                    EditorGUILayout.LabelField($"> {type}", EditorStyles.boldLabel, GUILayout.Width(135));
                    GUI.color = CATEGORY_NAME_COLOR;
                    EditorGUILayout.LabelField($"[Category]", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"[Diffuse]",  EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"[Specular]", EditorStyles.boldLabel, GUILayout.Width(105));
                    GUI.color = oldColor;
                GUILayout.EndHorizontal();
            }

            if (!materials.Any())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("No FernRP material found in this category");
                GUILayout.EndHorizontal();
            }
            else
            {
                bool breakNext = false;
                
                // Draw each material in the list
                foreach (var material in materials)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    // Draw material object
                    GUI.enabled = false;
                    if (selected) GUI.color = SELECTED_SLOT_COLOR;
                    EditorGUILayout.ObjectField(material, typeof (Material), true, GUILayout.Width(120));
                    if (selected) GUI.color = oldColor;
                    GUI.enabled = true;

                    // Draw material category dropdown
                    var newType = (FernMaterialCategory) EditorGUILayout.EnumPopup(type, GUILayout.Width(60));
                    if (newType != type)
                    {
                        // Move this material to another category
                        TargetMaterials[type].Remove(material);
                        TargetMaterials[newType].Add(material);
                        breakNext = true;
                    }

                    // Draw shader name (without the leading "FernRender/URP/FERNNPR" prefix)
                    var shaderName = material.shader.name[15..];
                    EditorGUILayout.LabelField(new GUIContent(shaderName[7..9], shaderName), EditorStyles.boldLabel, GUILayout.Width(15));

                    GUILayout.Space(5); // ============================================================================

                    // Draw diffuse info
                    var diffuseName = GetKeywordType(FERN_DIFFUSE_KEYWORDS, material);
                    EditorGUILayout.LabelField(new GUIContent(diffuseName[..4], diffuseName), GUILayout.Width(30));
                    GUI.enabled = false;
                    DrawColorFieldWithoutLabel(material.GetColor("_HighColor"), false,  true, GUILayout.Width(20));
                    DrawColorFieldWithoutLabel(material.GetColor("_DarkColor"), false, false, GUILayout.Width(20));
                    GUI.enabled = true;

                    GUILayout.Space(5); // ============================================================================

                    // Draw specular info
                    var specularName = GetKeywordType(FERN_SPECULAR_KEYWORDS, material);
                    
                    if (specularName != "None")
                    {
                        EditorGUILayout.LabelField(new GUIContent(specularName[..4], specularName), GUILayout.Width(30));
                        DrawColorFieldWithoutLabel(material.GetColor("_SpecularColor"), false, false, GUILayout.Width(20));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(new GUIContent(specularName[..4], specularName), GUILayout.Width(50));
                    }

                    GUILayout.EndHorizontal();

                    if (breakNext) break;
                }
            }
        }

        private void ApplyDiffuseColors(List<Material> materials, Color highColor, Color darkColor)
        {
            foreach (var material in materials)
            {
                material.SetColor("_HighColor", highColor);
                material.SetColor("_DarkColor", darkColor);
            }
        }

        private Vector2 scrollPos = Vector2.zero;

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            
                EditorGUILayout.LabelField("Target Selection", EditorStyles.boldLabel);

                GUI.enabled = false; // Draw the object to make it clear who's the current target

                EditorGUILayout.ObjectField("Target", target, typeof (GameObject), true);

                GUI.enabled = true;

                GUILayout.Space(10);

                if (target == null)
                {
                    GUILayout.Label("Please select a valid character object!");
                }
                else
                {
                    EditorGUILayout.LabelField("Material lists (categories are inferred and are not saved)", EditorStyles.boldLabel);

                    var drawHeader = true;

                    // Draw material lists
                    foreach (var (cat, list) in TargetMaterials)
                    {
                        DrawMaterialList(cat, list, ( currentCategoryMask & (1 << (int) cat) ) != 0, drawHeader);
                        // Header is drawn only once
                        drawHeader = false;
                    }

                    GUILayout.Space(10);

                    EditorGUILayout.LabelField($"Edit categories", EditorStyles.boldLabel);

                    int catIndex = 0;

                    foreach (FernMaterialCategory cat in Enum.GetValues(typeof (FernMaterialCategory)))
                    {
                        if (catIndex % 2 == 0) GUILayout.BeginHorizontal();
                        GUILayout.Space(10);

                        var selected = ( currentCategoryMask & (1 << (int) cat) ) != 0;
                        var nowSelected = EditorGUILayout.Toggle(cat.ToString(), selected);

                        if (nowSelected != selected)
                        {
                            currentCategoryMask ^= 1 << (int) cat; // Invert this bit
                        }
                        
                        if (catIndex % 2 == 1) GUILayout.EndHorizontal();

                        catIndex++;
                    }

                    // In case where the horizontal bar is not ended
                    if (catIndex % 2 != 0) GUILayout.EndHorizontal();
                    
                    GUILayout.Space(10);

                    EditorGUILayout.LabelField("Diffuse Colors", EditorStyles.boldLabel);

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.BeginVertical();

                        GUILayout.BeginHorizontal();
                            DiffuseHigh = DrawColorField("Lit Color",  DiffuseHigh, true, true );
                            DiffuseDark = DrawColorField("Dark Color", DiffuseDark, true, false);
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button("Apply diffuse colors to selected categories"))
                        {
                            ApplyDiffuseColors(GetSelectedMaterials(), DiffuseHigh, DiffuseDark);
                        }

                        usingKKSPanel = EditorGUILayout.Toggle("Expand KKS Panel", usingKKSPanel);

                        if (usingKKSPanel)
                        {
                            FernMaterialForKKS.DrawGUI(targetPrefabPath, ref selectedBakedType, TargetMaterials);
                        }

                        GUILayout.Space(20);

                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }
    
        #region GUI Util functions

        // A shared content for displaying texts without having to create new content objects
        private static readonly GUIContent tempContent = new();

        private static GUIContent TempContent(string text)
        {
            tempContent.tooltip = null;
            tempContent.text = text;
            tempContent.image = null;
            return tempContent;
        }

        private static Color DrawColorField(string label, Color value, bool showEyedropper, bool hdr, params GUILayoutOption[] options)
        {
            return EditorGUI.ColorField(EditorGUILayout.GetControlRect(hasLabel: true, 18f,
                    EditorStyles.colorField, options), TempContent(label), value, showEyedropper, showAlpha: false, hdr);
        }

        private static Color DrawColorFieldWithoutLabel(Color value, bool showEyedropper, bool hdr, params GUILayoutOption[] options)
        {
            return EditorGUI.ColorField(EditorGUILayout.GetControlRect(hasLabel: false, 18f,
                    EditorStyles.colorField, options), GUIContent.none, value, showEyedropper, showAlpha: false, hdr);
        }

        #endregion
    }
}
