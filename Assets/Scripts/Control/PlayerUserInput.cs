using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerUserInput : MonoBehaviour
    {
        public const float LONG_ATTACK_THRESHOLD = 0.25F;

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

                inputData.AttackPressed = Input.GetButtonDown("Attack");
                // To prevent false positive from Pause Screen click,
                // We add a 'inputData.AttackPressTime > 0F' check here
                // to ensure attack button was actually being pressed
                inputData.AttackReleased = inputData.AttackPressTime > 0F && Input.GetButtonUp("Attack");

                if (!inputData.AttackReleased)
                {
                    if (Input.GetButton("Attack"))
                    {
                        inputData.AttackPressTime += Time.unscaledDeltaTime;
                    }
                    else
                    {
                        inputData.AttackPressTime = 0F;
                    }
                }

                inputData.UseItem       = Input.GetButtonDown("UseItem");
                inputData.Sprint        = Input.GetButtonDown("Sprint");

                inputData.Ascend  = Input.GetButton("GoUp");
                inputData.Descend = Input.GetButton("GoDown");

                inputData.JumpFlag = Input.GetButtonDown("GoUp");
            }
            else
            {
                inputData.HorInputNormalized = Vector2.zero;

                inputData.AttackPressed = false;
                inputData.Ascend        = false;
                inputData.Descend       = false;
            }
        }

        private static float GetNormalized(bool pos, bool neg) => (pos ? 1F : 0F) + (neg ? -1F : 0F);
    }
}