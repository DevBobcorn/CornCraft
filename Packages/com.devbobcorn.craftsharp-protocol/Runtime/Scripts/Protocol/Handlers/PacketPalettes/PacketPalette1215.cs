using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette1215 : PacketTypePalette
    {
        private readonly Dictionary<int, PacketTypesIn> typeIn = new()
        {
            { 0x00, PacketTypesIn.Bundle },                     // Added in 1.19.4
            { 0x01, PacketTypesIn.SpawnEntity },                // Changed in 1.19 (Wiki name: Spawn Entity) 
            { 0x02, PacketTypesIn.EntityAnimation },            // (Wiki name: Entity Animation (clientbound))
            { 0x03, PacketTypesIn.Statistics },                 // (Wiki name: Award Statistics)
            { 0x04, PacketTypesIn.BlockChangedAck },            // Added 1.19 (Wiki name: Acknowledge Block Change)  
            { 0x05, PacketTypesIn.BlockBreakAnimation },        // (Wiki name: Set Block Destroy Stage)
            { 0x06, PacketTypesIn.BlockEntityData },            //
            { 0x07, PacketTypesIn.BlockAction },                //
            { 0x08, PacketTypesIn.BlockChange },                // (Wiki name: Block Update)
            { 0x09, PacketTypesIn.BossBar },                    //
            { 0x0A, PacketTypesIn.ServerDifficulty },           // (Wiki name: Change Difficulty)
            { 0x0B, PacketTypesIn.ChunkBatchFinished },         // Added in 1.20.2  
            { 0x0C, PacketTypesIn.ChunkBatchStarted },          // Added in 1.20.2  
            { 0x0D, PacketTypesIn.ChunksBiomes },               // Added in 1.19.4
            { 0x0E, PacketTypesIn.ClearTiles },                 //
            { 0x0F, PacketTypesIn.TabComplete },                // (Wiki name: Command Suggestions Response)
            { 0x10, PacketTypesIn.DeclareCommands },            // (Wiki name: Commands)
            { 0x11, PacketTypesIn.CloseInventory },             // (Wiki name: Close Container (clientbound))
            { 0x12, PacketTypesIn.InventoryItems },             // (Wiki name: Set Container Content)
            { 0x13, PacketTypesIn.InventoryProperty },          // (Wiki name: Set Container Property)
            { 0x14, PacketTypesIn.SetSlot },                    // (Wiki name: Set Container Slot)
            { 0x15, PacketTypesIn.CookieRequest },              // Added in 1.20.6
            { 0x16, PacketTypesIn.SetCooldown },                //
            { 0x17, PacketTypesIn.ChatSuggestions },            // Added in 1.19.1
            { 0x18, PacketTypesIn.PluginMessage },              // (Wiki name: Plugin Message (clientbound))
            { 0x19, PacketTypesIn.DamageEvent },                // Added in 1.19.4
            { 0x1A, PacketTypesIn.DebugSample },                // Added in 1.20.6
            { 0x1B, PacketTypesIn.HideMessage },                // Added in 1.19.1
            { 0x1C, PacketTypesIn.Disconnect },                 //
            { 0x1D, PacketTypesIn.ProfilelessChatMessage },     // Added in 1.19.3 (Wiki name: Disguised Chat Message)
            { 0x1E, PacketTypesIn.EntityStatus },               // (Wiki name: Entity Event)
            { 0x1F, PacketTypesIn.EntityPositionSync },         // Added in 1.21.2 (Wiki name: Teleport Entity (the new one, not to be confused with the EntityTeleport packet below))
            { 0x20, PacketTypesIn.Explosion },                  // Changed in 1.19 (Location fields are now Double instead of Float) (Wiki name: Explosion) 
            { 0x21, PacketTypesIn.UnloadChunk },                // (Wiki name: Forget Chunk)
            { 0x22, PacketTypesIn.ChangeGameState },            // (Wiki name: Game Event)
            { 0x23, PacketTypesIn.OpenHorseWindow },            // (Wiki name: Horse Screen Open)
            { 0x24, PacketTypesIn.HurtAnimation },              // Added in 1.19.4
            { 0x25, PacketTypesIn.InitializeWorldBorder },      //
            { 0x26, PacketTypesIn.KeepAlive },                  //
            { 0x27, PacketTypesIn.ChunkData },                  //
            { 0x28, PacketTypesIn.Effect },                     // (Wiki name: World Event)
            { 0x29, PacketTypesIn.Particle },                   // Changed in 1.19 (Wiki name: Level Particle)  (No need to be implemented)
            { 0x2A, PacketTypesIn.UpdateLight },                // (Wiki name: Light Update)
            { 0x2B, PacketTypesIn.JoinGame },                   // Changed in 1.20.2 (Wiki name: Login (play)) 
            { 0x2C, PacketTypesIn.MapData },                    // (Wiki name: Map Item Data)
            { 0x2D, PacketTypesIn.TradeList },                  // (Wiki name: Merchant Offers)
            { 0x2E, PacketTypesIn.EntityPosition },             // (Wiki name: Move Entity Position)
            { 0x2F, PacketTypesIn.EntityPositionAndRotation },  // (Wiki name: Move Entity Position and Rotation)
            { 0x30, PacketTypesIn.MoveMinecart },               // Added in 1.21.2 (Wiki name: Move Minecart Along Track)
            { 0x31, PacketTypesIn.EntityRotation },             // (Wiki name: Move Entity Rotation)
            { 0x32, PacketTypesIn.VehicleMove },                // (Wiki name: Move Vehicle)
            { 0x33, PacketTypesIn.OpenBook },                   //
            { 0x34, PacketTypesIn.OpenInventory },              // (Wiki name: Open Screen)
            { 0x35, PacketTypesIn.OpenSignEditor },             //
            { 0x36, PacketTypesIn.Ping },                       // (Wiki name: Ping (play))
            { 0x37, PacketTypesIn.PingResponse },               // Added in 1.20.2 
            { 0x38, PacketTypesIn.CraftRecipeResponse },        // (Wiki name: Place Ghost Recipe)
            { 0x39, PacketTypesIn.PlayerAbilities },            //
            { 0x3A, PacketTypesIn.ChatMessage },                // Changed in 1.19 (Completely changed) (Wiki name: Player Chat Message)
            { 0x3B, PacketTypesIn.EndCombatEvent },             // (Wiki name: End Combat)
            { 0x3C, PacketTypesIn.EnterCombatEvent },           // (Wiki name: Enter Combat)
            { 0x3D, PacketTypesIn.DeathCombatEvent },           // (Wiki name: Combat Death)
            { 0x3E, PacketTypesIn.PlayerRemove },               // Added in 1.19.3 (Not used)
            { 0x3F, PacketTypesIn.PlayerInfo },                 // Changed in 1.19 (Heavy changes) 
            { 0x40, PacketTypesIn.FacePlayer },                 // (Wiki name: Player Look At)
            { 0x41, PacketTypesIn.PlayerPositionAndLook },      // (Wiki name: Synchronize Player Position)
            { 0x42, PacketTypesIn.PlayerRotation },             // Added in 1.21.2 (Wiki name: Player Rotation)
            { 0x43, PacketTypesIn.RecipeBookAdd },              // Added in 1.21.2 (Wiki name: Update Recipe Book)
            { 0x44, PacketTypesIn.RecipeBookRemove },           // Added in 1.21.2 (Wiki name: Update Recipe Book)
            { 0x45, PacketTypesIn.RecipeBookSettings },         // Added in 1.21.2 (Wiki name: Update Recipe Book)
            { 0x46, PacketTypesIn.DestroyEntities },            // (Wiki name: Remove Entities)
            { 0x47, PacketTypesIn.RemoveEntityEffect },         //
            { 0x48, PacketTypesIn.ResetScore },                 // Added in 1.20.3
            { 0x49, PacketTypesIn.RemoveResourcePack },         // Added in 1.20.3
            { 0x4A, PacketTypesIn.ResourcePackSend },           // (Wiki name: Add Resource pack (play))
            { 0x4B, PacketTypesIn.Respawn },                    // Changed in 1.20.2 
            { 0x4C, PacketTypesIn.EntityHeadLook },             // (Wiki name: Set Head Rotation)
            { 0x4D, PacketTypesIn.MultiBlockChange },           // (Wiki name: Update Section Blocks)
            { 0x4E, PacketTypesIn.SelectAdvancementTab },       //
            { 0x4F, PacketTypesIn.ServerData },                 // Added in 1.19
            { 0x50, PacketTypesIn.ActionBar },                  // (Wiki name: Set Action Bar Text)
            { 0x51, PacketTypesIn.WorldBorderCenter },          // (Wiki name: Set Border Center)
            { 0x52, PacketTypesIn.WorldBorderLerpSize },        //
            { 0x53, PacketTypesIn.WorldBorderSize },            // (Wiki name: Set World Border Size)
            { 0x54, PacketTypesIn.WorldBorderWarningDelay },    // (Wiki name: Set World Border Warning Delay)
            { 0x55, PacketTypesIn.WorldBorderWarningReach },    // (Wiki name: Set Border Warning Distance)
            { 0x56, PacketTypesIn.Camera },                     // (Wiki name: Set Camera)
            { 0x57, PacketTypesIn.UpdateViewPosition },         // (Wiki name: Set Center Chunk)
            { 0x58, PacketTypesIn.UpdateViewDistance },         // (Wiki name: Set Render Distance)
            { 0x59, PacketTypesIn.SetCursorItem },              // Added in 1.21.2 (Wiki name: Set Cursor Item)
            { 0x5A, PacketTypesIn.SpawnPosition },              // (Wiki name: Set Default Spawn Position)
            { 0x5B, PacketTypesIn.DisplayScoreboard },          // (Wiki name: Set Display Objective)
            { 0x5C, PacketTypesIn.EntityMetadata },             // (Wiki name: Set Entity Metadata)
            { 0x5D, PacketTypesIn.AttachEntity },               // (Wiki name: Link Entities)
            { 0x5E, PacketTypesIn.EntityVelocity },             // (Wiki name: Set Entity Velocity)
            { 0x5F, PacketTypesIn.EntityEquipment },            // (Wiki name: Set Equipment)
            { 0x60, PacketTypesIn.SetExperience },              // Changed in 1.20.2 
            { 0x61, PacketTypesIn.UpdateHealth },               // (Wiki name: Set Health)
            { 0x62, PacketTypesIn.HeldItemChange },             // (Wiki name: Set Held Item)
            { 0x63, PacketTypesIn.ScoreboardObjective },        // (Wiki name: Update Objectives) - Changed in 1.20.3
            { 0x64, PacketTypesIn.SetPassengers },              //
            { 0x65, PacketTypesIn.SetPlayerInventorySlot },     // Added in 1.21.2
            { 0x66, PacketTypesIn.Teams },                      // (Wiki name: Update Teams)
            { 0x67, PacketTypesIn.UpdateScore },                // (Wiki name: Update Score)
            { 0x68, PacketTypesIn.UpdateSimulationDistance },   // (Wiki name: Set Simulation Distance)
            { 0x69, PacketTypesIn.SetTitleSubTitle },           // (Wiki name: Set Subtitle Test)
            { 0x6A, PacketTypesIn.TimeUpdate },                 // (Wiki name: Set Time)
            { 0x6B, PacketTypesIn.SetTitleText },               // (Wiki name: Set Title)
            { 0x6C, PacketTypesIn.SetTitleTime },               // (Wiki name: Set Title Animation Times)
            { 0x6D, PacketTypesIn.EntitySoundEffect },          // (Wiki name: Sound Entity)
            { 0x6E, PacketTypesIn.SoundEffect },                // Changed in 1.19 (Added "Seed" field) (Wiki name: Sound Effect)  (No need to be implemented)
            { 0x6F, PacketTypesIn.StartConfiguration },         // Added in 1.20.2 
            { 0x70, PacketTypesIn.StopSound },                  //
            { 0x71, PacketTypesIn.StoreCookie },                // Added in 1.20.6
            { 0x72, PacketTypesIn.SystemChat },                 // Added in 1.19 (Wiki name: System Chat Message)
            { 0x73, PacketTypesIn.PlayerListHeaderAndFooter },  // (Wiki name: Set Tab List Header And Footer)
            { 0x74, PacketTypesIn.NBTQueryResponse },           // (Wiki name: Tag Query Response)
            { 0x75, PacketTypesIn.CollectItem },                // (Wiki name: Pickup Item)
            { 0x76, PacketTypesIn.EntityTeleport },             // (Wiki name: Synchronize Vehicle Position)
            { 0x77, PacketTypesIn.TestInstanceBlockStatus },    // Added in 1.21.5
            { 0x78, PacketTypesIn.SetTickingState },            // Added in 1.20.3
            { 0x79, PacketTypesIn.StepTick },                   // Added in 1.20.3
            { 0x7A, PacketTypesIn.Transfer },                   // Added in 1.20.6
            { 0x7B, PacketTypesIn.Advancements },               // (Wiki name: Update Advancements) (Unused)
            { 0x7C, PacketTypesIn.EntityProperties },           // (Wiki name: Update Attributes)
            { 0x7D, PacketTypesIn.EntityEffect },               // Changed in 1.19 (Added "Has Factor Data" and "Factor Codec" fields) (Wiki name: Entity Effect) 
            { 0x7E, PacketTypesIn.DeclareRecipes },             // (Wiki name: Update Recipes) (Unused)
            { 0x7F, PacketTypesIn.Tags },                       // (Wiki name: Update Tags)
            { 0x80, PacketTypesIn.ProjectilePower },            // Added in 1.20.6
            { 0x81, PacketTypesIn.CustomReportDetails },        // Added in 1.21
            { 0x82, PacketTypesIn.ServerLinks }                 // Added in 1.21
        };

        private readonly Dictionary<int, PacketTypesOut> typeOut = new()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },             // (Wiki name: Confirm Teleportation)
            { 0x01, PacketTypesOut.QueryBlockNBT },               // (Wiki name: Query Block Entity Tag)
            { 0x02, PacketTypesOut.SelectBundleItem },            // Added in 1.21.2 (Wiki name: Bundle Item Selected)
            { 0x03, PacketTypesOut.SetDifficulty },               // (Wiki name: Change Difficulty)
            { 0x04, PacketTypesOut.MessageAcknowledgment },       // Added in 1.19.1
            { 0x05, PacketTypesOut.ChatCommand },                 // Added in 1.19
            { 0x06, PacketTypesOut.SignedChatCommand },           // Added in 1.20.6
            { 0x07, PacketTypesOut.ChatMessage },                 // Changed in 1.19 (Completely changed) (Wiki name: Chat)
            { 0x08, PacketTypesOut.PlayerSession },               // Added in 1.19.3
            { 0x09, PacketTypesOut.ChunkBatchReceived },          // Added in 1.20.2 
            { 0x0A, PacketTypesOut.ClientStatus },                // (Wiki name: Client Command)
            { 0x0B, PacketTypesOut.ClientTickEnd },               // Added in 1.21.2 (Wiki name: Client Tick End)
            { 0x0C, PacketTypesOut.ClientSettings },              // (Wiki name: Client Information)
            { 0x0D, PacketTypesOut.TabComplete },                 // (Wiki name: Command Suggestions Request)
            { 0x0E, PacketTypesOut.AcknowledgeConfiguration },    // Added in 1.20.2 
            { 0x0F, PacketTypesOut.ClickInventoryButton },        // (Wiki name: Click Container Button)
            { 0x10, PacketTypesOut.ClickInventory },              // (Wiki name: Click Container)
            { 0x11, PacketTypesOut.CloseInventory },              // (Wiki name: Close Container (serverbound))
            { 0x12, PacketTypesOut.ChangeInventorySlotState },    // Added in 1.20.3
            { 0x13, PacketTypesOut.CookieResponse },              // Added in 1.20.6
            { 0x14, PacketTypesOut.PluginMessage },               // (Wiki name: Serverbound Plugin Message)
            { 0x15, PacketTypesOut.DebugSampleSubscription },     // Added in 1.20.6
            { 0x16, PacketTypesOut.EditBook },                    //
            { 0x17, PacketTypesOut.EntityNBTRequest },            // (Wiki name: Query Entity Tag)
            { 0x18, PacketTypesOut.InteractEntity },              // (Wiki name: Interact)
            { 0x19, PacketTypesOut.GenerateStructure },           // (Wiki name: Jigsaw Generate)
            { 0x1A, PacketTypesOut.KeepAlive },                   // (Wiki name: Serverbound Keep Alive (play))
            { 0x1B, PacketTypesOut.LockDifficulty },              //
            { 0x1C, PacketTypesOut.PlayerPosition },              // (Wiki name: Move Player Position)
            { 0x1D, PacketTypesOut.PlayerPositionAndRotation },   // (Wiki name: Set Player Position and Rotation)
            { 0x1E, PacketTypesOut.PlayerRotation },              // (Wiki name: Set Player Rotation)
            { 0x1F, PacketTypesOut.PlayerMovement },              // (Wiki name: Set Player Movement Flags)
            { 0x20, PacketTypesOut.VehicleMove },                 // (Wiki name: Move Vehicle (serverbound))
            { 0x21, PacketTypesOut.SteerBoat },                   // (Wiki name: Paddle Boat)
            { 0x22, PacketTypesOut.PickItemFromBlock },           // Added in 1.21.4
            { 0x23, PacketTypesOut.PickItemFromEntity },          // Added in 1.21.4
            { 0x24, PacketTypesOut.PingRequest },                 // Added in 1.20.2 
            { 0x25, PacketTypesOut.CraftRecipeRequest },          // (Wiki name: Place recipe)
            { 0x26, PacketTypesOut.PlayerAbilities },             //
            { 0x27, PacketTypesOut.PlayerDigging },               // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Player Action) 
            { 0x28, PacketTypesOut.EntityAction },                // (Wiki name: Player Command)
            { 0x29, PacketTypesOut.SteerVehicle },                // (Wiki name: Player Input)
            { 0x2A, PacketTypesOut.PlayerLoaded },                // Added in 1.21.4
            { 0x2B, PacketTypesOut.Pong },                        // (Wiki name: Pong (play))
            { 0x2C, PacketTypesOut.SetDisplayedRecipe },          // (Wiki name: Recipe Book Change Settings)
            { 0x2D, PacketTypesOut.SetRecipeBookState },          // (Wiki name: Recipe Book Seen Recipe)
            { 0x2E, PacketTypesOut.NameItem },                    // (Wiki name: Rename Item)
            { 0x2F, PacketTypesOut.ResourcePackStatus },          // (Wiki name: Resource Pack (serverbound))
            { 0x30, PacketTypesOut.AdvancementTab },              // (Wiki name: Seen Advancements)
            { 0x31, PacketTypesOut.SelectTrade },                 //
            { 0x32, PacketTypesOut.SetBeaconEffect },             // Changed in 1.19 (No need to be implemented yet)
            { 0x33, PacketTypesOut.HeldItemChange },              // (Wiki name: Set Carried Item (serverbound))
            { 0x34, PacketTypesOut.UpdateCommandBlock },          // (Wiki name: Program Command Block)
            { 0x35, PacketTypesOut.UpdateCommandBlockMinecart },  // (Wiki name: Program Command Block Minecart)
            { 0x36, PacketTypesOut.CreativeInventoryAction },     // (Wiki name: Set Creative Mode Slot)
            { 0x37, PacketTypesOut.UpdateJigsawBlock },           // (Wiki name: Program Jigsaw Block)
            { 0x38, PacketTypesOut.UpdateStructureBlock },        // (Wiki name: Program Structure Block)
            { 0x39, PacketTypesOut.UpdateTestBlock },             // Added in 1.21.5 (Wiki name: Set Test Block)
            { 0x3A, PacketTypesOut.UpdateSign },                  // (Wiki name: Update Sign)
            { 0x3B, PacketTypesOut.Animation },                   // (Wiki name: Swing Arm)
            { 0x3C, PacketTypesOut.TestInstanceBlockAction },     // Added in 1.21.5
            { 0x3D, PacketTypesOut.Spectate },                    // (Wiki name: Teleport To Entity)
            { 0x3E, PacketTypesOut.PlayerBlockPlacement },        // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Use Item On) 
            { 0x3F, PacketTypesOut.UseItem },                     // Changed in 1.19 (Added a "Sequence" field) (Wiki name: Use Item) 
        };

        private readonly Dictionary<int, ConfigurationPacketTypesIn> configurationTypesIn = new()
        {
            { 0x00, ConfigurationPacketTypesIn.CookieRequest },
            { 0x01, ConfigurationPacketTypesIn.PluginMessage },
            { 0x02, ConfigurationPacketTypesIn.Disconnect },
            { 0x03, ConfigurationPacketTypesIn.FinishConfiguration },
            { 0x04, ConfigurationPacketTypesIn.KeepAlive },
            { 0x05, ConfigurationPacketTypesIn.Ping },
            { 0x06, ConfigurationPacketTypesIn.ResetChat },
            { 0x07, ConfigurationPacketTypesIn.RegistryData },
            { 0x08, ConfigurationPacketTypesIn.RemoveResourcePack },
            { 0x09, ConfigurationPacketTypesIn.ResourcePack },
            { 0x0A, ConfigurationPacketTypesIn.StoreCookie },
            { 0x0B, ConfigurationPacketTypesIn.Transfer },
            { 0x0C, ConfigurationPacketTypesIn.FeatureFlags },
            { 0x0D, ConfigurationPacketTypesIn.UpdateTags },
            { 0x0E, ConfigurationPacketTypesIn.KnownDataPacks },
            { 0x0F, ConfigurationPacketTypesIn.CustomReportDetails }, // Added in 1.21 (Not used)
            { 0x10, ConfigurationPacketTypesIn.ServerLinks }          // Added in 1.21 (Not used)
        };

        private readonly Dictionary<int, ConfigurationPacketTypesOut> configurationTypesOut = new()
        {
            { 0x00, ConfigurationPacketTypesOut.ClientInformation },
            { 0x01, ConfigurationPacketTypesOut.CookieResponse },
            { 0x02, ConfigurationPacketTypesOut.PluginMessage },
            { 0x03, ConfigurationPacketTypesOut.FinishConfiguration },
            { 0x04, ConfigurationPacketTypesOut.KeepAlive },
            { 0x05, ConfigurationPacketTypesOut.Pong },
            { 0x06, ConfigurationPacketTypesOut.ResourcePackResponse },
            { 0x07, ConfigurationPacketTypesOut.KnownDataPacks }
        };
        
        protected override Dictionary<int, PacketTypesIn> GetListIn() => typeIn;
        protected override Dictionary<int, PacketTypesOut> GetListOut() => typeOut;
        protected override Dictionary<int, ConfigurationPacketTypesIn> GetConfigurationListIn() => configurationTypesIn!;
        protected override Dictionary<int, ConfigurationPacketTypesOut> GetConfigurationListOut() => configurationTypesOut!;
    }
}