using MinecraftClient.UI;

namespace MinecraftClient.Event
{
    public class NotificationEvent : BaseEvent
    {
        public readonly string text;
        public readonly float duration;
        public readonly Notification.Type type;

        public NotificationEvent(string text)
        {
            this.text = text;
            this.duration = 1.4F; // 1.4 seconds by default
            this.type = Notification.Type.Notification;
        }

        public NotificationEvent(string text, float duration, Notification.Type type)
        {
            this.text = text;
            this.duration = duration;
            this.type = type;
        }
    }
}