using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CraftSharp
{
    public class BlockEntityPalette
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        public static readonly BlockEntityPalette INSTANCE = new();

        private readonly Dictionary<int, BlockEntityType> blockEntityTypeTable = new();
        private readonly Dictionary<ResourceLocation, int> dictId = new();

        public static readonly BlockEntityType UNKNOWN_BLOCK_ENTITY_TYPE = new(UNKNOWN_BLOCK_ENTITY_NUM_ID, new("unknown_block_entity"));
        public const int UNKNOWN_BLOCK_ENTITY_NUM_ID = -1;

        /// <summary>
        /// Get block entity type from numeral id
        /// </summary>
        /// <param name="id">BlockEntity type ID</param>
        /// <returns>BlockEntityType corresponding to the specified ID</returns>
        public BlockEntityType FromNumId(int id)
        {
            //1.14+ entities have the same set of IDs regardless of living status
            if (blockEntityTypeTable.ContainsKey(id))
                return blockEntityTypeTable[id];

            return UNKNOWN_BLOCK_ENTITY_TYPE;
        }

        /// <summary>
        /// Get numeral id from block entity type identifier
        /// </summary>
        public int ToNumId(ResourceLocation identifier)
        {
            if (dictId.ContainsKey(identifier))
                return dictId[identifier];
            
            Debug.LogWarning($"Unknown Entity Type {identifier}");
            return UNKNOWN_BLOCK_ENTITY_NUM_ID;
        }

        /// <summary>
        /// Get block entity type from block entity type identifier
        /// </summary>
        public BlockEntityType FromId(ResourceLocation identifier)
        {
            return FromNumId(ToNumId(identifier));
        }

        public void PrepareData(string dataVersion, DataLoadFlag flag)
        {
            // Clear loaded stuff...
            blockEntityTypeTable.Clear();
            dictId.Clear();

            var blockEntityTypeListPath = PathHelper.GetExtraDataFile($"blocks{SP}block_entity_types-{dataVersion}.json");
            //string listsPath  = PathHelper.GetExtraDataFile("block_entity_lists.json");

            if (!File.Exists(blockEntityTypeListPath)) // || !File.Exists(listsPath))
            {
                Debug.LogWarning("BlockEntity data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            /* First read special block entity lists...
            var lists = new Dictionary<string, HashSet<ResourceLocation>>();
            lists.Add("contains_item", new());

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            foreach (var pair in lists)
            {
                if (spLists.Properties.ContainsKey(pair.Key))
                {
                    foreach (var block in spLists.Properties[pair.Key].DataArray)
                        pair.Value.Add(ResourceLocation.FromString(block.StringValue));
                }
            }

            // References for later use
            var containsItem = lists["contains_item"]; */

            try
            {
                var entityTypeList = Json.ParseJson(File.ReadAllText(blockEntityTypeListPath, Encoding.UTF8));

                foreach (var blockEntityType in entityTypeList.Properties)
                {
                    int numId;
                    if (int.TryParse(blockEntityType.Key, out numId))
                    {
                        var blockEntityTypeId = ResourceLocation.FromString(blockEntityType.Value.StringValue);

                        blockEntityTypeTable.TryAdd(numId, new BlockEntityType(numId, blockEntityTypeId));
                        
                        dictId.TryAdd(blockEntityTypeId, numId);
                    }
                    else
                        Debug.LogWarning($"Invalid numeral block entity type key [{blockEntityType.Key}]");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading block entity types: {e.Message}");
                flag.Failed = true;
            }

            flag.Finished = true;
        }
    }
}
