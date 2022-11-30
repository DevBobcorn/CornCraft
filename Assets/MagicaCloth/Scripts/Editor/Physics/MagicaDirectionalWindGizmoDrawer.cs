// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaDirectionalWindのギズモ表示
    /// </summary>
    public class MagicaDirectionalWindGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        //static void DrawGizmo(MagicaDirectionalWind scr, GizmoType gizmoType)
        static void DrawGizmo(WindComponent scr, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0 || (ClothMonitorMenu.Monitor != null && ClothMonitorMenu.Monitor.UI.AlwaysWindShow);

            if (selected == false)
                return;

            if (ClothMonitorMenu.Monitor == null || ClothMonitorMenu.Monitor.UI.DrawWind)
                DrawWindGizmo(scr, selected);
        }

        private static void DrawWindGizmo(WindComponent scr, bool selected)
        {
            Gizmos.matrix = scr.transform.localToWorldMatrix;

            var size = scr.GetAreaSize();

            // エリア
            if (scr.GetWindType() == PhysicsManagerWindData.WindType.Area)
            {
                //Color areaCol = Color.white;
                Gizmos.color = Color.white;
                switch (scr.GetShapeType())
                {
                    case PhysicsManagerWindData.ShapeType.Box:
                        Gizmos.DrawWireCube(Vector3.zero, size * 2);
                        break;
                    case PhysicsManagerWindData.ShapeType.Sphere:
                        Gizmos.DrawWireSphere(Vector3.zero, size.x);
                        // ４５度回転させてもう一度
                        Gizmos.matrix = Gizmos.matrix * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 45, 0), Vector3.one);
                        Gizmos.DrawWireSphere(Vector3.zero, size.x);
                        Gizmos.matrix = scr.transform.localToWorldMatrix;
                        break;
                }
            }

            // メイン方向
            //Gizmos.color = GizmoUtility.ColorWind;
            Gizmos.color = Color.yellow;
            var pos = scr.transform.position;
            Vector3 offset = Vector3.zero;
            // Anchor
            //if (scr.GetWindType() == PhysicsManagerWindData.WindType.Area)
            //{
            //    offset = Vector3.Scale(size, scr.GetAnchor());
            //    pos += scr.transform.TransformDirection(offset);
            //}
            var rot = MathUtility.AxisQuaternion(scr.MainDirection);
            float gsize = 0.5f;
            switch (scr.GetDirectionType())
            {
                case PhysicsManagerWindData.DirectionType.OneDirection:
                    GizmoUtility.DrawWireArrow(pos, rot, new Vector3(gsize, gsize, gsize * 2), true);
                    break;
                case PhysicsManagerWindData.DirectionType.Radial:
                    //Gizmos.DrawWireCube(offset, Vector3.one * 0.5f);
                    Gizmos.DrawLine(new Vector3(0, -gsize, 0), new Vector3(0, gsize, 0));
                    Gizmos.DrawLine(new Vector3(-gsize, 0, 0), new Vector3(gsize, 0, 0));
                    Gizmos.DrawLine(new Vector3(0, 0, -gsize), new Vector3(0, 0, gsize));
                    break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
