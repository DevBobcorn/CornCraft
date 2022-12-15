using System;
using System.Collections;
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

        private readonly Dictionary<int, Item> itemsTable = new Dictionary<int, Item>();
        public Dictionary<int, Item> ItemsTable { get { return itemsTable; } }

        private readonly Dictionary<Item, int> dictReverse = new Dictionary<Item, int>();
        private readonly Dictionary<ResourceLocation, int> dictId = new Dictionary<ResourceLocation, int>();

        private readonly Dictionary<int, Func<ItemStack, float3[]>> itemColorRules = new();

        public Item FromId(int id)
        {
            // Unknown item types may appear on Forge servers for custom items
            if (!itemsTable.ContainsKey(id))
                return Item.UNKNOWN;

            return itemsTable[id];
        }

        public int ToId(Item itemType)
        {
            return dictReverse[itemType];
        }

        public int ToId(ResourceLocation identifier)
        {
            return dictId[identifier];
        }

        public bool IsTintable(int itemNumId)
        {
            return itemColorRules.ContainsKey(itemNumId);
        }

        public Func<ItemStack, float3[]> GetTintRule(int itemNumId)
        {
            if (itemColorRules.ContainsKey(itemNumId))
                return itemColorRules[itemNumId];
            return null;
        }

        public IEnumerator PrepareData(string dataVersion, CoroutineFlag flag, LoadStateInfo loadStateInfo)
        {
            loadStateInfo.infoText = "Loading items";

            // Clear loaded stuff...
            itemsTable.Clear();
            dictReverse.Clear();
            dictId.Clear();

            string itemsPath = PathHelper.GetExtraDataFile($"items-{dataVersion}.json");
            string listsPath  = PathHelper.GetExtraDataFile("item_lists.json");
            string colorsPath = PathHelper.GetExtraDataFile("item_colors.json");

            if (!File.Exists(itemsPath) || !File.Exists(listsPath) || !File.Exists(colorsPath))
                throw new FileNotFoundException("Item data not complete!");

            // First read special item lists...
            var lists = new Dictionary<string, HashSet<ResourceLocation>>();
            lists.Add("non_stackable", new());
            lists.Add("stacklimit_16", new());
            lists.Add("uncommon", new());
            lists.Add("rare", new());
            lists.Add("epic", new());

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            loadStateInfo.infoText = $"Reading special lists from {listsPath}";

            int count = 0, yieldCount = 200;

            foreach (var pair in lists)
            {
                if (spLists.Properties.ContainsKey(pair.Key))
                {
                    foreach (var block in spLists.Properties[pair.Key].DataArray)
                    {
                        pair.Value.Add(ResourceLocation.fromString(block.StringValue));
                        count++;
                        if (count % yieldCount == 0)
                            yield return null;
                    }
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
                    int numId;
                    if (int.TryParse(item.Key, out numId))
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

                        Item newItem = new Item(itemId)
                        {
                            Rarity = rarity,
                            StackLimit = stackLimit
                        };

                        itemsTable.TryAdd(numId, newItem);
                        //UnityEngine.Debug.Log($"Loading item {numId} {item.Value.StringValue}");
                    }

                    count++;
                    if (count % yieldCount == 0)
                        yield return null;
                }
            }

            yield return null;

            // Index reverse mappings for use in ToId()
            foreach (KeyValuePair<int, Item> entry in itemsTable)
            {
                dictReverse.Add(entry.Value, entry.Key);
                dictId.Add(entry.Value.ItemId, entry.Key);
            }

            // Hardcoded placeholder types for internal and network use
            dictReverse[Item.UNKNOWN] = -2;
            dictId[Item.UNKNOWN.ItemId] = -2;

            dictReverse[Item.NULL] = -1;
            dictId[Item.NULL.ItemId] = -1;

            yield return null;

            // Load item color rules...
            itemColorRules.Clear();
            loadStateInfo.infoText = $"Loading item color rules";
            yield return null;

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
                        Func<ItemStack, float3[]> ruleFunc = (itemStack) => new float3[] { fixedColor };

                        if (!itemColorRules.TryAdd(numId, ruleFunc))
                            Debug.LogWarning($"Failed to apply fixed color rules to {itemId} ({numId})!");
                        count++;
                        if (count % yieldCount == 0)
                            yield return null;
                        
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

                        Func<ItemStack, float3[]> ruleFunc = (itemStack) => fixedColors;

                        if (!itemColorRules.TryAdd(numId, ruleFunc))
                            Debug.LogWarning($"Failed to apply fixed multi-color rules to {itemId} ({numId})!");
                        count++;
                        if (count % yieldCount == 0)
                            yield return null;
                        
                    }
                    else
                        Debug.LogWarning($"Applying fixed multi-color rules to undefined item {itemId}!");
                }
            }

            flag.done = true;
        }
    }
}
