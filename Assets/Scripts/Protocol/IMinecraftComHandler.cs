using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

namespace MinecraftClient.Protocol
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
        string GetUserUUID();
        string GetSessionID();
        string[] GetOnlinePlayers();
        Dictionary<string, string> GetOnlinePlayersWithUUID();
        Location GetCurrentLocation();
        World GetWorld();
        int GetProtocolVersion();
        Container GetInventory(int inventoryID);

        /// <summary>
        /// Invoke a task on the main thread, wait for completion and retrieve return value.
        /// </summary>
        /// <param name="task">Task to run with any type or return value</param>
        /// <returns>Any result returned from task, result type is inferred from the task</returns>
        /// <example>bool result = InvokeOnNetReadThread(methodThatReturnsAbool);</example>
        /// <example>bool result = InvokeOnNetReadThread(() => methodThatReturnsAbool(argument));</example>
        /// <example>int result = InvokeOnNetReadThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        T InvokeOnNetReadThread<T>(Func<T> task);

        /// <summary>
        /// Invoke a task on the main thread and wait for completion
        /// </summary>
        /// <param name="task">Task to run without return value</param>
        /// <example>InvokeOnNetReadThread(methodThatReturnsNothing);</example>
        /// <example>InvokeOnNetReadThread(() => methodThatReturnsNothing(argument));</example>
        /// <example>InvokeOnNetReadThread(() => { yourCode(); });</example>
        void InvokeOnNetReadThread(Action task);

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        void OnGameJoined();

        /// <summary>
        /// This method is called when the protocol handler receives a chat message
        /// </summary>
        /// <param name="text">Text received from the server</param>
        /// <param name="isJson">TRUE if the text is JSON-Encoded</param>
        void OnTextReceived(string text, bool isJson);

        /// <summary>
        /// Will be called every animations of the hit and place block
        /// </summary>
        /// <param name="entityID">Player ID</param>
        /// <param name="animation">0 = LMB, 1 = RMB (RMB Corrent not work)</param>
        void OnEntityAnimation(int entityID, byte animation);

        /// <summary>
        /// Will be called every player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player ID</param>
        /// <param name="location">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        void OnBlockBreakAnimation(int entityID, Location location, byte stage);

        /// <summary>
        /// This method is called when the protocol handler receives a title
        /// </summary>
        void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json);
        
        /// <summary>
        /// Called when receiving a connection keep-alive from the server
        /// </summary>
        void OnServerKeepAlive();

        /// <summary>
        /// Called when an inventory is opened
        /// </summary>
        void OnInventoryOpen(int inventoryID, Container inventory);

        /// <summary>
        /// Called when an inventory is closed
        /// </summary>
        void OnInventoryClose(int inventoryID);

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        void OnRespawn();

        /// <summary>
        /// This method is called when a new player joins the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        /// <param name="name">Name of the player</param>
        void OnPlayerJoin(Guid uuid, string name);

        /// <summary>
        /// This method is called when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        void OnPlayerLeave(Guid uuid);

        /// <summary>
        /// Called when the server sets the new location for the player
        /// </summary>
        /// <param name="location">New location of the player</param>
        /// <param name="yaw">New yaw</param>
        /// <param name="pitch">New pitch</param>
        void UpdateLocation(Location location, float yaw, float pitch);

        /// <summary>
        /// This method is called when the connection has been lost
        /// </summary>
        void OnConnectionLost(DisconnectReason reason, string message);

        /// <summary>
        /// Called ~10 times per second (10 ticks per second)
        /// Useful for updating bots in other parts of the program
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// Called when an entity has spawned
        /// </summary>
        /// <param name="entity">Spawned entity</param>
        void OnSpawnEntity(Entity entity);
        
        /// <summary>
        /// Called when an entity has spawned
        /// </summary>
        /// <param name="entityid">Entity id</param>
        /// <param name="slot">Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)/param>
        /// <param name="item">Item/param>
        void OnEntityEquipment(int entityid, int slot, Item item);
        
        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="uuid">Entity UUID</param>
        /// <param name="location">Entity location</param>
        /// <param name="yaw">Player head yaw</param>
        /// <param name="pitch">Player head pitch</param>
        void OnSpawnPlayer(int entityID, Guid uuid, Location location, byte yaw, byte pitch);

        /// <summary>
        /// Called when entities have despawned
        /// </summary>
        /// <param name="EntityID">List of Entity ID that have despawned</param>
        void OnDestroyEntities(int[] EntityID);

        /// <summary>
        /// Called when an entity moved by coordinate offset
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="Dx">X offset</param>
        /// <param name="Dy">Y offset</param>
        /// <param name="Dz">Z offset</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityPosition(int entityID, Double dx, Double dy, Double dz, bool onGround);

        void OnEntityRotation(int entityID, float yaw, float pitch, bool onGround);

        /// <summary>
        /// Called when an entity moved to fixed coordinates
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="Dx">X</param>
        /// <param name="Dy">Y</param>
        /// <param name="Dz">Z</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityTeleport(int entityID, Double x, Double y, Double z, bool onGround);

        /// <summary>
        /// Called when additional properties have been received for an entity
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="prop">Dictionary of properties</param>
        void OnEntityProperties(int entityID, Dictionary<string, Double> prop);

        /// <summary>
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="status">Status ID</param>
        void OnEntityStatus(int entityID, byte status);

        /// <summary>
        /// Called when the world age has been updated
        /// </summary>
        /// <param name="WorldAge">World age</param>
        /// <param name="TimeOfDay">Time of Day</param>
        void OnTimeUpdate(long worldAge, long timeOfDay);

        /// <summary>
        /// Called when inventory items have been received
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list</param>
        void OnWindowItems(byte inventoryID, Dictionary<int, Item> itemList);

        /// <summary>
        /// Called when a single slot has been updated inside an inventory
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        void OnSetSlot(byte inventoryID, short slotID, Item item);

        /// <summary>
        /// Called when player health or hunger changed.
        /// </summary>
        /// <param name="health"></param>
        /// <param name="food"></param>
        void OnUpdateHealth(float health, int food);

        /// <summary>
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="health">The health of the entity</param>
        void OnEntityHealth(int entityID, float health);
        
        /// <summary>
        /// Called when entity metadata or metadata changed.
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="metadata">Entity metadata</param>
        void OnEntityMetadata(int EntityID, Dictionary<int, object> metadata);

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
        /// <param name="uuid">Affected player's UUID</param>
        /// <param name="gamemode">New game mode</param>
        void OnGamemodeUpdate(Guid uuid, int gamemode);
        
        /// <summary>
        /// Called when a player's latency has changed
        /// </summary>
        /// <param name="uuid">Affected player's UUID</param>
        /// <param name="latency">latency</param>
        void OnLatencyUpdate(Guid uuid, int latency);
        
        /// <summary>
        /// Called when Experience bar is updated
        /// </summary>
        /// <param name="Experiencebar">Experience bar level</param>
        /// <param name="Level">Player Level</param>
        /// <param name="TotalExperience">Total experience</param>
        void OnSetExperience(float Experiencebar, int Level, int TotalExperience);

        /// <summary>
        /// Called when client need to change slot.
        /// </summary>
        /// <remarks>Used for setting player slot after joining game</remarks>
        /// <param name="slot"></param>
        void OnHeldItemChange(byte slot);
        
        /// <summary>
        /// Called map data
        /// </summary>
        /// <param name="mapid"></param>
        /// <param name="scale"></param>
        /// <param name="trackingposition"></param>
        /// <param name="locked"></param>
        /// <param name="iconcount"></param>
        void OnMapData(int mapid, byte scale, bool trackingposition, bool locked, int iconcount);
        
        /// <summary>
        /// Called when the Player entity ID has been received from the server
        /// </summary>
        /// <param name="EntityID">Player entity ID</param>
        void OnReceivePlayerEntityID(int EntityID);
        
        /// <summary>
        /// Called when the Entity use effects
        /// </summary>
        /// <param name="entityid">entity ID</param>
        /// <param name="effect">effect id</param>
        /// <param name="amplifier">effect amplifier</param>
        /// <param name="duration">effect duration</param>
        /// <param name="flags">effect flags</param>
        void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags);
        
        /// <summary>
        /// Called when coreboardObjective
        /// </summary>
        /// <param name="objectivename">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectivevalue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type);
        
        /// <summary>
        /// Called when DisplayScoreboard
        /// </summary>
        /// <param name="entityname">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectivename">The name of the objective the score belongs to</param>
        /// <param name="value">he score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        void OnUpdateScore(string entityname, byte action, string objectivename, int value);

        /// <summary>
        /// Called when tradeList is received from server
        /// </summary>
        /// <param name="windowID">Window ID</param>
        /// <param name="trades">List of trades.</param>
        /// <param name="villagerLevel">The level the villager is.</param>
        /// <param name="experience">The amount of experience the villager has.</param>
        /// <param name="isRegularVillager">True if regular villagers and false if the wandering trader.</param>
        /// <param name="canRestock">If the villager can restock his trades at a workstation, True for regular villagers and false for the wandering trader.</param>
        void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo);

        void OnRainChange(bool begin);
    }
}
