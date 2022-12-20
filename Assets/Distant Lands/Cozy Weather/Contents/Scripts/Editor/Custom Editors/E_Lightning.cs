using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy.EditorScripts
{
    [CustomEditor(typeof(CozyThunder))]
    [CanEditMultipleObjects]
    public class E_thunder : Editor
    {

        CozyThunder cozythunder;




        void OnEnable()
        {

            cozythunder = (CozyThunder)target;


        }

        public override void OnInspectorGUI()
        {

            
            if (cozythunder == null)
                if (target)
                    cozythunder = (CozyThunder)target;
                else
                    return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ThunderSounds"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ThunderDelayRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LightIntensity"));
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

        }
    }
}