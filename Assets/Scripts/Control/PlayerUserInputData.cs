using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerUserInputData
    {
        public Vector2 horInputNormalized;
        public bool   ascend;
        public bool   descend;
        public bool   attack;
        public bool   useItem;
        public bool   sprint;

        public override string ToString() => $"{horInputNormalized}\nAsc: {ascend}\tDesc: {descend}\tAtk: {attack}";

    }
}