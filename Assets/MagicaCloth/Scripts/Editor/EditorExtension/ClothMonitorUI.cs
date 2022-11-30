// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using UnityEditor;
using UnityEngine;


namespace MagicaCloth
{
    [System.Serializable]
    public class ClothMonitorUI : ClothMonitorAccess
    {
        // クロス表示
        [SerializeField]
        private bool alwaysClothShow = false;

        [SerializeField]
        private bool drawCloth = true;

        [SerializeField]
        private bool drawClothVertex = true;

        [SerializeField]
        private bool drawClothRadius = true;

        [SerializeField]
        private bool drawClothDepth;

        [SerializeField]
        private bool drawClothBase;

        [SerializeField]
        private bool drawClothCollider = true;

        [SerializeField]
        private bool drawClothStructDistanceLine = true;

        [SerializeField]
        private bool drawClothBendDistanceLine;

        [SerializeField]
        private bool drawClothNearDistanceLine;

        [SerializeField]
        private bool drawClothRotationLine = true;

        [SerializeField]
        private bool drawClothTriangleBend = true;

        [SerializeField]
        private bool drawClothPenetration = false;

        [SerializeField]
        private bool drawClothAxis;

        [SerializeField]
        private bool drawClothSkinningBones = true;

        // デフォーマー表示
        [SerializeField]
        private bool alwaysDeformerShow = false;

        [SerializeField]
        private bool drawDeformer = true;

        [SerializeField]
        private bool drawDeformerVertexPosition;

        [SerializeField]
        private bool drawDeformerLine = true;

        [SerializeField]
        private bool drawDeformerTriangle = true;

        [SerializeField]
        private bool drawDeformerVertexAxis;

        // 風表示
        [SerializeField]
        private bool alwaysWindShow = true;
        [SerializeField]
        private bool drawWind = true;

#if MAGICACLOTH_DEBUG
        // デバッグ用
        [SerializeField]
        private bool drawClothVertexNumber;

        [SerializeField]
        private bool drawClothVertexIndex;

        [SerializeField]
        private bool drawClothFriction;

        [SerializeField]
        private bool drawClothStaticFriction;

        [SerializeField]
        private bool drawClothCollisionNormal;

        [SerializeField]
        private bool drawClothVelocity;

        [SerializeField]
        private bool drawClothVelocityVector;

        [SerializeField]
        private bool drawClothDepthNumber;

        [SerializeField]
        private bool drawPenetrationOrigin;

        [SerializeField]
        private int debugDrawDeformerTriangleNumber = -1;

        [SerializeField]
        private int debugDrawDeformerVertexNumber = -1;

        [SerializeField]
        private bool drawDeformerVertexNumber;

        [SerializeField]
        private bool drawDeformerTriangleNormal;

        [SerializeField]
        private bool drawDeformerTriangleNumber;

#endif

        //=========================================================================================
        Vector2 scrollPos;

        //=========================================================================================
        public override void Disable()
        {
        }

        public override void Enable()
        {
        }

        protected override void Create()
        {
        }

        public override void Destroy()
        {
        }

        public void OnGUI()
        {
            if (menu == null)
                return;

            Version();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            Information();
            DebugOption();
            EditorGUILayout.EndScrollView();
        }

        //=========================================================================================
        public bool AlwaysClothShow => alwaysClothShow;
        public bool AlwaysDeformerShow => alwaysDeformerShow;
        public bool DrawDeformer => drawDeformer;
        public bool DrawDeformerVertexPosition => drawDeformerVertexPosition;
        public bool DrawDeformerLine => drawDeformerLine;
        public bool DrawDeformerTriangle => drawDeformerTriangle;
        public bool DrawCloth => drawCloth;
        public bool DrawClothVertex => drawClothVertex;
        public bool DrawClothRadius => drawClothRadius;
        public bool DrawClothDepth => drawClothDepth;
        public bool DrawClothBase => drawClothBase;
        public bool DrawClothCollider => drawClothCollider;
        public bool DrawClothStructDistanceLine => drawClothStructDistanceLine;
        public bool DrawClothBendDistanceLine => drawClothBendDistanceLine;
        public bool DrawClothNearDistanceLine => drawClothNearDistanceLine;
        public bool DrawClothRotationLine => drawClothRotationLine;
        public bool DrawClothTriangleBend => drawClothTriangleBend;
        public bool DrawClothPenetration => drawClothPenetration;
        public bool DrawClothAxis => drawClothAxis;
        public bool DrawDeformerVertexAxis => drawDeformerVertexAxis;
        public bool AlwaysWindShow => alwaysWindShow;
        public bool DrawWind => drawWind;
        public bool DrawClothSkinningBones => drawClothSkinningBones;

#if MAGICACLOTH_DEBUG
        // デバッグ用
        public bool DrawClothVertexNumber => drawClothVertexNumber;
        public bool DrawClothVertexIndex => drawClothVertexIndex;
        public bool DrawClothFriction => drawClothFriction;
        public bool DrawClothStaticFriction => drawClothStaticFriction;
        public bool DrawClothCollisionNormal => drawClothCollisionNormal;
        public bool DrawClothVelocity => drawClothVelocity;
        public bool DrawClothVelocityVector => drawClothVelocityVector;
        public bool DrawClothDepthNumber => drawClothDepthNumber;
        public bool DrawPenetrationOrigin => drawPenetrationOrigin;
        public int DebugDrawDeformerTriangleNumber => debugDrawDeformerTriangleNumber;
        public int DebugDrawDeformerVertexNumber => debugDrawDeformerVertexNumber;
        public bool DrawDeformerTriangleNormal => drawDeformerTriangleNormal;
        public bool DrawDeformerTriangleNumber => drawDeformerTriangleNumber;
        public bool DrawDeformerVertexNumber => drawDeformerVertexNumber;
#endif

        //=========================================================================================
        void Version()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Magica Cloth. (Version " + AboutMenu.MagicaClothVersion + ")", EditorStyles.boldLabel);
        }

        void Information()
        {
            StaticStringBuilder.Clear();

            int teamCnt = 0;
            int normalTeamCnt = 0;
            int physicsTeamCnt = 0;
            int activeTeamCnt = 0;
            int teamCalculation = 0;

#if MAGICACLOTH_DEBUG
            int sharedVirtualMeshCnt = 0;
            int virtualMeshCnt = 0;
            int sharedChildMeshCnt = 0;
            int sharedRenderMeshCnt = 0;
            int renderMeshCnt = 0;

            int virtualMeshVertexCnt = 0;
            int virtualMeshTriangleCnt = 0;
            int renderMeshVertexCnt = 0;
#endif

            int virtualMeshUseCnt = 0;
            int virtualMeshUseCalculation = 0;
            int virtualMeshVertexUseCnt = 0;
            int renderMeshUseCnt = 0;
            int renderMeshUseCalculation = 0;
            int renderMeshVertexUseCnt = 0;

            int particleCnt = 0;
            int colliderCnt = 0;
            int restoreBoneCnt = 0;
            int readBoneCnt = 0;
            int writeBoneCnt = 0;
            int windCnt = 0;

            if (EditorApplication.isPlaying && MagicaPhysicsManager.IsInstance())
            {
                var manager = MagicaPhysicsManager.Instance;
                teamCnt = manager.Team.TeamCount;
                normalTeamCnt = manager.Team.NormalUpdateCount;
                physicsTeamCnt = manager.Team.PhysicsUpdateCount;
                activeTeamCnt = manager.Team.ActiveTeamCount;
                teamCalculation = activeTeamCnt - manager.Team.PauseCount;

#if MAGICACLOTH_DEBUG
                sharedVirtualMeshCnt = manager.Mesh.SharedVirtualMeshCount;
                virtualMeshCnt = manager.Mesh.VirtualMeshCount;
                sharedChildMeshCnt = manager.Mesh.SharedChildMeshCount;
                sharedRenderMeshCnt = manager.Mesh.SharedRenderMeshCount;
                renderMeshCnt = manager.Mesh.RenderMeshCount;

                virtualMeshVertexCnt = manager.Mesh.VirtualMeshVertexCount;
                virtualMeshTriangleCnt = manager.Mesh.VirtualMeshTriangleCount;
                renderMeshVertexCnt = manager.Mesh.RenderMeshVertexCount;
#endif

                virtualMeshUseCnt = manager.Mesh.VirtualMeshUseCount;
                virtualMeshUseCalculation = virtualMeshUseCnt - manager.Mesh.VirtualMeshPauseCount;
                virtualMeshVertexUseCnt = manager.Mesh.VirtualMeshVertexUseCount;
                renderMeshUseCnt = manager.Mesh.RenderMeshUseCount;
                renderMeshUseCalculation = renderMeshUseCnt - manager.Mesh.RenderMeshPauseCount;
                renderMeshVertexUseCnt = manager.Mesh.RenderMeshVertexUseCount;

                particleCnt = manager.Particle.Count;
                colliderCnt = manager.Particle.ColliderCount;
                restoreBoneCnt = manager.Bone.RestoreBoneCount;
                readBoneCnt = manager.Bone.ReadBoneCount;
                writeBoneCnt = manager.Bone.WriteBoneCount;
                windCnt = manager.Wind.Count;
            }

            //StaticStringBuilder.AppendLine("Cloth Team: ", teamCnt, "  (Normal: ", normalTeamCnt, " ,UnityPhysics: ", physicsTeamCnt, ")");
            StaticStringBuilder.AppendLine("Cloth Team: ", teamCnt);
            StaticStringBuilder.AppendLine("Active: ", activeTeamCnt);
            StaticStringBuilder.AppendLine("> Normal Update: ", normalTeamCnt);
            StaticStringBuilder.AppendLine("> Physics Update: ", physicsTeamCnt);
            StaticStringBuilder.AppendLine("Calculation: ", teamCalculation);
            StaticStringBuilder.AppendLine();

#if MAGICACLOTH_DEBUG
            StaticStringBuilder.AppendLine("Shared Virtual Mesh: ", sharedVirtualMeshCnt);
            StaticStringBuilder.AppendLine("Virtual Mesh: ", virtualMeshCnt);
            StaticStringBuilder.AppendLine("Shared Child Mesh: ", sharedChildMeshCnt);
            StaticStringBuilder.AppendLine("Shared Render Mesh: ", sharedRenderMeshCnt);
            StaticStringBuilder.AppendLine("Render Mesh: ", renderMeshCnt);
            StaticStringBuilder.AppendLine();

            StaticStringBuilder.AppendLine("Virtual Mesh Vertex: ", virtualMeshVertexCnt);
            StaticStringBuilder.AppendLine("Virtual Mesh Triangle: ", virtualMeshTriangleCnt);
            StaticStringBuilder.AppendLine("Render Mesh Vertex: ", renderMeshVertexCnt);
            StaticStringBuilder.AppendLine();
#endif

            StaticStringBuilder.AppendLine("Virtual Mesh Used: ", virtualMeshUseCnt);
            StaticStringBuilder.AppendLine("Virtual Mesh Calculation: ", virtualMeshUseCalculation);
            StaticStringBuilder.AppendLine("Virtual Mesh Vertex Used: ", virtualMeshVertexUseCnt);
            StaticStringBuilder.AppendLine();
            StaticStringBuilder.AppendLine("Render Mesh Used: ", renderMeshUseCnt);
            StaticStringBuilder.AppendLine("Render Mesh Calculation: ", renderMeshUseCalculation);
            StaticStringBuilder.AppendLine("Render Mesh Vertex Used: ", renderMeshVertexUseCnt);
            StaticStringBuilder.AppendLine();

            StaticStringBuilder.AppendLine("Particle: ", particleCnt);
            StaticStringBuilder.AppendLine("Collider: ", colliderCnt);
            StaticStringBuilder.AppendLine("Restore Transform: ", restoreBoneCnt);
            StaticStringBuilder.AppendLine("Read Transform: ", readBoneCnt);
            StaticStringBuilder.AppendLine("Write Transform: ", writeBoneCnt);
            StaticStringBuilder.Append("Wind: ", windCnt);

            EditorGUILayout.HelpBox(StaticStringBuilder.ToString(), MessageType.Info);
        }

        void DebugOption()
        {
            EditorGUI.BeginChangeCheck();


            EditorInspectorUtility.Foldout("Cloth Team Gizmos", "Cloth Team Gizmos",
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!drawCloth);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        alwaysClothShow = EditorGUILayout.Toggle("Always Show", alwaysClothShow);
                        drawClothVertex = EditorGUILayout.Toggle("Particle Position", drawClothVertex);
                        drawClothRadius = EditorGUILayout.Toggle("Particle Radius", drawClothRadius);
                        drawClothDepth = EditorGUILayout.Toggle("Particle Depth", drawClothDepth);
                        drawClothAxis = EditorGUILayout.Toggle("Particle Axis", drawClothAxis);
                        drawClothCollider = EditorGUILayout.Toggle("Collider", drawClothCollider);
                        drawClothBase = EditorGUILayout.Toggle("Base Pose", drawClothBase);
                        //drawClothSkinningBones = EditorGUILayout.Toggle("Skinning Bone", drawClothSkinningBones);
                        drawClothStructDistanceLine = EditorGUILayout.Toggle("Struct Distance Line", drawClothStructDistanceLine);
                        drawClothBendDistanceLine = EditorGUILayout.Toggle("Bend Distance Line", drawClothBendDistanceLine);
                        drawClothNearDistanceLine = EditorGUILayout.Toggle("Near Distance Line", drawClothNearDistanceLine);
                        drawClothRotationLine = EditorGUILayout.Toggle("Rotation Line", drawClothRotationLine);
                        drawClothTriangleBend = EditorGUILayout.Toggle("Triangle Bend", drawClothTriangleBend);
                        drawClothPenetration = EditorGUILayout.Toggle("Penetration", drawClothPenetration);
#if MAGICACLOTH_DEBUG
                        drawClothVertexNumber = EditorGUILayout.Toggle("[D] Particle Number", drawClothVertexNumber);
                        drawClothVertexIndex = EditorGUILayout.Toggle("[D] Particle Index", drawClothVertexIndex);
                        drawClothFriction = EditorGUILayout.Toggle("[D] Particle Friction", drawClothFriction);
                        drawClothStaticFriction = EditorGUILayout.Toggle("[D] Particle Static Friction", drawClothStaticFriction);
                        drawClothCollisionNormal = EditorGUILayout.Toggle("[D] Particle Col Normal", drawClothCollisionNormal);
                        drawClothVelocity = EditorGUILayout.Toggle("[D] Particle Velocity", drawClothVelocity);
                        drawClothVelocityVector = EditorGUILayout.Toggle("[D] Particle Velocity V", drawClothVelocityVector);
                        drawClothDepthNumber = EditorGUILayout.Toggle("[D] Particle Depth", drawClothDepthNumber);
                        drawPenetrationOrigin = EditorGUILayout.Toggle("[D] Penetration Origin", drawPenetrationOrigin);
#endif
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    drawCloth = sw;
                },
                drawCloth
                );

            EditorInspectorUtility.Foldout("Deformer Gizmos", "Deformer Gizmos",
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!drawDeformer);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        alwaysDeformerShow = EditorGUILayout.Toggle("Always Show", alwaysDeformerShow);
                        drawDeformerVertexPosition = EditorGUILayout.Toggle("Vertex Position", drawDeformerVertexPosition);
                        drawDeformerVertexAxis = EditorGUILayout.Toggle("Vertex Axis", drawDeformerVertexAxis);
                        drawDeformerLine = EditorGUILayout.Toggle("Line", drawDeformerLine);
                        drawDeformerTriangle = EditorGUILayout.Toggle("Triangle", drawDeformerTriangle);
#if MAGICACLOTH_DEBUG
                        drawDeformerVertexNumber = EditorGUILayout.Toggle("[D] Vertex Number", drawDeformerVertexNumber);
                        debugDrawDeformerVertexNumber = EditorGUILayout.IntField("[D] Vertex Number", debugDrawDeformerVertexNumber);
                        drawDeformerTriangleNormal = EditorGUILayout.Toggle("[D] Triangle Normal", drawDeformerTriangleNormal);
                        drawDeformerTriangleNumber = EditorGUILayout.Toggle("[D] Triangle Number", drawDeformerTriangleNumber);
                        debugDrawDeformerTriangleNumber = EditorGUILayout.IntField("[D] Triangle Number", debugDrawDeformerTriangleNumber);
#endif
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    drawDeformer = sw;
                },
                drawDeformer
                );

            EditorInspectorUtility.Foldout("Wind Gizmos", "Wind Gizmos",
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!drawWind);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        alwaysWindShow = EditorGUILayout.Toggle("Always Show", alwaysWindShow);
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    drawWind = sw;
                },
                drawWind
                );

            if (EditorGUI.EndChangeCheck())
            {
                // Sceneビュー更新
                SceneView.RepaintAll();
            }
        }

    }
}
