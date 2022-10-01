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

        private const float CANVAS_SCALE = 0.0006F;
        
        private const float SINGLE_SHOW_TIME = 0.25F, SINGLE_DELTA_TIME = 0.15F;
        private const float TOTAL_SHOW_TIME = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1) + SINGLE_SHOW_TIME;
        private const float TOTAL_HIDE_TIME = 0.5F;
        private static readonly float[] START_POS = {  800F,  560F,  320F,   80F, -160F };
        private static readonly float[] STOP_POS  = {  180F,   90F,    0F,  -90F, -180F };

        private const float ROLL_BUTTON_SPEED = 700F;

        private const float ROOT_MENU_OFFSET = 70F;
        private const float SUB_MENU_OFFSET  = 200F;
        
        private GameObject[] rootMenuPrefabs = new GameObject[BUTTON_COUNT];
        private GameObject[] buttonObjs = new GameObject[BUTTON_COUNT];
        private GameObject? newButtonObj;

        public WidgetState State { get; set; } = WidgetState.Hidden;

        private int selectedIndex = 0, rollingIndex = -1;

        private Stack<FirstPersonMenu> openedMenus = new();

        private float GetPosForButton(int index)
        {
            var posIndex = (index + BUTTON_COUNT - selectedIndex) % BUTTON_COUNT;
            var itsStartTime = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1 - posIndex);

            if (panelAnimTime <= itsStartTime) // This button's animation hasn't yet started
                return START_POS[posIndex];
            else if (panelAnimTime >= itsStartTime + SINGLE_SHOW_TIME) // This button's animation has already ended
                return STOP_POS[posIndex];
            else // Lerp to get its position
                return Mathf.Lerp(START_POS[posIndex], STOP_POS[posIndex], (panelAnimTime - itsStartTime) / SINGLE_SHOW_TIME);

        }

        private float panelAnimTime = -1F, rollOffset = 0F;
        private int rollCount = 0;

        private bool initialzed = false;

        private void Initialize()
        {
            // Find game instance
            game = CornClient.Instance;

            firstPersonCanvas = GetComponentInChildren<Canvas>();

            // Initialize panel animator
            firstPersonPanel = GetComponent<Animator>();
            firstPersonPanel.SetBool("Show", false);
            State = WidgetState.Hidden;

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
            rootMenuPrefabs[2] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Chat");
            rootMenuPrefabs[3] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Avatar");
            rootMenuPrefabs[4] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Settings");

            initialzed = true;
        }

        private void EnsureInitialized()
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

        private float GetHorizontalPosition()
        {
            float basePos;

            if (openedMenus.Count > 0 && openedMenus.Peek().leftSide)
            {
                basePos = ( Mathf.Max(0, openedMenus.Count - 1) * SUB_MENU_OFFSET) * CANVAS_SCALE;
            }
            else
            {
                basePos = (-Mathf.Max(0, openedMenus.Count - 1) * SUB_MENU_OFFSET + 100F) * CANVAS_SCALE;
            }

            if (State == WidgetState.Hide)
                return basePos - 1F;

            return basePos;
        }

        private void UnfoldRootMenu()
        {
            if (openedMenus.Count == 0)
            {
                var rootMenuObj = GameObject.Instantiate(rootMenuPrefabs[selectedIndex], Vector3.zero, Quaternion.identity);
                rootMenuObj!.transform.SetParent(firstPersonCanvas!.transform, false);

                var rootMenu = rootMenuObj!.GetComponent<FirstPersonMenu>();
                rootMenu.SetParent(this);

                if (rootMenu.leftSide)
                    rootMenuObj.transform.localPosition = new(-ROOT_MENU_OFFSET, 180F, 0F);
                else
                    rootMenuObj.transform.localPosition = new( ROOT_MENU_OFFSET, 180F, 0F);

                rootMenuObj.SetActive(true);

                openedMenus.Push(rootMenu);
            }
            else // Previously opened menus not closed properly
            {
                // TODO Handle properly
                Debug.LogWarning("Menus not properly closed.");
            }
        }

        public void ShowPanel()
        {
            EnsureInitialized();

            if (State != WidgetState.Hidden)
                return;
            
            // First calculate and set rotation
            var cameraRot = Camera.main.transform.eulerAngles.y;
            transform.eulerAngles = new(0F, cameraRot, 0F);

            // Teleport panel to the right position
            var canvasTransform = firstPersonCanvas!.transform;

            canvasTransform.Translate(GetHorizontalPosition() - canvasTransform.localPosition.x, 0F, 0F, Space.Self);

            // Select button on top
            buttonObjs[selectedIndex].GetComponent<Button>().Select();

            // Then play fade animation
            firstPersonPanel!.SetBool("Show", true);
            State = WidgetState.Show;
            panelAnimTime = 0F;
        }

        public void HidePanel()
        {
            EnsureInitialized();

            if (State != WidgetState.Shown)
                return;

            // Play hide animation
            firstPersonPanel!.SetBool("Show", false);
            State = WidgetState.Hide;
            panelAnimTime = 0F;
        }

        public void SetCameraCon(CameraController cameraCon)
        {
            EnsureInitialized();

            this.cameraCon = cameraCon;
            firstPersonCanvas!.worldCamera = cameraCon.ActiveCamera;
        }

        void Start()
        {
            EnsureInitialized();
        }

        void Update()
        {
            if (State == WidgetState.Show) // Panel is showing up
            {
                if (panelAnimTime < TOTAL_SHOW_TIME) // Play show animation
                {
                    panelAnimTime = Mathf.Min(panelAnimTime + Time.deltaTime, TOTAL_SHOW_TIME);

                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, GetPosForButton(i));
                    
                    return;
                }
                else // Complete show animation
                {
                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, STOP_POS[(i + BUTTON_COUNT - selectedIndex) % BUTTON_COUNT]);
                    
                    UnfoldRootMenu();
                    State = WidgetState.Shown;
                }

            }
            else if (State == WidgetState.Hide) // Panel is fading out
            {
                panelAnimTime = Mathf.Min(panelAnimTime + Time.deltaTime, TOTAL_HIDE_TIME);

                if (panelAnimTime >= TOTAL_HIDE_TIME) // Complete fade animation
                {
                    State = WidgetState.Hidden;

                    // Destroy all opened menus
                    foreach (var menu in openedMenus)
                        Destroy(menu.gameObject);
                    
                    openedMenus.Clear();
                }
            }
            else if (State == WidgetState.Shown) // Panel is shown
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
                else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked) // Detect click on a certain button
                {
                    var rayCaster = firstPersonCanvas!.GetComponent<GraphicRaycaster>();
                    //Debug.Log($"LMB down at {Input.mousePosition}");
                    // Simulate a mouse click, we need to do this because click events won't be triggered when mouse cursor is locked
                    var result = new List<RaycastResult>();
                    rayCaster.Raycast(new(EventSystem.current) { position = Input.mousePosition }, result);

                    string res = string.Empty;

                    foreach(var r in result)
                    {
                        var button = r.gameObject.GetComponent<Button>();
                        if (button is not null)
                        {
                            button.onClick.Invoke();
                            break;
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

            var canvasTransform = firstPersonCanvas!.transform;
            var horOffset = GetHorizontalPosition() - canvasTransform.localPosition.x;

            if (horOffset != 0F)
            {
                float mov;

                if (horOffset > 0)
                    mov = Mathf.Min(horOffset,  Time.deltaTime * 0.3F);
                else
                    mov = Mathf.Max(horOffset, -Time.deltaTime * 0.3F);

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
                            UnfoldRootMenu();
                    }
                }
                else // Pop all opened menus first
                {
                    var topMenu = openedMenus.Peek();

                    switch (topMenu.State)
                    {
                        case WidgetState.Hidden: // Fade out animation completed
                        case WidgetState.Error:
                            openedMenus.Pop();
                            Destroy(topMenu.gameObject);
                            break;
                        case WidgetState.Shown: // Start its fade out animation
                            topMenu.Hide();
                            break;
                    }

                }
            }

        }

    }
}
