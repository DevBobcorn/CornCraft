using CraftSharp.Protocol.Handlers.StructuredComponents.Components;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Registries
{
    public class StructuredComponentsRegistry1213 : StructuredComponentRegistry
    {
        public StructuredComponentsRegistry1213(IMinecraftDataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {
            RegisterComponent<CustomDataComponent>(0, StructuredComponentIds.CUSTOM_DATA_ID);
            RegisterComponent<MaxStackSizeComponent>(1, StructuredComponentIds.MAX_STACK_SIZE_ID);
            RegisterComponent<MaxDamageComponent>(2, StructuredComponentIds.MAX_DAMAGE_ID);
            RegisterComponent<DamageComponent>(3, StructuredComponentIds.DAMAGE_ID);
            RegisterComponent<UnbreakableComponent>(4, StructuredComponentIds.UNBREAKABLE_ID);
            RegisterComponent<CustomNameComponent>(5, StructuredComponentIds.CUSTOM_NAME_ID);
            RegisterComponent<ItemNameComponent>(6, StructuredComponentIds.ITEM_NAME_ID);
            RegisterComponent<LoreComponent>(7, StructuredComponentIds.LORE_ID);
            RegisterComponent<RarityComponent>(8, StructuredComponentIds.RARITY_ID);
            RegisterComponent<EnchantmentsComponent>(9, StructuredComponentIds.ENCHANTMENTS_ID);
            RegisterComponent<CanPlaceOnComponent>(10, StructuredComponentIds.CAN_PLACE_ON_ID);
            RegisterComponent<CanBreakComponent>(11, StructuredComponentIds.CAN_BREAK_ID);
            RegisterComponent<AttributeModifiersComponent>(12, StructuredComponentIds.ATTRIBUTE_MODIFIERS_ID);
            RegisterComponent<CustomModelDataComponent>(13, StructuredComponentIds.CUSTOM_MODEL_DATA_ID);
            RegisterComponent<HideAdditionalTooltipComponent>(14, StructuredComponentIds.HIDE_ADDITIONAL_TOOLTIP_ID);
            RegisterComponent<HideTooltipComponent>(15, StructuredComponentIds.HIDE_TOOLTIP_ID);
            RegisterComponent<RepairCostComponent>(16, StructuredComponentIds.REPAIR_COST_ID);
            RegisterComponent<CreativeSlotLockComponent>(17, StructuredComponentIds.CREATIVE_SLOT_LOCK_ID);
            RegisterComponent<EnchantmentGlintOverrideComponent>(18, StructuredComponentIds.ENCHANTMENT_GLINT_OVERRIDE_ID);
            RegisterComponent<IntangibleProjectileComponent>(19, StructuredComponentIds.INTANGIBLE_PROJECTILE_ID);
            RegisterComponent<FoodComponent>(20, StructuredComponentIds.FOOD_ID);
            RegisterComponent<FireResistantComponent>(21, StructuredComponentIds.FIRE_RESISTANT_ID);
            RegisterComponent<ToolComponent>(22, StructuredComponentIds.TOOL_ID);
            RegisterComponent<StoredEnchantmentsComponent>(23, StructuredComponentIds.STORED_ENCHANTMENTS_ID);
            RegisterComponent<DyedColorComponent>(24, StructuredComponentIds.DYED_COLOR_ID);
            RegisterComponent<MapColorComponent>(25, StructuredComponentIds.MAP_COLOR_ID);
            RegisterComponent<MapIdComponent>(26, StructuredComponentIds.MAP_ID_ID);
            RegisterComponent<MapDecorationsComponent>(27, StructuredComponentIds.MAP_DECORATIONS_ID);
            RegisterComponent<MapPostProcessingComponent>(28, StructuredComponentIds.MAP_POST_PROCESSING_ID);
            RegisterComponent<ChargedProjectilesComponent>(29, StructuredComponentIds.CHARGED_PROJECTILES_ID);
            RegisterComponent<BundleContentsComponent>(30, StructuredComponentIds.BUNDLE_CONTENTS_ID);
            RegisterComponent<PotionContentsComponent>(31, StructuredComponentIds.POTION_CONTENTS_ID);
            RegisterComponent<SuspiciousStewEffectsComponent>(32, StructuredComponentIds.SUSPICIOUS_STEW_EFFECTS_ID);
            RegisterComponent<WritableBookContentComponent>(33, StructuredComponentIds.WRITABLE_BOOK_CONTENT_ID);
            RegisterComponent<WrittenBookContentComponent>(34, StructuredComponentIds.WRITTEN_BOOK_CONTENT_ID);
            RegisterComponent<TrimComponent>(35, StructuredComponentIds.TRIM_ID);
            RegisterComponent<DebugStickStateComponent>(36, StructuredComponentIds.DEBUG_STICK_STATE_ID);
            RegisterComponent<EntityDataComponent>(37, StructuredComponentIds.ENTITY_DATA_ID);
            RegisterComponent<BucketEntityDataComponent>(38, StructuredComponentIds.BUCKET_ENTITY_DATA_ID);
            RegisterComponent<BlockEntityDataComponent>(39, StructuredComponentIds.BLOCK_ENTITY_DATA_ID);
            RegisterComponent<InstrumentComponent>(40, StructuredComponentIds.INSTRUMENT_ID);
            RegisterComponent<OminousBottleAmplifierComponent>(41, StructuredComponentIds.OMINOUS_BOTTLE_AMPLIFIER_ID);
            RegisterComponent<JukeboxPlayableComponent>(42, StructuredComponentIds.JUKEBOX_PLAYABLE_ID);
            RegisterComponent<RecipesComponent>(43, StructuredComponentIds.RECIPES_ID);
            RegisterComponent<LodestoneTrackerComponent>(44, StructuredComponentIds.LODESTONE_TRACKER_ID);
            RegisterComponent<FireworkExplosionComponent>(45, StructuredComponentIds.FIREWORK_EXPLOSION_ID);
            RegisterComponent<FireworksComponent>(46, StructuredComponentIds.FIREWORKS_ID);
            RegisterComponent<ProfileComponent>(47, StructuredComponentIds.PROFILE_ID);
            RegisterComponent<NoteBlockSoundComponent>(48, StructuredComponentIds.NOTE_BLOCK_SOUND_ID);
            RegisterComponent<BannerPatternsComponent>(49, StructuredComponentIds.BANNER_PATTERNS_ID);
            RegisterComponent<BaseColorComponent>(50, StructuredComponentIds.BASE_COLOR_ID);
            RegisterComponent<PotDecorationsComponent>(51, StructuredComponentIds.POT_DECORATIONS_ID);
            RegisterComponent<ContainerComponent>(52, StructuredComponentIds.CONTAINER_ID);
            RegisterComponent<BlockStateComponent>(53, StructuredComponentIds.BLOCK_STATE_ID);
            RegisterComponent<BeesComponent>(54, StructuredComponentIds.BEES_ID);
            RegisterComponent<LockComponent>(55, StructuredComponentIds.LOCK_ID);
            RegisterComponent<ContainerLootComponent>(56, StructuredComponentIds.CONTAINER_LOOT_ID);
        }
    }
}