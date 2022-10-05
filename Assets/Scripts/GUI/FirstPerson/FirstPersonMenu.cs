#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class FirstPersonMenu : MonoBehaviour
    {
        private const float SUB_MENU_FOCUS_OPCACITY = 0.7F;

        public GameObject? templateItem;

        private FirstPersonGUI? parentGUI;
        private CanvasGroup? canvasGroup, stripeCanvasGroup;
        protected Transform? itemList;
        
        public WidgetState State { get; set; } = WidgetState.Hidden;
        private MenuFocusState focusState = MenuFocusState.SelfFocused;

        private const float ITEM_SHOW_TIME = 0.1F;
        private const float MENU_HIDE_TIME = 0.1F;
        private float animTime = 0F;

        // Used for initializing items only
        public string[] itemTexts = { };
        public Sprite[] itemIcons = { };
        public FirstPersonMenu?[] itemSubMenus = { };

        protected List<FirstPersonListItem> items = new();
        protected FirstPersonListItem? focusItem = null;

        protected bool initialized = false;
        
        protected static int GetMiddlePos(int itemCount) => ((itemCount + 1) / 2) - 1;

        public void SetParent(FirstPersonGUI parent)
        {
            EnsureInitialized();

            parentGUI = parent;
        }

        public void Hide()
        {
            EnsureInitialized();

            State = WidgetState.Hide;
            animTime = 0F;
        }

        public bool UpDownActive()
        {
            return State == WidgetState.Shown && (items.Count <= 0 || focusState == MenuFocusState.ChildFocused);
        }

        public bool InputActive()
        {
            return State == WidgetState.Shown;
        }

        public void FocusPrevItem()
        {
            EnsureInitialized();

            if (items.Count <= 0)
                return;
            
            int selectedIndex = items.FindIndex((i) => i == focusItem);
            if (selectedIndex < 0)
                selectedIndex = 0;

            int prevItemIndex = (selectedIndex - 1 + items.Count) % items.Count;
            
            // Focus on previous item
            UnfocusItems();
            FocusItem(items[prevItemIndex]);

        }

        public void FocusNextItem()
        {
            EnsureInitialized();

            if (items.Count <= 0)
                return;
            
            int selectedIndex = items.FindIndex((i) => i == focusItem);
            if (selectedIndex < 0)
                selectedIndex = 0;

            int nextItemIndex = (selectedIndex + 1) % items.Count;
            
            // Focus on next item
            UnfocusItems();
            FocusItem(items[nextItemIndex]);

        }

        public void TryFocusMiddleItem()
        {
            EnsureInitialized();

            if (items.Count <= 0)
                return;
            
            FocusItem(items[GetMiddlePos(items.Count)]);

        }

        // Called when an item's sub menu is closed, and we refocus on this item itself again
        public void TryRefocusOnCurrentItem()
        {
            EnsureInitialized();

            if (focusItem is not null)
                FocusItem(focusItem);
            
        }

        public void TryFocusItem(FirstPersonListItem targetItem)
        {
            if (targetItem is not null && items.Contains(targetItem))
            {
                if (targetItem != focusItem)
                    FocusItem(targetItem);
                else
                    FocusSelf();
            }
            else
                Debug.LogWarning($"Trying to focus on an invalid item");

        }

        private void FocusItem(FirstPersonListItem targetItem)
        {
            EnsureInitialized();

            focusItem = targetItem;

            foreach (var item in items)
            {
                if (item != focusItem)
                    item.Unfocus();
                else
                {
                    // Focus on this item
                    item.Focus(false);

                }
            }

            stripeCanvasGroup!.alpha = 1F;
            canvasGroup!.alpha = 1F;

            focusState = MenuFocusState.ChildFocused;
        }

        public void UnfocusItems()
        {
            EnsureInitialized();

            focusItem = null;

            foreach (var item in items)
                item.Unfocus();

        }

        public void FocusSelf()
        {
            EnsureInitialized();

            stripeCanvasGroup!.alpha = 1F;
            canvasGroup!.alpha = 1F;

            UnfocusItems();

            focusState = MenuFocusState.SelfFocused;
        }

        private int topItemIndex = 0, rollCount = 0;
        private float rollCooldown = 0F;

        private void RollUp()
        {
            EnsureInitialized();

            if (rollCooldown > 0F || items.Count <= 1)
                return;
            
            // Move top item to bottom
            itemList!.GetChild(0).SetAsLastSibling();

            // Update top item index
            var newItemOnTop = itemList.GetChild(0);
            topItemIndex = items.FindIndex((i) => i == newItemOnTop.GetComponent<FirstPersonListItem>());

            rollCooldown = 0.2F;
        }

        private void RollDown()
        {
            EnsureInitialized();

            if (items.Count <= 1)
                return;
            
            // Move bottom item to top
            var newItemOnTop = itemList!.GetChild(itemList.childCount - 1);
            newItemOnTop.SetAsFirstSibling();

            // Update top item index
            topItemIndex = items.FindIndex((i) => i == newItemOnTop.GetComponent<FirstPersonListItem>());

            rollCooldown = 0.2F;
        }

        public void TryUnfoldSubMenu()
        {
            EnsureInitialized();

            if (focusItem is not null && focusItem.SubMenu is not null)
            {
                // This item has a sub menu, unfold it
                parentGUI!.UnfoldMenu(focusItem.SubMenu.gameObject);
                focusItem.Focus(true);

                stripeCanvasGroup!.alpha = 0F;
                canvasGroup!.alpha = SUB_MENU_FOCUS_OPCACITY;

                focusState = MenuFocusState.SubMenuFocused;

                // Roll the unfolded item to the middle of menu
                int rollTargetIndex = items.FindIndex((i) => i == focusItem);

                if (rollTargetIndex != -1 && topItemIndex != -1) // Sanity check
                {
                    int targetItemCurPos = (rollTargetIndex - topItemIndex + items.Count) % items.Count;
                    int middlePos = GetMiddlePos(items.Count);

                    rollCount = middlePos - targetItemCurPos;

                }

            }
            
        }

        protected void EnsureInitialized()
        {
            if (!initialized)
            {
                if (templateItem is null)
                {
                    Debug.LogWarning("Template list item not assigned.");
                    State = WidgetState.Error;
                    return;
                }

                itemList = transform.Find("Item List");
                canvasGroup = GetComponent<CanvasGroup>();

                var stripeObj = transform.Find("Side Stripe");
                stripeCanvasGroup = stripeObj.GetComponent<CanvasGroup>();

                bool empty = InitializeContent();

                State = empty ? WidgetState.Shown : WidgetState.Show;
                animTime = 0F;

                topItemIndex = empty ? -1 : 0;

                initialized = true;

            }
        }

        protected virtual bool InitializeContent()
        {
            int itemCount = itemTexts.Length;

            if (itemCount <= 0)
                return true;

            if (itemIcons.Length != itemCount * 2 || itemSubMenus.Length != itemCount)
            {
                Debug.LogWarning("Faulty list item data, set to empty.");
                return true;
            }

            for (int i = 0;i < itemCount;i++) // Initialize
            {
                var itemObj = GameObject.Instantiate(templateItem, Vector3.zero, Quaternion.identity);
                itemObj!.transform.SetParent(itemList, false);
                itemObj.name = $"{itemTexts[i]} Item";

                var item = itemObj.GetComponent<FirstPersonListItem>();
                item.SetContent(this, itemIcons[i * 2], itemIcons[i * 2 + 1], itemTexts[i]);
                item.SubMenu = itemSubMenus[i];

                item.SetAlpha(0F);
                items.Add(item);

                item.gameObject.SetActive(true);
            }

            return false;

        }

        void Start() => EnsureInitialized();

        void Update()
        {
            if (State == WidgetState.Show)
            {
                animTime += Time.deltaTime;

                int fullyShownCount = (int)Mathf.Floor(animTime / ITEM_SHOW_TIME);

                for (int i = 0;i < items.Count;i++)
                {
                    if (animTime > i * ITEM_SHOW_TIME)
                    {
                        if (i < fullyShownCount) // Fully shown already
                        {
                            items[i].SetAlpha(1F);
                            
                            if (i == items.Count - 1) // All items fully shown
                                State = WidgetState.Shown;
                        }
                        else // Fading in
                            items[i].SetAlpha((animTime - fullyShownCount * ITEM_SHOW_TIME) / ITEM_SHOW_TIME);
                        
                    }
                    
                }
            }
            else if (State == WidgetState.Hide)
            {
                animTime += Time.deltaTime;

                if (animTime < MENU_HIDE_TIME)
                {
                    canvasGroup!.alpha = 1F - (animTime / MENU_HIDE_TIME);
                }
                else
                {
                    canvasGroup!.alpha = 0F;
                    State = WidgetState.Hidden;
                }
            }
            else if (State == WidgetState.Shown)
            {
                if (rollCooldown > 0F)
                    rollCooldown = Mathf.Max(rollCooldown - Time.deltaTime, 0F);
                
                if (rollCount != 0 && rollCooldown <= 0F)
                {
                    if (rollCount > 0)
                    {
                        RollDown();
                        rollCount--;
                    }
                    else
                    {
                        RollUp();
                        rollCount++;
                    }
                }
            }
            else // Hidden, self destroy
            {
                Destroy(this.gameObject);
            }

        }

    }
}
