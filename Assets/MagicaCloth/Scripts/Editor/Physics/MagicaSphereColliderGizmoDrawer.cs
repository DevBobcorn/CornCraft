// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaSphereColliderのギズモ表示
    /// </summary>
    public class MagicaSphereColliderGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaSphereCollider scr, GizmoType gizmoType)
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

        public static void DrawGizmo(MagicaSphereCollider scr, bool selected)
        {
            if (scr == null)
                return;
            Gizmos.color = selected ? GizmoUtility.ColorCollider : GizmoUtility.ColorNonSelectedCollider;
            GizmoUtility.DrawWireSphere(
                //scr.transform.position,
                scr.transform.TransformPoint(scr.Center),
                scr.transform.rotation,
                Vector3.one * scr.GetScale(),
                scr.Radius,
                true,
                true
                );
        }
    }
}
