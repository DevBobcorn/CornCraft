using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Protocol.Handlers.PacketPalettes;

namespace CraftSharp.UI
{
    public class PacketItem : MonoBehaviour
    {
        public bool InBound { get; private set; } = false;
        public int PacketNumId { get; private set; } = -1;

        public byte[] PacketBytes;

        [SerializeField] private Color32 inBoundColor = Color.white;
        [SerializeField] private Color32 outBoundColor = Color.cyan;
        [SerializeField] private Color32 inspectedColor = Color.red;

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text packetText;

        private Action<PacketItem> selectedCallback = null;

        public void SetInfo(PacketTypePalette palette, bool inBound, int packetNumId, byte[] bytes, Action<PacketItem> selectedCallback)
        {
            InBound = inBound;
            PacketNumId = packetNumId;

            PacketBytes = bytes;

            // Update color
            background.color = InBound ? inBoundColor : outBoundColor;

            var packetName = InBound ? palette.GetIncomingTypeById(packetNumId).ToString()
                    : palette.GetOutgoingTypeById(packetNumId).ToString();

            // Update text
            packetText.text = (InBound ? "[S->C]" : "[C->S]") + $" [0x{packetNumId:X2}] {packetName}\n{bytes.Length}B";

            this.selectedCallback = selectedCallback;
        }

        public void SelectPacket()
        {
            selectedCallback?.Invoke(this);

            // Update color
            background.color = inspectedColor;
        }

        public void DeselectPacket()
        {
            // Update color
            background.color = InBound ? inBoundColor : outBoundColor;
        }
    }
}
