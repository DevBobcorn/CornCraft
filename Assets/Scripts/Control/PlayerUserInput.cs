using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerUserInput : MonoBehaviour
    {
        void Update()
        {
            var inputData = PlayerUserInputData.Current;
            if (!inputData.Paused)
            {
                float h = GetNormalized(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A));
                float v = GetNormalized(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));

                if (h == 0 && v == 0)
                    inputData.HorInputNormalized = Vector2.zero;
                else
                    inputData.HorInputNormalized = new Vector2(h, v).normalized;

                inputData.Attack  = Input.GetButtonDown("Attack");
                inputData.UseItem = Input.GetButtonDown("UseItem");
                inputData.Sprint  = Input.GetButtonDown("Sprint");

                inputData.Ascend  = Input.GetButton("GoUp");
                inputData.Descend = Input.GetButton("GoDown");
            }
            else
            {
                inputData.HorInputNormalized = Vector2.zero;

                inputData.Attack  = false;
                inputData.Ascend  = false;
                inputData.Descend = false;
            }
        }

        private static float GetNormalized(bool pos, bool neg) => (pos ? 1F : 0F) + (neg ? -1F : 0F);
    }
}