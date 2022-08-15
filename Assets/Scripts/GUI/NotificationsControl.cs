using System;
using UnityEngine;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    public class NotificationsControl : MonoBehaviour
    {
        private static int nextNumeralID = 1;
        private static bool[] posAvailable = new bool[1000];
        public GameObject notificationPrefab;
        public Sprite success, warning, error;

        public float startPos = -100F, notificationWidth = 40F;

        private Action<NotificationEvent> showCallback;
        private Action<NotificationExpireEvent> expireCallback;

        void Start()
        {
            for (int i = 0;i < 1000;i++)
            {
                posAvailable[i] = true;
            }

            showCallback = (e) => {
                for (int i = 0;i < 1000;i++)
                {
                    if (posAvailable[i])
                    {
                        // Make a new notification here...
                        GameObject notificationObj = GameObject.Instantiate(notificationPrefab);
                        notificationObj.transform.SetParent(this.transform, false);
                        notificationObj.transform.localPosition = new Vector3(0F, startPos - notificationWidth * i, 0F);

                        Notification notification = notificationObj.GetComponent<Notification>();

                        notification.SetInfo(nextNumeralID, i, e.text, e.duration);

                        switch (e.type)
                        {
                            case Notification.Type.Success:
                                notification.SetImage(success);
                                break;
                            case Notification.Type.Warning:
                                notification.SetImage(warning);
                                break;
                            case Notification.Type.Error:
                                notification.SetImage(error);
                                break;
                        }

                        posAvailable[i] = false;
                        nextNumeralID++;
                        break;
                    }
                }
                // No position available, ignore it...
            };

            expireCallback = (e) => {
                if (e.pos >= 0 && e.pos < posAvailable.Length)
                {
                    posAvailable[e.pos] = true;
                }
            };

            EventManager.Instance.Register<NotificationEvent>(showCallback);
            EventManager.Instance.Register<NotificationExpireEvent>(expireCallback);

        }

        void OnDestroy()
        {
            if (showCallback != null)
                EventManager.Instance.Unregister<NotificationEvent>(showCallback);

            if (expireCallback != null)
                EventManager.Instance.Unregister<NotificationExpireEvent>(expireCallback);
        }

    }
}