using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerUserInputData
    {
        public static readonly PlayerUserInputData Current = new();
        // User input
        public Vector2 HorInputNormalized;
        public bool    JumpFlag;
        public bool    Ascend;
        public bool    Descend;
        public bool    Attack;
        public bool    UseItem;
        public bool    Sprint;
        // UI state
        public bool    MouseScrollAbsorbed;
        public bool    Paused;

        public override string ToString() => $"{HorInputNormalized}\nAsc: {Ascend}\tDesc: {Descend}\tAtk: {Attack}\nMouseSrcl: {MouseScrollAbsorbed}";

    }
}