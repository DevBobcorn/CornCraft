// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaSphereColliderのギズモ表示
    /// </summary>
    public class MagicaSphereColliderGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(MagicaSphereCollider scr, GizmoType gizmoType)
        {
            ClothEditorManager.RegisterComponent(scr, gizmoType);
        }
    }
}
