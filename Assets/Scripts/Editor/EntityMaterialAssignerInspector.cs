using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CraftSharp.Rendering.Editor
{
    [CustomEditor(typeof (EntityMaterialAssigner))]
    public class EntityMaterialAssignerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var assigner = target as EntityMaterialAssigner;

            // Draw all renderers
            if (GUILayout.Button("Initialize"))
            {
                assigner.InitializeRenderers();
            }

            // Draw default inspector
            base.OnInspectorGUI();
        }
    }
}