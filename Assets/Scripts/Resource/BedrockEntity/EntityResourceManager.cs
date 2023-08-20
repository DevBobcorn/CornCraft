#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CraftSharp.Resource;

namespace CraftSharp
{
    public struct BedrockVersion : IComparable
    {
        public int a;
        public int b;
        public int c;

        public BedrockVersion(int va, int vb, int vc)
        {
            a = va;
            b = vb;
            c = vc;
        }

        public readonly int CompareTo(object obj)
        {
            if (obj is BedrockVersion ver)
            {
                if (a == ver.a)
                {
                    if (b == ver.b)
                    {
                        if (c == ver.c)
                        {
                            return 0;
                        }
                        else
                        {
                            return c > ver.c ? 1 : -1;
                        }
                    }
                    else
                    {
                        return b > ver.b ? 1 : -1;
                    }
                }
                else
                {
                    return a > ver.a ? 1 : -1;
                }
            }
            else
            {
                throw new InvalidDataException("Trying to compare a bedrock object to unknown object!");
            }
        }
    
        public static BedrockVersion FromString(string version)
        {
            var nums = version.Split(".");
            if (nums.Length == 3)
            {
                return new(int.Parse(nums[0]), int.Parse(nums[1]), int.Parse(nums[2]));
            }
            else
            {
                throw new InvalidDataException($"Malformed version string: {version}");
            }
        }

        public static bool operator >(BedrockVersion n1, BedrockVersion n2) => n1.CompareTo(n2) > 0;
        public static bool operator >=(BedrockVersion n1, BedrockVersion n2) => n1.CompareTo(n2) >= 0;
        public static bool operator <(BedrockVersion n1, BedrockVersion n2) => n1.CompareTo(n2) < 0;
        public static bool operator <=(BedrockVersion n1, BedrockVersion n2) => n1.CompareTo(n2) <= 0;

        public override readonly bool Equals(object obj)
        {
            if (obj is not BedrockVersion) return false;
            return Equals((BedrockVersion) obj);
        }

        public readonly bool Equals(BedrockVersion other)
        {
            return other.a == a && other.b == b && other.c == c;
        }

        public override readonly int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode() ^ c.GetHashCode();
        }

        public override readonly string ToString()
        {
            return $"[ {a}, {b}, {c} ]";
        }
    }

    public class EntityResourceManager
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        public readonly Dictionary<ResourceLocation, EntityDefinition> entityDefinitions = new();
        public readonly Dictionary<string, EntityGeometry> entityGeometries = new();
        
        private readonly string resourcePath;

        public EntityResourceManager(string resPath)
        {
            resourcePath = resPath;
        }

        public IEnumerator LoadEntityResources(DataLoadFlag flag, Action<string> updateStatus)
        {
            if (!Directory.Exists(resourcePath))
            {
                Debug.LogWarning("Bedrock resource not present!");
                yield break;
            }

            var defFolderDir = new DirectoryInfo($"{resourcePath}{SP}entity");
            foreach (var defFile in defFolderDir.GetFiles("*.json", SearchOption.TopDirectoryOnly)) // No sub folders...
            {
                var data = Json.ParseJson(File.ReadAllText(defFile.FullName));

                var entityDef = EntityDefinition.FromJson(data);
                var entityType = entityDef.EntityType;

                if (entityDefinitions.ContainsKey(entityType)) // Check version
                {
                    var prev = entityDefinitions[entityType];

                    if (prev.FormatVersion < entityDef.FormatVersion || prev.MinEngineVersion < entityDef.MinEngineVersion) // Update this entry
                    {
                        entityDefinitions[entityType] = entityDef;
                        //Debug.Log($"Updating entry: [{entityType}] {defFile} v{entityDef.MinEngineVersion}");
                    }
                }
                else // Just register
                {
                    entityDefinitions.Add(entityType, entityDef);
                    //Debug.Log($"Creating entry: [{entityType}] {defFile} v{entityDef.MinEngineVersion}");
                }
            }

            yield return null;

            var geoFolderDir = new DirectoryInfo($"{resourcePath}{SP}models");
            foreach (var geoFile in geoFolderDir.GetFiles("*.json", SearchOption.AllDirectories)) // Allow sub folders...
            {
                var data = Json.ParseJson(File.ReadAllText(geoFile.FullName));
                //Debug.Log($"START {geoFile}");

                try
                {
                    foreach (var pair in EntityGeometry.TableFromJson(data)) // For each geometry in this file
                    {
                        var geoName = pair.Key;
                        var geometry = pair.Value;

                        if (entityGeometries.ContainsKey(geoName)) // Check version
                        {
                            var prev = entityGeometries[geoName];

                            if (prev.FormatVersion < geometry.FormatVersion || prev.MinEngineVersion < geometry.MinEngineVersion) // Update this entry
                            {
                                entityGeometries[geoName] = geometry;
                                //Debug.Log($"Updating entry: [{geoName}] {geoFile} v{geometry.MinEngineVersion}");
                            }
                        }
                        else // Just register
                        {
                            entityGeometries.Add(geoName, geometry);
                            //Debug.Log($"Creating entry: [{geoName}] {geoFile} v{geometry.MinEngineVersion}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"An error occurred when parsing {geoFile}: {e}");
                }
            }
        }
    }
}