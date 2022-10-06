#nullable enable
using UnityEngine;

namespace MinecraftClient.UI
{
    public class FirstPersonMenu_Chat : FirstPersonMenu
    {
        private const string EMPTY = "Empty~  >_<";
        private CornClient? game;

        public override void Hide()
        {
            base.Hide();
            parentGUI!.HideChatPanel();
        }

        public override void FocusSelf()
        {
            base.FocusSelf();
            parentGUI!.HideChatPanel();
        }

        protected override bool InitializeContent()
        {
            var game = CornClient.Instance;

            int itemCount = itemTexts.Length;

            if (itemIcons.Length < 2)
            {
                Debug.LogWarning("Faulty chat icon data, list initialization cancelled.");
                return true;
            }

            if (itemCount > 0)
            {
                for (int i = 0;i < itemCount;i++)
                {
                    var itemObj = GameObject.Instantiate(templateItem, Vector3.zero, Quaternion.identity);
                    itemObj!.transform.SetParent(itemList, false);

                    itemObj.name = $"Friend {itemTexts[i]}";

                    var item = itemObj.GetComponent<FirstPersonListItem>();
                    item.SetContent(this, itemIcons[0], itemIcons[1], itemTexts[i]);

                    item.SetAlpha(0F);
                    items.Add(item);

                    var contact = itemTexts[i];
                    item.Callback += () => parentGUI!.ShowChatPanel(contact);

                    itemObj.SetActive(true);
                }
            }
            else
            {   // Tell player the friend list is empty, by adding a placeholder item
                var itemObj = GameObject.Instantiate(templateItem, Vector3.zero, Quaternion.identity);
                itemObj!.transform.SetParent(itemList, false);

                itemObj.name = EMPTY;

                var item = itemObj.GetComponent<FirstPersonListItem>();
                item.SetContent(this, itemIcons[0], itemIcons[1], EMPTY);

                item.SetAlpha(0F);
                items.Add(item);

                item.gameObject.SetActive(true);
            }

            return false;

        }
    
    }
}
