using System;
using UnityEngine;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    public class NotificationsControl : MonoBehaviour
    {
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private RectTransform container;
        [SerializeField] private Material notify, success, warning, error;

        private int nextNumeralID = 1;

        #nullable enable
        
        private Action<NotificationEvent>? showCallback;
        
        #nullable disable

        private void Start()
        {
            showCallback = e =>
            {
                // Make a new notification here...
                var notificationObj = Instantiate(notificationPrefab);
                notificationObj!.transform.SetParent(container, false);
                notificationObj!.transform.localScale = Vector3.one;
                
                Notification notification = notificationObj.GetComponent<Notification>();
                notification.SetInfo(nextNumeralID, e.Text, e.Duration);

                var material = e.Type switch {
                    Notification.Type.Success => success,
                    Notification.Type.Warning => warning,
                    Notification.Type.Error   => error,
                    _                         => notify
                };

                notification.SetMaterial(material);
                
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