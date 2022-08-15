using System;

namespace MinecraftClient.Event
{
    public interface IEventRegistrations { }

    public class EventRegistrations<T> : IEventRegistrations
    {
        public Action<T> actions = obj => { };
    }
}
