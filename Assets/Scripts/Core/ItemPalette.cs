using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

using MinecraftClient.Resource;
using MinecraftClient.Inventory;

namespace MinecraftClient.Mapping
{
    public class ItemPalette
    {
        public static readonly ItemPalette INSTANCE = new();

        private readonly Dictionary<int, Item> itemsTable = new();
        public Dictionary<int, Item> ItemsTable { get { return itemsTable; } }

        private readonly Dictionary<ResourceLocation, int> dictId = new();
        private readonly Dictionary<ResourceLocation, Func<ItemStack, float3[]>> itemColorRules = new();

        /// <summary>
        /// Get item from numeral id
        /// </summary>
        public Item FromNumId(int id)
        {
            // Unknown item types may appear on Forge servers for custom items
            if (!itemsTable.ContainsKey(id))
                return Item.UNKNOWN;

            return itemsTable[id];
        }

        /// <summary>
        /// Get numeral id from item identifier
        /// </summary>
        public int ToNumId(ResourceLocation identifier)
        {
            if (dictId.ContainsKey(identifier))
                return dictId[identifier];
            
            throw new InvalidDataException($"Unknown Item {identifier}");
        }

        /// <summary>
        /// Get item from item identifier
        /// </summary>
        public Item FromId(ResourceLocation identifier)
        {
            return FromNumId(ToNumId(identifier));
        }

        public bool IsTintable(ResourceLocation identifier)
        {
            return itemColorRules.ContainsKey(identifier);
        }

        public Func<ItemStack, float3[]> GetTintRule(ResourceLocation identifier)
        {
            if (itemColorRules.ContainsKey(identifier))
                return itemColorRules[identifier];
            return null;
        }

        public void PrepareData(string dataVersion, DataLoadFlag flag)
        {
            // Clear loaded stuff...
            itemsTable.Clear();
            dictId.Clear();

            string itemsPath = PathHelper.GetExtraDataFile($"items-{dataVersion}.json");
            string listsPath  = PathHelper.GetExtraDataFile("item_lists.json");
            string colorsPath = PathHelper.GetExtraDataFile("item_colors.json");

            if (!File.Exists(itemsPath) || !File.Exists(listsPath) || !File.Exists(colorsPath))
            {
                Debug.LogWarning("Item data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            // First read special item lists...
            var lists = new Dictionary<string, HashSet<ResourceLocation>>
            {
                { "non_stackable", new() },
                { "stacklimit_16", new() },
                { "uncommon", new() },
                { "rare", new() },
                { "epic", new() }
            };

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            foreach (var pair in lists)
            {
                if (spLists.Properties.ContainsKey(pair.Key))
                {
                    foreach (var block in spLists.Properties[pair.Key].DataArray)
                        pair.Value.Add(ResourceLocation.fromString(block.StringValue));
                }
            }

            // References for later use
            var rarityU = lists["uncommon"];
            var rarityR = lists["rare"];
            var rarityE = lists["epic"];
            var nonStackables = lists["non_stackable"];
            var stackLimit16s = lists["stacklimit_16"];

            if (File.Exists(itemsPath))
            {
                var items = Json.ParseJson(File.ReadAllText(itemsPath, Encoding.UTF8));

                foreach (var item in items.Properties)
                {
                    if (int.TryParse(item.Key, out int numId))
                    {
                        var itemId = ResourceLocation.fromString(item.Value.StringValue);

                        ItemRarity rarity = ItemRarity.Common;

                        if (rarityE.Contains(itemId))
                            rarity = ItemRarity.Epic;
                        else if (rarityR.Contains(itemId))
                            rarity = ItemRarity.Rare;
                        else if (rarityU.Contains(itemId))
                            rarity = ItemRarity.Uncommon;

                        int stackLimit = Item.DEFAULT_STACK_LIMIT;

                        if (nonStackables.Contains(itemId))
                            stackLimit = 1;
                        else if (stackLimit16s.Contains(itemId))
                            stackLimit = 16;

                        Item newItem = new(itemId)
                        {
                            Rarity = rarity,
                            StackLimit = stackLimit
                        };

                        itemsTable.TryAdd(numId, newItem);
                        dictId.TryAdd(itemId, numId);
                        //UnityEngine.Debug.Log($"Loading item {numId} {item.Value.StringValue}");
                    }
                }
            }

            // Hardcoded placeholder types for internal and network use
            dictId[Item.UNKNOWN.ItemId] = -2;
            dictId[Item.NULL.ItemId] = -1;

            // Load item color rules...
            itemColorRules.Clear();
            Json.JSONData colorRules = Json.ParseJson(File.ReadAllText(colorsPath, Encoding.UTF8));

            if (colorRules.Properties.ContainsKey("fixed"))
            {
                foreach (var fixedRule in colorRules.Properties["fixed"].Properties)
                {
                    var itemId = ResourceLocation.fromString(fixedRule.Key);

                    if (dictId.ContainsKey(itemId))
                    {
                        var numId = dictId[itemId];

                        var fixedColor = VectorUtil.Json2Float3(fixedRule.Value) / 255F;
                        float3[] ruleFunc(ItemStack itemStack) => new float3[] { fixedColor };

                        if (!itemColorRules.TryAdd(itemId, ruleFunc))
                            Debug.LogWarning($"Failed to apply fixed color rules to {itemId} ({numId})!");
                        
                    }
                    else
                        Debug.LogWarning($"Applying fixed color rules to undefined item {itemId}!");
                }
            }

            if (colorRules.Properties.ContainsKey("fixed_multicolor"))
            {
                foreach (var fixedRule in colorRules.Properties["fixed_multicolor"].Properties)
                {
                    var itemId = ResourceLocation.fromString(fixedRule.Key);

                    if (dictId.ContainsKey(itemId))
                    {
                        var numId = dictId[itemId];

                        var colorList = fixedRule.Value.DataArray.ToArray();
                        var fixedColors = new float3[colorList.Length];

                        for (int c = 0;c < colorList.Length;c++)
                            fixedColors[c] = VectorUtil.Json2Float3(colorList[c]) / 255F;

                        float3[] ruleFunc(ItemStack itemStack) => fixedColors;

                        if (!itemColorRules.TryAdd(itemId, ruleFunc))
                            Debug.LogWarning($"Failed to apply fixed multi-color rules to {itemId} ({numId})!");
                        
                    }
                    else
                        Debug.LogWarning($"Applying fixed multi-color rules to undefined item {itemId}!");
                }
            }

            flag.Finished = true;
        }
    }
}
