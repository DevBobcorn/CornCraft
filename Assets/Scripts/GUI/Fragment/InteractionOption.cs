#nullable enable
using UnityEngine;
using TMPro;

using CraftSharp.Event;
using CraftSharp.Control;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class InteractionOption : MonoBehaviour
    {
        private static readonly int SELECTED = Animator.StringToHash("Selected");
        private static readonly int EXPIRED  = Animator.StringToHash("Expired");
        private static readonly int EXECUTED = Animator.StringToHash("Executed");

        private Animator? anim;
        private int interactionKey;
        public int InteractionId => interactionKey;

        public InteractionInfo? interactionInfo;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void SetInfo(int id, InteractionInfo info)
        {
            interactionKey = id;
            interactionInfo = info;

            GetComponentInChildren<TMP_Text>().text = info.GetHint();
            gameObject.name = info.GetHint();
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

        public void Execute(BaseCornClient? client)
        {
            anim?.SetTrigger(EXECUTED); // Execution visual feedback

            if (client == null) return;
            interactionInfo?.RunInteraction(client);
        }

        // Called by animator after hide animation ends...
        void Expire()
        {
            EventManager.Instance.Broadcast<NotificationExpireEvent>(new(interactionKey));
            Destroy(this.gameObject);
        }

    }
}