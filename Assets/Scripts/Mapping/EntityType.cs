namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Biome
    /// </summary>
    public record EntityType
    {
        public static readonly ResourceLocation ALLAY_ID = new("allay");
        public static readonly ResourceLocation AREA_EFFECT_CLOUD_ID = new("area_effect_cloud");
        public static readonly ResourceLocation ARMOR_STAND_ID = new("armor_stand");
        public static readonly ResourceLocation ARROW_ID = new("arrow");
        public static readonly ResourceLocation AXOLOTL_ID = new("axolotl");
        public static readonly ResourceLocation BAT_ID = new("bat");
        public static readonly ResourceLocation BEE_ID = new("bee");
        public static readonly ResourceLocation BLAZE_ID = new("blaze");
        public static readonly ResourceLocation BOAT_ID = new("boat");
        public static readonly ResourceLocation CAT_ID = new("cat");
        public static readonly ResourceLocation CAVE_SPIDER_ID = new("cave_spider");
        public static readonly ResourceLocation CHEST_BOAT_ID = new("chest_boat");
        public static readonly ResourceLocation CHEST_MINECART_ID = new("chest_minecart");
        public static readonly ResourceLocation CHICKEN_ID = new("chicken");
        public static readonly ResourceLocation COD_ID = new("cod");
        public static readonly ResourceLocation COMMAND_BLOCK_MINECART_ID = new("command_block_minecart");
        public static readonly ResourceLocation COW_ID = new("cow");
        public static readonly ResourceLocation CREEPER_ID = new("creeper");
        public static readonly ResourceLocation DOLPHIN_ID = new("dolphin");
        public static readonly ResourceLocation DONKEY_ID = new("donkey");
        public static readonly ResourceLocation DRAGON_FIREBALL_ID = new("dragon_fireball");
        public static readonly ResourceLocation DROWNED_ID = new("drowned");
        public static readonly ResourceLocation EGG_ID = new("egg");
        public static readonly ResourceLocation ELDER_GUARDIAN_ID = new("elder_guardian");
        public static readonly ResourceLocation END_CRYSTAL_ID = new("end_crystal");
        public static readonly ResourceLocation ENDER_DRAGON_ID = new("ender_dragon");
        public static readonly ResourceLocation ENDER_PEARL_ID = new("ender_pearl");
        public static readonly ResourceLocation ENDERMAN_ID = new("enderman");
        public static readonly ResourceLocation ENDERMITE_ID = new("endermite");
        public static readonly ResourceLocation EVOKER_ID = new("evoker");
        public static readonly ResourceLocation EVOKER_FANGS_ID = new("evoker_fangs");
        public static readonly ResourceLocation EXPERIENCE_BOTTLE_ID = new("experience_bottle");
        public static readonly ResourceLocation EXPERIENCE_ORB_ID = new("experience_orb");
        public static readonly ResourceLocation EYE_OF_ENDER_ID = new("eye_of_ender");
        public static readonly ResourceLocation FALLING_BLOCK_ID = new("falling_block");
        public static readonly ResourceLocation FIREBALL_ID = new("fireball");
        public static readonly ResourceLocation FIREWORK_ROCKET_ID = new("firework_rocket");
        public static readonly ResourceLocation FISHING_BOBBER_ID = new("fishing_bobber");
        public static readonly ResourceLocation FOX_ID = new("fox");
        public static readonly ResourceLocation FROG_ID = new("frog");
        public static readonly ResourceLocation FURNACE_MINECART_ID = new("furnace_minecart");
        public static readonly ResourceLocation GHAST_ID = new("ghast");
        public static readonly ResourceLocation GIANT_ID = new("giant");
        public static readonly ResourceLocation GLOW_ITEM_FRAME_ID = new("glow_item_frame");
        public static readonly ResourceLocation GLOW_SQUID_ID = new("glow_squid");
        public static readonly ResourceLocation GOAT_ID = new("goat");
        public static readonly ResourceLocation GUARDIAN_ID = new("guardian");
        public static readonly ResourceLocation HOGLIN_ID = new("hoglin");
        public static readonly ResourceLocation HOPPER_MINECART_ID = new("hopper_minecart");
        public static readonly ResourceLocation HORSE_ID = new("horse");
        public static readonly ResourceLocation HUSK_ID = new("husk");
        public static readonly ResourceLocation ILLUSIONER_ID = new("illusioner");
        public static readonly ResourceLocation IRON_GOLEM_ID = new("iron_golem");
        public static readonly ResourceLocation ITEM_ID = new("item");
        public static readonly ResourceLocation ITEM_FRAME_ID = new("item_frame");
        public static readonly ResourceLocation LEASH_KNOT_ID = new("leash_knot");
        public static readonly ResourceLocation LIGHTNING_BOLT_ID = new("lightning_bolt");
        public static readonly ResourceLocation LLAMA_ID = new("llama");
        public static readonly ResourceLocation LLAMA_SPIT_ID = new("llama_spit");
        public static readonly ResourceLocation MAGMA_CUBE_ID = new("magma_cube");
        public static readonly ResourceLocation MARKER_ID = new("marker");
        public static readonly ResourceLocation MINECART_ID = new("minecart");
        public static readonly ResourceLocation MOOSHROOM_ID = new("mooshroom");
        public static readonly ResourceLocation MULE_ID = new("mule");
        public static readonly ResourceLocation OCELOT_ID = new("ocelot");
        public static readonly ResourceLocation PAINTING_ID = new("painting");
        public static readonly ResourceLocation PANDA_ID = new("panda");
        public static readonly ResourceLocation PARROT_ID = new("parrot");
        public static readonly ResourceLocation PHANTOM_ID = new("phantom");
        public static readonly ResourceLocation PIG_ID = new("pig");
        public static readonly ResourceLocation PIGLIN_ID = new("piglin");
        public static readonly ResourceLocation PIGLIN_BRUTE_ID = new("piglin_brute");
        public static readonly ResourceLocation PILLAGER_ID = new("pillager");
        public static readonly ResourceLocation PLAYER_ID = new("player");
        public static readonly ResourceLocation POLAR_BEAR_ID = new("polar_bear");
        public static readonly ResourceLocation POTION_ID = new("potion");
        public static readonly ResourceLocation PUFFERFISH_ID = new("pufferfish");
        public static readonly ResourceLocation RABBIT_ID = new("rabbit");
        public static readonly ResourceLocation RAVAGER_ID = new("ravager");
        public static readonly ResourceLocation SALMON_ID = new("salmon");
        public static readonly ResourceLocation SHEEP_ID = new("sheep");
        public static readonly ResourceLocation SHULKER_ID = new("shulker");
        public static readonly ResourceLocation SHULKER_BULLET_ID = new("shulker_bullet");
        public static readonly ResourceLocation SILVERFISH_ID = new("silverfish");
        public static readonly ResourceLocation SKELETON_ID = new("skeleton");
        public static readonly ResourceLocation SKELETON_HORSE_ID = new("skeleton_horse");
        public static readonly ResourceLocation SLIME_ID = new("slime");
        public static readonly ResourceLocation SMALL_FIREBALL_ID = new("small_fireball");
        public static readonly ResourceLocation SNOW_GOLEM_ID = new("snow_golem");
        public static readonly ResourceLocation SNOWBALL_ID = new("snowball");
        public static readonly ResourceLocation SPAWNER_MINECART_ID = new("spawner_minecart");
        public static readonly ResourceLocation SPECTRAL_ARROW_ID = new("spectral_arrow");
        public static readonly ResourceLocation SPIDER_ID = new("spider");
        public static readonly ResourceLocation SQUID_ID = new("squid");
        public static readonly ResourceLocation STRAY_ID = new("stray");
        public static readonly ResourceLocation STRIDER_ID = new("strider");
        public static readonly ResourceLocation TADPOLE_ID = new("tadpole");
        public static readonly ResourceLocation TNT_ID = new("tnt");
        public static readonly ResourceLocation TNT_MINECART_ID = new("tnt_minecart");
        public static readonly ResourceLocation TRADER_LLAMA_ID = new("trader_llama");
        public static readonly ResourceLocation TRIDENT_ID = new("trident");
        public static readonly ResourceLocation TROPICAL_FISH_ID = new("tropical_fish");
        public static readonly ResourceLocation TURTLE_ID = new("turtle");
        public static readonly ResourceLocation VEX_ID = new("vex");
        public static readonly ResourceLocation VILLAGER_ID = new("villager");
        public static readonly ResourceLocation VINDICATOR_ID = new("vindicator");
        public static readonly ResourceLocation WANDERING_TRADER_ID = new("wandering_trader");
        public static readonly ResourceLocation WARDEN_ID = new("warden");
        public static readonly ResourceLocation WITCH_ID = new("witch");
        public static readonly ResourceLocation WITHER_ID = new("wither");
        public static readonly ResourceLocation WITHER_SKELETON_ID = new("wither_skeleton");
        public static readonly ResourceLocation WITHER_SKULL_ID = new("wither_skull");
        public static readonly ResourceLocation WOLF_ID = new("wolf");
        public static readonly ResourceLocation ZOGLIN_ID = new("zoglin");
        public static readonly ResourceLocation ZOMBIE_ID = new("zombie");
        public static readonly ResourceLocation ZOMBIE_HORSE_ID = new("zombie_horse");
        public static readonly ResourceLocation ZOMBIE_VILLAGER_ID = new("zombie_villager");
        public static readonly ResourceLocation ZOMBIFIED_PIGLIN_ID = new("zombified_piglin");

        public int NumeralId { get; }

        public ResourceLocation EntityId { get; }

        public bool ContainsItem { get; }

        public EntityType(int numId, ResourceLocation id, bool containsItem = false)
        {
            NumeralId = numId;
            EntityId = id;
            ContainsItem = containsItem;
        }

        public override string ToString()
        {
            return EntityId.ToString();
        }
    }
}