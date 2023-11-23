// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    public static partial class Define
    {
        public enum Result
        {
            None = 0,

            /// <summary>
            /// It is not an error, but the data is empty.
            /// </summary>
            Empty = 1,

            Success = 2,
            Cancel = 3,
            Process = 4,

            ///////////////////////////////////////////////////////////////////
            // State(1000 - 9999)
            ///////////////////////////////////////////////////////////////////
            //EmptyData = 1000,
            //Init_Complete = 1000,
            //Cloth_Running = 2000,

            ///////////////////////////////////////////////////////////////////
            // Warning(10000 - 19999)
            ///////////////////////////////////////////////////////////////////
            Warning = 10000,

            RenderMesh_UnknownWarning = 10100,
            RenderMesh_VertexWeightIs5BonesOrMore,

            ///////////////////////////////////////////////////////////////////
            // Error(20000 - )
            ///////////////////////////////////////////////////////////////////
            Error = 20000,

            // Validating serialized data
            SerializeData_InvalidData = 20050,
            SerializeData_Over15Renderers,

            // init
            Init_InvalidData = 20100,
            Init_InvalidPaintMap,
            Init_PaintMapNotReadable,

            // RenderSetup
            RenderSetup_Exception = 20200,
            RenderSetup_UnknownError,
            RenderSetup_InvalidSource,
            RenderSetup_NoMeshOnRenderer,
            RenderSetup_InvalidType,
            RenderSetup_Unreadable,
            RenderSetup_Over65535vertices,

            // VirtualMesh
            VirtualMesh_UnknownError = 20300,
            VirtualMesh_InvalidSetup,
            VirtualMesh_InvalidRenderData,
            VirtualMesh_ImportError,
            VirtualMesh_SelectionException,
            VirtualMesh_SelectionUnknownError,
            VirtualMesh_InvalidSelection,

            // Create Cloth
            CreateCloth_Exception = 20400,
            CreateCloth_UnknownError,
            CreateCloth_InvalidCloth,
            CreateCloth_InvalidSerializeData,
            CreateCloth_InvalidSetupList,
            CreateCloth_NoRenderer,
            CreateCloth_InvalidPaintMap,
            CreateCloth_PaintMapNotReadable,
            CreateCloth_PaintMapCountMismatch,

            // Reduction
            Reduction_Exception = 20500,
            Reduction_UnknownError,
            Reduction_InitError,
            Reduction_SameDistanceException,
            Reduction_SimpleDistanceException,
            Reduction_ShapeDistanceException,
            Reduction_MaxSideLengthZero,
            Reduction_OrganizationError,
            Reduction_StoreVirtualMeshError,
            Reduction_CalcAverageException,

            // Optimize
            Optimize_Exception = 20600,

            // ProxyMesh
            ProxyMesh_Exception = 20700,
            ProxyMesh_UnknownError,
            ProxyMesh_ApplySelectionError,
            ProxyMesh_ConvertError,
            ProxyMesh_Over32767Vertices,
            ProxyMesh_Over32767Edges,
            ProxyMesh_Over32767Triangles,

            // MappingMesh
            MappingMesh_Exception = 20800,
            MappingMesh_UnknownError,
            MappingMesh_ProxyError,

            // ClothInit
            ClothInit_Exception = 22000,
            ClothInit_FailedAddRenderer,

            // ClothProcess
            ClothProcess_Exception = 22100,
            ClothProcess_UnknownError,
            ClothProcess_Invalid,
            ClothProcess_InvalidRenderHandleList,
            ClothProcess_GenerateSelectionError,
            ClothProcess_OverflowTeamCount4096,

            // Constraint
            Constraint_Exception = 22200,
            Constraint_UnknownError,
            Constraint_CreateDistanceException,
            Constraint_CreateTriangleBendingException,
            Constraint_CreateInertiaException,
            Constraint_CreateSelfCollisionException,

            // MagicaMesh
            MagicaMesh_UnknownError = 22500,
            MagicaMesh_Invalid,
            MagicaMesh_InvalidRenderer,
            MagicaMesh_InvalidMeshFilter,
        }
    }
}
