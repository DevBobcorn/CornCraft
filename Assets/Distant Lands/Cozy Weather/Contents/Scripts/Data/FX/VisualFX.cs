// Distant Lands 2022.



using System.Collections;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#elif COZY_WEATHER_URP 
using UnityEngine.Rendering;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Post Processing FX", order = 361)]
    public class VisualFX : FXProfile
    {

        public int layer;
        public float priority = 100;
#if UNITY_POST_PROCESSING_STACK_V2
        public PostProcessProfile effectSettings;
        PostProcessVolume _volume;
#elif COZY_WEATHER_URP 
        public VolumeProfile effectSettings;
        Volume _volume;
#endif

        public override void PlayEffect()
        {
            #if UNITY_POST_PROCESSING_STACK_V2 || COZY_WEATHER_URP
            if (!_volume)
                if (!InitializeEffect(VFXMod))
                    return;

            if (_volume.transform.parent == null)
            {
                _volume.transform.parent = VFXMod.postFXManager.parent;
                _volume.transform.localPosition = Vector3.zero;
            }

            _volume.weight = 1;
            #endif
        }

        public override void PlayEffect(float i)
        {
            #if UNITY_POST_PROCESSING_STACK_V2 || COZY_WEATHER_URP
            if (!_volume)
                if (!InitializeEffect(VFXMod))
                    return;

            
            if (i <= 0.03f)
            {
                StopEffect();
                return;
            }

            if (_volume.transform.parent == null)
            {
                _volume.transform.parent = VFXMod.postFXManager.parent;
                _volume.transform.localPosition = Vector3.zero;
            }
            _volume.weight = Mathf.Clamp01(transitionTimeModifier.Evaluate(i));


            if (i == 0)
            {
                Destroy(_volume.gameObject);
                return;
            }
            #endif
        }

        public override void StopEffect() { 
            #if UNITY_POST_PROCESSING_STACK_V2 || COZY_WEATHER_URP
            _volume.weight = 0; Destroy(_volume.gameObject); 
            #endif 
            }


        public override bool InitializeEffect(VFXModule VFX)
        {

            if (VFX == null)
                VFX = CozyWeather.instance.VFX;
                
            #if UNITY_POST_PROCESSING_STACK_V2 || COZY_WEATHER_URP
            VFXMod = VFX;

            if (!VFX.postFXManager.isEnabled)
            {

                return false;

            }

#if UNITY_POST_PROCESSING_STACK_V2
            _volume = new GameObject().AddComponent<PostProcessVolume>();
#elif COZY_WEATHER_URP 
            _volume = new GameObject().AddComponent<Volume>();
#endif
            _volume.gameObject.layer = layer;
            _volume.profile = effectSettings;
            _volume.priority = priority;
            _volume.weight = 0;
            _volume.isGlobal = true;
            _volume.gameObject.name = name;
            _volume.transform.parent = VFX.postFXManager.parent;

            #endif

            return true;
            

        }


    }
#if UNITY_EDITOR
    [CustomEditor(typeof(VisualFX))]
    [CanEditMultipleObjects]
    public class E_VisualFX : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LayerField(new GUIContent("Volume Layer"), serializedObject.FindProperty("layer").intValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("priority"), new GUIContent("Priority"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionTimeModifier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("effectSettings"), new GUIContent("Post Processing Profile"));
            EditorGUILayout.Space();
            if (serializedObject.FindProperty("effectSettings").objectReferenceValue)
                CreateEditor(serializedObject.FindProperty("effectSettings").objectReferenceValue).OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();

        }

        public override void RenderInWindow(Rect pos)
        {

            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var propPosA = new Rect(pos.x, pos.y + space, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosB = new Rect(pos.x, pos.y + space * 2, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosC = new Rect(pos.x, pos.y + space * 3, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosD = new Rect(pos.x, pos.y + space * 4, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosE = new Rect(pos.x, pos.y + space * 5, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosF = new Rect(pos.x, pos.y + space * 6, pos.width, EditorGUIUtility.singleLineHeight);

            serializedObject.Update();

            EditorGUI.LayerField(propPosA, new GUIContent("Volume Layer"), serializedObject.FindProperty("layer").intValue);
            EditorGUI.PropertyField(propPosB, serializedObject.FindProperty("priority"));
            EditorGUI.PropertyField(propPosC, serializedObject.FindProperty("effectSettings"));

            EditorGUI.PropertyField(propPosD, serializedObject.FindProperty("transitionTimeModifier"));
            
            serializedObject.ApplyModifiedProperties();
        }

        public override float GetLineHeight()
        {

            return 4;
            
        }

    }
#endif
}