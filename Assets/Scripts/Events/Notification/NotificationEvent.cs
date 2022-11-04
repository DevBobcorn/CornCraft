using MinecraftClient.UI;

namespace MinecraftClient.Event
{
    public record NotificationEvent : BaseEvent
    {
        public string Text { get; }
        public float Duration { get; }
        public Notification.Type Type { get; }

        public NotificationEvent(string text)
        {
            this.Text = text;
            this.Duration = 6F; // 6 seconds by default
            this.Type = Notification.Type.Notification;
        }

        public NotificationEvent(string text, float duration, Notification.Type type)
        {
            this.Text = text;
            this.Duration = duration;
            this.Type = type;
        }
    }
}