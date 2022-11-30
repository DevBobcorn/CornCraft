// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaPlaneColliderのギズモ表示
    /// </summary>
    public class MagicaPlaneColliderGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaPlaneCollider scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0;

            if (selected == false)
            {
                if (ClothMonitorMenu.Monitor == null)
                    return;

                if (ClothMonitorMenu.Monitor.UI.AlwaysClothShow == false || ClothMonitorMenu.Monitor.UI.DrawClothCollider == false)
                    return;
                selected = true;
            }

            DrawGizmo(scr, selected);
        }

        public static void DrawGizmo(MagicaPlaneCollider scr, bool selected)
        {
            Gizmos.matrix = scr.transform.localToWorldMatrix;

            var cen = new Vector3(0.0f, scr.Center.y, 0.0f);

            Gizmos.color = selected ? GizmoUtility.ColorCollider : GizmoUtility.ColorNonSelectedCollider;
            Vector3 size = new Vector3(1.0f, 0.0f, 1.0f);
            Gizmos.DrawWireCube(cen, size);
            Gizmos.DrawLine(cen, cen + Vector3.up * 0.1f);

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
