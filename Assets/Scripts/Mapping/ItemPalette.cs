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
            string colorsPath = PathHelper.GetExtraDataFile("item_colors-1.19.json");

            int count = 0, yieldCount = 200;

            if (File.Exists(itemsPath))
            {
                var items = Json.ParseJson(File.ReadAllText(itemsPath, Encoding.UTF8));

                foreach (var item in items.Properties)
                {
                    int numId;
                    if (int.TryParse(item.Key, out numId))
                    {
                        var itemId = ResourceLocation.fromString(item.Value.StringValue);

                        Item newItem = new Item(itemId);

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
                dictId.Add(entry.Value.itemId, entry.Key);
            }

            // Hardcoded placeholder types for internal and network use
            dictReverse[Item.UNKNOWN] = -1;
            dictId[Item.UNKNOWN.itemId] = -1;

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


            flag.done = true;
        }
    }
}
