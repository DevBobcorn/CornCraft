using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEditor;

public static class TimeAreaTools
{
    private static MethodInfo applyWireMaterialMethodInfor = null;
    public static void ReflectionApplyWireMaterial()
    {
        if (applyWireMaterialMethodInfor == null)
        {
            Type type = typeof(HandleUtility);
            applyWireMaterialMethodInfor = type.GetMethod(
                "ApplyWireMaterial",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                null,
                new Type[] { },
                null
                );
        }
        applyWireMaterialMethodInfor.Invoke(null, null);
    }

    public static void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
    {
        ReflectionApplyWireMaterial();
        GL.Begin(1);
        GL.Color(col);
        float num = Vector3.Distance(p1, p2);
        int num2 = Mathf.CeilToInt(num / segmentsLength);
        for (int i = 0; i < num2; i += 2)
        {
            GL.Vertex(Vector3.Lerp(p1, p2, (float)i * segmentsLength / num));
            GL.Vertex(Vector3.Lerp(p1, p2, (float)(i + 1) * segmentsLength / num));
        }
        GL.End();
    }
}
