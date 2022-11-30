// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaAvatarのギズモ表示
    /// </summary>
    public class MagicaAvatarGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        static void DrawGizmo(MagicaAvatar scr, GizmoType gizmoType)
        {
            //bool selected = (gizmoType & GizmoType.Selected) != 0 || (ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysClothShow);

            //if (scr.VerifyData() != Define.Error.None)
            //{
            //    DrawRootLine(scr);
            //    return;
            //}

            //if (ClothMonitorMenu.Monitor == null)
            //    return;

            //if (selected == false)
            //    return;
        }

        //=========================================================================================
    }
}
