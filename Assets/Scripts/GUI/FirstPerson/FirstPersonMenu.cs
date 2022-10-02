#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class FirstPersonMenu : MonoBehaviour
    {
        public GameObject? templateItem;

        private FirstPersonGUI? parentGUI;
        private CanvasGroup? canvasGroup;
        private Transform? itemList;

        public bool Focused { get; set; } = false;
        
        public WidgetState State { get; set; } = WidgetState.Hidden;
        private const float ITEM_SHOW_TIME = 0.1F;
        private const float MENU_HIDE_TIME = 0.1F;
        private float animTime = 0F;

        // Used for initializing items only
        public string[] itemTexts = { };
        public Sprite[] itemIcons = { };

        private List<FirstPersonListItem> items = new();
        private FirstPersonListItem? selectedItem = null;
        
        public void SetParent(FirstPersonGUI parent)
        {
            parentGUI = parent;
        }

        public void Hide()
        {
            State = WidgetState.Hide;
            animTime = 0F;
        }

        public void Select(FirstPersonListItem target)
        {
            if (target is not null && items.Contains(target))
            {
                selectedItem = target;
                foreach (var item in items)
                {
                    if (item != selectedItem)
                        item.Deselect();
                }
            }
            else
            {
                Debug.LogWarning($"Trying to select an invalid item");
            }
        }

        void Start()
        {
            if (templateItem is null)
            {
                Debug.LogWarning("Template list item not assigned.");
                State = WidgetState.Error;
                return;
            }

            int itemCount = itemTexts.Length;
            if (itemIcons.Length != itemCount * 2)
            {
                Debug.LogWarning("Please check list item data.");
                State = WidgetState.Error;
                return;
            }

            itemList = transform.Find("Item List");
            canvasGroup = GetComponent<CanvasGroup>();

            for (int i = 0;i < itemCount;i++) // Initialize
            {
                var itemObj = GameObject.Instantiate(templateItem, Vector3.zero, Quaternion.identity);
                itemObj.transform.SetParent(itemList, false);
                itemObj.name = $"{itemTexts[i]} Item";

                var item = itemObj.GetComponent<FirstPersonListItem>();
                item.SetContent(this, itemIcons[i * 2], itemIcons[i * 2 + 1], itemTexts[i]);

                item.SetAlpha(0F);
                items.Add(item);

                item.gameObject.SetActive(true);
            }

            State = WidgetState.Show;
            animTime = 0F;
        }

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
        }
    }
}
