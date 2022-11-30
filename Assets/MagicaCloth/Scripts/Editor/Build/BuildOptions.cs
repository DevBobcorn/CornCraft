// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaCloth
{
    public class BuildOptions
    {
        // Components
        public bool buildBoneCloth = true;
        public bool buildBoneSpring = true;
        public bool buildMeshCloth = true;
        public bool buildMeshSpring = true;
        public bool buildRenderDeformer = true;
        public bool buildVirtualDeformer = true;

        // Conditions
        public bool forceBuild = false;
        public bool notCreated = true;
        public bool upgradeFormatAndAlgorithm = true;

        // Options
        public bool includeInactive = true;
        public bool errorStop = false;
        public bool verificationOnly = false;
    }
}
