// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaMeshClothのギズモ表示
    /// </summary>
    public class MagicaMeshClothGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaMeshCloth scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0 || (ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysClothShow);

            if (ClothMonitorMenu.Monitor == null)
                return;

            if (selected == false)
                return;


            // 常に表示
            ClothGizmoDrawer.AlwaysDrawClothGizmo(scr, scr.Params);

            // デフォーマー状態表示
            //if (scr.Deformer != null && scr.Deformer.VerifyData())
            //{
            //    if (PointSelector.EditEnable == false)
            //    {
            //        // デフォーマーギズモ
            //        DeformerGizmoDrawer.DrawDeformerGizmo(scr.Deformer, scr, 0.01f);
            //    }
            //}

            if (scr.VerifyData() == Define.Error.None)
            {
                // クロスギズモ
                if (PointSelector.EditEnable == false)
                {
                    ClothGizmoDrawer.DrawClothGizmo(scr, scr.ClothData, scr.Params, scr.Setup, scr, scr);
                }
            }
        }
    }
}
