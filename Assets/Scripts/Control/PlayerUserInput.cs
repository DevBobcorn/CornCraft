using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (PlayerController))]
    public class PlayerUserInput : MonoBehaviour
    {
        public void UpdateInputs(PlayerUserInputData inputData, Perspective perspective)
        {
            if (!CornApp.CurrentClient.IsPaused())
            {
                float h = GetNormalized(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A));
                float v = GetNormalized(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));

                if (h == 0 && v == 0)
                    inputData.horInputNormalized = Vector2.zero;
                else
                    inputData.horInputNormalized = new Vector2(h, v).normalized;

                inputData.attack  = Input.GetButtonDown("Attack");

                if (perspective == Perspective.ThirdPerson)
                {
                    // Sprinting with use item button is also possible in third person mode
                    inputData.sprint = Input.GetButtonDown("UseItem") || Input.GetButtonDown("Sprint");
                }
                else
                {
                    inputData.useItem = Input.GetButtonDown("UseItem");
                    inputData.sprint  = Input.GetButtonDown("Sprint");
                }

                inputData.ascend  = Input.GetButton("GoUp");
                inputData.descend = Input.GetButton("GoDown");

                if (Input.GetKeyDown(KeyCode.LeftControl))
                    GetComponent<PlayerController>().ToggleWalkMode();
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