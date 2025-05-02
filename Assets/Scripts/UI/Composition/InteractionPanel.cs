using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private LineRenderer interactionTargetHintLine;
        [SerializeField] private RectTransform interactionTargetHint;
        [SerializeField] private Animator interactionTargetAnimator;

        private Animator scrollHint;
        private ScrollRect scrollRect;

        private readonly List<InteractionOption> interactionOptions = new();

        private int selectedIndex = 0;
        private bool targetHintVisible = false;

        public bool ShouldConsumeMouseScroll => interactionOptions.Count > 1;

        #nullable enable

        private Action<InteractionAddEvent>? addCallback;
        private Action<InteractionRemoveEvent>? removeCallback;
        private Action<HarvestInteractionUpdateEvent>? harvestInteractionUpdateCallback;
        private Action<TargetBlockLocUpdateEvent>? targetBlockLocChangeEvent;

        public delegate void ItemCountEventHandler(int newCount);
        public event ItemCountEventHandler? OnItemCountChange;

        #nullable disable

        private void Start()
        {
            // Initialize controls
            scrollHint = transform.Find("Scroll Hint").GetComponent<Animator>();

            scrollRect = GetComponent<ScrollRect>();
            scrollHint.SetBool(SHOW_HASH, false);

            // Events
            addCallback = e =>
            {
                if (e.Info is BlockTriggerInteractionInfo triggerInfo && !triggerInfo.Definition.ShowInList)
                {
                    return;
                }

                AddInteractionOption(e.InteractionId, e.AddAndSelect, e.UseProgress, e.Info);
            };

            removeCallback = e =>
            {
                RemoveInteractionOption(e.InteractionId);
            };

            harvestInteractionUpdateCallback = e =>
            {
                var curValue = Mathf.Clamp01(e.Progress);

                foreach (var t in interactionOptions
                         .Where(t => t.InteractionId == e.InteractionId))
                {
                    t.UpdateInfoText();

                    if (t is InteractionProgressOption progressOption)
                    {
                        progressOption.UpdateProgress(curValue);
                    }

                    break;
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

        public void UpdateItemIconsAndTargetHintVisibility(bool visible)
        {
            foreach (var option in interactionOptions)
            {
                option.UpdateItemIconVisibility(visible);
            }
            
            if (!visible) // Make sure target hint is hidden
                UpdateTargetHintVisibility(false);
        }

        private void UpdateTargetHintVisibility(bool visible)
        {
            interactionTargetAnimator.ResetTrigger(SHOW_HASH);
            interactionTargetAnimator.ResetTrigger(HIDE_HASH);
            interactionTargetAnimator.SetTrigger(visible ? SHOW_HASH : HIDE_HASH);
            
            targetHintVisible = visible;
        }

        private void OnDestroy()
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
            var option = !optionObj ? null : optionObj.GetComponent<InteractionOption>();

            if (option)
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

                option.UpdateKeyHintText(useProgress ? "keybinding.mouse.lmb" : "X");

                scrollHint.SetBool(SHOW_HASH, interactionOptions.Count > 1); // Show or hide scroll hint
            }
            else
            {
                Debug.LogWarning("Interaction option prefab is not valid");

                if (optionObj)
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
                UpdateTargetHintVisibility(false);
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
                    var worldPoint = CoordConvert.MC2Unity(originOffset, blockLoc.ToLocation());

                    // Update interaction hint position
                    var offsetType = ResourcePackManager.Instance.StateModelTable[blockInfo.Block.StateId].OffsetType;
                    worldPoint += (Vector3) ChunkRenderBuilder.GetBlockOffsetInBlock(offsetType,
                        blockLoc.X >> 4, blockLoc.Z >> 4, blockLoc.X & 0xF, blockLoc.Z & 0xF) + Vector3.one * 0.5F;

                    if (IsWorldPointInViewport(camControl.RenderCamera, worldPoint)) // Inside viewport
                    {
                        var newPos = uiCamera.ViewportToWorldPoint(camControl.GetPointViewportPos(worldPoint));

                        // Don't modify z coordinate
                        interactionTargetHint.position = new Vector3(newPos.x, newPos.y, interactionTargetHint.position.z);
                        interactionTargetHintLine.SetPosition(0, interactionTargetHint.position);
                        interactionTargetHintLine.SetPosition(1, selectedOption.KeyHintTransform.position);

                        if (!targetHintVisible) UpdateTargetHintVisibility(true);
                    }
                    else
                    {
                        if (targetHintVisible) UpdateTargetHintVisibility(false);
                    }
                }
            }
        }

        private static bool IsWorldPointInViewport(Camera camera, Vector3 worldPoint)
        {
            if (!camera)
            {
                Debug.LogError("Camera reference is null in IsWorldPointInViewport!");
                return false;
            }

            // Convert world point to viewport point
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPoint);

            // Check if the x and y coordinates are within the 0-1 range
            bool isInHorizontalView = viewportPoint.x is >= 0 and <= 1;
            bool isInVerticalView = viewportPoint.y is >= 0 and <= 1;

            // Check if the z coordinate is positive (in front of the camera)
            // Note: Points exactly *on* the near clip plane might still render,
            // but points behind the camera plane (z <= 0) are definitely not visible
            // from that camera's perspective in a standard projection.
            bool isForward = viewportPoint.z > 0;

            // The point is in the viewport if it's within the horizontal and vertical
            // bounds AND in front of the camera.
            return isInHorizontalView && isInVerticalView && isForward;
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

            if (scrollRect)
            {
                scrollRect.verticalScrollbar.value = scrollPos;
            }

            for (int i = 0; i < interactionOptions.Count; i++)
            {
                interactionOptions[i].SetSelected(i == selIndex);
            }
        }
    }
}