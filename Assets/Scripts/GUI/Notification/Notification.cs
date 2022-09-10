using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    public class Notification : MonoBehaviour
    {
        public enum Type {
            Notification,
            Success,
            Warning,
            Error
        }

        private Animator anim;
        private int numeralID, selfPos;
        private float timeLeft = float.MaxValue;

        public void SetInfo(int id, int pos, string text, float duration)
        {
            numeralID = id;
            name = "Notification " + id;

            selfPos = pos;
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
            EventManager.Instance.Broadcast<NotificationExpireEvent>(new(numeralID, selfPos));
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
                anim.SetBool("Expired", true);
            }
        }

    }
}