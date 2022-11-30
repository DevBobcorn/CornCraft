// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaMeshSpringのギズモ表示
    /// </summary>
    public class MagicaMeshSpringGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaMeshSpring scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0;
            bool allshow = ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysClothShow;

            if (PointSelector.EditEnable)
                return;

            // スプリング球
            if (selected)
                DrawSpringSphere(scr);

            if (ClothMonitorMenu.Monitor == null)
                return;

            // データ整合性チェック
            if (scr.VerifyData() != Define.Error.None)
                return;

            // デフォーマーギズモ
            /*var dcnt = scr.Contents.DeformerCount;
            for (int i = 0; i < dcnt; i++)
            {
                var deformer = scr.Contents.GetDeformer(i);
                if (deformer == null || deformer.IsValidData() == false)
                    continue;

                var datalist = scr.Contents.SpringData.deformerDataList;
                if (i >= datalist.Count)
                    continue;

                var springData = datalist[i];
                if (springData.vertexCount == 0)
                    continue;

                //DeformerGizmoDrawer.DrawDeformerGizmo(deformer, springData);
            }*/

            if (ClothMonitorMenu.Monitor.UI.DrawCloth == false)
                return;

            if ((selected || allshow) == false)
                return;


            // クロスギズモ
            ClothGizmoDrawer.DrawClothGizmo(
                scr,
                scr.ClothData,
                scr.Params,
                scr.Setup,
                scr,
                scr
                );
        }

        /// <summary>
        /// スプリングの範囲球ギズモ
        /// </summary>
        /// <param name="scr"></param>
        static void DrawSpringSphere(MagicaMeshSpring scr)
        {
            var t = scr.CenterTransform;
            if (t == null)
                return;

            Gizmos.color = Color.cyan;
            GizmoUtility.DrawWireSphere(t.position, t.rotation, scr.Params.SpringRadiusScale, scr.Params.SpringRadius, true, true);

            // 軸矢印
            Handles.color = Color.yellow;
            Handles.Slider(t.position, scr.CenterTransformDirection, scr.Params.SpringRadius, Handles.ArrowHandleCap, 1.0f);
        }
    }
}
