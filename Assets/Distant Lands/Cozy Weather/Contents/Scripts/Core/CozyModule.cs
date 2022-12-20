//Distant Lands 2022.

//Empty COZY: Weather Module that contains all necessary references and is used as a base class for all subsequent modules.



using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;


namespace DistantLands.Cozy
{

    public abstract class CozyModule : MonoBehaviour
    {


        [HideInInspector]
        public CozyWeather weatherSphere;

        public virtual void SetupModule()
        {

            if (!enabled)
                return;
            weatherSphere = CozyWeather.instance;



        }


        private void OnDisable()
        {
            DisableModule();
        }

        public virtual void DisableModule()
        {


        }

    }

#if UNITY_EDITOR
    public class E_CozyModule : Editor
    {



        void OnEnable()
        {


        }

        public virtual GUIContent GetGUIContent()
        {

            return new GUIContent();

        }


        public override void OnInspectorGUI()
        {


        }

        public virtual void DisplayInCozyWindow()
        {


            serializedObject.Update();



            serializedObject.ApplyModifiedProperties();


        }

    }
#endif
}