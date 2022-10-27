using UnityEngine;

namespace MinecraftClient.Control
{
    public class PlayerUserInputData
    {
        public Vector2 horInputNormalized;
        public bool   ascend;
        public bool   descend;
        public bool   attack;

        public string GetDescription() => $"{horInputNormalized}\nAsc: {ascend}\tDesc: {descend}\tAtkL {attack}";

    }
}