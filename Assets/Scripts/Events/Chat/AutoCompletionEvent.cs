namespace MinecraftClient.Event
{
    public class AutoCompletionEvent : BaseEvent
    {
        public static AutoCompletionEvent EMPTY = new(0, 0, new string[0]);

        public int start, length;
        public readonly string[] options;
        
        public AutoCompletionEvent(int start, int length, string[] options)
        {
            this.start   = start;
            this.length  = length;
            this.options = options;
        }

    }
}
