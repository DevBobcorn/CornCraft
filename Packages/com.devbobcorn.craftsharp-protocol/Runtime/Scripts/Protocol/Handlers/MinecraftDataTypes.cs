#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using CraftSharp.Inventory;
using CraftSharp.Inventory.Recipe;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Handlers.StructuredComponents.Registries;
using CraftSharp.Protocol.Message;

namespace CraftSharp.Protocol.Handlers
{
    public class MinecraftDataTypes : IMinecraftDataTypes
    {
        /// <summary>
        /// Protocol version for adjusting data types
        /// </summary>
        private readonly int protocolVersion;

        public bool UseAnonymousNBT => protocolVersion >= ProtocolMinecraft.MC_1_20_2_Version;

        public bool UseResourceLocationForMobAttributeModifierId =>
                protocolVersion >= ProtocolMinecraft.MC_1_21_1_Version;
        
        /// <summary>
        /// Initialize a new MinecraftDataTypes instance
        /// </summary>
        /// <param name="protocol">Protocol version</param>
        public MinecraftDataTypes(int protocol)
        {
            protocolVersion = protocol;
        }

        #region Complex data readers

        /// <summary>
        /// Read a single item slot from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public ItemStack? ReadNextItemSlot(Queue<byte> cache, ItemPalette itemPalette)
        {
            if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
            {
                var structuredComponentsToAdd = new Dictionary<ResourceLocation, StructuredComponent>();
                var structuredComponentsToRemove = new List<ResourceLocation>();

                var itemCount = DataTypes.ReadNextVarInt(cache);

                if (itemCount <= 0) return null;
                
                var itemId = DataTypes.ReadNextVarInt(cache);
                var item = new ItemStack(itemPalette.GetByNumId(itemId), itemCount);
                    
                var numberOfComponentsToAdd = DataTypes.ReadNextVarInt(cache);
                var numberOfComponentsToRemove = DataTypes.ReadNextVarInt(cache);
                
                var structuredComponentRegistry = ItemPalette.INSTANCE.ComponentRegistry;

                var componentsBytes = cache.ToArray();
                var byteCount = componentsBytes.Length;
                var copyStart = 0;

                if (numberOfComponentsToAdd > 0)
                {
                    item.ReceivedComponentsToAdd = new Dictionary<int, byte[]>();

                    for (var i = 0; i < numberOfComponentsToAdd; i++)
                    {
                        var componentTypeNumId = DataTypes.ReadNextVarInt(cache);
                        var component = structuredComponentRegistry.ParseComponent(componentTypeNumId, cache);

                        structuredComponentsToAdd.Add(
                            structuredComponentRegistry.GetIdByNumId(componentTypeNumId), component);

                        var length = byteCount - cache.Count; // Length of this component in bytes, including the varint num id

                        var componentBytes = new byte[length]; // Store bytes of this component
                        Array.Copy(componentsBytes, copyStart, componentBytes, 0, length);

                        item.ReceivedComponentsToAdd.Add(componentTypeNumId, componentBytes);

                        byteCount -= length;
                        copyStart += length;
                    }
                }

                if (numberOfComponentsToRemove > 0)
                {
                    item.ReceivedComponentsToRemove = new HashSet<int>();

                    for (var i = 0; i < numberOfComponentsToRemove; i++)
                    {
                        var componentTypeNumId = DataTypes.ReadNextVarInt(cache); // The type of component to remove
                        structuredComponentsToRemove.Add(structuredComponentRegistry.GetIdByNumId(componentTypeNumId));

                        item.ReceivedComponentsToRemove.Add(componentTypeNumId);
                    }
                }
                
                // Apply these changed components
                item.ApplyComponents(structuredComponentsToAdd, structuredComponentsToRemove);
                
                return item;
            }
            else // MC 1.13.2 and greater
            {
                var itemPresent = DataTypes.ReadNextBool(cache);

                if (!itemPresent)
                    return null;

                var itemId = DataTypes.ReadNextVarInt(cache);

                if (itemId == -1)
                    return null;

                var type = itemPalette.GetByNumId(itemId);
                var itemCount = DataTypes.ReadNextByte(cache);
                var nbt = DataTypes.ReadNextNbt(cache, UseAnonymousNBT);
                return new ItemStack(type, itemCount, nbt);
            }
        }

        /// <summary>
        /// Read entity information from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="entityPalette">Mappings for converting entity type Ids to EntityType</param>
        /// <param name="living">TRUE for living entities (layout differs)</param>
        /// <returns>Entity information</returns>
        public EntityData ReadNextEntity(Queue<byte> cache, EntityTypePalette entityPalette, bool living)
        {
            var entityId = DataTypes.ReadNextVarInt(cache);
            var entityUUID = DataTypes.ReadNextUUID(cache); // MC 1.8+

            EntityType entityType;
            // Entity type data type change from byte to varint after 1.14
            entityType = entityPalette.GetByNumId(DataTypes.ReadNextVarInt(cache));

            var entityX = DataTypes.ReadNextDouble(cache);
            var entityY = DataTypes.ReadNextDouble(cache);
            var entityZ = DataTypes.ReadNextDouble(cache);

            var data = -1;
            byte entityPitch, entityYaw, entityHeadYaw;

            if (living)
            {
                entityYaw = DataTypes.ReadNextByte(cache); // Yaw
                entityPitch = DataTypes.ReadNextByte(cache); // Pitch
                entityHeadYaw = DataTypes.ReadNextByte(cache); // Head Yaw
            }
            else
            {
                entityPitch = DataTypes.ReadNextByte(cache); // Pitch
                entityYaw = DataTypes.ReadNextByte(cache); // Yaw
                entityHeadYaw = entityYaw;

                if (protocolVersion >= ProtocolMinecraft.MC_1_19_Version)
                    entityYaw = DataTypes.ReadNextByte(cache); // Head Yaw

                // Data
                data = protocolVersion >= ProtocolMinecraft.MC_1_19_Version 
                    ? DataTypes.ReadNextVarInt(cache) : DataTypes.ReadNextInt(cache);
            }

            return new EntityData(entityId, entityType, new Location(entityX, entityY, entityZ), entityYaw, entityPitch, entityHeadYaw, data);
        }

        /// <summary>
        /// Read a Entity MetaData and remove it from the cache
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="itemPalette"></param>
        /// <param name="metadataPalette"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public Dictionary<int, object?> ReadNextMetadata(Queue<byte> cache, ItemPalette itemPalette, EntityMetadataPalette metadataPalette)
        {
            Dictionary<int, object?> data = new();
            byte key = DataTypes.ReadNextByte(cache);
            const byte terminateValue = (byte) 0xff; // 1.9+

            while (key != terminateValue)
            {
                int typeId = DataTypes.ReadNextVarInt(cache); // 1.9+

                EntityMetadataType type;
                try
                {
                    type = metadataPalette.GetDataType(typeId);
                }
                catch (KeyNotFoundException)
                {
                    throw new InvalidDataException("Unknown Metadata Type ID " + typeId + ". Is this up to date for new MC Version?");
                }

                // Value's data type is depended on Type
                object? value = null;

                switch (type)
                {
                    case EntityMetadataType.Short: // 1.8 only
                        value = DataTypes.ReadNextShort(cache);
                        break;
                    case EntityMetadataType.Int: // 1.8 only
                        value = DataTypes.ReadNextInt(cache);
                        break;
                    case EntityMetadataType.Vector3Int: // 1.8 only
                        value = new Vector3Int(
                            DataTypes.ReadNextInt(cache),
                            DataTypes.ReadNextInt(cache),
                            DataTypes.ReadNextInt(cache)
                        );
                        break;
                    case EntityMetadataType.Byte: // byte
                        value = DataTypes.ReadNextByte(cache);
                        break;
                    case EntityMetadataType.VarInt: // VarInt
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.VarLong: // Long
                        value = DataTypes.ReadNextVarLong(cache);
                        break;
                    case EntityMetadataType.Float: // Float
                        value = DataTypes.ReadNextFloat(cache);
                        break;
                    case EntityMetadataType.String: // String
                        value = DataTypes.ReadNextString(cache);
                        break;
                    case EntityMetadataType.Chat: // Chat
                        value = ReadNextChat(cache);
                        break;
                    case EntityMetadataType.OptionalChat: // Optional Chat
                        if (DataTypes.ReadNextBool(cache))
                            value = ReadNextChat(cache);
                        break;
                    case EntityMetadataType.Slot: // Slot
                        value = ReadNextItemSlot(cache, itemPalette);
                        break;
                    case EntityMetadataType.Boolean: // Boolean
                        value = DataTypes.ReadNextBool(cache);
                        break;
                    case EntityMetadataType.Rotations: // Rotations (3x floats)
                        value = new Vector3
                        (
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache)
                        );
                        break;
                    case EntityMetadataType.Position: // Position
                        value = DataTypes.ReadNextLocation(cache);
                        break;
                    case EntityMetadataType.OptionalPosition: // Optional Position
                        if (DataTypes.ReadNextBool(cache))
                        {
                            value = DataTypes.ReadNextLocation(cache);
                        }
                        break;
                    case EntityMetadataType.Direction: // Direction (VarInt)
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.OptionalUUID: // Optional UUID
                        if (DataTypes.ReadNextBool(cache))
                        {
                            value = DataTypes.ReadNextUUID(cache);
                        }
                        break;
                    case EntityMetadataType.BlockState: // BlockID (VarInt)
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.OptionalBlockState: // Optional BlockID (VarInt)
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.Nbt: // NBT
                        value = DataTypes.ReadNextNbt(cache, UseAnonymousNBT);
                        break;
                    case EntityMetadataType.Particle: // Particle
                        value = ReadParticleData(cache, itemPalette);
                        break;
                    case EntityMetadataType.Particles: // Particles
                        value = ReadParticlesData(cache, itemPalette);
                        break;
                    case EntityMetadataType.VillagerData: // Villager Data (3x VarInt)
                        value = new Vector3Int
                        (
                            DataTypes.ReadNextVarInt(cache),
                            DataTypes.ReadNextVarInt(cache),
                            DataTypes.ReadNextVarInt(cache)
                        );
                        break;
                    case EntityMetadataType.OptionalVarInt: // Optional VarInt
                        if (DataTypes.ReadNextBool(cache))
                        {
                            value = DataTypes.ReadNextVarInt(cache);
                        }
                        break;
                    case EntityMetadataType.Pose: // Pose
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.CatVariant: // Cat Variant
                    case EntityMetadataType.CowVariant: // Cow Variant
                    case EntityMetadataType.WolfVariant: // Wolf Variant
                    case EntityMetadataType.WolfSoundVariant: // Wolf Sound Variant
                    case EntityMetadataType.FrogVariant: // Frog Variant
                    case EntityMetadataType.PigVariant: // Pig Variant
                    case EntityMetadataType.ChickenVariant: // Chicken Variant
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.GlobalPosition: // GlobalPos
                        // Dimension and blockLoc, currently not in use
                        value = new Tuple<string, Location>(DataTypes.ReadNextString(cache), DataTypes.ReadNextLocation(cache));
                        break;
                    case EntityMetadataType.OptionalGlobalPosition:
                        // FIXME: wiki.vg is bool + string + location
                        //        but minecraft-data is bool + string
                        if (DataTypes.ReadNextBool(cache))
                        {
                            // Dimension and blockLoc, currently not in use
                            value = new Tuple<string, Location>(DataTypes.ReadNextString(cache), DataTypes.ReadNextLocation(cache));
                        }
                        break;
                    case EntityMetadataType.PaintingVariant: // Painting Variant
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.SnifferState: // Sniffer state
                    case EntityMetadataType.ArmadilloState: // Armadillo state
                        value = DataTypes.ReadNextVarInt(cache);
                        break;
                    case EntityMetadataType.Vector3: // Vector 3f
                        value = new Vector3
                        (
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache)
                        );
                        break;
                    case EntityMetadataType.Quaternion: // Quaternion
                        value = new Quaternion
                        (
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache),
                            DataTypes.ReadNextFloat(cache)
                        );
                        break;
                }

                data[key] = value;
                key = DataTypes.ReadNextByte(cache);
            }
            return data;
        }

        /// <summary>
        /// Read an array of particles with optional extra data (Added in 1.20.5)
        /// </summary>
        private ParticleExtraData[] ReadParticlesData(Queue<byte> cache, ItemPalette itemPalette)
        {
            var count = DataTypes.ReadNextVarInt(cache);
            var result = new ParticleExtraData[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = ReadParticleData(cache, itemPalette);
            }
            
            return result;
        }

        /// <summary>
        /// Read particle with optional extra data
        /// </summary>
        public ParticleExtraData ReadParticleData(Queue<byte> cache, ItemPalette itemPalette)
        {
            var particleNumId = DataTypes.ReadNextVarInt(cache);
            var particleType = ParticleTypePalette.INSTANCE.GetByNumId(particleNumId);

            return particleType.ExtraDataType switch
            {
                ParticleExtraDataType.None                => ParticleExtraData.Empty,
                ParticleExtraDataType.Block               => ReadBlockParticle(cache),
                ParticleExtraDataType.Dust                => ReadDustParticle(cache,
                        useFloats:    protocolVersion < ProtocolMinecraft.MC_1_21_4_Version),
                ParticleExtraDataType.DustColorTransition => ReadDustColorTransitionParticle(cache,
                        useFloats:    protocolVersion < ProtocolMinecraft.MC_1_21_4_Version),
                ParticleExtraDataType.EntityEffect        => ReadEntityEffectParticle(cache),
                ParticleExtraDataType.SculkCharge         => ReadSculkChargeParticle(cache),
                ParticleExtraDataType.Item                => ReadItemParticle(cache, itemPalette),
                ParticleExtraDataType.Vibration           => ReadVibrationParticle(cache,
                        hasOrigin:    protocolVersion <= ProtocolMinecraft.MC_1_18_2_Version,
                        hasEyeHeight: protocolVersion >= ProtocolMinecraft.MC_1_19_2_Version,
                        useTypeId:    protocolVersion <= ProtocolMinecraft.MC_1_20_4_Version),
                ParticleExtraDataType.Shriek              => ReadShriekParticle(cache),

                _                                         => ParticleExtraData.Empty,
            };
        }

        private static BlockParticleExtraData ReadBlockParticle(Queue<byte> cache)
        {
            var stateId = DataTypes.ReadNextVarInt(cache);

            return new BlockParticleExtraData(stateId);
        }

        private static DustParticleExtraData ReadDustParticle(Queue<byte> cache, bool useFloats)
        {
            Color32 color;

            if (useFloats)
            {
                var r = DataTypes.ReadNextFloat(cache); // Red
                var g = DataTypes.ReadNextFloat(cache); // Green
                var b = DataTypes.ReadNextFloat(cache); // Blue
                color = new Color32((byte) (r * 255), (byte) (g * 255), (byte) (b * 255), 255);
            }
            else
            {
                var rgb = DataTypes.ReadNextInt(cache); // 0xRRGGBB
                color = new Color32((byte) ((rgb & 0xFF0000) >> 16), (byte) ((rgb & 0xFF00) >> 8), (byte) (rgb & 0xFF), 255);
            }

            var scale = DataTypes.ReadNextFloat(cache); // Scale

            return new DustParticleExtraData(color, scale);
        }

        private static DustColorTransitionParticleExtraData ReadDustColorTransitionParticle(Queue<byte> cache, bool useFloats)
        {
            Color32 colorFrom, colorTo;
            float scale;

            if (useFloats)
            {
                var fr = DataTypes.ReadNextFloat(cache); // From red
                var fg = DataTypes.ReadNextFloat(cache); // From green
                var fb = DataTypes.ReadNextFloat(cache); // From blue
                scale  = DataTypes.ReadNextFloat(cache); // Scale
                var tr = DataTypes.ReadNextFloat(cache); // To red
                var tg = DataTypes.ReadNextFloat(cache); // To green
                var tb = DataTypes.ReadNextFloat(cache); // To Blue

                colorFrom = new Color32((byte) (fr * 255), (byte) (fg * 255), (byte) (fb * 255), 255);
                colorTo   = new Color32((byte) (tr * 255), (byte) (tg * 255), (byte) (tb * 255), 255);
            }
            else
            {
                var rgbFrom = DataTypes.ReadNextInt(cache); // 0xRRGGBB
                var rgbT = DataTypes.ReadNextInt(cache); // 0xRRGGBB
                scale  = DataTypes.ReadNextFloat(cache); // Scale

                colorFrom = new Color32((byte) ((rgbFrom & 0xFF0000) >> 16), (byte) ((rgbFrom & 0xFF00) >> 8), (byte) (rgbFrom & 0xFF), 255);
                colorTo   = new Color32((byte) ((rgbT & 0xFF0000) >> 16), (byte) ((rgbT & 0xFF00) >> 8), (byte) (rgbT & 0xFF), 255);
            }

            return new DustColorTransitionParticleExtraData(colorFrom, colorTo, scale);
        }

        private static EntityEffectParticleExtraData ReadEntityEffectParticle(Queue<byte> cache)
        {
            var argb = DataTypes.ReadNextInt(cache); // 0xAARRGGGBB
            var color = new Color32((byte) ((argb & 0xFF0000) >> 16), (byte) ((argb & 0xFF00) >> 8), (byte) (argb & 0xFF), (byte) (argb >> 24));

            return new EntityEffectParticleExtraData(color);
        }

        private ItemParticleExtraData ReadItemParticle(Queue<byte> cache, ItemPalette itemPalette)
        {
            var itemStack = ReadNextItemSlot(cache, itemPalette);

            return new ItemParticleExtraData(itemStack);
        }

        private static SculkChargeParticleExtraData ReadSculkChargeParticle(Queue<byte> cache)
        {
            var roll = DataTypes.ReadNextFloat(cache);

            return new SculkChargeParticleExtraData(roll);
        }

        private static ShriekParticleExtraData ReadShriekParticle(Queue<byte> cache)
        {
            var delay = DataTypes.ReadNextVarInt(cache);

            return new ShriekParticleExtraData(delay);
        }

        /// <summary>
        /// For version 1.17 - 1.20.4, type ids('minecraft:block' and 'minecraft:entity')
        /// are used. For 1.20.5+, the numeral ids of these types are sent instead.
        /// <br/>
        /// During version 1.17 - 1.18.2, there's also an 'origin' field of Location type
        /// sent before destination data. For 1.19+, an 'eye height' field is included for
        /// 'minecraft:entity' position source type, following the entity id field.
        /// </summary>
        private static VibrationParticleExtraData ReadVibrationParticle(Queue<byte> cache, bool hasOrigin, bool hasEyeHeight, bool useTypeId)
        {
            if (hasOrigin) // 1.17 - 1.18.2
            {
                // Ignore it
                DataTypes.ReadNextLocation(cache); // Origin location
            }

            bool useBlockPos; // Use a bool here since only 2 types are defined

            if (useTypeId) // 1.17 - 1.20.4
            {
                var positionSourceTypeId = DataTypes.ReadNextString(cache); // Position source type
                useBlockPos = positionSourceTypeId switch
                {
                    "minecraft:block"  => true,
                    "minecraft:entity" => false,

                    _                  => throw new InvalidDataException($"Unknown position source type id: {positionSourceTypeId}")
                };
            }
            else // 1.20.5+
            {
                var positionSourceTypeNumId = DataTypes.ReadNextVarInt(cache); // Position source type's numeral id
                useBlockPos = positionSourceTypeNumId switch
                {
                    0 => true,
                    1 => false,

                    _ => throw new InvalidDataException($"Unknown position source type num id: {positionSourceTypeNumId}")
                };
            }

            if (useBlockPos) // minecraft:block
            {
                var loc = DataTypes.ReadNextLocation(cache);
                var ticks = DataTypes.ReadNextVarInt(cache);

                return new VibrationParticleExtraData(loc, ticks);
            }
            else // minecraft:entity (Not really used)
            {
                var entityId = DataTypes.ReadNextVarInt(cache);
                var eyeHeight = hasEyeHeight ? DataTypes.ReadNextFloat(cache) : 0F;
                var ticks = DataTypes.ReadNextVarInt(cache);

                return new VibrationParticleExtraData(entityId, eyeHeight, ticks);
            }
        }

        /// <summary>
        /// Read recipe with optional extra data
        /// </summary>
        public (BaseRecipeType, ResourceLocation, RecipeExtraData) ReadRecipeData(Queue<byte> cache, ItemPalette itemPalette)
        {
            BaseRecipeType recipeType;
            ResourceLocation recipeId;

            if (protocolVersion < ProtocolMinecraft.MC_1_20_6_Version) // Prior to 1.20.5, sent as id
            {
                var recipeTypeId = ResourceLocation.FromString(DataTypes.ReadNextString(cache));
                recipeType = RecipeTypePalette.INSTANCE.GetById(recipeTypeId);
                
                recipeId = ResourceLocation.FromString(DataTypes.ReadNextString(cache));
                
                //Debug.Log($"Reading recipe {recipeId} of type {recipeTypeId}");
            }
            else // 1.20.5+, sent as num id
            {
                recipeId = ResourceLocation.FromString(DataTypes.ReadNextString(cache));
                
                var recipeTypeNumId = DataTypes.ReadNextVarInt(cache);
                recipeType = RecipeTypePalette.INSTANCE.GetByNumId(recipeTypeNumId);
                
                //Debug.Log($"Reading recipe {recipeId} of type {recipeTypeNumId}");
            }

            RecipeExtraData recipeData = recipeType.ExtraDataType switch
            {
                RecipeExtraDataType.CraftingShaped        => ReadCraftingShapedRecipe(cache, itemPalette),
                RecipeExtraDataType.CraftingShapeless     => ReadCraftingShapelessRecipe(cache, itemPalette),
                RecipeExtraDataType.Cooking               => ReadCookingRecipe(cache, itemPalette),
                RecipeExtraDataType.Stonecutting          => ReadStonecuttingRecipe(cache, itemPalette),
                RecipeExtraDataType.Smithing              => ReadLegacySmithingRecipe(cache, itemPalette),
                RecipeExtraDataType.SmithingTransform     => ReadSmithingTransformRecipe(cache, itemPalette),
                RecipeExtraDataType.SmithingTrim          => ReadSmithingTrimRecipe(cache, itemPalette),
                _                                         => ReadCraftingSpecialRecipe(cache)
            };

            return (recipeType, recipeId, recipeData);
        }

        private ItemStack?[] ReadIngredient(Queue<byte> cache, ItemPalette itemPalette)
        {
            int count = DataTypes.ReadNextVarInt(cache);
            ItemStack?[] ingredient = new ItemStack?[count];

            for (int i = 0; i < count; i++)
                ingredient[i] = ReadNextItemSlot(cache, itemPalette);
            
            return ingredient;
        }

        private CraftingShapedExtraData ReadCraftingShapedRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            int width = 0, height = 0;
            
            if (protocolVersion < ProtocolMinecraft.MC_1_20_4_Version)
            {
                width = DataTypes.ReadNextVarInt(cache);
                height = DataTypes.ReadNextVarInt(cache);
            }
            
            var group = DataTypes.ReadNextString(cache);
            // Category field added in 1.19.3
            var category = protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version
                ? (CraftingRecipeCategory) DataTypes.ReadNextVarInt(cache) : CraftingRecipeCategory.Misc;
            
            if (protocolVersion >= ProtocolMinecraft.MC_1_20_4_Version)
            {
                width = DataTypes.ReadNextVarInt(cache);
                height = DataTypes.ReadNextVarInt(cache);
            }
            
            var ingredients = new List<ItemStack?[]>(width * height);
            for (int i = 0; i < width * height; i++)
                ingredients.Add(ReadIngredient(cache, itemPalette));
            
            var result = ReadNextItemSlot(cache, itemPalette)!;
            // ShowNotification field added in 1.19.4, use true for previous versions
            var showNotification = protocolVersion < ProtocolMinecraft.MC_1_19_4_Version || DataTypes.ReadNextBool(cache);
            
            return new CraftingShapedExtraData(group, category, width, height, ingredients, result, showNotification);
        }

        private CraftingShapelessExtraData ReadCraftingShapelessRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            var group = DataTypes.ReadNextString(cache);
            var category = protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version
                ? (CraftingRecipeCategory) DataTypes.ReadNextVarInt(cache) : CraftingRecipeCategory.Misc;
            
            var ingredientCount = DataTypes.ReadNextVarInt(cache);
            
            var ingredients = new List<ItemStack?[]>(ingredientCount);
            for (int i = 0; i < ingredientCount; i++)
                ingredients.Add(ReadIngredient(cache, itemPalette));
            
            var result = ReadNextItemSlot(cache, itemPalette)!;
            
            return new CraftingShapelessExtraData(group, category, ingredientCount, ingredients, result);
        }

        private CraftingSpecialExtraData ReadCraftingSpecialRecipe(Queue<byte> cache)
        {
            var category = protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version
                ? (CraftingRecipeCategory) DataTypes.ReadNextVarInt(cache) : CraftingRecipeCategory.Misc;
            
            return new CraftingSpecialExtraData(category);
        }

        private CookingExtraData ReadCookingRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            var group = DataTypes.ReadNextString(cache);
            var category = protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version
                ? (CookingRecipeCategory) DataTypes.ReadNextVarInt(cache) : CookingRecipeCategory.Misc;

            var ingredient = ReadIngredient(cache, itemPalette);
            var result = ReadNextItemSlot(cache, itemPalette)!;
            var experience = DataTypes.ReadNextFloat(cache);
            var cookingTime = DataTypes.ReadNextVarInt(cache);

            return new CookingExtraData(group, category, ingredient, result, experience, cookingTime);
        }

        private StonecuttingExtraData ReadStonecuttingRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            var group = DataTypes.ReadNextString(cache);
            var ingredient = ReadIngredient(cache, itemPalette);
            var result = ReadNextItemSlot(cache, itemPalette)!;
            
            return new StonecuttingExtraData(group, ingredient, result);
        }
        
        private SmithingExtraData ReadLegacySmithingRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            var @base = ReadIngredient(cache, itemPalette);
            var addition = ReadIngredient(cache, itemPalette);
            var result = ReadNextItemSlot(cache, itemPalette)!;
            
            return new SmithingExtraData(@base, addition, result);
        }
        
        private SmithingTransformExtraData ReadSmithingTransformRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            var template = ReadIngredient(cache, itemPalette);
            var @base = ReadIngredient(cache, itemPalette);
            var addition = ReadIngredient(cache, itemPalette);
            var result = ReadNextItemSlot(cache, itemPalette)!;
            
            return new SmithingTransformExtraData(template, @base, addition, result);
        }
        
        private SmithingTrimExtraData ReadSmithingTrimRecipe(Queue<byte> cache, ItemPalette itemPalette)
        {
            var template = ReadIngredient(cache, itemPalette);
            var @base = ReadIngredient(cache, itemPalette);
            var addition = ReadIngredient(cache, itemPalette);
            
            return new SmithingTrimExtraData(template, @base, addition);
        }
        
        /// <summary>
        /// Read a single villager trade from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The item that was read or NULL for an empty slot</returns>
        public VillagerTrade ReadNextTrade(Queue<byte> cache, ItemPalette itemPalette)
        {
            ItemStack inputItem1 = ReadNextItemSlot(cache, itemPalette)!;
            ItemStack outputItem = ReadNextItemSlot(cache, itemPalette)!;

            ItemStack? inputItem2 = null;

            if (protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version)
                inputItem2 = ReadNextItemSlot(cache, itemPalette);
            else
            {
                if (DataTypes.ReadNextBool(cache)) //check if villager has second item
                    inputItem2 = ReadNextItemSlot(cache, itemPalette);
            }

            bool tradeDisabled = DataTypes.ReadNextBool(cache);
            int numberOfTradeUses = DataTypes.ReadNextInt(cache);
            int maximumNumberOfTradeUses = DataTypes.ReadNextInt(cache);
            int xp = DataTypes.ReadNextInt(cache);
            int specialPrice = DataTypes.ReadNextInt(cache);
            float priceMultiplier = DataTypes.ReadNextFloat(cache);
            int demand = DataTypes.ReadNextInt(cache);
            return new VillagerTrade(inputItem1, outputItem, inputItem2, tradeDisabled, numberOfTradeUses,
                maximumNumberOfTradeUses, xp, specialPrice, priceMultiplier, demand);
        }

        public string ReadNextChat(Queue<byte> cache)
        {
            if (protocolVersion >= ProtocolMinecraft.MC_1_20_4_Version)
            {
                // Read as NBT
                var r = DataTypes.ReadNextNbt(cache, UseAnonymousNBT);
                return ChatParser.ParseText(r);
            }
            else
            {
                // Read as String
                var json = DataTypes.ReadNextString(cache);
                return ChatParser.ParseText(json);
            }
        }
        
        public Json.JSONData ReadNextChatAsJson(Queue<byte> cache)
        {
            if (protocolVersion >= ProtocolMinecraft.MC_1_20_4_Version)
            {
                // Read as NBT
                var r = DataTypes.ReadNextNbt(cache, UseAnonymousNBT);
                return Json.Object2JSONData(r);
            }
            else
            {
                // Read as String
                var json = DataTypes.ReadNextString(cache);
                return Json.ParseJson(json);
            }
        }

        #endregion
        
        #region Complex data getters

        /// <summary>
        /// Get a byte array representing the given item as an item slot
        /// </summary>
        /// <param name="item">Item</param>
        /// <param name="itemPalette">Item Palette</param>
        /// <returns>Item slot representation</returns>
        public byte[] GetItemSlot(ItemStack? item, ItemPalette itemPalette)
        {
            List<byte> slotData = new();

            if (protocolVersion >= ProtocolMinecraft.MC_1_20_6_Version)
            {
                if (item == null || item.IsEmpty)
                {
                    slotData.AddRange(DataTypes.GetVarInt(0)); // No item
                }
                else
                {
                    slotData.AddRange(DataTypes.GetVarInt(item.Count)); // Item is present
                    slotData.AddRange(DataTypes.GetVarInt(itemPalette.GetNumIdById(item.ItemType.ItemId)));

                    // TODO: Check and fix sending components data in 1.20.5+

                    /*

                    if (item.ReceivedComponentsToAdd is not null && item.ReceivedComponentsToAdd.Count > 0)
                    {
                        slotData.AddRange(DataTypes.GetVarInt(item.ReceivedComponentsToAdd.Count));

                        foreach (var (_, bytes) in item.ReceivedComponentsToAdd)
                        {
                            slotData.AddRange(bytes);

                            var bytesQueue = new Queue<byte>(bytes);
                            var numId = DataTypes.ReadNextVarInt(bytesQueue);

                            Debug.Log($"Component to add: [{numId}] {ItemPalette.INSTANCE.ComponentRegistry.GetIdByNumId(numId)} Total length: {bytes.Length} bytes");
                        }
                    }
                    else*/
                    {
                        slotData.AddRange(DataTypes.GetVarInt(0)); // No components to add
                    }

                    /*
                    if (item.ReceivedComponentsToRemove is not null && item.ReceivedComponentsToRemove.Count > 0)
                    {
                        slotData.AddRange(DataTypes.GetVarInt(item.ReceivedComponentsToRemove.Count));

                        foreach (var numId in item.ReceivedComponentsToRemove)
                        {
                            slotData.AddRange(DataTypes.GetVarInt(numId));
                        }
                    }
                    else*/
                    {
                        slotData.AddRange(DataTypes.GetVarInt(0)); // No components to remove
                    }
                }
            }
            else
            {
                // MC 1.13 and greater
                if (item == null || item.IsEmpty)
                {
                    slotData.AddRange(DataTypes.GetBool(false)); // No item
                }
                else
                {
                    slotData.AddRange(DataTypes.GetBool(true)); // Item is present
                    slotData.AddRange(DataTypes.GetVarInt(itemPalette.GetNumIdById(item.ItemType.ItemId)));
                    slotData.Add((byte)item.Count);
                    slotData.AddRange(DataTypes.GetNbt(item.NBT));
                }
            }

            return slotData.ToArray();
        }

        /// <summary>
        /// Get a byte array representing an array of item slots
        /// </summary>
        /// <param name="items">Items</param>
        /// <param name="itemPalette">Item Palette</param>
        /// <returns>Array of Item slot representations</returns>
        public byte[] GetSlotsArray(Dictionary<int, ItemStack> items, ItemPalette itemPalette)
        {
            byte[] slotsArray = new byte[items.Count];

            foreach (KeyValuePair<int, ItemStack> item in items)
            {
                slotsArray = DataTypes.ConcatBytes(slotsArray, DataTypes.GetShort((short)item.Key), GetItemSlot(item.Value, itemPalette));
            }

            return slotsArray;
        }

        /// <summary>
        /// Get protocol block face from Direction
        /// </summary>
        /// <param name="direction">Direction</param>
        /// <returns>Block face byte enum</returns>
        public byte GetBlockFace(Direction direction)
        {
            return direction switch
            {
                Direction.Down => 0,
                Direction.Up => 1,
                Direction.North => 2,
                Direction.South => 3,
                Direction.West => 4,
                Direction.East => 5,
                _ => throw new NotImplementedException("Unknown direction: " + direction.ToString()),
            };
        }

        /// <summary>
        /// Write LastSeenMessageList
        /// </summary>
        /// <param name="msgList">Message.LastSeenMessageList</param>
        /// <param name="isOnlineMode">Whether the server is in online mode</param>
        /// <returns>Message.LastSeenMessageList Packet Data</returns>
        public byte[] GetLastSeenMessageList(Message.LastSeenMessageList msgList, bool isOnlineMode)
        {
            if (!isOnlineMode)
                return DataTypes.GetVarInt(0); // Message list size
            else
            {
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(msgList.entries.Length)); // Message list size
                foreach (Message.LastSeenMessageList.AcknowledgedMessage entry in msgList.entries)
                {
                    fields.AddRange(entry.profileId.ToBigEndianBytes()); // UUID
                    fields.AddRange(DataTypes.GetVarInt(entry.signature.Length)); // Signature length
                    fields.AddRange(entry.signature); // Signature data
                }

                return fields.ToArray();
            }
        }

        /// <summary>
        /// Write LastSeenMessageList.Acknowledgment
        /// </summary>
        /// <param name="ack">Acknowledgment</param>
        /// <param name="isOnlineMode">Whether the server is in online mode</param>
        /// <returns>Acknowledgment Packet Data</returns>
        public byte[] GetAcknowledgment(Message.LastSeenMessageList.Acknowledgment ack, bool isOnlineMode)
        {
            List<byte> fields = new();
            fields.AddRange(GetLastSeenMessageList(ack.lastSeen, isOnlineMode));
            if (!isOnlineMode || ack.lastReceived == null)
                fields.AddRange(DataTypes.GetBool(false)); // Has last received message
            else
            {
                fields.AddRange(DataTypes.GetBool(true));
                fields.AddRange(ack.lastReceived.profileId.ToBigEndianBytes()); // Has last received message
                fields.AddRange(DataTypes.GetVarInt(ack.lastReceived.signature.Length)); // Last received message signature length
                fields.AddRange(ack.lastReceived.signature); // Last received message signature data
            }

            return fields.ToArray();
        }

        #endregion
        
    }
}