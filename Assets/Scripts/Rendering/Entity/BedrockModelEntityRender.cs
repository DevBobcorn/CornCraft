using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Molang.Runtime;
using CraftSharp.Molang.Runtime.Value;
using CraftSharp.Molang.Utils;
using CraftSharp.Resource;
using CraftSharp.Resource.BedrockEntity;

namespace CraftSharp.Rendering
{
    public class BedrockModelEntityRender : EntityRender
    {
        
        
        #nullable enable
        
        private EntityRenderDefinition? entityDefinition = null;

        private readonly Dictionary<string, GameObject> boneObjects = new();

        public string[] TextureNames = { };
        public EntityRenderType RenderType;
        private EntityMaterialManager? materialManager;
        
        private readonly List<MeshRenderer> renderers = new();

        private EntityGeometry? geometry = null;

        public string[] AnimationNames = { };
        private EntityAnimation?[] animations = { };
        private EntityAnimation? currentAnimation = null;

        private readonly MoScope scope = new(new MoLangRuntime());
        private readonly MoLangEnvironment env = new();
        
        #nullable disable

        public void SetDefinitionData(EntityRenderDefinition def)
        {
            entityDefinition = def;
        }

        public void BuildEntityModel(BedrockEntityResourceManager entityResManager, EntityMaterialManager matManager)
        {
            renderers.Clear();

            materialManager = matManager;
            
            if (entityDefinition is null)
            {
                Debug.LogError("Entity definition not assigned!");
                return;
            }

            if (entityDefinition.GeometryNames.Count == 0)
            {
                Debug.LogWarning("Entity definition has no geometry!");
                return;
            }

            var geometryName = entityDefinition.GeometryNames.First().Value;
            gameObject.name += $" ({geometryName})";

            if (!entityResManager.EntityGeometries.TryGetValue(geometryName, out geometry))
            {
                // TODO: Debug.LogWarning($"Entity geometry [{geometryName}] not loaded!");
                return;
            }
            
            TextureNames = entityDefinition.TexturePaths.Select(x => x.Key).ToArray();
            var matId = entityDefinition.MaterialIdentifiers.First().Value;
            RenderType = entityResManager.MaterialRenderTypes.GetValueOrDefault(matId);

            // Build mesh for each bone
            foreach (var bone in geometry!.Bones.Values)
            {
                var boneObj = new GameObject($"Bone [{bone.Name}]");
                boneObj.transform.SetParent(transform, false);

                //var boneMeshObj = new GameObject($"Mesh [{bone.Name}]");
                //boneMeshObj.transform.SetParent(boneObj.transform, false);
                var boneMeshFilter = boneObj.AddComponent<MeshFilter>();
                var boneMeshRenderer = boneObj.AddComponent<MeshRenderer>();

                var visualBuffer = new EntityVertexBuffer();

                for (int i = 0;i < bone.Cubes.Length;i++)
                {
                    EntityCubeGeometry.Build(ref visualBuffer, geometry.TextureWidth, geometry.TextureHeight,
                            bone.MirrorUV, bone.Pivot, bone.Cubes[i]);
                }

                boneMeshFilter!.sharedMesh = EntityVertexBufferBuilder.BuildMesh(visualBuffer);
                renderers.Add(boneMeshRenderer);

                boneObjects.Add(bone.Name, boneObj);
            }
            // Setup initial bone pose
            foreach (var bone in geometry.Bones.Values)
            {
                var boneTransform = boneObjects[bone.Name].transform;

                if (bone.ParentName is not null) // Set parent transform
                {
                    if (boneObjects.TryGetValue(bone.ParentName, out var boneObj))
                    {
                        boneTransform.SetParent(boneObj.transform, false);
                        boneTransform.localPosition = (bone.Pivot - geometry.Bones[bone.ParentName].Pivot) / 16F;
                        boneTransform.localRotation = Rotations.RotationFromEulersXYZ(bone.Rotation);
                    }
                    else
                    {
                        Debug.LogWarning($"In {geometryName}: parent bone {bone.ParentName} not found!");
                    }
                }
                else // Root bone
                {
                    boneTransform.localPosition = bone.Pivot / 16F;
                    boneTransform.localRotation = Rotations.RotationFromEulersXYZ(bone.Rotation);
                }
            }
            
            // Load first texture
            SetTexture(0);
            
            // Prepare animations
            AnimationNames = entityDefinition.AnimationNames.Select(x => $"{x.Key} ({x.Value})").ToArray();
            animations = entityDefinition.AnimationNames.Select(x => 
                    {
                        var anim = entityResManager.EntityAnimations.GetValueOrDefault(x.Value);

                        // TODO: Debug.LogWarning($"Animation [{x.Value}] not loaded!");
                        return anim; 
                    }).ToArray();
        }

        public void SetTexture(int index)
        {
            if (index >= 0 && index < TextureNames.Length &&
                geometry is not null && materialManager && renderers.Any())
            {
                foreach (var _renderer in renderers)
                {
                    materialManager.ApplyBedrockMaterial(RenderType, TextureNames[index], mat =>
                    {
                        _renderer.sharedMaterial = mat;
                    }, geometry.TextureWidth, geometry.TextureHeight);
                }
            }
            else
            {
                throw new System.Exception($"Invalid texture index: {index}");
            }
        }

        public EntityAnimation SetAnimation(int index, float initialTime)
        {
            if (index >= 0 && index < animations.Length)
            {
                currentAnimation = animations[index];
                UpdateAnimation(initialTime);

                return currentAnimation;
            }

            throw new System.Exception($"Invalid animation index: {index}");
        }

        public override void UpdateAnimation(float time)
        {
            if (currentAnimation != null && geometry != null) // An animation file is present
            {
                foreach (var boneAnim in currentAnimation.BoneAnimations)
                {
                    if (boneObjects.ContainsKey(boneAnim.Key))
                    {
                        var (trans, scale, rot) = boneAnim.Value.Evaluate(time, scope, env);
                        UpdateBone(boneAnim.Key, trans, scale, rot);
                    }
                    else
                    {
                        Debug.Log($"Trying to update bone [{boneAnim.Key}] which is not present!");
                    }
                }
            }
        }

        public void UpdateMolangValue(MoPath varName, IMoValue value)
        {
            env.SetValue(varName, value);
        }

        private void UpdateBone(string boneName, float3? trans, float3? scale, float3? rot)
        {
            var boneTransform = boneObjects[boneName].transform;
            var bone = geometry!.Bones[boneName];

            if (trans is not null)
            {
                var converted = trans.Value.zyx;
                converted.z = -converted.z;

                float3 offset;

                if (bone.ParentName is not null)
                    offset = (converted + bone.Pivot - geometry.Bones[bone.ParentName].Pivot) / 16F;
                else
                    offset = (converted + bone.Pivot) / 16F;

                boneTransform.localPosition = offset;
            }
            
            if (rot is not null)
            {
                var converted = rot.Value.zyx;
                converted.x = -converted.x;
                boneTransform.localRotation = Rotations.RotationFromEulersXYZ(converted);
            }
        }
    }
}