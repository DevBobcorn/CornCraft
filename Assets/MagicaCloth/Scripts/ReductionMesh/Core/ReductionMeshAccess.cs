// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaReductionMesh
{
    public abstract class ReductionMeshAccess
    {
        protected ReductionMesh parent;

        protected MeshData MeshData
        {
            get
            {
                return parent.MeshData;
            }
        }

        protected ReductionData ReductionData
        {
            get
            {
                return parent.ReductionData;
            }
        }

        protected DebugData DebugData
        {
            get
            {
                return parent.DebugData;
            }
        }

        //=========================================================================================
        public virtual void SetParent(ReductionMesh parent)
        {
            this.parent = parent;
        }
    }
}
