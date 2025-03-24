#nullable enable
using System;
using UnityEngine;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    public class NotificationsControl : MonoBehaviour
    {
        [SerializeField] private GameObject? notificationPrefab;
        [SerializeField] private RectTransform? container;
        [SerializeField] private Sprite? notify, success, warning, error;

        private int nextNumeralID = 1;

        private Action<NotificationEvent>? showCallback;

        private void Start()
        {
            showCallback = (e) => {
                // Make a new notification here...
                var notificationObj = GameObject.Instantiate(notificationPrefab);
                notificationObj!.transform.SetParent(container, false);
                notificationObj!.transform.localScale = Vector3.one;
                
                Notification notification = notificationObj.GetComponent<Notification>();
                notification.SetInfo(nextNumeralID, e.Text, e.Duration);

                var image = e.Type switch {
                    Notification.Type.Success => success,
                    Notification.Type.Warning => warning,
                    Notification.Type.Error   => error,
                    Notification.Type.Notify  => notify,

                    _                         => notify
                };

                notification.SetImage(image!);
                
                nextNumeralID++;
            };

            EventManager.Instance.Register(showCallback);
        }

        private void OnDestroy()
        {
            if (showCallback is not null)
                EventManager.Instance.Unregister(showCallback);

        }
    }
}