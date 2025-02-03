using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using CraftSharp.Event;
using CraftSharp.Control;
using CraftSharp.Resource;
using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (ScrollRect))]
    public class InteractionPanel : MonoBehaviour
    {
        private static readonly int SHOW_HASH = Animator.StringToHash("Show");
        private static readonly int HIDE_HASH = Animator.StringToHash("Hide");

        [SerializeField] private GameObject interactionOptionPrefab;
        [SerializeField] private GameObject interactionProgressOptionPrefab;
        [SerializeField] private RectTransform container;
        [SerializeField] private LineRenderer iteractionTargetHintLine;
        [SerializeField] private RectTransform iteractionTargetHint;
        [SerializeField] private Animator interactionTargetAnimator;

        private Animator scrollHint;
        private ScrollRect scrollRect;

        private readonly List<InteractionOption> interactionOptions = new();

        private int selectedIndex = 0;

        public bool ShouldConsumeMouseScroll => interactionOptions.Count > 1;

        #nullable enable

        private Action<InteractionAddEvent>? addCallback;
        private Action<InteractionRemoveEvent>? removeCallback;
        private Action<HarvestInteractionUpdateEvent>? harvestInteractionUpdateCallback;
        private Action<TargetBlockLocChangeEvent>? targetBlockLocChangeEvent;

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
            addCallback = (e) =>
            {
                AddInteractionOption(e.InteractionId, e.AddAndSelect, e.UseProgress, e.Info);
            };

            removeCallback = (e) =>
            {
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

            targetBlockLocChangeEvent = (e) =>
            {
                if (e.BlockLoc != null)
                {
                    if (selectedIndex >= 0 && selectedIndex < interactionOptions.Count)
                    {
                        var selectedOption = interactionOptions[selectedIndex];

                        if (selectedOption.interactionInfo is BlockInteractionInfo blockInfo)
                        {
                            if (blockInfo.BlockLoc == e.BlockLoc)
                            {
                                return; // Selected interaction is also on the block, don't update
                            }
                        }
                    }

                    for (int i = 0; i < interactionOptions.Count; i++)
                    {
                        if (interactionOptions[i].interactionInfo is BlockInteractionInfo blockInfo
                                && blockInfo.BlockLoc == e.BlockLoc) {
                            
                            SetSelected(i); // Select this option
                            break;
                        }
                    }
                }
            };

            EventManager.Instance.Register(addCallback);
            EventManager.Instance.Register(removeCallback);
            EventManager.Instance.Register(harvestInteractionUpdateCallback);
            EventManager.Instance.Register(targetBlockLocChangeEvent);
        }

        public void ShowItemIconsAndTargetHint()
        {
            foreach (var option in interactionOptions)
            {
                option.ShowItemIcon();
            }

            UpdateTargetHintVisibility();
        }

        public void HideItemIconsAndTargetHint()
        {
            foreach (var option in interactionOptions)
            {
                option.HideItemIcon();
            }

            HideTargetHint();
        }

        private void UpdateTargetHintVisibility()
        {
            if (selectedIndex >= 0 && selectedIndex < interactionOptions.Count &&
                    interactionOptions[selectedIndex].interactionInfo is BlockInteractionInfo)
            {
                // Fade in interaction hint
                ShowTargetHint();
            }
            else
            {
                HideTargetHint();
            }
        }

        private void ShowTargetHint()
        {
            interactionTargetAnimator.ResetTrigger(SHOW_HASH);
            interactionTargetAnimator.ResetTrigger(HIDE_HASH);
            interactionTargetAnimator.SetTrigger(SHOW_HASH);
        }

        private void HideTargetHint()
        {
            interactionTargetAnimator.ResetTrigger(SHOW_HASH);
            interactionTargetAnimator.ResetTrigger(HIDE_HASH);
            interactionTargetAnimator.SetTrigger(HIDE_HASH);
        }

        void OnDestroy()
        {
            if (addCallback is not null)
                EventManager.Instance.Unregister(addCallback);
            
            if (removeCallback is not null)
                EventManager.Instance.Unregister(removeCallback);
            
            if (harvestInteractionUpdateCallback is not null)
                EventManager.Instance.Unregister(harvestInteractionUpdateCallback);
            
            if (targetBlockLocChangeEvent is not null)
                EventManager.Instance.Unregister(targetBlockLocChangeEvent);
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
                }

                if (useProgress)
                {
                    option.UpdateKeyHintText("keybinding.mouse.lmb");
                }
                else
                {
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
                targetOption.Execute();
            }
        }

        public void RemoveInteractionOption(int id)
        {
            for (int i = 0; i < interactionOptions.Count; i++)
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

            if (interactionOptions.Count <= 0)
            {
                interactionTargetAnimator.ResetTrigger(SHOW_HASH);
                interactionTargetAnimator.ResetTrigger(HIDE_HASH);
                interactionTargetAnimator.SetTrigger(HIDE_HASH);
            }
        }

        public void UpdateInteractionTargetHint(Vector3Int originOffset, Camera uiCamera, CameraController camControl)
        {
            if (selectedIndex > 0 || selectedIndex < interactionOptions.Count)
            {
                var selectedOption = interactionOptions[selectedIndex];

                if (selectedOption.interactionInfo is BlockInteractionInfo blockInfo)
                {
                    var blockLoc = blockInfo.BlockLoc;
                    var worldPoint = CoordConvert.MC2Unity(originOffset, (blockLoc.X >> 4) << 4, (blockLoc.Y >> 4) << 4, (blockLoc.Z >> 4) << 4);

                    var offsetType = ResourcePackManager.Instance.StateModelTable[blockInfo.Block.StateId].OffsetType;
                    var pointOffset = (Vector3) ChunkRenderBuilder.GetBlockOffset(offsetType,
                        blockLoc.X >> 4, blockLoc.Z >> 4, blockLoc.X & 0xF, blockLoc.Y & 0xF, blockLoc.Z & 0xF) + Vector3.one * 0.5F;

                    // Update interaction hint position
                    var newPos = uiCamera.ViewportToWorldPoint(camControl.GetPointViewportPos(worldPoint + pointOffset));

                    // Don't modify z coordinate
                    iteractionTargetHint.position = new Vector3(newPos.x, newPos.y, iteractionTargetHint.position.z);
                    iteractionTargetHintLine.SetPosition(0, iteractionTargetHint.position);
                    iteractionTargetHintLine.SetPosition(1, selectedOption.KeyHintTransform.position);

                    return;
                }
            }
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

            var scrollPos = 1F - selectedIndex / (float) (interactionOptions.Count - 1);

            if (scrollRect != null)
            {
                scrollRect.verticalScrollbar.value = scrollPos;
            }

            for (int i = 0; i < interactionOptions.Count; i++)
            {
                interactionOptions[i].SetSelected(i == selIndex);
            }

            UpdateTargetHintVisibility();
        }
    }
}