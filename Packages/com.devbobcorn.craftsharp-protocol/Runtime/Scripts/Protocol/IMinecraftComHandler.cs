#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Message;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol
{
    public enum DisconnectReason { InGameKick, LoginRejected, ConnectionLost, UserLogout };
    
    /// <summary>
    /// Interface for the MinecraftCom Handler.
    /// It defines some callbacks that the MinecraftCom handler must have.
    /// It allows the protocol handler to abstract from the other parts of the program.
    /// </summary>
    public interface IMinecraftComHandler
    {
        /* The MinecraftCom Handler must
         * provide these getters */

        int GetServerPort();
        string GetServerHost();
        string GetUsername();
        Guid GetUserUUID();
        string GetUserUUIDStr();
        string GetSessionId();
        string[] GetOnlinePlayers();
        Dictionary<string, string> GetOnlinePlayersWithUUID();
        PlayerInfo? GetPlayerInfo(Guid targetUUID);
        Location GetCurrentLocation();
        IChunkRenderManager GetChunkRenderManager();

        public void SetCanSendMessage(bool canSend);

        void GetCookie(string key, out byte[]? data);
        void SetCookie(string key, byte[] data);
        void DeleteCookie(string key);

        void Transfer(string newHost, int newPort);

        /// <summary>
        /// Invoke a task on the main thread, wait for completion and retrieve return value.
        /// </summary>
        /// <param name="task">Task to run with any type or return value</param>
        /// <returns>Any result returned from task, result type is inferred from the task</returns>
        /// <example>bool result = InvokeOnNetMainThread(methodThatReturnsABool);</example>
        /// <example>bool result = InvokeOnNetMainThread(() => methodThatReturnsABool(argument));</example>
        /// <example>int result = InvokeOnNetMainThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        T InvokeOnNetMainThread<T>(Func<T> task);

        /// <summary>
        /// Invoke a task on the main thread and wait for completion
        /// </summary>
        /// <param name="task">Task to run without return value</param>
        /// <example>InvokeOnNetMainThread(methodThatReturnsNothing);</example>
        /// <example>InvokeOnNetMainThread(() => methodThatReturnsNothing(argument));</example>
        /// <example>InvokeOnNetMainThread(() => { yourCode(); });</example>
        void InvokeOnNetMainThread(Action task);

        /// <summary>
        /// Called when a network packet received or sent
        /// </summary>
        /// <remarks>
        /// Only called if <see cref="ProtocolSettings.CapturePackets"/> is set to True
        /// </remarks>
        /// <param name="packetId">Packet Id</param>
        /// <param name="packetData">A copy of Packet Data</param>
        /// <param name="currentState">The current game state in which the packet is handled</param>
        /// <param name="isInbound">The packet is received from server or sent by client</param>
        void OnNetworkPacket(int packetId, byte[] packetData, CurrentState currentState, bool isInbound);

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        void OnGameJoined(bool isOnlineMode);

        /// <summary>
        /// Called when the protocol handler receives a chat message
        /// </summary>
        /// <param name="message">Message received</param>
        public void OnTextReceived(ChatMessage message);

        /// <summary>
        /// Will be called every animations of the hit and place block
        /// </summary>
        /// <param name="entityId">Player Id</param>
        /// <param name="entityAnimation">0 = LMB, 1 = RMB (RMB Currently not working)</param>
        void OnEntityAnimation(int entityId, byte entityAnimation);

        /// <summary>
        /// Will be called every player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player Id</param>
        /// <param name="blockLoc">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        void OnBlockBreakAnimation(int entityId, BlockLoc blockLoc, byte stage);

        /// <summary>
        /// Called when the protocol handler receives a title
        /// </summary>
        void OnTitle(int action, string titleText, string subtitleText, string actionbarText, int fadeIn, int stay, int fadeOut, string json);

        /// <summary>
        /// Called when receiving a connection keep-alive from the server
        /// </summary>
        void OnServerKeepAlive();

        /// <summary>
        /// Called when the protocol handler receives server data
        /// </summary>
        /// <param name="hasMotd">Indicates if the server has a motd message</param>
        /// <param name="motd">Server MOTD message</param>
        /// <param name="hasIcon">Indicates if the server has a an icon</param>
        /// <param name="iconBase64">Server icon in Base 64 format</param>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        void OnServerDataReceived(bool hasMotd, string motd, bool hasIcon, string iconBase64, bool previewsChat);

        /// <summary>
        /// Called when the protocol handler receives "Set Display Chat Preview" packet
        /// </summary>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnChatPreviewSettingUpdate(bool previewsChat);

        /// <summary>
        /// Called tab complete suggestion is received
        /// </summary>
        void OnTabCompleteDone(int completionStart, int completionLength, string[] completeResults);

        /// <summary>
        /// Called when an inventory is opened
        /// </summary>
        void OnInventoryOpen(int inventoryId, InventoryData inventoryData);

        /// <summary>
        /// Called when an inventory is closed
        /// </summary>
        void OnInventoryClose(int inventoryId);

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        void OnRespawn();

        /// <summary>
        /// Called when a new player joins the game
        /// </summary>
        /// <param name="player">player info</param>
        public void OnPlayerJoin(PlayerInfo player);

        /// <summary>
        /// Called when a player has left the game
        /// </summary>
        /// <param name="playerUUID">UUID of the player</param>
        void OnPlayerLeave(Guid playerUUID);

        /// <summary>
        /// Called when a player has been killed by another entity
        /// </summary>
        /// <param name="killerEntityId">Killer's entity Id</param>
        /// <param name="chatMessage">message sent in chat when player is killed</param>
        void OnPlayerKilled(int killerEntityId, string chatMessage);

        /// <summary>
        /// Called when the server sets the new location for the player
        /// </summary>
        /// <param name="location">New location of the player</param>
        /// <param name="yaw">Yaw value, measured in degrees</param>
        /// <param name="pitch">Pitch value, measured in degrees</param>
        void UpdateLocation(Location location, float yaw, float pitch);

        /// <summary>
        /// Called when the connection has been lost
        /// </summary>
        void OnConnectionLost(DisconnectReason reason, string message);

        /// <summary>
        /// Called ~20 times per second (20 ticks per second)
        /// </summary>
        void OnHandlerUpdate(int pc);

        /// <summary>
        /// Called when an entity has spawned
        /// </summary>
        /// <param name="entity">Spawned entity</param>
        void OnSpawnEntity(EntityData entity);

        /// <summary>
        /// Called when an entity has spawned
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="slot">Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item">Item</param>
        void OnEntityEquipment(int entityId, int slot, ItemStack item);

        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="playerUUID">Entity UUID</param>
        /// <param name="location">Entity location</param>
        /// <param name="yaw">Player head yaw</param>
        /// <param name="pitch">Player head pitch</param>
        void OnSpawnPlayer(int entityId, Guid playerUUID, Location location, byte yaw, byte pitch);

        /// <summary>
        /// Called when entities have despawned
        /// </summary>
        /// <param name="entityId">List of Entity Id that have despawned</param>
        void OnDestroyEntities(int[] entityId);

        /// <summary>
        /// Called when an entity moved by coordinate offset
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="dx">X offset</param>
        /// <param name="dy">Y offset</param>
        /// <param name="dz">Z offset</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityPosition(int entityId, double dx, double dy, double dz, bool onGround);

        /// <summary>
        /// Called when an entity's yaw or pitch changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="yaw">New yaw</param>
        /// <param name="pitch">New pitch</param>
        /// <param name="onGround">Whether the entity is grounded</param>
        void OnEntityRotation(int entityId, byte yaw, byte pitch, bool onGround);

        /// <summary>
        /// Called when an entity's head yaw changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="headYaw">New head yaw</param>
        void OnEntityHeadLook(int entityId, byte headYaw);

        /// <summary>
        /// Called when an entity moved to fixed coordinates
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityTeleport(int entityId, double x, double y, double z, bool onGround);

        /// <summary>
        /// Called when additional properties have been received for an entity
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="prop">Dictionary of properties</param>
        void OnEntityProperties(int entityId, Dictionary<string, double> prop);

        /// <summary>
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="status">Status Id</param>
        void OnEntityStatus(int entityId, byte status);

        /// <summary>
        /// Called when the world age has been updated
        /// </summary>
        /// <param name="worldAge">World age</param>
        /// <param name="timeOfDay">Time of Day</param>
        void OnTimeUpdate(long worldAge, long timeOfDay);

        /// <summary>
        /// When received inventory property from server.
        /// 
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="propertyId">Property Id</param>
        /// <param name="propertyValue">Property Value</param>
        public void OnInventoryProperty(byte inventoryId, short propertyId, short propertyValue);

        /// <summary>
        /// Called when inventory items have been received
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="itemList">Item list</param>
        /// <param name="stateId">State Id</param>
        void OnInventoryItems(byte inventoryId, Dictionary<int, ItemStack?> itemList, int stateId);

        /// <summary>
        /// Called when a single slot has been updated inside an inventory
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="slot">Slot Id</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        /// <param name="stateId">State Id</param>
        /// <param name="fromClient">Whether this is sent from client</param>
        public void OnInventorySlot(byte inventoryId, short slot, ItemStack? item, int stateId, bool fromClient);

        /// <summary>
        /// Called when player health or hunger changed.
        /// </summary>
        /// <param name="health"></param>
        /// <param name="food"></param>
        void OnUpdateHealth(float health, int food);

        /// <summary>
        /// Called when entity metadata or metadata changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="metadata">Entity metadata</param>
        void OnEntityMetadata(int entityId, Dictionary<int, object?> metadata);

        /// <summary>
        /// Called when and explosion occurs on the server
        /// </summary>
        /// <param name="location">Explosion location</param>
        /// <param name="strength">Explosion strength</param>
        /// <param name="affectedBlocks">Amount of affected blocks</param>
        void OnExplosion(Location location, float strength, int affectedBlocks);

        /// <summary>
        /// Called when a player's game mode has changed
        /// </summary>
        /// <param name="playerUUID">Affected player's UUID</param>
        /// <param name="gamemode">New game mode</param>
        void OnGamemodeUpdate(Guid playerUUID, int gamemode);

        /// <summary>
        /// Called when a player's latency has changed
        /// </summary>
        /// <param name="playerUUID">Affected player's UUID</param>
        /// <param name="latency">latency</param>
        void OnLatencyUpdate(Guid playerUUID, int latency);

        /// <summary>
        /// Called when experience is updated
        /// </summary>
        /// <param name="expBar">Experience bar level</param>
        /// <param name="expLevel">Player Level</param>
        /// <param name="totalExp">Total experience</param>
        void OnSetExperience(float expBar, int expLevel, int totalExp);

        /// <summary>
        /// Called when held item is changed
        /// </summary>
        /// <remarks>Used for setting player slot after joining game</remarks>
        /// <param name="slot">Selected hotbar slot</param>
        void OnHeldItemChange(byte slot);

        /// <summary>
        /// Called when an update of the map is sent by the server, take a look at https://wiki.vg/Protocol#Map_Data for more info on the fields
        /// Map format and colors: https://minecraft.fandom.com/wiki/Map_item_format
        /// </summary>
        /// <param name="mapId">Map Id of the map being modified</param>
        /// <param name="scale">A scale of the Map, from 0 for a fully zoomed-in map (1 block per pixel) to 4 for a fully zoomed-out map (16 blocks per pixel)</param>
        /// <param name="trackingPosition">Specifies whether player and item frame icons are shown </param>
        /// <param name="locked">True if the map has been locked in a cartography table </param>
        /// <param name="icons">A list of MapIcon objects of map icons, send only if trackingPosition is true</param>
        /// <param name="colsUpdated">Numbs of columns that were updated (map width) (NOTE: If it is 0, the next fields are not used/are set to default values of 0 and null respectively)</param>
        /// <param name="rowsUpdated">Map height</param>
        /// <param name="mapColX">x offset of the westernmost column</param>
        /// <param name="mapRowZ">z offset of the northernmost row</param>
        /// <param name="colors">a byte array of colors on the map</param>
        void OnMapData(int mapId, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte colsUpdated, byte rowsUpdated, byte mapColX, byte mapRowZ, byte[]? colors);

        /// <summary>
        /// Called when the Player entity Id has been received from the server
        /// </summary>
        /// <param name="entityId">Player entity Id</param>
        void OnReceivePlayerEntityId(int entityId);

        /// <summary>
        /// Called when the Entity use effects
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="effect">Effect Id</param>
        /// <param name="amplifier">Effect amplifier</param>
        /// <param name="duration">Effect duration</param>
        /// <param name="flags">Effect flags</param>
        /// <param name="hasFactorData">Has factor data</param>
        /// <param name="factorCodec">FactorCodec</param>
        void OnEntityEffect(int entityId, Effects effect, int amplifier, int duration, byte flags, bool hasFactorData, Dictionary<String, object>? factorCodec);

        /// <summary>
        /// Called when ScoreboardObjective is updated
        /// </summary>
        /// <param name="objectiveName">Objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectiveValue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        void OnScoreboardObjective(string objectiveName, byte mode, string objectiveValue, int type);

        /// <summary>
        /// Called when DisplayScoreboard is updated
        /// </summary>
        /// <param name="entityName">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectiveName">The name of the objective the score belongs to</param>
        /// <param name="objectiveDisplayName">The name of the objective the score belongs to, but with chat formatting</param>
        /// <param name="objectiveValue">The score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        /// <param name="numberFormat">Number format: 0 - blank, 1 - styled, 2 - fixed</param>
        void OnUpdateScore(string entityName, int action, string objectiveName, string objectiveDisplayName, int objectiveValue, int numberFormat);

        /// <summary>
        /// Called when tradeList is received from server
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="trades">List of trades</param>
        /// <param name="villagerInfo">Contains Level, Experience, IsRegularVillager and CanRestock</param>
        void OnTradeList(int inventoryId, List<VillagerTrade> trades, VillagerInfo villagerInfo);

        /// <summary>
        /// Called when rain starts or stops
        /// </summary>
        /// <param name="begin">Whether the rain is about to begin</param>
        void OnRainChange(bool begin);

        /// <summary>
        /// This method is called when the protocol handler receives "Login Success" packet
        /// </summary>
        /// <param name="playerUUID">The player's UUID received from the server</param>
        /// <param name="userName">The player's username received from the server</param>
        /// <param name="playerProperty">Tuple(Name, Value, Signature(empty if there is no signature))</param>
        public void OnLoginSuccess(Guid playerUUID, string userName, Tuple<string, string, string>[]? playerProperty);

    }
}
