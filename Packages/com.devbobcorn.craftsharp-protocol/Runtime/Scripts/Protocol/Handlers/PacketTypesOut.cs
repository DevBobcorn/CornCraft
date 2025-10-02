﻿namespace CraftSharp.Protocol.Handlers
{
    /// <summary>
    /// Outgoing packet types
    /// </summary>
    public enum PacketTypesOut
    {
        AcknowledgeConfiguration,    // Added in 1.20.2
        AdvancementTab,              //
        Animation,                   //
        ChangeInventorySlotState,    // Added in 1.20.3
        ChatCommand,                 // Added in 1.19
        ChatMessage,                 //
        ChatPreview,                 // Added in 1.19
        ChunkBatchReceived,          // Added in 1.20.2
        ClickInventory,              //
        ClickInventoryButton,        //
        ClientTickEnd,               // Added in 1.21.2
        ClientSettings,              //
        ClientStatus,                //
        CloseInventory,              //
        CraftRecipeRequest,          //
        CreativeInventoryAction,     //
        CookieResponse,              // Added in 1.20.6
        DebugSampleSubscription,     // Added in 1.20.6
        EditBook,                    //
        EnchantItem,                 // For 1.13.2 or below
        EntityAction,                //
        EntityNBTRequest,            //
        GenerateStructure,           // Added in 1.16
        HeldItemChange,              //
        InteractEntity,              //
        KeepAlive,                   //
        KnownDataPacks,              // Added in 1.20.6
        LockDifficulty,              //
        MessageAcknowledgment,       // Added in 1.19.1 (1.19.2)
        NameItem,                    //
        PickItem,                    //
        PingRequest,                 // Added in 1.20.2
        PlayerAbilities,             //
        PlayerBlockPlacement,        //
        PlayerDigging,               //
        PlayerMovement,              //
        PlayerPosition,              //
        PlayerPositionAndRotation,   //
        PlayerRotation,              //
        PlayerSession,               // Added in 1.19.3
        PluginMessage,               //
        Pong,                        //
        PrepareCraftingGrid,         // For 1.12 - 1.12.1 only
        QueryBlockNBT,               //
        RecipeBookData,              //
        ResourcePackStatus,          //
        SelectTrade,                 //
        SelectBundleItem,            // Added in 1.21.2
        SetBeaconEffect,             //
        SetDifficulty,               //
        SetDisplayedRecipe,          // Added in 1.16.2
        SetRecipeBookState,          // Added in 1.16.2
        SignedChatCommand,           // Added in 1.20.6
        Spectate,                    //
        SteerBoat,                   //
        SteerVehicle,                //
        TabComplete,                 //
        TeleportConfirm,             //
        Unknown,                     // For old version packet that have been removed and not used by mcc
        UpdateCommandBlock,          //
        UpdateCommandBlockMinecart,  //
        UpdateJigsawBlock,           //
        UpdateSign,                  //
        UpdateStructureBlock,        //
        UseItem,                     //
        VehicleMove,                 //
        InventoryConfirmation,       //
    }
}
