using System;
using System.Collections.Generic;

namespace MinecraftClient.Event
{
    // Singleton Event Manager
    public class EventManager
    {
        private static EventManager instance;

        public static EventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EventManager();
                }
                return instance;
            }
        }
        
        private Dictionary<Type, IEventRegistrations> eventTable = new Dictionary<Type, IEventRegistrations>();

        public void Register<T>(Action<T> callback)
        {
            var t = typeof (T);

            if (!eventTable.ContainsKey(t))
            {
                var registrations = new EventRegistrations<T>();
                registrations.actions += callback;
                eventTable.Add(t, registrations);
            }
            else
            {
                var registrations = eventTable[t] as EventRegistrations<T>;
                registrations.actions += callback;
            }
        }

        public void Unregister<T>(Action<T> callback)
        {
            var t = typeof (T);

            if (eventTable.ContainsKey(t) && eventTable[t] != null)
            {
                var registrations = eventTable[t] as EventRegistrations<T>;
                if (registrations.actions != null)
                {
                    registrations.actions -= callback;
                    return;
                }
            }
            throw new Exception("Failed to remove listener");
        }

        public void Broadcast<T>(T message)
        {
            Type t = typeof (T);

            if (eventTable.ContainsKey(t) && eventTable[t] != null)
            {
                var registrations = eventTable[t] as EventRegistrations<T>;

                registrations.actions?.Invoke(message);
            }
        }

        public void BroadcastOnUnityThread<T>(T message)
        {
            Type t = typeof (T);

            if (eventTable.ContainsKey(t) && eventTable[t] != null)
            {
                var registrations = eventTable[t] as EventRegistrations<T>;

                Loom.QueueOnMainThread(() => registrations.actions?.Invoke(message));
            }
        }
    }

}
