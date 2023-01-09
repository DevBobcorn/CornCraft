#nullable enable
using UnityEngine;

using MinecraftClient.Control;
using MinecraftClient.UI;
using MinecraftClient.Rendering;

using DistantLands.Cozy;

namespace MinecraftClient
{
    public class SceneObjectHolder : MonoBehaviour
    {
        // World Objects ========================================================
        // GUI Objects
        [SerializeField] public ScreenControl? screenControl;
        [SerializeField] public HUDScreen? hudScreen;

        // Camera Controller Object
        [SerializeField] public CameraController? cameraController;

        // Cozy Weather Object
        [SerializeField] public CozyWeather? cozyWeather;

        // World Render Manager Objects
        [SerializeField] public EntityRenderManager? entityRenderManager;
        [SerializeField] public ChunkRenderManager? chunkRenderManager;

        // Game Prefabs =========================================================
        // Player Prefab
        [SerializeField] public GameObject? playerPrefab;

        public bool AllPresent()
        {
            return screenControl is not null
                    && hudScreen is not null
                    && cameraController is not null
                    // && cozyWeather is not null
                    && entityRenderManager is not null
                    && chunkRenderManager is not null
                    && playerPrefab is not null;
        }
    }

}