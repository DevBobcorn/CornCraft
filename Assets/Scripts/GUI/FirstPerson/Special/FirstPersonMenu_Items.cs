#nullable enable
using System.Linq;
using UnityEngine;

namespace MinecraftClient.UI
{
    public class FirstPersonMenu_Items : FirstPersonMenu
    {
        private const string EMPTY = "Empty~  >_<";
        private CornClient? game;

        protected override bool InitializeContent()
        {
            var game = CornClient.Instance;

            if (itemIcons.Length < 2)
            {
                Debug.LogWarning("Faulty items icon data, list initialization cancelled.");
                return true;
            }

            var inventory = game.GetInventory(0);

            if (inventory is not null && inventory.Items.Count > 0)
            {
                int itemCount = inventory.Items.Count;
                var slots = inventory.Items.Keys.ToArray();

                for (int i = 0;i < itemCount;i++)
                {
                    int slot = slots[i];

                    var invItem = inventory.Items[slot];

                    var itemObj = GameObject.Instantiate(templateItem, Vector3.zero, Quaternion.identity);
                    itemObj!.transform.SetParent(itemList, false);

                    itemObj.name = invItem.ToString();

                    var item = itemObj.GetComponent<FirstPersonListItem>();
                    item.SetContent(this, itemIcons[0], itemIcons[1], $"[{slot}] {invItem}");

                    item.SetAlpha(0F);
                    items.Add(item);

                    itemObj.SetActive(true);
                }
            }
            else
            {   // Tell player the inventory is empty, by adding a placeholder item
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
