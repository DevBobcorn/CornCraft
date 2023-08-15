namespace CraftSharp.Resource
{
    public enum DisplayPosition
    {
        ThirdPersonRightHand,
        ThirdPersonLeftHand,
        FirstPersonRightHand,
        FirstPersonLeftHand,
        GUI,
        Head,
        Fixed,
        Ground,
        
        Unknown
    }

    public static class DisplayPositionHelper
    {
        public static DisplayPosition FromString(string displayPos)
        {
            return displayPos.ToLower() switch
            {
                "thirdperson_righthand"   => DisplayPosition.ThirdPersonRightHand,
                "thirdperson_lefthand"    => DisplayPosition.ThirdPersonLeftHand,
                "firstperson_righthand"   => DisplayPosition.FirstPersonRightHand,
                "firstperson_lefthand"    => DisplayPosition.FirstPersonLeftHand,
                "gui"                     => DisplayPosition.GUI,
                "head"                    => DisplayPosition.Head,
                "fixed"                   => DisplayPosition.Fixed,
                "ground"                  => DisplayPosition.Ground,

                _                         => DisplayPosition.Unknown
            };
        }
    }
}