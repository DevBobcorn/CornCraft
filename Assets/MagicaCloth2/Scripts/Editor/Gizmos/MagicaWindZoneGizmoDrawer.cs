// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    public class MagicaWindZoneGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(MagicaWindZone scr, GizmoType gizmoType)
        {
            ClothEditorManager.RegisterComponent(scr, gizmoType);
        }
    }
}
