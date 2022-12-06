using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Mathematics;

namespace MinecraftClient.Resource
{
    public class ItemModelLoader
    {
        private const string GENERATED = "builtin/generated";
        private const string ENTITY    = "builtin/entity";

        public static JsonModel INVALID_MODEL = new JsonModel();
        public static JsonModel EMPTY_MODEL   = new JsonModel();

        private readonly ResourcePackManager manager;

        public ItemModelLoader(ResourcePackManager manager)
        {
            this.manager = manager;
        }

        // 
        private static Dictionary<int2, List<JsonModelElement>> generatedModels = new();

        public List<JsonModelElement> GetGeneratedItemModelElements(int layerCount, int precision, bool useItemColor)
        {
            int2 modelKey = new(layerCount, precision);

            if (!generatedModels.ContainsKey(modelKey)) // Not present yet, generate it
            {
                //Debug.Log($"Generating item model... Layer count: {layerCount} Precision: {precision}");
                var model = new List<JsonModelElement>();
                var stepLength = 16F / (float) precision;

                for (int layer = 0;layer < layerCount;layer++)
                {
                    var elem = new JsonModelElement();
                    var layerTexName = $"layer{layer}";

                    elem.from = new(8F - stepLength / 2F,  0F,  0F);
                    elem.to   = new(8F + stepLength / 2F, 16F, 16F);

                    elem.faces.Add(FaceDir.NORTH, new() {
                        uv = new(0F, 0F, 16F, 16F),
                        texName = layerTexName,
                        tintIndex = useItemColor ? layer : -1
                    });

                    elem.faces.Add(FaceDir.SOUTH, new() {
                        uv = new(16F, 0F, 0F, 16F),
                        texName = layerTexName,
                        tintIndex = useItemColor ? layer : -1
                    });

                    for (int i = 0;i < precision;i++)
                    {
                        var fracL1 =       i * stepLength;
                        var fracR1 = (i + 1) * stepLength;
                        var fracL2 = (precision -       i) * stepLength;
                        var fracR2 = (precision - (i + 1)) * stepLength;

                        var vertStripe = new JsonModelElement();
                        var horzStripe = new JsonModelElement();

                        vertStripe.from = new(8F - stepLength / 2F,  0F, fracL2);
                        vertStripe.to   = new(8F + stepLength / 2F, 16F, fracR2);
                        horzStripe.from = new(8F - stepLength / 2F, fracL2,  0F);
                        horzStripe.to   = new(8F + stepLength / 2F, fracR2, 16F);

                        // Left faces
                        vertStripe.faces.Add(FaceDir.EAST, new() {
                            uv = new(fracL1,       0F,       fracR1, 16F),
                            texName = layerTexName,
                            tintIndex = useItemColor ? layer : -1
                        });
                        // Right faces
                        vertStripe.faces.Add(FaceDir.WEST, new() {
                            uv = new(16F - fracL2, 0F, 16F - fracR2, 16F),
                            texName = layerTexName,
                            tintIndex = useItemColor ? layer : -1
                        });
                        // Top faces
                        horzStripe.faces.Add(FaceDir.UP, new() {
                            uv = new(16F,       fracL1, 0F,       fracR1),
                            texName = layerTexName,
                            tintIndex = useItemColor ? layer : -1
                        });
                        // Bottom faces
                        horzStripe.faces.Add(FaceDir.DOWN, new() {
                            uv = new(16F, 16F - fracL2, 0F, 16F - fracR2),
                            texName = layerTexName,
                            tintIndex = useItemColor ? layer : -1
                        });

                        model.Add(vertStripe);
                        model.Add(horzStripe);
                    }

                    model.Add(elem);
                }

                // Generation complete, add it into the dictionary
                generatedModels.Add(modelKey, model);
            }

            return generatedModels[modelKey];
        }

        // Accepts the assets path of current resource pack so that it can easily find other model
        // files(when searching for a parent model which is not loaded yet, for example)
        public JsonModel LoadItemModel(ResourceLocation identifier)
        {
            // Check if this model is loaded already...
            if (manager.RawItemModelTable.ContainsKey(identifier))
                return manager.RawItemModelTable[identifier];
            
            var modelFilePath = manager.ItemModelFileTable[identifier];

            if (File.Exists(modelFilePath))
            {
                JsonModel model = new JsonModel();

                string modelText = File.ReadAllText(modelFilePath);
                Json.JSONData modelData = Json.ParseJson(modelText);

                bool containsTextures = modelData.Properties.ContainsKey("textures");
                bool containsElements = modelData.Properties.ContainsKey("elements");

                if (modelData.Properties.ContainsKey("parent"))
                {
                    ResourceLocation parentIdentifier = ResourceLocation.fromString(modelData.Properties["parent"].StringValue.Replace('\\', '/'));
                    
                    switch (parentIdentifier.Path) {
                        case GENERATED:
                            if (manager.GeneratedItemModels.Add(identifier))
                            {
                                //Debug.Log($"Marked item model {identifier} as generated (Direct)");
                            }
                            model = new(); // Return a placeholder model
                            break;
                        case ENTITY:
                            break;
                        default:
                            bool parentIsGenerated = false;
                            JsonModel parentModel;

                            if (manager.RawItemModelTable.ContainsKey(parentIdentifier)
                                    && !manager.GeneratedItemModels.Contains(parentIdentifier))
                            {
                                // This parent is not generated and is already loaded, get it...
                                parentModel = manager.RawItemModelTable[parentIdentifier];
                            }
                            else if (manager.BlockModelTable.ContainsKey(parentIdentifier))
                            {
                                // This parent is already loaded as a block model, get it...
                                parentModel = manager.BlockModelTable[parentIdentifier];
                            }
                            else {
                                // This parent is not yet loaded or is a generated model, load it...
                                parentModel = LoadItemModel(parentIdentifier);
                                parentIsGenerated = manager.GeneratedItemModels.Contains(parentIdentifier);

                                if (parentIsGenerated) // Also mark self as generated
                                    if (manager.GeneratedItemModels.Add(identifier))
                                    {
                                        //Debug.Log($"Marked item model {identifier} as generated (Inherited)");
                                    }

                                if (parentModel == INVALID_MODEL)
                                    Debug.LogWarning($"Failed to load parent of {identifier}");
                            }

                            // Inherit parent textures...
                            foreach (var tex in parentModel.Textures)
                            {
                                model.Textures.Add(tex.Key, tex.Value);
                            }

                            // Inherit parent elements only if itself doesn't have those defined...
                            if (!containsElements)
                            {
                                foreach (var elem in parentModel.Elements)
                                {
                                    model.Elements.Add(elem);
                                }
                            }

                            break;
                    }

                }
                
                if (containsTextures) // Add / Override texture references
                {
                    var texData = modelData.Properties["textures"].Properties;
                    foreach (var texItem in texData)
                    {
                        TextureReference texRef;
                        if (texItem.Value.StringValue.StartsWith('#'))
                        {
                            texRef = new TextureReference(true, texItem.Value.StringValue.Substring(1)); // Remove the leading '#'...
                        }
                        else
                        {
                            texRef = new TextureReference(false, texItem.Value.StringValue);
                        }

                        if (model.Textures.ContainsKey(texItem.Key)) // Override this texture reference...
                        {
                            model.Textures[texItem.Key] = texRef;
                        }
                        else // Add a new texture reference...
                        {
                            model.Textures.Add(texItem.Key, texRef);
                        }
                    }

                }

                if (containsElements) // Discard parent elements and use own ones
                {
                    var elemData = modelData.Properties["elements"].DataArray;
                    foreach (var elemItem in elemData)
                    {
                        model.Elements.Add(JsonModelElement.fromJson(elemItem));
                    }
                }

                // It's also possible that this model is added somewhere before
                // during parent loading process (though it shouldn't happen)
                if (manager.RawItemModelTable.TryAdd(identifier, model))
                {
                    //Debug.Log("Model loaded: " + identifier);
                }
                else
                    Debug.LogWarning($"Trying to add model twice: {identifier}");

                return model;
            }
            else
            {
                Debug.LogWarning($"Item model file not found: {modelFilePath}");
                return INVALID_MODEL;
            }
        }
    }
}