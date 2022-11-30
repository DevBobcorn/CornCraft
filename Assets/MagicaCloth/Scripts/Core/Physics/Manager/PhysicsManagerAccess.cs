// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;

namespace MagicaCloth
{
    /// <summary>
    /// 物理マネージャの内容物参照
    /// </summary>
    public abstract class PhysicsManagerAccess : IDisposable
    {
        protected MagicaPhysicsManager manager;

        public UpdateTimeManager UpdateTime
        {
            get
            {
                return manager.UpdateTime;
            }
        }

        protected PhysicsManagerParticleData Particle
        {
            get
            {
                return manager.Particle;
            }
        }

        protected PhysicsManagerBoneData Bone
        {
            get
            {
                return manager.Bone;
            }
        }

        protected PhysicsManagerMeshData Mesh
        {
            get
            {
                return manager.Mesh;
            }
        }

        protected PhysicsManagerTeamData Team
        {
            get
            {
                return manager.Team;
            }
        }

        protected PhysicsManagerWindData Wind
        {
            get
            {
                return manager.Wind;
            }
        }

        protected PhysicsManagerComponent Component
        {
            get
            {
                return manager.Component;
            }
        }

        protected PhysicsManagerCompute Compute
        {
            get
            {
                return manager.Compute;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 親参照設定
        /// </summary>
        /// <param name="manager"></param>
        public void SetParent(MagicaPhysicsManager manager)
        {
            this.manager = manager;
        }

        /// <summary>
        /// 初期設定
        /// </summary>
        public abstract void Create();

        /// <summary>
        /// 破棄
        /// </summary>
        public abstract void Dispose();
    }
}
