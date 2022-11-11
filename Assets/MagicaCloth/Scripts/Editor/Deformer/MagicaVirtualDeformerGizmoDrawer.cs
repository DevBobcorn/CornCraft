// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaVirtualDeformerのギズモ表示
    /// </summary>
    public class MagicaVirtualDeformerGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaVirtualDeformer scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0 || (ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysDeformerShow);

            if (PointSelector.EditEnable)
                return;
            if (ClothMonitorMenu.Monitor == null)
                return;
            if (ClothMonitorMenu.Monitor.UI.DrawDeformer == false)
                return;

            if (selected == false)
                return;


            // データ整合性チェック
            if (scr.VerifyData() != Define.Error.None)
                return;

            // デフォーマーギズモ
            DeformerGizmoDrawer.DrawDeformerGizmo(scr, scr);
        }
    }
}
