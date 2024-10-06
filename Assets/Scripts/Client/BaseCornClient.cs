using System;
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Control;
using CraftSharp.Protocol;
using CraftSharp.Rendering;
using CraftSharp.UI;
using CraftSharp.Inventory;

namespace CraftSharp
{
    [RequireComponent(typeof (InteractionUpdater))]
    public abstract class BaseCornClient : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private Transform m_WorldAnchor;
        [SerializeField] private ChunkRenderManager m_ChunkRenderManager;
        [SerializeField] private EntityRenderManager m_EntityRenderManager;
        [SerializeField] private BaseEnvironmentManager m_EnvironmentManager;
        [SerializeField] private ChunkMaterialManager m_ChunkMaterialManager;
        [SerializeField] private EntityMaterialManager m_EntityMaterialManager;

        public ChunkRenderManager ChunkRenderManager => m_ChunkRenderManager;
        public EntityRenderManager EntityRenderManager => m_EntityRenderManager;
        public BaseEnvironmentManager EnvironmentManager => m_EnvironmentManager;
        public ChunkMaterialManager ChunkMaterialManager => m_ChunkMaterialManager;
        public EntityMaterialManager EntityMaterialManager => m_EntityMaterialManager;
        
        [SerializeField] private PlayerController m_PlayerController;
        public PlayerController PlayerController => m_PlayerController;
        [SerializeField] protected GameObject[] playerRenderPrefabs = { };
        [SerializeField] protected int selectedRenderPrefab;
        [SerializeField] protected CameraController m_CameraController;
        public CameraController CameraController => m_CameraController;
        [SerializeField] protected ScreenControl screenControl;
        public ScreenControl ScreenControl => screenControl;
        [SerializeField] protected Camera m_UICamera;
        public Camera UICamera => m_UICamera;
        #endregion

        public bool InputPaused { get; private set; } = false;
        public void EnableInput(bool enable)
        {
            if (m_CameraController != null && m_PlayerController != null)
            {
                if (enable)
                {
                    m_PlayerController.EnableInput();
                    m_CameraController.EnableInput();

                    InputPaused = false;
                }
                else
                {
                    m_PlayerController.DisableInput();
                    m_CameraController.DisableInput();

                    InputPaused = true;
                }
            }
        }

        public void EnableCameraZoom(bool enable)
        {
            if (m_CameraController != null)
            {
                if (enable)
                {
                    m_CameraController.EnableZoom();
                }
                else
                {
                    m_CameraController.DisableZoom();
                }
            }
        }

        public GameMode GameMode { get; protected set; } = GameMode.Survival;
        public byte CurrentSlot { get; protected set; } = 0;

        public Vector3Int WorldOriginOffset { get; private set; } = Vector3Int.zero;

        protected virtual void SetWorldOriginOffset(Vector3Int offset)
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

        #region Getters: Retrieve data for use in other methods
        #nullable enable

        // Retrieve client connection info
        public abstract string GetServerHost();
        public abstract int GetServerPort();
        public abstract int GetProtocolVersion();
        public abstract string GetUsername();
        public abstract Guid GetUserUuid();
        public abstract string GetUserUuidStr();
        public abstract string GetSessionID();
        public abstract double GetServerTPS();
        public abstract float GetTickMilSec();
        public abstract int GetPacketCount();
        // Retrieve gameplay info
        public abstract IChunkRenderManager GetChunkRenderManager();
        
        public abstract Container? GetInventory(int inventoryId);
        public abstract ItemStack? GetActiveItem();
        public abstract Location GetCurrentLocation();
        public abstract Vector3 GetPosition();
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
        public abstract bool UseItemOnHand();
        public abstract bool PlaceBlock(BlockLoc blockLoc, Direction blockFace, Hand hand = Hand.MainHand);
        public abstract bool DigBlock(BlockLoc blockLoc, bool swingArms = true, bool lookAtBlock = true);
        public abstract bool ChangeSlot(short slot);

        #endregion

    }
}