using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    public class ExampleModule : CozyModule
    {

        /* __________________________________________________________________
        
        
        This script shows an example of an empty module that you can use as a 
        base for creating your own custom modules! 
        
        _____________________________________________________________________*/



    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ExampleModule))]
    [CanEditMultipleObjects]
    public class E_ExampleModule : E_CozyModule
    {


        public override GUIContent GetGUIContent()
        {

            //Place your module's GUI content here.
            return new GUIContent("    Example Module", (Texture)Resources.Load("MoreOptions"), "Empty module to be used as a base for custom modules.");

        }

        void OnEnable()
        {

        }

        public override void DisplayInCozyWindow()
        {
            serializedObject.Update();

            //Place custom inspector code here.

            serializedObject.ApplyModifiedProperties();

        }

    }
#endif
}