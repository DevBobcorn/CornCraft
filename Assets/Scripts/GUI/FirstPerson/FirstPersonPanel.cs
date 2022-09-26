#nullable enable
using UnityEngine;
using UnityEngine.UI;
using Coffee.UISoftMask;
using MinecraftClient.Control;

namespace MinecraftClient.UI
{
    public class FirstPersonPanel : MonoBehaviour
    {
        public float followSpeed = 75F;
        public float maxDeltaAngle = 25F;

        private PlayerController? player;
        private CornClient? game;
        private Animator? panel;

        private SoftMask?   buttonListMask;
        private Image? buttonListMaskImage;
        private const int BUTTON_COUNT = 5;
        private GameObject[] buttonObjs = new GameObject[BUTTON_COUNT];
        private GameObject? newButtonObj;
        private int selectedIndex = 0, rollingIndex = -1;
        private const float SINGLE_SHOW_TIME = 0.25F, SINGLE_DELTA_TIME = 0.15F;
        private const float TOTAL_SHOW_TIME = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1) + SINGLE_SHOW_TIME;
        private static readonly float[] START_POS = {  800F,  560F,  320F,   80F, -160F };
        private static readonly float[] STOP_POS  = {  180F,   90F,    0F,  -90F, -180F };

        private float GetPosForButton(int index)
        {
            var posIndex = (index + BUTTON_COUNT - selectedIndex) % BUTTON_COUNT;
            var itsStartTime = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1 - posIndex);

            if (showPanelTime <= itsStartTime) // This button's animation hasn't yet started
                return START_POS[posIndex];
            else if (showPanelTime >= itsStartTime + SINGLE_SHOW_TIME) // This button's animation has already ended
                return STOP_POS[posIndex];
            else // Lerp to get its position
                return Mathf.Lerp(START_POS[posIndex], STOP_POS[posIndex], (showPanelTime - itsStartTime) / SINGLE_SHOW_TIME);

        }

        private float showPanelTime = -1F, buttonsOffset = 0F;

        private bool panelShown = false;
        public bool PanelShown
        {
            get {
                return panelShown;
            }
        }

        private void ShowPanel()
        {
            // First calculate and set rotation
            var cameraRot = Camera.main.transform.eulerAngles.y;
            transform.eulerAngles = new(0F, cameraRot, 0F);

            // Select button on top
            buttonObjs[selectedIndex].GetComponent<Button>().Select();

            // Then play fade animation
            panel!.SetBool("Show", true);
            showPanelTime = 0F;
        }

        private void SelectAdjacentButton(bool next)
        {
            if (buttonsOffset != 0F)
                return;

            if (next)
            {
                rollingIndex = selectedIndex; // Current top button
                selectedIndex = (selectedIndex + 1) % BUTTON_COUNT;
            }
            else
            {
                rollingIndex = (selectedIndex + BUTTON_COUNT - 1) % BUTTON_COUNT; // Current bottom button
                selectedIndex = (selectedIndex + BUTTON_COUNT - 1) % BUTTON_COUNT;
            }

            // Make a copy of newly selected button
            newButtonObj = GameObject.Instantiate(buttonObjs[rollingIndex]);
            newButtonObj.name = buttonObjs[rollingIndex].name;

            if (next)
                buttonObjs[selectedIndex].GetComponent<Button>().Select();
            else
                newButtonObj.GetComponent<Button>().Select();
            
            var newButtonTransform = newButtonObj.GetComponent<RectTransform>();
            var oldButtonTransform = buttonObjs[rollingIndex].GetComponent<RectTransform>();

            newButtonTransform.SetParent(oldButtonTransform.parent);

            newButtonTransform.position = oldButtonTransform.position;
            newButtonTransform.rotation = oldButtonTransform.rotation;
            newButtonTransform.localScale = oldButtonTransform.localScale;

            newButtonTransform.anchorMin = oldButtonTransform.anchorMin;
            newButtonTransform.anchorMax = oldButtonTransform.anchorMax;
            newButtonTransform.sizeDelta = oldButtonTransform.sizeDelta;

            newButtonTransform.localPosition = new(0F, next ? -270F : 270F);
            
            buttonsOffset = next ? -90F : 90F;

            buttonListMask!.enabled = true;
            buttonListMaskImage!.color = Color.white;

        }

        public void HidePanel()
        {
            // Play hide animation
            panel!.SetBool("Show", false);
            panelShown = false;
            showPanelTime = -1F;
        }

        void Start()
        {
            // Find game instance
            game = CornClient.Instance;

            // Initialize panel animator
            panel = GetComponent<Animator>();
            panel.SetBool("Show", false);
            panelShown = false;

            var canvas = panel.GetComponentInChildren<Canvas>();

            // Get button list and buttons
            buttonListMask = FindHelper.FindChildRecursively(transform, "Button List").GetComponent<SoftMask>();
            buttonListMask.enabled = false;
            buttonListMaskImage = buttonListMask.GetComponent<Image>();
            buttonListMaskImage.color = new(0F, 0F, 0F, 0F);

            buttonObjs[0] = buttonListMask.transform.Find("Avatar Button").gameObject;
            buttonObjs[1] = buttonListMask.transform.Find("Social Button").gameObject;
            buttonObjs[2] = buttonListMask.transform.Find("Chat Button").gameObject;
            buttonObjs[3] = buttonListMask.transform.Find("Map Button").gameObject;
            buttonObjs[4] = buttonListMask.transform.Find("Settings Button").gameObject;

            selectedIndex = 0;

        }

        void Update()
        {
            if (showPanelTime >= 0F) // Panel should be shown
            {
                if (showPanelTime < TOTAL_SHOW_TIME) // Play show animation
                {
                    showPanelTime = Mathf.Min(showPanelTime + Time.deltaTime, TOTAL_SHOW_TIME);

                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, GetPosForButton(i));
                    
                    return;
                }
                else if (!panelShown) // Complete show animation
                {
                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, STOP_POS[(i + BUTTON_COUNT - selectedIndex) % BUTTON_COUNT]);
                    
                    panelShown = true;

                    Debug.Log("Panel show complete");
                }

            }

            if (buttonsOffset != 0F)
            {
                float newOffset;

                if (buttonsOffset > 0F)
                    newOffset = Mathf.Max(0F, buttonsOffset - 240F * Time.deltaTime);
                else
                    newOffset = Mathf.Min(0F, buttonsOffset + 240F * Time.deltaTime);

                for (int i = 0;i < BUTTON_COUNT;i++)
                {
                    RectTransform buttonRect = buttonObjs[i].GetComponent<RectTransform>();
                    buttonRect.anchoredPosition = new(0F, buttonRect.anchoredPosition.y + (newOffset - buttonsOffset));
                }

                var newButtonRect = newButtonObj!.GetComponent<RectTransform>();
                newButtonRect.anchoredPosition = new(0F, newButtonRect.anchoredPosition.y + (newOffset - buttonsOffset));

                buttonsOffset = newOffset;

                if (buttonsOffset == 0F) // Complete button selection
                {
                    // Destroy old button
                    Destroy(buttonObjs[rollingIndex]);
                    buttonObjs[rollingIndex] = newButtonObj;

                    buttonListMask!.enabled = false;
                    buttonListMaskImage!.color = new(0F, 0F, 0F, 0F);
                }

            }

            if (game!.IsPaused())
                return;

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (panelShown)
                    HidePanel();
                else
                    ShowPanel();
            }

            if (panelShown)
            {
                if (Input.GetKeyDown(KeyCode.U)) // Previous button
                {
                    SelectAdjacentButton(false);
                }
                else if (Input.GetKeyDown(KeyCode.O)) // Next button
                {
                    SelectAdjacentButton(true);
                }
            }

            if (Camera.main is not null)
            {
                var cameraRot = Camera.main.transform.eulerAngles.y;
                var ownRot = transform.eulerAngles.y;

                var deltaRot = Mathf.DeltaAngle(ownRot, cameraRot);

                if (Mathf.Abs(deltaRot) > maxDeltaAngle)
                {
                    if (deltaRot > 0F)
                        transform.eulerAngles = new(0F, ownRot + followSpeed * Time.deltaTime, 0F);
                    else
                        transform.eulerAngles = new(0F, ownRot - followSpeed * Time.deltaTime, 0F);

                }
                
            }

        }

    }
}
