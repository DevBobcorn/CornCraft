using System;
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Control;
using CraftSharp.Protocol;
using CraftSharp.Rendering;
using CraftSharp.UI;
using CraftSharp.Inventory;
using UnityEngine.Serialization;

namespace CraftSharp
{
    [RequireComponent(typeof (InteractionUpdater))]
    public abstract class BaseCornClient : MonoBehaviour
    {
        #region Inspector Fields
        // World Fields
        [SerializeField] private Transform m_WorldAnchor;
        [SerializeField] private ChunkRenderManager m_ChunkRenderManager;
        [SerializeField] private EntityRenderManager m_EntityRenderManager;
        [SerializeField] private BaseEnvironmentManager m_EnvironmentManager;
        [SerializeField] private ChunkMaterialManager m_ChunkMaterialManager;
        [SerializeField] private EntityMaterialManager m_EntityMaterialManager;
        public ChunkRenderManager ChunkRenderManager => m_ChunkRenderManager;
        public EntityRenderManager EntityRenderManager => m_EntityRenderManager;
        protected BaseEnvironmentManager EnvironmentManager => m_EnvironmentManager;
        public ChunkMaterialManager ChunkMaterialManager => m_ChunkMaterialManager;
        public EntityMaterialManager EntityMaterialManager => m_EntityMaterialManager;
        public IChunkRenderManager GetChunkRenderManager() => m_ChunkRenderManager;
        
        // Player Fields
        [SerializeField] private PlayerController m_PlayerController;
        [SerializeField] private GameObject[] m_PlayerRenderPrefabs = { };
        private int selectedRenderPrefab;
        protected PlayerController PlayerController => m_PlayerController;
        [SerializeField] protected InteractionUpdater interactionUpdater;

        // Camera Fields
        [SerializeField] private GameObject[] m_CameraControllerPrefabs = { };
        private int selectedCameraController = 0;
        public CameraController CameraController { get; private set; }

        [FormerlySerializedAs("MainCamera")] public Camera m_MainCamera;
        [FormerlySerializedAs("SpriteCamera")] public Camera m_SpriteCamera;

        // UI Fields
        [SerializeField] protected ScreenControl m_ScreenControl;
        public ScreenControl ScreenControl => m_ScreenControl;
        [SerializeField] protected Camera m_UICamera;
        public Camera UICamera => m_UICamera;
        #endregion

        public bool ControllerInputPaused { get; private set; } = false;
        
        public void SetControllerInputPaused(bool pause)
        {
            if (CameraController && m_PlayerController)
            {
                if (pause)
                {
                    m_PlayerController.DisableInput();
                    CameraController.DisableCinemachineInput();

                    ControllerInputPaused = true;
                }
                else
                {
                    m_PlayerController.EnableInput();
                    CameraController.EnableCinemachineInput();

                    ControllerInputPaused = false;
                }
            }
        }

        public void SetCameraZoomEnabled(bool enable)
        {
            if (CameraController)
            {
                if (enable)
                {
                    CameraController.EnableZoom();
                }
                else
                {
                    CameraController.DisableZoom();
                }
            }
        }

        protected void SwitchToFirstCameraController()
        {
            if (m_CameraControllerPrefabs.Length == 0) return;

            selectedCameraController = 0;
            SwitchCameraController(m_CameraControllerPrefabs[0]);
        }

        protected void SwitchCameraControllerBy(int indexOffset)
        {
            if (m_CameraControllerPrefabs.Length == 0) return;

            var index = selectedCameraController + indexOffset;
            while (index < 0) index += m_CameraControllerPrefabs.Length;

            selectedCameraController = index % m_CameraControllerPrefabs.Length;
            SwitchCameraController(m_CameraControllerPrefabs[selectedCameraController]);
        }

        private void SwitchCameraController(GameObject controllerPrefab)
        {
            var prevControllerObj = !CameraController ? null : CameraController.gameObject;

            // Destroy the old one
            if (prevControllerObj)
            {
                Destroy(prevControllerObj);
            }

            var cameraControllerObj = GameObject.Instantiate(controllerPrefab);
            CameraController = cameraControllerObj.GetComponent<CameraController>();

            // Assign Cameras
            CameraController.SetCameras(m_MainCamera, m_SpriteCamera);

            // Call player controller handler
            m_PlayerController.HandleCameraControllerSwitch(CameraController);

            // Set camera controller for interaction updater
            interactionUpdater.SetControllers(this, CameraController, PlayerController);
        }

        protected void SwitchToFirstPlayerRender(EntityData clientEntity)
        {
            if (m_PlayerRenderPrefabs.Length == 0) return;

            selectedRenderPrefab = 0;
            PlayerController.SwitchPlayerRenderFromPrefab(clientEntity, m_PlayerRenderPrefabs[0]);
        }

        protected void SwitchPlayerRenderBy(EntityData clientEntity, int indexOffset)
        {
            if (m_PlayerRenderPrefabs.Length == 0) return;

            var index = selectedRenderPrefab + indexOffset;
            while (index < 0) index += m_PlayerRenderPrefabs.Length;

            selectedRenderPrefab = index % m_PlayerRenderPrefabs.Length;
            PlayerController.SwitchPlayerRenderFromPrefab(clientEntity, m_PlayerRenderPrefabs[selectedRenderPrefab]);
        }

        public GameMode GameMode { get; protected set; } = GameMode.Survival;
        protected byte CurrentSlot { get; set; } = 0;
        public abstract bool CheckAddDragged(ItemStack slotItem, Func<ItemStack, bool> slotPredicate);

        public Vector3Int WorldOriginOffset { get; private set; } = Vector3Int.zero;

        protected void SetWorldOriginOffset(Vector3Int offset)
        {
            var delta = offset - WorldOriginOffset;
            var posDelta = CoordConvert.GetPosDelta(delta);

            // Move world anchor
            m_WorldAnchor.position = CoordConvert.MC2Unity(offset, Location.Zero);

            // Move chunk renders
            ChunkRenderManager.SetWorldOriginOffset(posDelta, offset);

            // Move entities
            EntityRenderManager.SetWorldOriginOffset(posDelta, offset);

            // Move client player
            var playerDelta = PlayerController.SetWorldOriginOffset(offset);

            // Move active camera
            CameraController.TeleportByDelta(playerDelta);

            WorldOriginOffset = offset;
        }

        public abstract bool StartClient(StartLoginInfo info);
        
        public abstract void Disconnect();

        #region Thread-Invoke: Cross-thread method calls

        public abstract T InvokeOnNetMainThread<T>(Func<T> task);

        public abstract void InvokeOnNetMainThread(Action task);

        #endregion

        #region Getters: Retrieve data for use in other methods
#nullable enable

        // Retrieve client connection info
        public abstract string GetServerHost();
        public abstract int GetServerPort();
        public abstract int GetProtocolVersion();
        public abstract string GetUsername();
        public abstract Guid GetUserUUID();
        public abstract string GetUserUUIDStr();
        public abstract string GetSessionId();
        public abstract double GetLatestServerTps();
        public abstract double GetServerAverageTps();
        public abstract float GetTickMilSec();
        public abstract int GetPacketCount();
        public abstract int GetClientEntityId();
        public abstract double GetClientFoodSaturation();
        public abstract double GetClientExperienceLevel();
        public abstract double GetClientTotalExperience();
        // Retrieve gameplay info
        public abstract InventoryData? GetInventory(int inventoryId);
        public abstract ItemStack? GetActiveItem();
        public abstract Location GetCurrentLocation();
        public abstract Vector3 GetPosition();
        public abstract float GetCameraYaw();
        public abstract float GetCameraPitch();
        public abstract string GetInfoString(bool withDebugInfo);
        public abstract Dictionary<string, int> GetPlayersLatency();
        public abstract int GetOwnLatency();
        public abstract PlayerInfo? GetPlayerInfo(Guid uuid);
        public abstract string[] GetOnlinePlayers();
        public abstract Dictionary<string, string> GetOnlinePlayersWithUUID();

        #endregion

        #region Action methods: Perform an action on the Server

        public abstract void TrySendChat(string text);
        public abstract bool SendRespawnPacket();
        public abstract bool SendEntityAction(EntityActionType entityAction);
        public abstract void SendAutoCompleteRequest(string text);
        public abstract bool UseItemOnMainHand();
        public abstract bool UseItemOnOffHand();
        public abstract void OpenPlayerInventory();
        public abstract bool DoInventoryAction(int inventoryId, int slot, InventoryActionType actionType);
        public abstract bool DoCreativeGive(int slot, Item itemType, int count, Dictionary<string, object>? nbt = null);
        public abstract bool DoAnimation(int playerAnimation);
        public abstract bool SetBeaconEffects(int primary, int secondary);
        public abstract bool CloseInventory(int inventoryId);
        public abstract bool ClearInventories();
        public abstract bool InteractEntity(int entityId, int type, Hand hand = Hand.MainHand);
        public abstract bool PlaceBlock(BlockLoc blockLoc, Direction blockFace, float x, float y, float z, Hand hand = Hand.MainHand);
        public abstract bool DigBlock(BlockLoc blockLoc, Direction blockFace, DiggingStatus status);
        public abstract bool DropItem(bool dropEntireStack);
        public abstract bool SwapItemOnHands();
        public abstract bool ChangeHotbarSlot(short slot);

        public bool ChangeHotbarSlotBy(short offset)
        {
            var index = CurrentSlot + offset;
            while (index < 0) index += 9;

            return ChangeHotbarSlot((short) (index % 9));
        }

        #endregion

    }
}