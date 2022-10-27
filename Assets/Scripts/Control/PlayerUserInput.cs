using UnityEngine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (PlayerController))]
    public class PlayerUserInput : MonoBehaviour
    {
        public void UpdateInputs(PlayerUserInputData inputData)
        {
            if (!CornClient.Instance.IsPaused())
            {
                float h = GetNormalized(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A));
                float v = GetNormalized(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));

                if (h == 0 && v == 0)
                    inputData.horInputNormalized = Vector2.zero;
                else
                    inputData.horInputNormalized = new Vector2(h, v).normalized;

                inputData.attack  = Input.GetButton("Attack");
                inputData.ascend  = Input.GetButton("GoUp");
                inputData.descend = Input.GetButton("GoDown");
            }
            else
            {
                inputData.horInputNormalized = Vector2.zero;

                inputData.attack  = false;
                inputData.ascend  = false;
                inputData.descend = false;
            }
        }

        private static float GetNormalized(bool pos, bool neg) => (pos ? 1F : 0F) + (neg ? -1F : 0F);
    }
}