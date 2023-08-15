#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class Notification : MonoBehaviour
    {
        private static readonly int EXPIRED  = Animator.StringToHash("Expired");

        public enum Type {
            Notify,
            Success,
            Warning,
            Error
        }

        private Animator? anim;
        private int numeralID;
        private float timeLeft = float.MaxValue;

        public void SetInfo(int id, string text, float duration)
        {
            numeralID = id;
            name = $"Notification #{id}";

            GetComponentInChildren<TMP_Text>().text = text;

            timeLeft = duration;
        }

        public void SetImage(Sprite image)
        {
            GetComponentInChildren<Image>().sprite = image;
        }

        // Called by animator after hide animation ends...
        void Expire()
        {
            EventManager.Instance.Broadcast<NotificationExpireEvent>(new(numeralID));
            Destroy(this.gameObject);
        }

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        void Update()
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft <= 0F) // Time to go....
            {
                // Play fade away animation...
                anim?.SetBool(EXPIRED, true);
            }
        }

    }
}