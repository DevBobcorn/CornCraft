using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CraftSharp.Rendering.Editor
{
    public class WaterAssetUtils
    {
        private enum InjectionType
        {
            Before,
            After,
            Replace
        }

        [MenuItem("CornCraft/Water Asset Utils/Generate SW3 Dynamic Color Assets")]
        private static void GenerateSW3DynamicColorAssets()
        {
            Debug.Log("Generating SW3 Dynamic Color Assets...");

            var stylizedWaterFolder = "Assets/Stylized Water 3";
            
            // Check if the folder exists
            if (!AssetDatabase.IsValidFolder(stylizedWaterFolder))
            {
                throw new Exception($"The folder '{stylizedWaterFolder}' does not exist. Check your SW3 installation.");
            }

            // Define source and destination paths
            var srcVertexPath = Path.Combine(stylizedWaterFolder, "Shaders/Libraries/Vertex.hlsl");
            var dstVertexPath = Path.Combine(stylizedWaterFolder, "Shaders/Libraries/VertexDynamicColor.hlsl");
            var srcPassPath = Path.Combine(stylizedWaterFolder, "Shaders/Passes/ForwardPass.hlsl");
            var dstPassPath = Path.Combine(stylizedWaterFolder, "Shaders/Passes/ForwardPassDynamicColor.hlsl");
            var srcShaderPath = Path.Combine(stylizedWaterFolder, "Shaders/StylizedWater3_Standard.watershader3");
            var dstShaderPath = Path.Combine(stylizedWaterFolder, "Shaders/StylizedWater3_DynamicColor.watershader3");
            var srcShaderMetaPath = Path.Combine(stylizedWaterFolder, "Shaders/StylizedWater3_Standard.watershader3.meta");
            var dstShaderMetaPath = Path.Combine(stylizedWaterFolder, "Shaders/StylizedWater3_DynamicColor.watershader3.meta");

            // Check if source file exists
            if (!File.Exists(srcPassPath) || !File.Exists(srcVertexPath) || !File.Exists(srcShaderPath))
            {
                throw new Exception($"Some or all source files are missing. Check your SW3 installation.");
            }

            // Copy the files
            File.Copy(srcPassPath, dstPassPath, overwrite: true);
            File.Copy(srcVertexPath, dstVertexPath, overwrite: true);
            File.Copy(srcShaderPath, dstShaderPath, overwrite: true);
            File.Copy(srcShaderMetaPath, dstShaderMetaPath, overwrite: true);

            // Patch copied files

            // Detect newline format from the first file
            var firstFileContent = File.ReadAllText(srcVertexPath);
            string newLine;
            if (firstFileContent.Contains("\r\n"))
            {
                newLine = "\r\n";
            }
            else if (firstFileContent.Contains("\n"))
            {
                newLine = "\n";
            }
            else if (firstFileContent.Contains("\r"))
            {
                newLine = "\r";
            }
            else
            {
                // Default to system newline if none detected
                newLine = Environment.NewLine;
            }

            // - Patch VertexDynamicColor.hlsl
            var vertexContent = File.ReadAllText(dstVertexPath);
            vertexContent = InjectStringAtNthOccurrence(
                vertexContent,
                "COLOR0;",
                $"{newLine}\tfloat4 dynamicColor : COLOR1;",
                -1,
                InjectionType.After
            );
            vertexContent = InjectStringAtNthOccurrence(
                vertexContent,
                "float4 vertexColor",
                $"output.dynamicColor = input.color;{newLine}\t",
                1,
                InjectionType.Before
            );
            File.WriteAllText(dstVertexPath, vertexContent);

            // - Patch ForwardPassDynamicColor.hlsl
            var passContent = File.ReadAllText(dstPassPath);
            passContent = InjectStringAtNthOccurrence(
                passContent,
                "lerp(_ShallowColor, _BaseColor, water.fog)",
                "lerp(float4(input.dynamicColor.rgb, 0.1), float4(input.dynamicColor.rgb, 0.95), water.fog)",
                1,
                InjectionType.Replace
            );
            File.WriteAllText(dstPassPath, passContent);

            // - Patch StylizedWater3_DynamicColor.watershader3
            var shaderContent = File.ReadAllText(dstShaderPath);
            shaderContent = InjectStringAtNthOccurrence(
                shaderContent,
                "Vertex.hlsl",
                "VertexDynamicColor.hlsl",
                0,
                InjectionType.Replace
            );
            shaderContent = InjectStringAtNthOccurrence(
                shaderContent,
                "ForwardPass.hlsl",
                "ForwardPassDynamicColor.hlsl",
                0,
                InjectionType.Replace
            );
            File.WriteAllText(dstShaderPath, shaderContent);

            // - Patch StylizedWater3_DynamicColor.watershader3.meta
            var shaderMetaContent = File.ReadAllText(dstShaderMetaPath);
            shaderMetaContent = InjectStringAtNthOccurrence(
                shaderMetaContent,
                "Vertex.hlsl",
                "VertexDynamicColor.hlsl",
                0,
                InjectionType.Replace
            );
            shaderMetaContent = InjectStringAtNthOccurrence(
                shaderMetaContent,
                "ForwardPass.hlsl",
                "ForwardPassDynamicColor.hlsl",
                0,
                InjectionType.Replace
            );
            shaderMetaContent = InjectStringAtNthOccurrence(
                shaderMetaContent,
                "823f6b206953b674a9a64f9e3ec57752",
                "736ec786f5b19444fa49d07e41df79ca",
                0,
                InjectionType.Replace
            );
            File.WriteAllText(dstShaderMetaPath, shaderMetaContent);

            // Tell Unity to import the assets and generate meta files
            AssetDatabase.ImportAsset(dstPassPath);
            AssetDatabase.ImportAsset(dstVertexPath);
            AssetDatabase.ImportAsset(dstShaderPath);
            
            Debug.Log($"Successfully created '{dstPassPath}', '{dstVertexPath}' and '{dstShaderPath}' and generated meta files.");
        }

        /// <summary>
        /// Injects a string before or after the nth occurrence of a search string in the source text.
        /// </summary>
        /// <param name="source">The source text to modify</param>
        /// <param name="searchString">The string to search for</param>
        /// <param name="injectionString">The string to inject</param>
        /// <param name="nthOccurrence">The occurrence number (1-based, e.g., 1 for first, 2 for second). Use negative values to count backwards (e.g., -1 for last, -2 for second-to-last). Use 0 to inject at every occurrence.</param>
        /// <param name="injectionType">The type of injection to perform</param>
        /// <returns>The modified string with the injection, or the original string if the nth occurrence is not found</returns>
        private static string InjectStringAtNthOccurrence(string source, string searchString, string injectionString, int nthOccurrence, InjectionType injectionType = InjectionType.After)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(searchString))
            {
                return source;
            }

            // Collect all occurrences
            var occurrences = new List<int>();
            var currentIndex = 0;

            while (currentIndex < source.Length)
            {
                var foundIndex = source.IndexOf(searchString, currentIndex, StringComparison.Ordinal);
                
                if (foundIndex == -1)
                {
                    // No more occurrences found
                    break;
                }

                occurrences.Add(foundIndex);
                currentIndex = foundIndex + 1;
            }

            if (occurrences.Count == 0)
            {
                // No occurrences found
                Debug.LogWarning($"InjectStringAtNthOccurrence: No occurrences of '{searchString}' found in source text.");
                return source;
            }

            // Handle nthOccurrence == 0: inject at every occurrence
            if (nthOccurrence == 0)
            {
                // Process from end to beginning to maintain correct indices
                var result = source;
                for (var i = occurrences.Count - 1; i >= 0; i--)
                {
                    var targetIndex = occurrences[i];
                    switch (injectionType)
                    {
                        case InjectionType.Before:
                            result = result.Insert(targetIndex, injectionString);
                            break;
                        case InjectionType.After:
                            result = result.Insert(targetIndex + searchString.Length, injectionString);
                            break;
                        case InjectionType.Replace:
                            result = result.Remove(targetIndex, searchString.Length).Insert(targetIndex, injectionString);
                            break;
                        default:
                            throw new Exception($"Invalid injection type: {injectionType}");
                    }
                }
                return result;
            }

            // Determine which occurrence to use
            int targetIndex2;
            if (nthOccurrence > 0)
            {
                // Positive: count from beginning (1-based)
                if (nthOccurrence > occurrences.Count)
                {
                    // Nth occurrence not found
                    Debug.LogWarning($"InjectStringAtNthOccurrence: Requested occurrence {nthOccurrence} not found. Only {occurrences.Count} occurrence(s) of '{searchString}' found in source text.");
                    return source;
                }
                targetIndex2 = occurrences[nthOccurrence - 1];
            }
            else
            {
                // Negative: count from end (-1 is last, -2 is second-to-last, etc.)
                var absNth = -nthOccurrence;
                if (absNth > occurrences.Count)
                {
                    // Nth occurrence from end not found
                    Debug.LogWarning($"InjectStringAtNthOccurrence: Requested occurrence {nthOccurrence} (from end) not found. Only {occurrences.Count} occurrence(s) of '{searchString}' found in source text.");
                    return source;
                }
                targetIndex2 = occurrences[^absNth];
            }

            // Inject at the target position
            switch (injectionType)
            {
                case InjectionType.Before:
                    return source.Insert(targetIndex2, injectionString);
                case InjectionType.After:
                    return source.Insert(targetIndex2 + searchString.Length, injectionString);
                case InjectionType.Replace:
                    return source.Remove(targetIndex2, searchString.Length).Insert(targetIndex2, injectionString);
                default:
                    throw new Exception($"Invalid injection type: {injectionType}");
            }
        }
    }
}