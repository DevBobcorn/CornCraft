// Distant Lands 2022.



using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Audio FX", order = 361)]
    public class AudioFX : FXProfile
    {

        public AudioClip clip;
        private AudioSource runtimeRef;
        public float maximumVolume = 1;

        public override void PlayEffect()
        {
            if (!runtimeRef)
                if (InitializeEffect(VFXMod) == false)
                    return;

            if (runtimeRef.transform.parent == null)
            {
                runtimeRef.transform.parent = VFXMod.particleManager.parent;
                runtimeRef.transform.localPosition = Vector3.zero;
            }


            if (!runtimeRef.isPlaying)
                runtimeRef.Play();
            runtimeRef.volume = maximumVolume * VFXMod.audioManager.volumeMultiplier;

        }

        public override void PlayEffect(float vol)
        {


            if (!runtimeRef)
                if (InitializeEffect(VFXMod) == false)
                    return;

            if (vol <= 0.03f)
            {
                StopEffect();
                return;
            }

            if (runtimeRef.transform.parent == null)
            {
                runtimeRef.transform.parent = VFXMod.particleManager.parent;
                runtimeRef.transform.localPosition = Vector3.zero;
            }

            if (vol != 0)
            {

                if (!runtimeRef.isPlaying && runtimeRef.isActiveAndEnabled)
                    runtimeRef.Play();
                runtimeRef.volume = Mathf.Clamp01(transitionTimeModifier.Evaluate(vol)) * maximumVolume * VFXMod.audioManager.volumeMultiplier;
            }
            else
            {
                Destroy(runtimeRef.gameObject);
                return;
            }
        }

        public override void StopEffect()
        {
            if (!runtimeRef)
                return;

            if (runtimeRef.isPlaying)
                runtimeRef.Stop();
            runtimeRef.volume = 0;
            Destroy(runtimeRef.gameObject);

        }

        public override bool InitializeEffect(VFXModule VFX)
        {

            if (VFX == null)
                VFX = CozyWeather.instance.VFX;

            VFXMod = VFX;

            if (!VFX.audioManager.isEnabled)
            {

                return false;

            }

            runtimeRef = new GameObject().AddComponent<AudioSource>();

            runtimeRef.gameObject.name = name;
            runtimeRef.transform.parent = VFX.audioManager.parent;
            runtimeRef.clip = clip;
            runtimeRef.outputAudioMixerGroup = VFX.audioManager.weatherFXMixer;
            runtimeRef.volume = 0;
            runtimeRef.loop = true;

            return true;

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AudioFX))]
    [CanEditMultipleObjects]
    public class E_AudioFX : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumVolume"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionTimeModifier"));

            serializedObject.ApplyModifiedProperties();

        }

        public override void RenderInWindow(Rect pos)
        {

            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var propPosA = new Rect(pos.x, pos.y + space, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosB = new Rect(pos.x, pos.y + space * 2, pos.width, EditorGUIUtility.singleLineHeight);

            var propPosD = new Rect(pos.x, pos.y + space * 3, pos.width, EditorGUIUtility.singleLineHeight);

            serializedObject.Update();

            EditorGUI.PropertyField(propPosA, serializedObject.FindProperty("clip"));
            EditorGUI.PropertyField(propPosB, serializedObject.FindProperty("maximumVolume"));
            EditorGUI.PropertyField(propPosD, serializedObject.FindProperty("transitionTimeModifier"));

            serializedObject.ApplyModifiedProperties();
        }

        public override float GetLineHeight()
        {

            return 3;

        }

    }
#endif
}