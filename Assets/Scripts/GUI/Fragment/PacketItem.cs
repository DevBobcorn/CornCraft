using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Protocol.Handlers.PacketPalettes;

namespace CraftSharp.UI
{
    public class PacketItem : MonoBehaviour
    {
        private bool inBound = false;
        private int packetId = -1;

        [SerializeField] private Color32 inBoundColor = Color.white;
        [SerializeField] private Color32 outBoundColor = Color.cyan;

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text packetText;

        public void SetInfo(PacketTypePalette palette, bool inBound, int packetId, byte[] bytes)
        {
            this.inBound = inBound;
            this.packetId = packetId;

            // Update color
            background.color = this.inBound ? inBoundColor : outBoundColor;

            var packetName = this.inBound ? palette.GetIncomingTypeById(packetId).ToString()
                    : palette.GetOutgoingTypeById(packetId).ToString();

            // Update text
            packetText.text = (this.inBound ? "[S->C]" : "[C->S]") + $" [0x{packetId:X2}] {packetName}\n{bytes.Length}B";
        }
    }
}
