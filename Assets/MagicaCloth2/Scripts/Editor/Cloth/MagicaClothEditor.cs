// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネントのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaCloth))]
    [CanEditMultipleObjects]
    public class MagicaClothEditor : Editor
    {
        //=========================================================================================
        private void Awake()
        {
            // 選択のたびに呼ばれるのでMonoの動作とは異なる
            //Debug.Log("MagicaClothEditor.Awake");
        }

        private void OnEnable()
        {
            //Debug.Log("MagicaClothEditor.OnEnable");
            ClothEditorManager.OnEditMeshBuildComplete += OnEditMeshBuildComplete;
        }

        private void OnDisable()
        {
            //Debug.Log("MagicaClothEditor.OnDisable");
            ClothEditorManager.OnEditMeshBuildComplete -= OnEditMeshBuildComplete;
            ClothPainter.ExitPaint();
        }

        private void OnDestroy()
        {
            // 選択が外れるたびに呼ばれるのでMonoの動作とは異なる
            //Debug.Log("MagicaClothEditor.OnDestroy");
            //Debug.Log(target != null);
        }

        private void OnValidate()
        {
            // どうもMonoのValidateは違う
            //Debug.Log("MagicaClothEditor.OnValidate");
        }

        private void Reset()
        {
            //Debug.Log("MagicaClothEditor.Reset");
        }

        //=========================================================================================
        int oldAcitve = -1;

        //=========================================================================================
        /// <summary>
        /// 編集用のセレクションデータを取得する
        /// </summary>
        /// <param name="cloth"></param>
        /// <param name="editMesh"></param>
        /// <returns></returns>
        public SelectionData GetSelectionData(MagicaCloth cloth, VirtualMesh editMesh)
        {
            // すでにセレクションデータが存在し、かつユーザー編集データならばコンバートする
            var selectionData = ClothEditorManager.CreateAutoSelectionData(cloth, cloth.SerializeData, editMesh);
            if (cloth.GetSerializeData2().selectionData != null && cloth.GetSerializeData2().selectionData.userEdit)
            {
                //Debug.Log($"セレクションデータコンバート!");
                selectionData.ConvertFrom(cloth.GetSerializeData2().selectionData);
                selectionData.userEdit = true;
            }

            return selectionData;
        }

        /// <summary>
        /// エディットメッシュの構築完了通知（成否問わず）
        /// </summary>
        void OnEditMeshBuildComplete()
        {
            //Debug.Log($"MagicaClothInspector. OnEditMeshBuildComplete.");
            Repaint();
        }

        /// <summary>
        /// インスペクターGUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            var cloth = target as MagicaCloth;

            // 状態
            DispVersion();
            DispStatus();
            DispProxyMesh();

            // 設定
            serializedObject.Update();
            Undo.RecordObject(cloth, "MagicaCloth2");
            EditorGUILayout.Space();
            ClothMainInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            ClothParameterInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GizmoInspector();
            EditorGUILayout.Space();
            ClothPreBuildInspector();
            serializedObject.ApplyModifiedProperties();

            //DrawDefaultInspector();

            // アクティブが変更された場合は編集メッシュを再構築する
            int nowActive = cloth.isActiveAndEnabled ? 1 : 0;
            if (nowActive != oldAcitve)
            {
                //Debug.Log($"[{cloth.name}] rebuild. active:{nowActive}");
                oldAcitve = nowActive;
                ClothEditorManager.RegisterComponent(cloth, nowActive > 0 ? GizmoType.Active : 0, true);
            }
        }

        /// <summary>
        /// クロスペイントの適用
        /// </summary>
        /// <param name="selectiondata"></param>
        internal void ApplyClothPainter(SelectionData selectionData)
        {
            if (selectionData == null || selectionData.IsValid() == false)
                return;

            var cloth = target as MagicaCloth;

            // セレクションデータ格納
            ClothEditorManager.ApplySelectionData(cloth, selectionData);
        }

        /// <summary>
        /// クロスペイントの変更による編集メッシュの再構築
        /// </summary>
        internal void UpdateEditMesh()
        {
            var cloth = target as MagicaCloth;

            // 編集用メッシュの再構築
            ClothEditorManager.RegisterComponent(cloth, GizmoType.Active, true); // 強制更新
        }

        //=========================================================================================
        void DispVersion()
        {
            EditorGUILayout.LabelField($"Version {AboutMenu.MagicaClothVersion}");
        }

        void DispStatus()
        {
            var cloth = target as MagicaCloth;

            if (EditorApplication.isPlaying)
            {
                StaticStringBuilder.Clear();
                StaticStringBuilder.AppendLine("[State]");
                StaticStringBuilder.Append(cloth.Process.IsState(ClothProcess.State_UsePreBuild) ? "Pre-Build Construction" : "Runtime Construction");
                DispClothStatus(StaticStringBuilder.ToString(), cloth.Process.Result, true);
            }
            else
            {
                var result = ClothEditorManager.GetResultCode(cloth);
                var preBuildData = cloth.GetSerializeData2().preBuildData;
                if (preBuildData.enabled)
                {
                    // pre-build
                    if (result.IsError() == false)
                        result = preBuildData.DataValidate();
                    DispClothStatus("[Pre-Build Construction]", result, false);
                }
                else
                {
                    // runtime
                    DispClothStatus("[Runtime Construction]", result, true);
                }
            }
        }

        void DispClothStatus(string title, ResultCode result, bool dispWarning)
        {
            StaticStringBuilder.Clear();
            StaticStringBuilder.AppendLine(title);

            // normal / error
            MessageType mtype = MessageType.Info;
            if (result.IsError())
                mtype = MessageType.Error;
            var infoMessage = result.GetResultInformation();
            if (infoMessage != null)
            {
                StaticStringBuilder.AppendLine(result.GetResultString());
                StaticStringBuilder.AppendLine(infoMessage);
            }
            else
            {
                StaticStringBuilder.AppendLine(result.GetResultString());
            }
            EditorGUILayout.HelpBox(StaticStringBuilder.ToString(), mtype);

            // warning
            if (dispWarning && result.IsWarning())
            {
                mtype = MessageType.Warning;
                infoMessage = result.GetWarningInformation();
                if (infoMessage != null)
                    EditorGUILayout.HelpBox($"{result.GetWarningString()}\n{infoMessage}", mtype);
                else
                    EditorGUILayout.HelpBox(result.GetWarningString(), mtype);
            }
        }

        void DispProxyMesh()
        {
            var cloth = target as MagicaCloth;

            VirtualMeshContainer cmesh;
            if (EditorApplication.isPlaying)
            {
                cmesh = cloth.Process?.ProxyMeshContainer;
            }
            else
            {
                cmesh = ClothEditorManager.GetEditMeshContainer(cloth);
            }
            if (cmesh == null || cmesh.shareVirtualMesh == null)
                return;

            var vmesh = cmesh.shareVirtualMesh;

            StaticStringBuilder.Clear();
            if (EditorApplication.isPlaying)
                StaticStringBuilder.AppendLine("[Proxy Mesh]");
            else
                StaticStringBuilder.AppendLine("[Edit Mesh]");
            if (EditorApplication.isPlaying)
                StaticStringBuilder.AppendLine($"Visible: {!cloth.Process.IsCullingInvisible()}");
            StaticStringBuilder.AppendLine($"Vertex: {vmesh.VertexCount}");
            StaticStringBuilder.AppendLine($"Edge: {vmesh.EdgeCount}");
            StaticStringBuilder.AppendLine($"Triangle: {vmesh.TriangleCount}");
            StaticStringBuilder.AppendLine($"SkinBoneCount: {vmesh.SkinBoneCount}");
            StaticStringBuilder.Append($"TransformCount: {cmesh.GetTransformCount()}");

            EditorGUILayout.HelpBox(StaticStringBuilder.ToString(), MessageType.Info);
        }


        void ClothMainInspector()
        {
            var cloth = target as MagicaCloth;
            var clothType = cloth.SerializeData.clothType;
            bool isBoneSpring = clothType == ClothProcess.ClothType.BoneSpring;

            bool runtime = EditorApplication.isPlaying;

            // 同期状態
            bool sync = EditorApplication.isPlaying && cloth.SyncCloth != null;

            EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);

            // Cloth
            {
                var clothTypeProperty = serializedObject.FindProperty("serializeData.clothType");

                using (new EditorGUI.DisabledScope(runtime))
                {
                    EditorGUILayout.PropertyField(clothTypeProperty, new GUIContent("Cloth Type"));
                }

                var paintMode = serializedObject.FindProperty("serializeData.paintMode");

                using (new EditorGUI.IndentLevelScope())
                {
                    if (clothType == ClothProcess.ClothType.BoneCloth)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.rootBones"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.connectionMode"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.rootRotation"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.rotationalInterpolation"));
                    }
                    else if (clothType == ClothProcess.ClothType.BoneSpring)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.rootBones"));
                        // BoneSpringでは接続モードは指定させない。内部ではLineで固定される。
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("ConnectionMode");
                            EditorGUILayout.LabelField("[Line]");
                        }
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.rootRotation"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.rotationalInterpolation"));
                    }
                    else if (clothType == ClothProcess.ClothType.MeshCloth)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.sourceRenderers"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.reductionSetting"));
                    }

                    EditorGUILayout.Space();
                    if (sync == false)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.updateMode"));
                    else
                    {
                        // 同期中は操作不可
                        using (new EditorGUI.DisabledScope(true))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Update Mode");
                                EditorGUILayout.LabelField("(Synchronizing)");
                            }
                        }
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.animationPoseRatio"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.normalAxis"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.normalAlignmentSetting.alignmentMode"), new GUIContent("Normal Alignment"));
                    if (cloth.SerializeData.normalAlignmentSetting.alignmentMode == NormalAlignmentSettings.AlignmentMode.Transform)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.normalAlignmentSetting.adjustmentTransform"));
                    }

                    if (clothType == ClothProcess.ClothType.MeshCloth)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(paintMode);
                        if (paintMode.enumValueIndex != 0)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.paintMaps"));
                        }
                    }
                }

                // ペイントボタン
                if (paintMode.enumValueIndex == 0)
                {
                    EditorGUILayout.Space();
                    PaintButton(ClothPainter.PaintMode.Attribute);
                }
                else
                    EditorGUILayout.Space();
            }

            // Custom Skinning
            if (isBoneSpring == false)
            {
                Foldout("Custom Skinning", serializedObject.FindProperty("serializeData.customSkinningSetting.enable"), null, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.customSkinningSetting.skinningBones"));
                });
            }

            // Culling
            Foldout("Culling", null, () =>
            {
                if (sync == false)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.cullingSettings.cameraCullingMode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.cullingSettings.cameraCullingMethod"));
                    if (cloth.SerializeData.cullingSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.ManualRenderer)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.cullingSettings.cameraCullingRenderers"));
                    }
                }
                else
                {
                    // 同期中は操作不可
                    using (new EditorGUI.DisabledScope(true))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Camera Culling Mode");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Camera Culling Method");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                    }
                }
            });
        }

        void ClothPreBuildInspector()
        {
            var cloth = target as MagicaCloth;

            bool generation = false;

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                Foldout("Pre-Build", serializedObject.FindProperty("serializeData2.preBuildData.enabled"), null, () =>
                {
                    // information
                    var preBuildData = cloth.GetSerializeData2().preBuildData;
                    if (preBuildData.UsePreBuild())
                    {
                        DispClothStatus("[Pre-Build Construction]", preBuildData.DataValidate(), false);
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData2.preBuildData.buildId"), new GUIContent("Build ID"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData2.preBuildData.preBuildScriptableObject"), new GUIContent("Write Object"));
                    using (var horizontalScope = new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.Space();
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("Create PreBuild Data"))
                        {
                            generation = true;
                        }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.Space();
                    }
                });
            }

            if (generation)
            {
                // PreBuildデータ構築実行
                Develop.Log($"Start PreBuild data creation.");
                var preBuildResult = PreBuildDataCreation.CreatePreBuildData(cloth);
                Develop.Log($"PreBuild data creation completed. [{cloth.GetSerializeData2().preBuildData.buildId}] : {preBuildResult.GetResultString()}");
            }
        }

        void ClothParameterInspector()
        {
            var cloth = target as MagicaCloth;
            var clothType = cloth.SerializeData.clothType;
            bool isBoneSpring = clothType == ClothProcess.ClothType.BoneSpring;

            // 同期状態
            bool sync = EditorApplication.isPlaying && cloth.SyncCloth != null;

            ClothPresetUtility.DrawPresetButton(cloth, cloth.SerializeData);

            // Force
            Foldout("Force", null, () =>
            {
                if (isBoneSpring == false)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.gravity"), new GUIContent("Gravity"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.gravityDirection"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.gravityFalloff"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.damping"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.stablizationTimeAfterReset"), new GUIContent("Stablization Time"));
            });

            // Spring
            if (isBoneSpring)
            {
                Foldout("Spring", serializedObject.FindProperty("serializeData.springConstraint.useSpring"), null, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.springConstraint.springPower"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.springConstraint.limitDistance"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.springConstraint.normalLimitRatio"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.springConstraint.springNoise"));
                });
            }

            // Angle Restoration
            Foldout("Angle Restoration", serializedObject.FindProperty("serializeData.angleRestorationConstraint.useAngleRestoration"), null, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.angleRestorationConstraint.stiffness"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.angleRestorationConstraint.velocityAttenuation"));
                if (isBoneSpring == false)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.angleRestorationConstraint.gravityFalloff"));
                }
            }
            );

            // Angle Limit
            Foldout("Angle Limit", serializedObject.FindProperty("serializeData.angleLimitConstraint.useAngleLimit"), null, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.angleLimitConstraint.limitAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.angleLimitConstraint.stiffness"));
            }
            );

            // Shape
            // BoneSpringではすべて定数なので隠蔽する
            if (isBoneSpring == false)
            {
                Foldout("Shape Restoration", null, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.distanceConstraint.stiffness"), new GUIContent("Distance Stiffness"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.tetherConstraint.distanceCompression"), new GUIContent("Tether Compression"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.triangleBendingConstraint.stiffness"), new GUIContent("Triangle Bending Stiffness"));
                });
            }

            // Inertia
            Foldout("Inertia", null, () =>
            {
                if (sync == false)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.anchor"));
                    if (cloth.SerializeData.inertiaConstraint.anchor != null)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.anchorInertia"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.worldInertia"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.movementInertiaSmoothing"), new GUIContent("World Inertia Smoothing"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.movementSpeedLimit"), new GUIContent("World Movement Speed Limit"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.rotationSpeedLimit"), new GUIContent("World Rotation Speed Limit"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.teleportMode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.teleportDistance"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.teleportRotation"));
                }
                else
                {
                    // 同期中は操作不可
                    using (new EditorGUI.DisabledScope(true))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Anchor");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Anchor Inertia");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        EditorGUILayout.Space();
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("World Inertia");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("World Inertia Smoothing");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("World Movement Speed Limit");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("World Rotation Speed Limit");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Teleport Mode");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Teleport Distance");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Teleport Rotation");
                            EditorGUILayout.LabelField("(Synchronizing)");
                        }
                    }
                }
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.localInertia"), new GUIContent("Local Inertia"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.localMovementSpeedLimit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.localRotationSpeedLimit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.depthInertia"), new GUIContent("Local Depth Inertia"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.centrifualAcceleration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.inertiaConstraint.particleSpeedLimit"));
            });

            // Motion
            if (isBoneSpring == false)
            {
                Foldout("Movement Limit", null, () =>
                {
                    var useMaxDistance = serializedObject.FindProperty("serializeData.motionConstraint.useMaxDistance");
                    var useBackstop = serializedObject.FindProperty("serializeData.motionConstraint.useBackstop");
                    EditorGUILayout.PropertyField(useMaxDistance);
                    using (new EditorGUI.DisabledScope(!useMaxDistance.boolValue))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.motionConstraint.maxDistance"));
                    }
                    EditorGUILayout.PropertyField(useBackstop);
                    using (new EditorGUI.DisabledScope(!useBackstop.boolValue))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.motionConstraint.backstopRadius"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.motionConstraint.backstopDistance"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.motionConstraint.stiffness"));

                    var paintMode = serializedObject.FindProperty("serializeData.paintMode");
                    if (paintMode.enumValueIndex == 0)
                        PaintButton(ClothPainter.PaintMode.Motion);
                }
                );
            }

            // Collider Collision
            Foldout("Collider Collision", null, () =>
            {
                if (isBoneSpring)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Mode");
                        EditorGUILayout.LabelField("[Point]");
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.colliderCollisionConstraint.mode"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.radius"));
                if (clothType == ClothProcess.ClothType.BoneSpring)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.colliderCollisionConstraint.limitDistance"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.colliderCollisionConstraint.collisionBones"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.colliderCollisionConstraint.friction"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.colliderCollisionConstraint.colliderList"));
            }
            );

            // Self Collision
            if (isBoneSpring == false)
            {
                Foldout("Self Collision", "Self Collision (Beta)", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.selfCollisionConstraint.selfMode"));
                    var syncMode = serializedObject.FindProperty("serializeData.selfCollisionConstraint.syncMode");
                    EditorGUILayout.PropertyField(syncMode);
                    if (syncMode.enumValueIndex != 0)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.selfCollisionConstraint.syncPartner"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.selfCollisionConstraint.surfaceThickness"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.selfCollisionConstraint.clothMass"));
                }
                );
            }

            // Wind
            Foldout("Wind", null, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.influence"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.frequency"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.turbulence"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.blend"), new GUIContent("Noise Blend"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.synchronization"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.depthWeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serializeData.wind.movingWind"));
            });
        }

        /// <summary>
        /// 各プロパティの設定範囲.デフォルトは(0.0 ~ 1.0)
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static Vector2 GetPropertyMinMax(string propertyName)
        {
            var minmax = new Vector2(0.0f, 1.0f);

            switch (propertyName)
            {
                case "radius":
                    minmax.Set(0.001f, 0.5f);
                    break;
                case "limitAngle":
                    minmax.Set(0.0f, 180.0f);
                    break;
                case "maxDistance":
                    minmax.Set(0.0f, 5.0f);
                    break;
                case "surfaceThickness":
                    minmax.Set(Define.System.SelfCollisionThicknessMin, Define.System.SelfCollisionThicknessMax);
                    break;
                case "movementSpeedLimit":
                case "localMovementSpeedLimit":
                    minmax.Set(0.0f, Define.System.MaxMovementSpeedLimit);
                    break;
                case "rotationSpeedLimit":
                case "localRotationSpeedLimit":
                    minmax.Set(0.0f, Define.System.MaxRotationSpeedLimit);
                    break;
                case "particleSpeedLimit":
                    minmax.Set(0.0f, Define.System.MaxParticleSpeedLimit);
                    break;
            }

            return minmax;
        }

        void GizmoInspector()
        {
#if MC2_DEBUG
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData"));
#else
            FoldOut("Gizmos", null, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.always"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.enable"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.ztest"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.position"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.axis"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.shape"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.baseLine"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.depth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.collider"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.animatedPosition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.animatedAxis"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.animatedShape"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.inertiaCenter"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.basicPosition"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.basicAxis"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSerializeData.clothDebugSettings.basicShape"));
            });
#endif
        }

        //=========================================================================================
        /// <summary>
        /// 折りたたみ制御
        /// </summary>
        /// <param name="foldKey">折りたたみ保存キー</param>
        /// <param name="title"></param>
        /// <param name="drawAct">内容描画アクション</param>
        /// <param name="enableAct">有効フラグアクション(null=無効)</param>
        /// <param name="enable">現在の有効フラグ</param>
        public void Foldout(
            string foldKey,
            string title = null,
            System.Action drawAct = null,
            System.Action<bool> enableAct = null,
            bool enable = true
            )
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);

            GUI.backgroundColor = Color.white;
            GUI.Box(rect, title ?? foldKey, style);

            var e = Event.current;
            bool foldOut = EditorPrefs.GetBool(foldKey);

            if (enableAct == null)
            {
                if (e.type == EventType.Repaint)
                {
                    var arrowRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                    EditorStyles.foldout.Draw(arrowRect, false, false, foldOut, false);
                }
            }
            else
            {
                // 有効チェック
                var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
                bool sw = GUI.Toggle(toggleRect, enable, string.Empty, new GUIStyle("ShurikenCheckMark"));
                if (sw != enable)
                {
                    enableAct(sw);
                }
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                foldOut = !foldOut;
                EditorPrefs.SetBool(foldKey, foldOut);
                e.Use();
            }

            if (foldOut && drawAct != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(!enable))
                    {
                        drawAct();
                    }
                }
            }
        }

        /// <summary>
        /// 折りたたみ制御（Boolプロパティによるチェックあり）
        /// </summary>
        /// <param name="foldKey"></param>
        /// <param name="boolProperty"></param>
        /// <param name="title"></param>
        /// <param name="drawAct"></param>
        public void Foldout(
            string foldKey,
            SerializedProperty boolProperty,
            string title = null,
            System.Action drawAct = null
            )
        {
            Foldout(
                foldKey, title, drawAct,
                (sw) => boolProperty.boolValue = sw,
                boolProperty.boolValue
                );
        }

        void FoldOut(string key, string title = null, System.Action drawAct = null)
        {
            bool foldOut1 = EditorPrefs.GetBool(key);
            bool foldOut2 = EditorGUILayout.Foldout(foldOut1, title ?? key);
            if (foldOut2)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    drawAct?.Invoke();
                }
            }
            if (foldOut1 != foldOut2)
            {
                EditorPrefs.SetBool(key, foldOut2);
            }
        }

        void PaintButton(ClothPainter.PaintMode paintMode)
        {
            if (EditorApplication.isPlaying)
                return;

            var cloth = target as MagicaCloth;

            using (new EditorGUILayout.HorizontalScope())
            {
                switch (paintMode)
                {
                    case ClothPainter.PaintMode.Attribute:
                        GUI.backgroundColor = new Color(0.5f, 1.0f, 0.5f);
                        break;
                    case ClothPainter.PaintMode.Motion:
                        GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f);
                        break;
                }

                EditorGUILayout.Space();

                bool edit = ClothPainter.HasEditCloth(cloth);
                //var icon = edit ? EditorGUIUtility.IconContent("winbtn_win_close") : EditorGUIUtility.IconContent("d_editicon.sml");
                //var icon = EditorGUIUtility.IconContent("d_Grid.PaintTool");// 良い
                var icon = EditorGUIUtility.IconContent("d_editicon.sml");
                if (GUILayout.Button(icon, GUILayout.Width(40)))
                {
                    if (edit == false)
                    {
                        // 最新の編集メッシュからセレクションデータを生成する
                        var editMeshContainer = ClothEditorManager.GetEditMeshContainer(cloth);
                        if (editMeshContainer != null && editMeshContainer.shareVirtualMesh != null)
                        {
                            // すでにセレクションデータが存在し、かつユーザー編集データならばコンバートする
                            var selectionData = GetSelectionData(cloth, editMeshContainer.shareVirtualMesh);

                            // セレクションデータにメッシュの最大接続距離を記録する
                            selectionData.maxConnectionDistance = editMeshContainer.shareVirtualMesh.maxVertexDistance.Value;

                            // ペイント開始
                            ClothPainter.EnterPaint(paintMode, this, cloth, editMeshContainer, selectionData);
                            SceneView.RepaintAll();
                        }
                    }
                    else
                    {
                        ClothPainter.ExitPaint();
                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.Space(10);
        }
    }
}
