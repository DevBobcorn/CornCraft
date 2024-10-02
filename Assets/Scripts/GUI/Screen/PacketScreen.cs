using CraftSharp.Event;
using CraftSharp.Protocol.Handlers;
using CraftSharp.Protocol.Handlers.PacketPalettes;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class PacketScreen : BaseScreen
    {
        private const int MAX_PACKET_COUNT = 20;

        [SerializeField] private GameObject packetItemPrefab;
        [SerializeField] private RectTransform packetListTransform;

        [SerializeField] private Button recordButton;
        [SerializeField] private Button clearButton;

        private ObjectPool<PacketItem> packetItemPool;
        private readonly Queue<PacketItem> displayedPackItems = new();

        private PacketTypePalette packetPalette;

        // UI controls and objects
        [SerializeField] private Animator screenAnimator;

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator.SetBool(SHOW_HASH, isActive);
            }

            get {
                return isActive;
            }
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseInput()
        {
            return true;
        }

        public void Back2Game()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;

            client.ScreenControl.TryPopScreen();
        }

#nullable enable
        private Action<PacketEvent>? packetCallback;
#nullable disable

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            packetItemPool = new(CreateNewPacketItem, null, OnReleasePacketItem, null, false, 25);

            packetCallback = RecordNewPacketItem;
            EventManager.Instance.Register(packetCallback);

            // Prepare buttons
            recordButton.onClick.AddListener(() =>
            {
                CornGlobal.CapturePackets = !CornGlobal.CapturePackets;
                recordButton.GetComponentInChildren<TMP_Text>().text = CornGlobal.CapturePackets ? "\u25a0" : "\u25b6";
            });
            clearButton.onClick.AddListener(() =>
            {
                foreach (var packetItem in displayedPackItems) // Recycle all of them
                {
                    packetItemPool.Release(packetItem);
                }

                displayedPackItems.Clear();
            });
        }

        void OnDestroy()
        {
            if (packetCallback is not null)
            {
                EventManager.Instance.Unregister(packetCallback);
            }
        }

        private PacketItem CreateNewPacketItem()
        {
            var packetItemObj = GameObject.Instantiate(packetItemPrefab);
            packetItemObj.transform.SetParent(packetListTransform, false);

            return packetItemObj.GetComponent<PacketItem>();
        }

        private void OnReleasePacketItem(PacketItem packetItem)
        {
            // Hide
            packetItem.gameObject.SetActive(false);
        }

        private void RecordNewPacketItem(PacketEvent packetEvent)
        {
            if (!isActive)
            {
                return;
            }

            if (packetPalette is null)
            {
                var client = CornApp.CurrentClient;
                var protocolVersion = client.GetProtocolVersion();

                Debug.Log($"Protocol version: {protocolVersion}");

                // Create packet palette for interpreting packets
                packetPalette = new PacketTypeHandler(protocolVersion, false).GetTypeHandler();
            }

            var packetItem = packetItemPool.Get();

            // Assign packet information
            packetItem.SetInfo(packetPalette, packetEvent.InBound, packetEvent.PacketId, packetEvent.Bytes);

            // Move to end of the list and show
            packetItem.gameObject.SetActive(true);
            packetItem.transform.SetAsLastSibling();

            // Add to displayed queue
            displayedPackItems.Enqueue(packetItem);

            if (displayedPackItems.Count > MAX_PACKET_COUNT) // Too many displayed items, recycle one
            {
                var recycled = displayedPackItems.Dequeue();

                packetItemPool.Release(recycled);
            }
        }

        public override void UpdateScreen()
        {
            // Escape key cannot be used here, otherwise it will push pause screen back after poping it
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Back2Game();
            }
        }
    }
}
