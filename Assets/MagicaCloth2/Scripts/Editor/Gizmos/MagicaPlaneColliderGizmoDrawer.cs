// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaPlaneColliderのギズモ表示
    /// </summary>
    public class MagicaPlaneColliderGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(MagicaPlaneCollider scr, GizmoType gizmoType)
        {
            ClothEditorManager.RegisterComponent(scr, gizmoType);
        }
    }
}
