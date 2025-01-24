using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using CraftSharp.Event;
using CraftSharp.Control;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (ScrollRect))]
    public class InteractionPanel : MonoBehaviour
    {
        private static readonly int SHOW_HASH = Animator.StringToHash("Show");

        [SerializeField] private GameObject interactionOptionPrefab;
        [SerializeField] private GameObject interactionProgressOptionPrefab;
        [SerializeField] private RectTransform container;

        private Animator scrollHint;
        private ScrollRect scrollRect;

        private readonly List<InteractionOption> interactionOptions = new();

        private int selectedIndex = 0;

        public bool ShouldConsumeMouseScroll => interactionOptions.Count > 1;

        #nullable enable

        private Action<InteractionAddEvent>? addCallback;
        private Action<InteractionRemoveEvent>? removeCallback;
        private Action<HarvestInteractionUpdateEvent>? harvestInteractionUpdateCallback;

        public delegate void ItemCountEventHandler(int newCount);
        public event ItemCountEventHandler? OnItemCountChange;

        #nullable disable

        void Start()
        {
            // Initialize controls
            scrollHint = transform.Find("Scroll Hint").GetComponent<Animator>();

            scrollRect = GetComponent<ScrollRect>();
            scrollHint.SetBool(SHOW_HASH, false);

            // Events
            addCallback = (e) => {
                AddInteractionOption(e.InteractionId, e.UseProgress, e.AddAndSelect, e.Info);
            };

            removeCallback = (e) => {
                RemoveInteractionOption(e.InteractionId);
            };

            harvestInteractionUpdateCallback = (e) =>
            {
                var curValue = Mathf.Clamp01(e.Progress);
                
                for (int i = 0;i < interactionOptions.Count;i++)
                {
                    if (interactionOptions[i].InteractionId == e.InteractionId)
                    {
                        interactionOptions[i].UpdateInfoText();

                        if (interactionOptions[i] is InteractionProgressOption progressOption)
                        {
                            progressOption.UpdateProgress(e.Progress);
                        }

                        break;
                    }
                }
            };

            EventManager.Instance.Register(addCallback);
            EventManager.Instance.Register(removeCallback);
            EventManager.Instance.Register(harvestInteractionUpdateCallback);
        }

        public void ShowItemIcons()
        {
            foreach (var option in interactionOptions)
            {
                option.ShowItemIcon();
            }
        }

        public void HideItemIcons()
        {
            foreach (var option in interactionOptions)
            {
                option.HideItemIcon();
            }
        }

        void OnDestroy()
        {
            if (addCallback is not null)
                EventManager.Instance.Unregister(addCallback);
            
            if (removeCallback is not null)
                EventManager.Instance.Unregister(removeCallback);
            
            if (harvestInteractionUpdateCallback is not null)
                EventManager.Instance.Unregister(harvestInteractionUpdateCallback);
        }

        public void AddInteractionOption(int id, bool addAndSelect, bool useProgress, InteractionInfo info)
        {
            var optionObj = Instantiate(useProgress ? interactionProgressOptionPrefab : interactionOptionPrefab);
            var option = optionObj == null ? null : optionObj.GetComponent<InteractionOption>();

            if (option != null)
            {
                option.SetId(id);
                option.SetInfo(info);
                

                optionObj.transform.SetParent(container, false);
                optionObj.transform.localScale = Vector3.one;

                if (addAndSelect) // Add this one to top of the list, and select it
                {
                    interactionOptions.Insert(0, option); // Prepend to list
                    optionObj.transform.SetAsFirstSibling();

                    selectedIndex = 0;
                    SetSelected(0); // Select the first option

                    option.UpdateKeyHintText("LMB");
                }
                else // Add this one to bottom of the list
                {
                    interactionOptions.Add(option); // Append to list
                    optionObj.transform.SetAsLastSibling();

                    if (interactionOptions.Count == 1 || selectedIndex < 0 || selectedIndex >= interactionOptions.Count)
                    {
                        selectedIndex = 0;
                        SetSelected(0); // Select the first option
                    }

                    option.UpdateKeyHintText("X");
                }

                scrollHint.SetBool(SHOW_HASH, interactionOptions.Count > 1); // Show or hide scroll hint
            }
            else
            {
                Debug.LogWarning("Interaction option prefab is not valid");

                if (optionObj != null)
                    Destroy(optionObj);
            }

            OnItemCountChange?.Invoke(interactionOptions.Count);
        }

        public void RunInteractionOption()
        {
            if (selectedIndex >= 0 && selectedIndex < interactionOptions.Count)
            {
                var targetOption = interactionOptions[selectedIndex];
                targetOption.Execute(CornApp.CurrentClient);
            }
        }

        public void RemoveInteractionOption(int id)
        {
            for (int i = 0;i < interactionOptions.Count;i++)
            {
                if (interactionOptions[i].InteractionId == id)
                {
                    // Play fade away animation
                    interactionOptions[i].Remove();
                    // And remove it from our dictionary
                    interactionOptions.RemoveAt(i);

                    if (interactionOptions.Count > 0) // If still not empty
                    {
                        // Focus on another option
                        if (selectedIndex < 0 || selectedIndex >= interactionOptions.Count)
                            SetSelected(0);
                        else
                            SetSelected(selectedIndex);
                    }

                    break; // End iteration after removing
                }
            }

            scrollHint.SetBool(SHOW_HASH, interactionOptions.Count > 1); // Show or hide scroll hint
            OnItemCountChange?.Invoke(interactionOptions.Count);
        }

        public void SelectPrevOption()
        {
            SetSelected(Mathf.Clamp(selectedIndex - 1, 0, interactionOptions.Count - 1));
        }

        public void SelectNextOption()
        {
            SetSelected(Mathf.Clamp(selectedIndex + 1, 0, interactionOptions.Count - 1));
        }

        private void SetSelected(int selIndex)
        {
            selectedIndex = selIndex;

            var scrollPos = 1F - (float)selectedIndex / (float)(interactionOptions.Count - 1);

            if (scrollRect != null)
                scrollRect.verticalScrollbar.value = scrollPos;

            for (int i = 0;i < interactionOptions.Count;i++)
                interactionOptions[i].SetSelected(i == selIndex);
        }
    }
}