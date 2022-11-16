#nullable enable
using System;
using UnityEngine;
using TMPro;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (Animator))]
    public class InteractionOption : MonoBehaviour
    {
        private static readonly int SELECTED = Animator.StringToHash("Selected");
        private static readonly int EXPIRED  = Animator.StringToHash("Expired");

        private Animator? anim;
        private int numeralID;
        public int NumeralID => numeralID;

        public Action<InteractionPanel, InteractionOption>? InteractionAction;
        public string Name = string.Empty;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void SetInfo(int id, string name, Action<InteractionPanel, InteractionOption>? action)
        {
            numeralID = id;
            Name = name;
            InteractionAction = action;

            GetComponentInChildren<TMP_Text>().text = name;

            name = $"Interaction Option #{id} [{name}]";
        }

        public void SetSelected(bool selected)
        {
            // Set selected param in animator
            anim?.SetBool(SELECTED, selected);
        }

        public void Remove()
        {
            // Play fade away animation...
            anim?.SetBool(EXPIRED, true);
        }

        public void Execute(InteractionPanel panel, InteractionOption option)
        {
            InteractionAction?.Invoke(panel, option);
        }

        // Called by animator after hide animation ends...
        void Expire()
        {
            EventManager.Instance.Broadcast<NotificationExpireEvent>(new(numeralID));
            Destroy(this.gameObject);
        }

    }
}