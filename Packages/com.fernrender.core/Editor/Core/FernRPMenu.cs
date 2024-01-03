using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.FernRenderPipeline;

namespace UnityEditor.Rendering.FernRenderPipeline
{
    public class FernRPMenu : MonoBehaviour
    {
        [MenuItem("GameObject/Rendering/FernRP", false, 100)]
        public static void CreateFernRP()
        {
            var fernRP = GameObject.FindObjectOfType<FernRP>();
            if (fernRP == null)
            {
                var fernRenderGameObject = new GameObject();
                fernRenderGameObject.name = "FernRP";
                var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                fernRP = fernRenderGameObject.AddComponent<FernRP>();
                fernRP.renderPipelineAsset = renderPipelineAsset;
                GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
            }
            
            Selection.activeObject = fernRP.gameObject;
        }
    }
}