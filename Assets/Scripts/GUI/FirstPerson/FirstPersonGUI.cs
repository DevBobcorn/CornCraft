#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UISoftMask;
using MinecraftClient.Control;
using MinecraftClient.Mapping;

namespace MinecraftClient.UI
{
    public class FirstPersonGUI : MonoBehaviour
    {
        public const float CANVAS_SCALE = 0.0006F;
        public const int BUTTON_COUNT = 5;
        
        private const float SINGLE_SHOW_TIME = 0.25F, SINGLE_DELTA_TIME = 0.15F;
        private const float TOTAL_SHOW_TIME = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1) + SINGLE_SHOW_TIME;
        private const float TOTAL_HIDE_TIME = 0.5F;
        private static readonly float[] START_POS = {  800F,  560F,  320F,   80F, -160F };
        private static readonly float[] STOP_POS  = {  180F,   90F,    0F,  -90F, -180F };

        private const float ROLL_BUTTON_SPEED = 700F;

        private const float ROOT_MENU_OFFSET =  70F;
        private const float SUB_MENU_OFFSET  = 190F;

        public float followSpeed = 1F;
        public float maxDeltaAngle = 30F;
        private CornClient? game;

        private CameraController? cameraCon;

        private Canvas?   canvas;
        private Animator? canvasAnim;

        private SoftMask?   buttonListMask;
        private Image? buttonListMaskImage;
        
        private FirstPersonPanel? firstPersonPanel = null;
        private FirstPersonChat?  firstPersonChat  = null;

        private GameObject[] rootMenuPrefabs = new GameObject[BUTTON_COUNT];
        private GameObject[] buttonObjs = new GameObject[BUTTON_COUNT];
        private FirstPersonButton[] buttons = new FirstPersonButton[BUTTON_COUNT];
        private GameObject? newButtonObj;

        public WidgetState State { get; set; } = WidgetState.Hidden;

        // Selected button: The currently selected button
        // Rolling button:  The button which is currently on the edge of the list and has a visual duplication, -1 if the list is not rolling
        // Target button:   The target button of rolling, -1 if the list is not rolling
        private int selectedButton = 0, rollingButton = -1, targetButton = -1;

        private Stack<FirstPersonMenu> openedMenus = new();

        private float GetPosForButton(int index)
        {
            var posIndex = (index + BUTTON_COUNT - selectedButton) % BUTTON_COUNT;
            var itsStartTime = SINGLE_DELTA_TIME * (BUTTON_COUNT - 1 - posIndex);

            if (GUIAnimTime <= itsStartTime) // This button's animation hasn't yet started
                return START_POS[posIndex];
            else if (GUIAnimTime >= itsStartTime + SINGLE_SHOW_TIME) // This button's animation has already ended
                return STOP_POS[posIndex];
            else // Lerp to get its position
                return Mathf.Lerp(START_POS[posIndex], STOP_POS[posIndex], (GUIAnimTime - itsStartTime) / SINGLE_SHOW_TIME);

        }

        private float GUIAnimTime = -1F, rollOffset = 0F;
        private int rollCount = 0;

        private bool initialzed = false;

        private void Initialize()
        {
            // Find game instance
            game = CornClient.Instance;

            canvas = GetComponentInChildren<Canvas>();

            // Initialize panel animator
            canvasAnim = canvas.GetComponent<Animator>();
            canvasAnim.SetBool("Show", false);
            State = WidgetState.Hidden;

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
                int clickedButton = i;

                button.onClick.AddListener(() => {
                    if (rollOffset == 0F && rollCount == 0 && clickedButton != selectedButton)
                    {
                        //rollCount = clickedButton - selectedButton;

                        int d1 = (clickedButton - selectedButton + BUTTON_COUNT) % BUTTON_COUNT;

                        if (d1 <= (BUTTON_COUNT / 2))
                            rollCount =  (clickedButton - selectedButton + BUTTON_COUNT) % BUTTON_COUNT;
                        else
                            rollCount = -(selectedButton - clickedButton + BUTTON_COUNT) % BUTTON_COUNT;

                        targetButton = clickedButton;
                        RollButtons();
                    }
                });

                buttons[i] = buttonObjs[i].GetComponent<FirstPersonButton>();

            }

            selectedButton = 0;

            rootMenuPrefabs[0] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Avatar");
            rootMenuPrefabs[1] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Social");
            rootMenuPrefabs[2] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Chat");
            rootMenuPrefabs[3] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Map");
            rootMenuPrefabs[4] = Resources.Load<GameObject>("Prefabs/GUI/First Person Menu Settings");

            var firstPersonPanelPrefab = Resources.Load<GameObject>("Prefabs/GUI/First Person Panel");
            var firstPersonPanelObj = GameObject.Instantiate(firstPersonPanelPrefab, Vector3.zero, Quaternion.identity);
            firstPersonPanelObj.transform.SetParent(canvas.transform, false);
            firstPersonPanelObj.transform.localPosition = new(-70F, STOP_POS[0], 0F);

            firstPersonPanel = firstPersonPanelObj.GetComponent<FirstPersonPanel>();

            var chatPrefab = Resources.Load<GameObject>("Prefabs/GUI/First Person Chat");
            var chatObj = GameObject.Instantiate(chatPrefab, Vector3.zero, Quaternion.identity);
            chatObj.transform.SetParent(canvas.transform, false);
            chatObj.transform.localPosition = new(70F, STOP_POS[0], 0F);

            firstPersonChat = chatObj.GetComponent<FirstPersonChat>();

            initialzed = true;
        }

        private void EnsureInitialized()
        {
            if (!initialzed)
                Initialize();
        }

        private void UnfocusButtons()
        {
            foreach (var button in buttons)
                button.Unfocus();
        }

        private void RollButtons()
        {
            var next = (rollCount > 0);

            if (next)
            {
                rollingButton = selectedButton; // Current top button
                selectedButton = (selectedButton + 1) % BUTTON_COUNT;
            }
            else
            {
                rollingButton = (selectedButton + BUTTON_COUNT - 1) % BUTTON_COUNT; // Current bottom button
                selectedButton = (selectedButton + BUTTON_COUNT - 1) % BUTTON_COUNT;
            }

            // Make a visual copy of newly selected button
            newButtonObj = GameObject.Instantiate(buttonObjs[rollingButton]);
            newButtonObj.name = buttonObjs[rollingButton].name;

            // First unfocus all buttons
            UnfocusButtons();

            if (next) // Then focus on the next target button
                buttons[selectedButton].Focus();
            else // Focus on both the original button and its duplication
            {
                buttons[rollingButton].Focus();
                newButtonObj.GetComponent<FirstPersonButton>().Focus();
            }
            
            var newButtonTransform = newButtonObj.GetComponent<RectTransform>();
            var oldButtonTransform = buttonObjs[rollingButton].GetComponent<RectTransform>();

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

        private void SelectPrevButton()
        {
            if (rollOffset == 0F && rollCount == 0)
            {
                targetButton = (selectedButton + BUTTON_COUNT - 1) % BUTTON_COUNT;
                rollCount = -1;
                RollButtons();
            }
        }

        private void SelectNextButton()
        {
            if (rollOffset == 0F && rollCount == 0)
            {
                targetButton = (selectedButton + 1) % BUTTON_COUNT;
                rollCount =  1;
                RollButtons();
            }
        }

        private float GetHorizontalPosition()
        {
            int refIndex = (targetButton == -1) ? selectedButton : targetButton;

            float basePos;

            if (!buttons[refIndex].itemsOnLeftSide)
                basePos = (-Mathf.Max(0, openedMenus.Count - 1) * SUB_MENU_OFFSET) * CANVAS_SCALE;
            else
                basePos = ( Mathf.Max(0, openedMenus.Count - 1) * SUB_MENU_OFFSET) * CANVAS_SCALE;
            
            if (firstPersonChat!.Shown)
                basePos -= 190F * CANVAS_SCALE;

            if (State == WidgetState.Hide) // Move to the left when fading out
                return basePos - 1F;

            return basePos;
        }

        private void UnfoldCurrentRootMenu()
        {
            if (openedMenus.Count == 0)
            {
                // Unfold root menu of selected button
                UnfoldMenu(rootMenuPrefabs[selectedButton]);
                
                var rootButton = buttons[selectedButton];

                 // Change panel visibility if necessary
                if (rootButton.panelSize != Vector2.zero)
                {
                    firstPersonPanel!.Show(rootButton.panelSize, rootButton.panelTitle, rootButton.avatarOnPanel);

                    // TODO Display corresponding UI on the panel
                    
                }
                else
                    firstPersonPanel!.Hide();

            }
            else // Previously opened menus not closed properly
            {
                // TODO Handle properly
                Debug.LogWarning("Menus not properly closed.");
            }
        }

        public void UnfoldMenu(GameObject menuPrefab)
        {
            var menuObj = GameObject.Instantiate(menuPrefab, Vector3.zero, Quaternion.identity);

            menuObj!.transform.SetParent(canvas!.transform, false);

            var rootMenu = menuObj!.GetComponent<FirstPersonMenu>();
            rootMenu.SetParent(this);

            if (buttons[selectedButton].itemsOnLeftSide)
                menuObj.transform.localPosition = new(-ROOT_MENU_OFFSET - openedMenus.Count * SUB_MENU_OFFSET, STOP_POS[0], 0F);
            else
                menuObj.transform.localPosition = new( ROOT_MENU_OFFSET + openedMenus.Count * SUB_MENU_OFFSET, STOP_POS[0], 0F);

            menuObj.SetActive(true);

            openedMenus.Push(rootMenu);
            rootMenu.FocusSelf();

        }

        public void ShowChatPanel(string contact)
        {
            EnsureInitialized();

            firstPersonChat!.Show(contact);
        }

        public void HideChatPanel()
        {
            EnsureInitialized();

            firstPersonChat!.Hide();
        }

        public void ShowGUI()
        {
            EnsureInitialized();

            if (State != WidgetState.Hidden)
                return;
            
            // First calculate and set rotation
            var cameraRot = Camera.main.transform.eulerAngles.y;
            transform.eulerAngles = new(0F, cameraRot, 0F);

            // Teleport panel to the right position
            var canvasTransform = canvas!.transform;

            canvasTransform.Translate(GetHorizontalPosition() - canvasTransform.localPosition.x, 0F, 0F, Space.Self);

            // Select button on top
            UnfocusButtons();
            buttons[selectedButton].Focus();

            // Then play fade animation
            canvasAnim!.SetBool("Show", true);
            State = WidgetState.Show;
            GUIAnimTime = 0F;
        }

        public void HideGUI()
        {
            EnsureInitialized();

            if (State != WidgetState.Shown)
                return;

            // Play hide animation
            canvasAnim!.SetBool("Show", false);
            State = WidgetState.Hide;
            GUIAnimTime = 0F;

            firstPersonPanel!.Hide();
            HideChatPanel();
        }

        public void SetCameraCon(CameraController cameraCon)
        {
            EnsureInitialized();

            this.cameraCon = cameraCon;
            canvas!.worldCamera = cameraCon.ActiveCamera;
        }

        void Start() => EnsureInitialized();

        void Update()
        {
            if (State == WidgetState.Show) // Panel is showing up
            {
                if (GUIAnimTime < TOTAL_SHOW_TIME) // Play show animation
                {
                    GUIAnimTime = Mathf.Min(GUIAnimTime + Time.deltaTime, TOTAL_SHOW_TIME);

                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, GetPosForButton(i));
                    
                    return;
                }
                else // Complete show animation
                {
                    for (int i = 0;i < BUTTON_COUNT;i++)
                        buttonObjs[i].GetComponent<RectTransform>().anchoredPosition = new(0F, STOP_POS[(i + BUTTON_COUNT - selectedButton) % BUTTON_COUNT]);
                    
                    UnfoldCurrentRootMenu();
                    State = WidgetState.Shown;
                }

            }
            else if (State == WidgetState.Hide) // Panel is fading out
            {
                GUIAnimTime = Mathf.Min(GUIAnimTime + Time.deltaTime, TOTAL_HIDE_TIME);

                if (GUIAnimTime >= TOTAL_HIDE_TIME) // Complete fade animation
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
                bool goBackFlag = 
                    buttons[selectedButton].itemsOnLeftSide ? Input.GetKeyDown(KeyCode.RightArrow) : Input.GetKeyDown(KeyCode.LeftArrow);
                
                bool goForwardFlag =
                    buttons[selectedButton].itemsOnLeftSide ? Input.GetKeyDown(KeyCode.LeftArrow) : Input.GetKeyDown(KeyCode.RightArrow);

                if (goBackFlag)
                {
                    if (openedMenus.Count == 1 && openedMenus.Peek().InputActive())
                        openedMenus.Peek().FocusSelf();
                    else if (openedMenus.Count > 1 && openedMenus.Peek().InputActive())
                    {
                        // Try closing this sub menu
                        openedMenus.Pop().Hide();

                        if (openedMenus.Count > 0) // If the stack is still not empty
                            openedMenus.Peek().TryRefocusOnCurrentItem();
                        
                    }
                }
                else if (goForwardFlag)
                {   // TODO Implement
                    if (openedMenus.Count > 0)
                        openedMenus.Peek().TryFocusMiddleItem();
                }

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (openedMenus.Count > 0 && openedMenus.Peek().InputActive())
                        openedMenus.Peek().TryUnfoldSubMenuOrCallAction();
                }

                if (Input.GetKeyDown(KeyCode.UpArrow)) // Previous
                {
                    if (openedMenus.Count > 1 && openedMenus.Peek().InputActive())
                        openedMenus.Peek().FocusPrevItem();
                    else if (openedMenus.Count > 0 && openedMenus.Peek().UpDownActive())
                        openedMenus.Peek().FocusPrevItem();
                    else
                        SelectPrevButton();
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow)) // Next
                {
                    if (openedMenus.Count > 1 && openedMenus.Peek().InputActive())
                        openedMenus.Peek().FocusNextItem();
                    else if (openedMenus.Count > 0 && openedMenus.Peek().UpDownActive())
                        openedMenus.Peek().FocusNextItem();
                    else
                        SelectNextButton();
                }
                else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked) // Detect click on a certain button
                {
                    var rayCaster = canvas!.GetComponent<GraphicRaycaster>();
                    //Debug.Log($"LMB down at {Input.mousePosition}");
                    // Simulate a mouse click, we need to do this because click events won't be triggered when mouse cursor is locked
                    var result = new List<RaycastResult>();
                    rayCaster.Raycast(new(EventSystem.current) { position = Input.mousePosition }, result);

                    string res = string.Empty;

                    foreach (var r in result)
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
                if (game!.GetPlayer().Perspective == Perspective.FirstPerson && cameraCon is not null)
                {
                    var cameraRot = cameraCon.ActiveCamera!.transform.eulerAngles.y;
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
            else // panel is hidden
                return;

            var canvasTransform = canvas!.transform;
            var horOffset = GetHorizontalPosition() - canvasTransform.localPosition.x;

            if (horOffset != 0F)
            {
                float mov;

                if (horOffset > 0)
                    mov = Mathf.Min(horOffset,  Time.deltaTime * 0.4F);
                else
                    mov = Mathf.Max(horOffset, -Time.deltaTime * 0.4F);

                canvasTransform.Translate(mov, 0F, 0F, Space.Self);
            }

            if (openedMenus.Count == 0) // Roll button
            {
                if (rollOffset != 0F)
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
                        buttonObjs[rollingButton].transform.position = newButtonObj.transform.position;
                        Destroy(newButtonObj);

                        // And select the selected button
                        buttons[selectedButton].Focus();

                        buttonListMask!.enabled = false;
                        buttonListMaskImage!.color = new(0F, 0F, 0F, 0F);

                        // Update offset count
                        if (rollCount > 0)
                            rollCount--;
                        else if (rollCount < 0)
                            rollCount++;
                        
                        rollingButton = -1;
                        
                        // Continue to roll buttons if necessary
                        if (rollCount != 0)
                            RollButtons();
                        else
                        {
                            // Rolled to target button, unfolder its root menu
                            UnfoldCurrentRootMenu();
                            targetButton = -1;
                        }
                    }
                    
                }
            }
            else // Update states of menus in stack
            {
                var topMenu = openedMenus.Peek();

                switch (topMenu.State)
                {
                    case WidgetState.Error:
                        openedMenus.Pop();
                        Destroy(topMenu.gameObject);
                        break;
                    case WidgetState.Shown:
                        if (rollOffset != 0F) // Pop stack and start its fade out animation
                            openedMenus.Pop().Hide();
                        
                        if (openedMenus.Count > 0) // If the stack is still not empty
                            openedMenus.Peek().TryRefocusOnCurrentItem();
                        
                        break;
                }

            }
            

        }

    }
}
