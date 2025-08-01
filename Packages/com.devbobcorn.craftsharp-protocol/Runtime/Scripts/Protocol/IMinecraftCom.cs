#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Inventory;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Session;

namespace CraftSharp.Protocol
{
    /// <summary>
    /// Interface for the Minecraft protocol handler.
    /// A protocol handler is used to communicate with the Minecraft server.
    /// This interface allows to abstract from the underlying minecraft version in other parts of the program.
    /// The protocol handler will take care of parsing and building the appropriate network packets.
    /// </summary>
    public interface IMinecraftCom : IDisposable
    {
        /// <summary>
        /// Start the login procedure once connected to the server
        /// </summary>
        /// <returns>True if login was successful</returns>
        bool Login(PlayerKeyPair? playerKeyPair, SessionToken session, string accountLower);

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        int GetMaxChatMessageLength();

        /// <summary>
        /// Get the current protocol version.
        /// </summary>
        /// <remarks>
        /// Version-specific operations should be handled inside the Protocol handled whenever possible.
        /// </remarks>
        /// <returns>Minecraft Protocol version number</returns>
        int GetProtocolVersion();

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="message">Text to send</param>
        /// <param name="playerKeyPair">Player info</param>
        /// <returns>True if successfully sent</returns>
        bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair = null);

        /// <summary>
        /// Send a auto complete request to the server
        /// </summary>
        /// <param name="text">Text to complete</param>
        /// <returns>True if request successfully sent</returns>
        bool SendAutoCompleteText(string text);

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        bool SendRespawnPacket();

        /// <summary>
        /// Inform the server of the client being used to connect
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>
        bool SendBrandInfo(string brandInfo);

        /// <summary>
        /// Inform the server of the client's Minecraft settings
        /// </summary>
        /// <param name="language">Client language eg en_US</param>
        /// <param name="viewDistance">View distance, in chunks</param>
        /// <param name="difficulty">Game difficulty (client-side...)</param>
        /// <param name="chatMode">Chat mode (allows muting yourself)</param>
        /// <param name="chatColors">Show chat colors</param>
        /// <param name="skinParts">Show skin layers</param>
        /// <param name="mainHand">1.9+ main hand</param>
        /// <returns>True if client settings were successfully sent</returns>
        bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand);

        /// <summary>
        /// Send a location update telling that we moved to that location
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="yaw">The new yaw (optional)</param>
        /// <param name="pitch">The new pitch (optional)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendLocationUpdate(Location location, bool onGround, float? yaw, float? pitch);

        /// <summary>
        /// Send a plugin channel packet to the server.
        /// </summary>
        /// <see href="http://dinnerbone.com/blog/2012/01/13/minecraft-plugin-channels-messaging/" />
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        /// <returns>True if message was successfully sent</returns>
        bool SendPluginChannelPacket(string channel, byte[] data);
        
        /// <summary>
        /// Send Entity Action packet to the server.
        /// </summary>
        /// <param name="entityId">PlayerId</param>
        /// <param name="type">Type of packet to send</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendEntityAction(int entityId, int type);
        
        /// <summary>
        /// Send a held item change packet to the server.
        /// </summary>
        /// <param name="slot">New active slot in the inventory hotbar</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendHeldItemChange(short slot);

        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="entityId">Entity Id to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int entityId, int type);

        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="entityId">Entity Id to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <param name="X">X coordinate for "interact at"</param>
        /// <param name="Y">Y coordinate for "interact at"</param>
        /// <param name="Z">Z coordinate for "interact at"</param>
        /// <param name="hand">Player hand (0: main hand, 1: off hand)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int entityId, int type, float X, float Y, float Z, int hand);

        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="entityId">Entity Id to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <param name="X">X coordinate for "interact at"</param>
        /// <param name="Y">Y coordinate for "interact at"</param>
        /// <param name="Z">Z coordinate for "interact at"</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int entityId, int type, float X, float Y, float Z);
        
        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="entityId">Entity Id to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <param name="hand">Only if Type is interact or interact at; 0: main hand, 1: off hand</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int entityId, int type, int hand);

        /// <summary>
        /// Send a use item packet to the server
        /// </summary>
        /// <param name="hand">0: main hand, 1: off hand</param>
        /// <param name="sequenceId">Sequence ID used for synchronization</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendUseItem(int hand, int sequenceId);
        
        /// <summary>
        /// Send a pick item packet to the server
        /// </summary>
        /// <param name="slotToUse">Hotbar slot to use</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendPickItem(int slotToUse);

        /// <summary>
        /// Send a click inventory slot packet to the server
        /// </summary>
        /// <param name="inventoryId">Id of the inventory being clicked</param>
        /// <param name="slotId">Id of the clicked slot</param>
        /// <param name="action">Action to perform</param>
        /// <param name="item">Item in the clicked slot</param>
        /// <param name="changedSlots">Slots that have been changed in this event: List(SlotId, Changed Items)</param>
        /// <param name="stateId">Inventory's stateId</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInventoryAction(int inventoryId, int slotId, InventoryActionType action, ItemStack? item, List<Tuple<short, ItemStack?>> changedSlots, int stateId);

        /// <summary>
        /// Send a click inventory button packet to the server
        /// </summary>
        /// <param name="inventoryId">Id of the inventory being clicked</param>
        /// <param name="buttonId">Id of the clicked button</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInventoryButtonClick(int inventoryId, int buttonId);
        
        /// <summary>
        /// Request Creative Mode item creation into regular/survival Player Inventory
        /// </summary>
        /// <remarks>(obviously) requires to be in creative mode</remarks>
        /// <param name="slot">Destination inventory slot</param>
        /// <param name="itemType">Item type</param>
        /// <param name="count">Item count</param>
        /// <param name="nbt">Optional item NBT</param>
        /// <returns>TRUE if item given successfully</returns>
        bool SendCreativeInventoryAction(int slot, Item itemType, int count, Dictionary<string, object>? nbt);

        /// <summary>
        /// Plays animation
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <param name="playerId">Player Entity Id</param>
        /// <returns>TRUE if item given successfully</returns>
        bool SendAnimation(int animation, int playerId);
        
        /// <summary>
        /// Set anvil rename text
        /// </summary>
        /// <param name="itemName">New name for item</param>
        /// <returns>TRUE if new name successfully set</returns>
        bool SendRenameItem(string itemName);
        
        /// <summary>
        /// Set beacon effects
        /// </summary>
        /// <param name="primary">Primary effect num id</param>
        /// <param name="secondary">Secondary effect num id</param>
        /// <returns>TRUE if effects successfully sent</returns>
        bool SendBeaconEffects(int primary, int secondary);

        /// <summary>
        /// Send a close inventory packet to the server
        /// </summary>
        /// <param name="inventoryId">Id of the inventory being closed</param>
        bool SendCloseInventory(int inventoryId);

        /// <summary>
        /// Send player block placement packet to the server
        /// </summary>
        /// <param name="hand">0: main hand, 1: off hand</param>
        /// <param name="blockLoc">Location to place block at</param>
        /// <param name="x">Block x</param>
        /// <param name="y">Block y</param>
        /// <param name="z">Block z</param>
        /// <param name="face">Block face</param>
        /// <param name="sequenceId">Sequence ID (use for synchronization)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendPlayerBlockPlacement(int hand, BlockLoc blockLoc, float x, float y, float z, Direction face, int sequenceId);

        /// <summary>
        /// Send player blog digging packet to the server. This packet needs to be called at least twice: Once to begin digging, then a second time to finish digging
        /// </summary>
        /// <param name="status">0 to start digging, 1 to cancel, 2 to finish ( https://wiki.vg/Protocol#Player_Digging )</param>
        /// <param name="blockLoc">Location</param>
        /// <param name="face">Block face</param>
        /// <param name="sequenceId">Sequence ID (use for synchronization)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendPlayerDigging(int status, BlockLoc blockLoc, Direction face, int sequenceId);

        /// <summary>
        /// Send player action packet to the server.
        /// </summary>
        /// <param name="status">3-6 for various purposes ( https://wiki.vg/Protocol#Player_Action )</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendPlayerAction(int status);

        /// <summary>
        /// Change text on a sign
        /// </summary>
        /// <param name="blockLoc">Location of Sign block</param>
        /// <param name="front">Front or back</param>
        /// <param name="line1">New line 1</param>
        /// <param name="line2">New line 2</param>
        /// <param name="line3">New line 3</param>
        /// <param name="line4">New line 4</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendUpdateSign(BlockLoc blockLoc, bool front, string line1, string line2, string line3, string line4);

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0</param>
        bool SelectTrade(int selectedSlot);

        /// <summary>
        /// Spectate a player/entity
        /// </summary>
        /// <param name="uuid">The uuid of the player/entity to spectate/teleport to.</param>
        bool SendSpectate(Guid uuid);

        /// <summary>
        /// Send player session
        /// </summary>
        /// <returns></returns>
        bool SendPlayerSession(PlayerKeyPair? playerKeyPair);

        /// <summary>
        /// Get net main thread Id
        /// </summary>
        /// <returns>Net main thread Id</returns>
        int GetNetMainThreadId();
    }
}
