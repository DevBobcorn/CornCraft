// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
namespace MagicaCloth
{
    public abstract class ClothMonitorAccess
    {
        protected ClothMonitorMenu menu;

        protected ClothMonitorUI UI
        {
            get
            {
                return menu.UI;
            }
        }

        //=========================================================================================
        public virtual void Init(ClothMonitorMenu menu)
        {
            this.menu = menu;
            Create();
        }

        protected abstract void Create();

        public abstract void Enable();

        public abstract void Disable();

        public abstract void Destroy();
    }
}
