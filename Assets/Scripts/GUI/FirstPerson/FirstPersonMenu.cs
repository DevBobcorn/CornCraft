#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class FirstPersonMenu : MonoBehaviour
    {
        public enum State
        {
            Hidden, Show, Shown, Hide, Error
        }

        public GameObject? templateItem;
        private CanvasGroup? canvasGroup;
        private Transform? itemList;
        
        public State state = State.Hidden;
        private const float ITEM_SHOW_TIME = 0.1F;
        private const float MENU_HIDE_TIME = 0.1F;
        private float animTime = 0F;

        // Used for initializing items only
        public string[] itemTexts = { };
        public Sprite[] itemIcons = { };

        private List<FirstPersonListItem> items = new();
        
        void Start()
        {
            if (templateItem is null)
            {
                Debug.LogWarning("Template list item not assigned.");
                state = State.Error;
                return;
            }

            int itemCount = itemTexts.Length;
            if (itemIcons.Length != itemCount * 2)
            {
                Debug.LogWarning("Please check list item data.");
                state = State.Error;
                return;
            }

            itemList = transform.Find("Item List");
            canvasGroup = GetComponent<CanvasGroup>();

            for (int i = 0;i < itemCount;i++) // Initialize
            {
                var itemObj = GameObject.Instantiate(templateItem, Vector3.zero, Quaternion.identity);
                itemObj.transform.SetParent(itemList, false);

                var item = itemObj.GetComponent<FirstPersonListItem>();
                item.SetContent(itemIcons[i * 2], itemIcons[i * 2 + 1], itemTexts[i]);
                item.SetAlpha(0F);
                items.Add(item);

                item.gameObject.SetActive(true);
            }

            state = State.Show;
            animTime = 0F;
        }

        public void Hide()
        {
            state = State.Hide;
            animTime = 0F;
        }

        void Update()
        {
            if (state == State.Show)
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
                                state = State.Shown;
                        }
                        else // Fading in
                            items[i].SetAlpha((animTime - fullyShownCount * ITEM_SHOW_TIME) / ITEM_SHOW_TIME);
                        
                    }
                    
                }
            }
            else if (state == State.Hide)
            {
                animTime += Time.deltaTime;

                if (animTime < MENU_HIDE_TIME)
                {
                    canvasGroup!.alpha = 1F - (animTime / MENU_HIDE_TIME);
                }
                else
                {
                    canvasGroup!.alpha = 0F;
                    state = State.Hidden;
                }
            }
        }
    }
}
