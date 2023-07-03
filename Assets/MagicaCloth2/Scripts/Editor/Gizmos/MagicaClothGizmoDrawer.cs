// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネントのギズモ表示
    /// </summary>
    public class MagicaClothGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(MagicaCloth cloth, GizmoType gizmoType)
        {
            ClothEditorManager.RegisterComponent(cloth, gizmoType);
        }
    }
}
