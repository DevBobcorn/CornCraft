#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (ScrollRect))]
    public class InteractionPanel : MonoBehaviour
    {
        private static readonly int SHOW = Animator.StringToHash("Show");

        [SerializeField] private GameObject? interactionOptionPrefab;

        private Animator? scrollHint;
        private ScrollRect? scrollRect;

        private readonly List<InteractionOption> interactionOptions = new();

        private int nextNumeralID = 1, selectedIndex = 0;
        private Transform? container;

        public bool ShouldAbsordMouseScroll => interactionOptions.Count > 1;

        void Start()
        {
            // Initialize controls
            container = FindHelper.FindChildRecursively(transform, "Interactions");
            scrollHint = transform.Find("Scroll Hint").GetComponent<Animator>();

            scrollRect = GetComponent<ScrollRect>();
            scrollHint.SetBool(SHOW, false);

        }

        public void AddInteractionOption(string name, Action<InteractionPanel, InteractionOption>? action = null)
        {
            var optionObj = GameObject.Instantiate(interactionOptionPrefab);
            var option = optionObj?.GetComponent<InteractionOption>();

            if (option is not null)
            {
                option.SetInfo(nextNumeralID, name, action);
                interactionOptions.Add(option);

                optionObj!.transform.SetParent(container);
                optionObj!.transform.localScale = Vector3.one;
                optionObj!.transform.SetAsLastSibling();

                nextNumeralID++;

                if (interactionOptions.Count == 1)
                {
                    selectedIndex = 0;
                    SetSelected(0); // Select the only available option
                }
                else if (selectedIndex < 0 || selectedIndex >= interactionOptions.Count)
                    SetSelected(0); // There's at least 1 option available after adding
                
                scrollHint!.SetBool(SHOW, interactionOptions.Count > 1); // Show mouse hint
            }
            else
            {
                Debug.LogWarning("Interaction option prefab is not valid");

                if (optionObj is not null)
                    Destroy(optionObj);
            }
        }

        public void RunInteractionOption()
        {
            if (selectedIndex >= 0 && selectedIndex < interactionOptions.Count)
            {
                var targetOption = interactionOptions[selectedIndex];
                targetOption.Execute(this, targetOption);
            }
        }

        public void RemoveInteractionOption(int id)
        {
            for (int i = 0;i < interactionOptions.Count;i++)
            {
                if (interactionOptions[i].NumeralID == id)
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

            scrollHint!.SetBool(SHOW, interactionOptions.Count > 1); // Show mouse hint
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

            if (scrollRect is not null)
                scrollRect.verticalScrollbar.value = scrollPos;

            for (int i = 0;i < interactionOptions.Count;i++)
                interactionOptions[i].SetSelected(i == selIndex);
        }
    }
}