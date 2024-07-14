namespace CraftSharp
{
    public class MCSettings
    {
        public bool Enabled = true;
        public string Locale = "en_US";
        public byte Difficulty = 0;
        public byte RenderDistance = 8;
        public byte ChatMode = 0;
        public bool ChatColors = true;
        public byte MainHand = 0;
        public bool Skin_Hat = true;
        public bool Skin_Cape = true;
        public bool Skin_Jacket = false;
        public bool Skin_Sleeve_Left = false;
        public bool Skin_Sleeve_Right = false;
        public bool Skin_Pants_Left = false;
        public bool Skin_Pants_Right = false;
        public byte Skin_All
        {
            get
            {
                return (byte)(
                      ((Skin_Cape ? 1 : 0) << 0)
                    | ((Skin_Jacket ? 1 : 0) << 1)
                    | ((Skin_Sleeve_Left ? 1 : 0) << 2)
                    | ((Skin_Sleeve_Right ? 1 : 0) << 3)
                    | ((Skin_Pants_Left ? 1 : 0) << 4)
                    | ((Skin_Pants_Right ? 1 : 0) << 5)
                    | ((Skin_Hat ? 1 : 0) << 6)
                );
            }
        }
    }
}