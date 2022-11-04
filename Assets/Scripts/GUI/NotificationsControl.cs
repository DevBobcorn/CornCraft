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

                        notification.SetInfo(nextNumeralID, i, e.Text, e.Duration);

                        switch (e.Type)
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
                if (e.ExpireIndex >= 0 && e.ExpireIndex < posAvailable.Length)
                {
                    posAvailable[e.ExpireIndex] = true;
                }
            };

            EventManager.Instance.Register(showCallback);
            EventManager.Instance.Register(expireCallback);

        }

        void OnDestroy()
        {
            if (showCallback is not null)
                EventManager.Instance.Unregister(showCallback);

            if (expireCallback is not null)
                EventManager.Instance.Unregister(expireCallback);

        }

    }
}