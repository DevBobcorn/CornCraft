using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using TMPro;

using CraftSharp.Event;
using CraftSharp.Protocol.Handlers;
using CraftSharp.Protocol.Handlers.PacketPalettes;
using CraftSharp.Protocol.ProtoDef;
using System.Text;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class PacketScreen : BaseScreen
    {
        private const int MAX_PACKET_COUNT = 20;

        private static readonly JsonSerializerSettings SERIALIZER_SETTINGS = new()
        {
            Formatting = Formatting.Indented,
        };

        [SerializeField] private GameObject packetItemPrefab;
        [SerializeField] private RectTransform packetListTransform;

        [SerializeField] private Button recordButton;
        [SerializeField] private Button clearButton;

        [SerializeField] private TMP_Text BytesPreviewText;
        [SerializeField] private TMP_Text ParsedPreviewText;
        [SerializeField] private TMP_Text InfoText;

        private ObjectPool<PacketItem> packetItemPool;
        private readonly Queue<PacketItem> displayedPackItems = new();

        private PacketTypePalette packetPalette;

        // UI controls and objects
        [SerializeField] private Animator screenAnimator;

        private PacketItem inspectedPacketItem;

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

        private void CloseScreen()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;

            client.ScreenControl.TryPopScreen();
        }

#nullable enable
        private Action<InGamePacketEvent>? packetCallback;
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
                ProtocolSettings.CapturePackets = !ProtocolSettings.CapturePackets;
                recordButton.GetComponentInChildren<TMP_Text>().text = ProtocolSettings.CapturePackets ? "\u25a0" : "\u25b6";
            });
            clearButton.onClick.AddListener(() =>
            {
                foreach (var packetItem in displayedPackItems) // Recycle all of them
                {
                    packetItemPool.Release(packetItem);
                }

                ClearPacketPreview();
                displayedPackItems.Clear();
            });

            // Clear on start
            ClearPacketPreview();
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

        private void RecordNewPacketItem(InGamePacketEvent packetEvent)
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
            packetItem.SetInfo(packetPalette, packetEvent.InBound, packetEvent.PacketId, packetEvent.Bytes, InspectPacketItem);

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

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 3); // More than 2 times bytes count, use 3 times

            int count = 0;

            foreach (byte b in ba)
            {
                if (count % 16 == 0)
                {
                    hex.AppendFormat("[{0:X2}] ", count / 16);
                }

                hex.AppendFormat("{0:X2} ", b);

                count++;

                if (count % 8 == 0)
                {
                    if (count % 16 == 0)
                    {
                        hex.AppendLine();
                    }
                    else
                    {
                        hex.Append("  ");
                    }
                }
            }

            return hex.ToString();
        }

        private void InspectPacketItem(PacketItem packetItem)
        {
            if (inspectedPacketItem != null)
            {
                inspectedPacketItem.DeselectPacket();
            }

            // Update selection
            inspectedPacketItem = packetItem;

            var parserProtocol = CornApp.Instance.ParserProtocol;

            // Update packet bytes preview
            BytesPreviewText.text = ByteArrayToString(packetItem.PacketBytes);

            // Update parsed packet preview & info text
            var typeId = new ResourceLocation(packetItem.InBound ? "play/toClient" : "play/toServer", "packet");
            PacketDefTypeHandlerBase.TryGetLoadedHandler(typeId, out PacketDefTypeHandlerBase packetHandler);

            if (packetHandler is not null)
            {
                try
                {
                    var byteQueue = new Queue<byte>(packetItem.PacketBytes.Length + 1);

                    // Add the packet num id back to the bytes as varint
                    int pInt = packetItem.PacketNumId;
                    while ((pInt & -128) != 0)
                    {
                        byteQueue.Enqueue((byte) (pInt & 127 | 128));
                        pInt = (int) (((uint) pInt) >> 7);
                    }
                    byteQueue.Enqueue((byte) pInt);

                    // And append all packet content bytes
                    for (int i = 0; i < packetItem.PacketBytes.Length; i++)
                    {
                        byteQueue.Enqueue(packetItem.PacketBytes[i]);
                    }

                    // Use the handler to read the packet
                    var packetValue = packetHandler.ReadValue(new PacketRecord(parserProtocol), string.Empty, byteQueue);

                    // Serialize the read object as json string
                    var serialized = JsonConvert.SerializeObject(packetValue, SERIALIZER_SETTINGS);

                    ParsedPreviewText.text = serialized;
                    InfoText.text = $"Parser protocol version: {parserProtocol}";
                }
                catch (Exception ex)
                {
                    ParsedPreviewText.text = "XwX";
                    InfoText.text = ex.Message;
                }
            }
            else
            {
                ParsedPreviewText.text = "This packet is not supported by packet inspector.";
                var dir = packetItem.InBound ? "[C2S]" : "[S2C]";
                InfoText.text = $"Parser protocol version: {parserProtocol}\nPacket {dir} [0x{packetItem.PacketNumId:X2}] is not supported.";
            }
        }

        private void ClearPacketPreview()
        {
            if (inspectedPacketItem != null)
            {
                inspectedPacketItem.DeselectPacket();

                inspectedPacketItem = null;
            }

            BytesPreviewText.text = "UwU";
            ParsedPreviewText.text = "OwO";

            InfoText.text = "Select a packet to preview.";
        }

        public override void UpdateScreen()
        {
            // Escape key cannot be used here, otherwise it will push pause screen back after poping it
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseScreen();
            }
        }
    }
}
