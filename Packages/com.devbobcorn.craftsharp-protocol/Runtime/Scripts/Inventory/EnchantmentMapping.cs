﻿using System;
using System.Collections.Generic;
using System.Linq;
using CraftSharp.Protocol;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Inventory
{
    public class EnchantmentMapping
    {
#pragma warning disable format // @formatter:off

        // 1.16 - 1.18
        private static readonly Dictionary<short, Enchantments> enchantmentMappings116 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrieder },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.Sharpness },
            { 13,   Enchantments.Smite },
            { 14,   Enchantments.BaneOfArthropods },
            { 15,   Enchantments.Knockback },
            { 16,   Enchantments.FireAspect },
            { 17,   Enchantments.Looting },
            { 18,   Enchantments.Sweeping },
            { 19,   Enchantments.Efficency },
            { 20,   Enchantments.SilkTouch },
            { 21,   Enchantments.Unbreaking },
            { 22,   Enchantments.Fortune },
            { 23,   Enchantments.Power },
            { 24,   Enchantments.Punch },
            { 25,   Enchantments.Flame },
            { 26,   Enchantments.Infinity },
            { 27,   Enchantments.LuckOfTheSea },
            { 28,   Enchantments.Lure },
            { 29,   Enchantments.Loyality },
            { 30,   Enchantments.Impaling },
            { 31,   Enchantments.Riptide },
            { 32,   Enchantments.Channeling },
            { 33,   Enchantments.Multishot },
            { 34,   Enchantments.QuickCharge },
            { 35,   Enchantments.Piercing },
            { 36,   Enchantments.Mending },
            { 37,   Enchantments.VanishingCurse }
        };

        // 1.19 - 1.20.4
        private static readonly Dictionary<short, Enchantments> enchantmentMappings119 = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrieder },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.SwiftSneak },
            { 13,   Enchantments.Sharpness },
            { 14,   Enchantments.Smite },
            { 15,   Enchantments.BaneOfArthropods },
            { 16,   Enchantments.Knockback },
            { 17,   Enchantments.FireAspect },
            { 18,   Enchantments.Looting },
            { 19,   Enchantments.Sweeping },
            { 20,   Enchantments.Efficency },
            { 21,   Enchantments.SilkTouch },
            { 22,   Enchantments.Unbreaking },
            { 23,   Enchantments.Fortune },
            { 24,   Enchantments.Power },
            { 25,   Enchantments.Punch },
            { 26,   Enchantments.Flame },
            { 27,   Enchantments.Infinity },
            { 28,   Enchantments.LuckOfTheSea },
            { 29,   Enchantments.Lure },
            { 30,   Enchantments.Loyality },
            { 31,   Enchantments.Impaling },
            { 32,   Enchantments.Riptide },
            { 33,   Enchantments.Channeling },
            { 34,   Enchantments.Multishot },
            { 35,   Enchantments.QuickCharge },
            { 36,   Enchantments.Piercing },
            { 37,   Enchantments.Mending },
            { 38,   Enchantments.VanishingCurse }
        };
        
        // 1.20.6+
        private static readonly Dictionary<short, Enchantments> enchantmentMappings = new()
        {
            //id    type 
            { 0,    Enchantments.Protection },
            { 1,    Enchantments.FireProtection },
            { 2,    Enchantments.FeatherFalling },
            { 3,    Enchantments.BlastProtection },
            { 4,    Enchantments.ProjectileProtection },
            { 5,    Enchantments.Respiration },
            { 6,    Enchantments.AquaAffinity },
            { 7,    Enchantments.Thorns },
            { 8,    Enchantments.DepthStrieder },
            { 9,    Enchantments.FrostWalker },
            { 10,   Enchantments.BindingCurse },
            { 11,   Enchantments.SoulSpeed },
            { 12,   Enchantments.SwiftSneak },
            { 13,   Enchantments.Sharpness },
            { 14,   Enchantments.Smite },
            { 15,   Enchantments.BaneOfArthropods },
            { 16,   Enchantments.Knockback },
            { 17,   Enchantments.FireAspect },
            { 18,   Enchantments.Looting },
            { 19,   Enchantments.Sweeping },
            { 20,   Enchantments.Efficency },
            { 21,   Enchantments.SilkTouch },
            { 22,   Enchantments.Unbreaking },
            { 23,   Enchantments.Fortune },
            { 24,   Enchantments.Power },
            { 25,   Enchantments.Punch },
            { 26,   Enchantments.Flame },
            { 27,   Enchantments.Infinity },
            { 28,   Enchantments.LuckOfTheSea },
            { 29,   Enchantments.Lure },
            { 30,   Enchantments.Loyality },
            { 31,   Enchantments.Impaling },
            { 32,   Enchantments.Riptide },
            { 33,   Enchantments.Channeling },
            { 34,   Enchantments.Multishot },
            { 35,   Enchantments.QuickCharge },
            { 36,   Enchantments.Piercing },
            { 37,   Enchantments.Density },
            { 38,   Enchantments.Breach },
            { 39,   Enchantments.WindBurst },
            { 40,   Enchantments.Mending },
            { 41,   Enchantments.VanishingCurse }
        };
#pragma warning restore format // @formatter:on

        public static Enchantments GetEnchantmentById(int protocolVersion, short id)
        {
            var map = protocolVersion switch
            {
                < ProtocolMinecraft.MC_1_19_Version => enchantmentMappings116,
                >= ProtocolMinecraft.MC_1_19_Version and < ProtocolMinecraft.MC_1_21_Version => enchantmentMappings119,
                _ => enchantmentMappings
            };

            if (!map.TryGetValue(id, out var value))
                throw new Exception($"Got an Unknown Enchantment ID {id}, please update the Mappings!");

            return value;
        }

        public static string GetEnchantmentName(Enchantments enchantment)
        {
            var translation = ChatParser.TranslateString("Enchantments.minecraft." + enchantment.ToString().ToUnderscoreCase());
            return string.IsNullOrEmpty(translation) ? $"Unknown Enchantment with ID: {(short)enchantment} (Probably not named in the code yet)" : translation;
        }
    }
}
