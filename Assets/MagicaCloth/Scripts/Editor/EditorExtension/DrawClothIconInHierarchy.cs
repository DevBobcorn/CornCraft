// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ヒエラルキーへアイコンの表示
    /// </summary>
    [InitializeOnLoad]
    public class DrawClothIconInHierarchy
    {
        const int iconSize = 16;

        static DrawClothIconInHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIcon;
        }

        static void DrawIcon(int instanceId, Rect rect)
        {
            rect.width = iconSize;
            GameObject obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (obj == null)
                return;
            rect.x += EditorStyles.label.CalcSize(obj.name).x;
            rect.y += 1;
            rect.x += iconSize + 4;
            //rect.x += iconSize + 28;

            foreach (var component in obj.GetComponents<Component>())
            {
                if (component is MagicaRenderDeformer
                    || component is MagicaVirtualDeformer
                    || component is MagicaBoneCloth
                    || component is MagicaMeshCloth
                    || component is MagicaMeshSpring
                    || component is MagicaSphereCollider
                    || component is MagicaCapsuleCollider
                    || component is MagicaPlaneCollider
                    || component is MagicaPhysicsManager
                    || component is MagicaBoneSpring
                    || component is MagicaDirectionalWind
                    || component is MagicaAreaWind
                    || component is MagicaAvatar
                    || component is MagicaAvatarParts
                    )
                {
                    var icon = AssetPreview.GetMiniThumbnail(component);
                    GUI.Label(rect, icon);
                    rect.x += iconSize;
                }
            }
        }
    }

    /// <summary>
    /// テキストのサイズを取得
    /// </summary>
    public static class GUIStyleExtensions
    {
        public static Vector2 CalcSize(this GUIStyle self, string text)
        {
            var content = new GUIContent(text);
            var size = self.CalcSize(content);
            return size;
        }
    }
}
