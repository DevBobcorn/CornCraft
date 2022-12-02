using System.IO;
using UnityEngine;

namespace MinecraftClient.Resource
{
    public class ItemModelLoader
    {
        private const string GERERATED = "builtin/generated";
        private const string ENTITY    = "builtin/entity";

        public static JsonModel INVALID_MODEL = new JsonModel();
        public static JsonModel EMPTY_MODEL   = new JsonModel();

        private readonly ResourcePackManager manager;

        public ItemModelLoader(ResourcePackManager manager)
        {
            this.manager = manager;
        }

        public JsonModel GenerateItemModel(Json.JSONData modelData, ref bool generated)
        {
            JsonModel model = new();

            var elem = new JsonModelElement();

            elem.faces.Add(FaceDir.UP, new() {
                uv = new(0F, 0F, 16F, 16F),
                texName = "layer0"
            });

            elem.faces.Add(FaceDir.DOWN, new() {
                uv = new(0F, 0F, 16F, 16F),
                texName = "layer0"
            });

            //Debug.Log("Generating model: " + modelData.StringValue);
            model.Elements.Add(elem);

            generated = true;

            return model;
        }

        // Accepts the assets path of current resource pack so that it can easily find other model
        // files(when searching for a parent model which is not loaded yet, for example)
        public JsonModel LoadItemModel(ResourceLocation identifier, ref bool generated, string assetsPath)
        {
            // Check if this model is loaded already...
            if (manager.RawItemModelTable.ContainsKey(identifier))
                return manager.RawItemModelTable[identifier];
            
            string modelPath = $"{assetsPath}/{identifier.Namespace}/models/{identifier.Path}.json";
            if (File.Exists(modelPath))
            {
                JsonModel model = new JsonModel();

                string modelText = File.ReadAllText(modelPath);
                Json.JSONData modelData = Json.ParseJson(modelText);

                bool containsTextures = modelData.Properties.ContainsKey("textures");
                bool containsElements = modelData.Properties.ContainsKey("elements");

                if (modelData.Properties.ContainsKey("parent"))
                {
                    ResourceLocation parentIdentifier = ResourceLocation.fromString(modelData.Properties["parent"].StringValue.Replace('\\', '/'));
                    JsonModel parentModel;

                    bool parentIsGenerated = manager.GeneratedItemModels.Contains(parentIdentifier);

                    if (manager.RawItemModelTable.ContainsKey(parentIdentifier) && !parentIsGenerated)
                    {
                        // This parent is already loaded, get it...
                        parentModel = manager.RawItemModelTable[parentIdentifier];
                    }
                    else if (manager.BlockModelTable.ContainsKey(parentIdentifier))
                    {
                        // This parent is already loaded, get it...
                        parentModel = manager.BlockModelTable[parentIdentifier];
                    }
                    else
                    {
                        if (parentIsGenerated)
                        {   // Clear this parent from model cache, and re-generate it
                            if (manager.RawItemModelTable.ContainsKey(parentIdentifier))
                                manager.RawItemModelTable.Remove(parentIdentifier);
                        }

                        parentModel = parentIdentifier.Path switch {
                            GERERATED    => GenerateItemModel(modelData, ref generated),
                            ENTITY       => EMPTY_MODEL,
                            
                            // This parent is not yet loaded, load it...
                            _            => LoadItemModel(parentIdentifier, ref generated, assetsPath)
                        };

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
                
                if (generated && !manager.GeneratedItemModels.Contains(identifier))
                {
                    manager.GeneratedItemModels.Add(identifier);
                    //Debug.Log($"Marked item model {identifier} as generated");
                }

                return model;
            }
            else
            {
                Debug.LogWarning($"Item model file not found: {modelPath}");
                return INVALID_MODEL;
            }
        }
    }
}