// Distant Lands 2022.



using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Event FX", order = 361)]
    public class EventFX : FXProfile
    {

        public CozyEventManager events;
        public bool playing;

        public delegate void OnCall();
        public event OnCall onCall;
        public void RaiseOnCall()
        {
            if (onCall != null)
                onCall();
        }
        public delegate void OnEnd();
        public event OnEnd onEnd;
        public void RaiseOnEnd()
        {
            if (onEnd != null)
                onEnd();
        }

        public override void PlayEffect()
        {

            if (!playing)
            {
                playing = true;
                if (onCall != null)
                onCall.Invoke();
            }
        }

        public override void PlayEffect(float intensity)
        {

            if (intensity > 0.5f)
                PlayEffect();
            else
                StopEffect();

        }

        public override void StopEffect()
        {

            if (playing)
            {
                playing = false;
                if (onEnd != null)
                onEnd.Invoke();
            }
        }

        public override bool InitializeEffect(VFXModule VFX)
        {

            if (events == null)
                events = CozyWeather.instance.GetModule<CozyEventManager>();

            VFXMod = VFX;


            return true;

        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EventFX))]
    [CanEditMultipleObjects]
    public class E_EventFX : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("No other properties to adjust! Set events in the Cozy Event Module!", MessageType.Info);

            serializedObject.ApplyModifiedProperties();

        }

        public override void RenderInWindow(Rect pos)
        {

        }

        public override float GetLineHeight()
        {

            return 0;

        }

    }
#endif
}