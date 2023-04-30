#nullable enable
using UnityEngine;

using MinecraftClient.Control;
using MinecraftClient.UI;
using MinecraftClient.Rendering;

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

        // World Render Manager Objects
        [SerializeField] public EntityRenderManager? entityRenderManager;
        [SerializeField] public ChunkRenderManager? chunkRenderManager;

        // Game Prefabs =========================================================
        // Player Prefab
        [SerializeField] public GameObject? playerPrefab;
    }

}