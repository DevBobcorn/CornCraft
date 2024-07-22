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

        private Animator animator;
        private int interactionKey;
        public int InteractionId => interactionKey;

        public InteractionInfo interactionInfo;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetInfo(int id, InteractionInfo info)
        {
            interactionKey = id;
            interactionInfo = info;

            var paramTexts = info.GetParamTexts();
            var hintText = Translations.Get(info.GetHintKey(), paramTexts);

            GetComponentInChildren<TMP_Text>().text = hintText;
            gameObject.name = info.GetHintKey();
        }

        public void SetSelected(bool selected)
        {
            // Set selected param in animator
            animator.SetBool(SELECTED, selected);
        }

        public void Remove()
        {
            // Play fade away animation...
            animator.SetBool(EXPIRED, true);
        }

        public void Execute(BaseCornClient client)
        {
            animator.SetTrigger(EXECUTED); // Execution visual feedback

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