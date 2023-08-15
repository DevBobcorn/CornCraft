namespace CraftSharp.Event
{
    public record AutoCompletionEvent : BaseEvent
    {
        public static AutoCompletionEvent EMPTY = new(0, 0, new string[0]);

        public int Start { get; }
        public int Length { get; }
        public string[] Options { get; }
        
        public AutoCompletionEvent(int start, int length, string[] options)
        {
            this.Start   = start;
            this.Length  = length;
            this.Options = options;
        }

    }
}
