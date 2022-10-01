#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UISoftMask;
using MinecraftClient.Control;

namespace MinecraftClient.UI
{
    public class FirstPersonGUI : MonoBehaviour
    {
        public float followSpeed = 1F;
        public float maxDeltaAngle = 30F;
        private CornClient? game;

        private CameraController? cameraCon;

        private Canvas? firstPersonCanvas;
        private Animator? firstPersonPanel;

        private SoftMask?   buttonListMask;
        private Image? buttonListMaskImage;
        private const int BUTTON_COUNT = 5;
        
        private const float SINGLE_SHOW_TIME = 0.25F, SINGLE_DELTA_TIME = 0.15F;
        private const float TOTAL_SHOW_TIME = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1) + SINGLE_SHOW_TIME;
        private static readonly float[] START_POS = {  800F,  560F,  320F,   80F, -160F };
        private static readonly float[] STOP_POS  = {  180F,   90F,    0F,  -90F, -180F };

        private const float ROLL_BUTTON_SPEED = 300F;

        private const float ROOT_MENU_OFFSET = 190F;
        private const float SUB_MENU_OFFSET  = 200F;
        
        private GameObject[] rootMenuPrefabs = new GameObject[BUTTON_COUNT];
        private GameObject[] buttonObjs = new GameObject[BUTTON_COUNT];
        private GameObject? newButtonObj;
        private int selectedIndex = 0, rollingIndex = -1;

        private Stack<FirstPersonMenu> openedMenus = new();

        private float GetPosForButton(int index)
        {
            var posIndex = (index + BUTTON_COUNT - selectedIndex) % BUTTON_COUNT;
            var itsStartTime = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1 - posIndex);

            if (buttonListShowTime <= itsStartTime) // This button's animation hasn't yet started
                return START_POS[posIndex];
            else if (buttonListShowTime >= itsStartTime + SINGLE_SHOW_TIME) // This button's animation has already ended
                return STOP_POS[posIndex];
            else // Lerp to get its position
                return Mathf.Lerp(START_POS[posIndex], STOP_POS[posIndex], (buttonListShowTime - itsStartTime) / SINGLE_SHOW_TIME);

        }

        private float buttonListShowTime = -1F, rollOffset = 0F;
        private int rollCount = 0;

        private bool initialzed = false, buttonListActive = false;

        private void Initialize()
        {
            // Find game instance
            game = CornClient.Instance;

            firstPersonCanvas = GetComponentInChildren<Canvas>();

            // Initialize panel animator
            firstPersonPanel = GetComponent<Animator>();
            firstPersonPanel.SetBool("Show", false);
            buttonListActive = false;

            var canvas = firstPersonPanel.GetComponentInChildren<Canvas>();

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

            for (int i = 0;i < BUTTON_COUNT;i++)
            {
                var button = buttonObjs[i].GetComponent<Button>();
                int clickedIndex = i;

                button.onClick.AddListener(() => {
                    if (rollOffset == 0F && rollCount == 0)
                    {
                        rollCount = clickedIndex - selectedIndex;
                        RollButtons();
                    }
                });

            }

            selectedIndex = 0;

            rootMenuPrefabs[0] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Avatar");
            rootMenuPrefabs[1] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Avatar");
            rootMenuPrefabs[2] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Avatar");
            rootMenuPrefabs[3] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Avatar");
            rootMenuPrefabs[4] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Settings");

            initialzed = true;
        }

        public void EnsureInitialized()
        {
            if (!initialzed)
                Initialize();
        }

        private void RollButtons()
        {
            var next = (rollCount > 0);

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

            // Make a visual copy of newly selected button
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
            
            rollOffset = next ? -90F : 90F;

            buttonListMask!.enabled = true;
            buttonListMaskImage!.color = Color.white;

        }

        public void ShowPanel()
        {
            EnsureInitialized();

            // First calculate and set rotation
            var cameraRot = Camera.main.transform.eulerAngles.y;
            transform.eulerAngles = new(0F, cameraRot, 0F);

            // Select button on top
            buttonObjs[selectedIndex].GetComponent<Button>().Select();

            // Then play fade animation
            firstPersonPanel!.SetBool("Show", true);
            buttonListShowTime = 0F;
        }

        public void HidePanel()
        {
            EnsureInitialized();
            
            // Play hide animation
            firstPersonPanel!.SetBool("Show", false);
            buttonListActive = false;
            buttonListShowTime = -1F;
        }

        public void SetCameraCon(CameraController cameraCon)
        {
            this.cameraCon = cameraCon;
            firstPersonCanvas!.worldCamera = cameraCon.ActiveCamera;
        }

        void Start()
        {
            if (!initialzed)
                Initialize();
        }

        void Update()
        {
            if (buttonListShowTime >= 0F) // Panel should be shown
            {
                if (buttonListShowTime < TOTAL_SHOW_TIME) // Play show animation
                {
                    buttonListShowTime = Mathf.Min(buttonListShowTime + Time.deltaTime, TOTAL_SHOW_TIME);

                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, GetPosForButton(i));
                    
                    return;
                }
                else if (!buttonListActive) // Complete show animation
                {
                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, STOP_POS[(i + BUTTON_COUNT - selectedIndex) % BUTTON_COUNT]);
                    
                    buttonListActive = true;

                    Debug.Log("Panel show complete");
                }

            }

            var canvasTransform = firstPersonCanvas!.transform;

            float targetHor = -Mathf.Max(0, openedMenus.Count - 1) * SUB_MENU_OFFSET * 0.0005F;
            float horOffset = targetHor - canvasTransform.localPosition.x;

            if (horOffset != 0F)
            {
                float mov;

                if (horOffset > 0)
                    mov = Mathf.Min(horOffset,  Time.deltaTime * 0.5F);
                else
                    mov = Mathf.Max(horOffset, -Time.deltaTime * 0.5F);

                canvasTransform.Translate(mov, 0F, 0F, Space.Self);
            }

            if (rollOffset != 0F)
            {
                if (openedMenus.Count == 0) // Roll button
                {
                    float newOffset;

                    if (rollOffset > 0F)
                        newOffset = Mathf.Max(0F, rollOffset - ROLL_BUTTON_SPEED * Time.deltaTime);
                    else
                        newOffset = Mathf.Min(0F, rollOffset + ROLL_BUTTON_SPEED * Time.deltaTime);

                    for (int i = 0;i < BUTTON_COUNT;i++)
                    {
                        RectTransform buttonRect = buttonObjs[i].GetComponent<RectTransform>();
                        buttonRect.anchoredPosition = new(0F, buttonRect.anchoredPosition.y + (newOffset - rollOffset));
                    }

                    var newButtonRect = newButtonObj!.GetComponent<RectTransform>();
                    newButtonRect.anchoredPosition = new(0F, newButtonRect.anchoredPosition.y + (newOffset - rollOffset));

                    rollOffset = newOffset;

                    if (rollOffset == 0F) // Complete button roll
                    {
                        // Teleport old button to new button and destroy new button
                        buttonObjs[rollingIndex].transform.position = newButtonObj.transform.position;
                        Destroy(newButtonObj);

                        // And select the selected button
                        buttonObjs[selectedIndex].GetComponent<Button>().Select();

                        buttonListMask!.enabled = false;
                        buttonListMaskImage!.color = new(0F, 0F, 0F, 0F);

                        // Update offset count
                        if (rollCount > 0)
                            rollCount--;
                        else if (rollCount < 0)
                            rollCount++;
                        
                        // Continue to roll buttons if necessary
                        if (rollCount != 0)
                            RollButtons();
                        else // Rolled to target button, unfolder its root menu
                        {
                            if (openedMenus.Count == 0)
                            {
                                var rootMenuObj = GameObject.Instantiate(rootMenuPrefabs[selectedIndex], Vector3.zero, Quaternion.identity);
                                rootMenuObj!.transform.SetParent(firstPersonCanvas!.transform, false);
                                rootMenuObj.transform.localPosition = new(ROOT_MENU_OFFSET + openedMenus.Count * SUB_MENU_OFFSET, 180F, 0F);
                                rootMenuObj.SetActive(true);

                                var rootMenu = rootMenuObj!.GetComponent<FirstPersonMenu>();

                                openedMenus.Push(rootMenu);
                            }
                            else // Previously opened menus not closed properly
                            {
                                // TODO Handle properly
                                Debug.LogWarning("Menus not properly closed.");
                            }
                        }
                    }
                }
                else // Pop all opened menus first
                {
                    var topMenu = openedMenus.Peek();

                    switch (topMenu.state)
                    {
                        case FirstPersonMenu.State.Hidden:
                        case FirstPersonMenu.State.Error:
                            openedMenus.Pop();
                            Destroy(topMenu);
                            break;
                        case FirstPersonMenu.State.Shown:
                            topMenu.Hide();
                            break;
                    }

                }
            }

            if (buttonListActive)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) // Previous button
                {
                    if (rollOffset == 0F && rollCount == 0)
                    {
                        rollCount = -1;
                        RollButtons();
                    }
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow)) // Next button
                {
                    if (rollOffset == 0F && rollCount == 0)
                    {
                        rollCount =  1;
                        RollButtons();
                    }
                }
                else if (Input.GetMouseButtonDown(0)) // Detect click on a certain button
                {
                    var rayCaster = firstPersonCanvas!.GetComponent<GraphicRaycaster>();
                    //Debug.Log($"LMB down at {Input.mousePosition}");
                    // Simulate a mouse click, we need to do this because click events won't be triggered when mouse cursor is locked
                    var result = new List<RaycastResult>();
                    rayCaster.Raycast(new(EventSystem.current) { position = Input.mousePosition }, result);
                    foreach(var r in result)
                    {
                        var button = r.gameObject.GetComponent<Button>();
                        if (button is not null)
                            button.onClick.Invoke();
                    }
                }
            }

            // Follow player view
            if (game!.CurrentPerspective == CornClient.Perspective.FirstPerson && cameraCon is not null)
            {
                var cameraRot = cameraCon.ActiveCamera.transform.eulerAngles.y;
                var ownRot = transform.eulerAngles.y;

                var deltaRot = Mathf.DeltaAngle(ownRot, cameraRot);

                if (Mathf.Abs(deltaRot) > maxDeltaAngle)
                {
                    if (deltaRot > 0F)
                        transform.eulerAngles = new(0F, Mathf.LerpAngle(ownRot, cameraRot + maxDeltaAngle, followSpeed * Time.deltaTime));
                    else
                        transform.eulerAngles = new(0F, Mathf.LerpAngle(ownRot, cameraRot - maxDeltaAngle, followSpeed * Time.deltaTime));

                }
                
            }

        }

    }
}
