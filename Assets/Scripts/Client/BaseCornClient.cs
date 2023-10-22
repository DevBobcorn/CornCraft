#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using CraftSharp.Control;
using CraftSharp.Protocol;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Handlers.Forge;
using CraftSharp.Protocol.Session;
using CraftSharp.Rendering;
using CraftSharp.UI;
using CraftSharp.Inventory;

namespace CraftSharp
{
    [RequireComponent(typeof (InteractionUpdater))]
    public abstract class BaseCornClient : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] public ChunkRenderManager? ChunkRenderManager;
        [SerializeField] public EntityRenderManager? EntityRenderManager;
        [SerializeField] public BaseEnvironmentManager? EnvironmentManager;
        [SerializeField] public MaterialManager? MaterialManager;
        
        [SerializeField] protected PlayerController? playerController;
        [SerializeField] protected GameObject? playerRenderPrefab;
        [SerializeField] protected CameraController? cameraController;
        public CameraController CameraController => cameraController!;
        [SerializeField] protected ScreenControl? screenControl;
        public ScreenControl ScreenControl => screenControl!;
        [SerializeField] protected HUDScreen? HUDScreen;
        [SerializeField] protected LoadingScreen? LoadingScreen;
        #endregion

        public bool InputPaused { get; private set; } = false;
        public void EnableInput(bool enable)
        {
            if (enable)
            {
                playerController?.EnableInput();
                cameraController?.EnableInput();

                InputPaused = false;
            }
            else
            {
                playerController?.DisableInput();
                cameraController?.DisableInput();

                InputPaused = true;
            }
        }

        public void EnableCameraZoom(bool enable)
        {
            if (enable)
            {
                cameraController?.EnableZoom();
            }
            else
            {
                cameraController?.DisableZoom();
            }
        }

        public GameMode GameMode { get; protected set; } = GameMode.Survival;
        public byte CurrentSlot { get; protected set; } = 0;

        public abstract bool StartClient(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp,
                ushort port, int protocol, ForgeInfo? forgeInfo, string accountLower);
        
        public abstract void Disconnect();

        #region Getters: Retrieve data for use in other methods

        // Retrieve client connection info
        public abstract string GetServerHost();
        public abstract int GetServerPort();
        public abstract string GetUsername();
        public abstract Guid GetUserUuid();
        public abstract string GetUserUuidStr();
        public abstract string GetSessionID();
        public abstract double GetServerTPS();
        public abstract float GetTickMilSec();
        // Retrieve gameplay info
        public abstract ChunkRenderManager GetChunkRenderManager();
        public abstract Container? GetInventory(int inventoryId);
        public abstract ItemStack? GetActiveItem();
        public abstract Location GetLocation();
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