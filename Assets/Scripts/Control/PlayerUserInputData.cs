using UnityEngine;

namespace MinecraftClient.Control
{
    public class PlayerUserInputData
    {
        public Vector2 horInputNormalized;
        public bool   ascend;
        public bool   descend;
        public bool   attack;

        public override string ToString() => $"{horInputNormalized}\nAsc: {ascend}\tDesc: {descend}\tAtk: {attack}";

    }
}