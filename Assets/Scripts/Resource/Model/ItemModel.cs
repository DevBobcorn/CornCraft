using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.Resource
{
    public class ItemModel
    {
        public readonly ItemGeometry Geometry;
        public readonly RenderType RenderType;

        public readonly Dictionary<ItemModelPredicate, ItemGeometry> Overrides = new();

        public ItemModel(ItemGeometry geometry, RenderType renderType)
        {
            Geometry = geometry;
            RenderType = renderType;
        }

        public void AddOverride(ItemModelPredicate predicate, ItemGeometry geometry)
        {
            if (!Overrides.TryAdd(predicate, geometry))
            {
                Debug.LogWarning($"Trying to add predicate {predicate} to an item model twice!");
            }
        }

    }

}