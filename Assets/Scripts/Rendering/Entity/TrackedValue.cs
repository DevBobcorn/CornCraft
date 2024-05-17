namespace CraftSharp.Rendering
{
    public class TrackedValue<T>
    {
        private T _value;

        public delegate void ValueUpdateHandler(T prevValue, T newValue);
        public event ValueUpdateHandler OnValueUpdate;

        /// <summary>
        /// Whether update event should be called if new
        /// value is the same as the old one
        /// </summary>
        private readonly bool updateIfValueUnchanged;

        public T Value
        {
            get => _value;
            set
            {
                if (_value == null) // Old value is null
                {
                    if (updateIfValueUnchanged || value != null)
                    {
                        OnValueUpdate?.Invoke(_value, value);

                        _value = value;
                    }
                }
                else if (updateIfValueUnchanged || !_value.Equals(value)) // The new value is different from the old one
                {
                    OnValueUpdate?.Invoke(_value, value);

                    _value = value;
                }
            }
        }

        public TrackedValue(T defaultValue, bool updateIfValueUnchanged = false)
        {
            this.updateIfValueUnchanged = updateIfValueUnchanged;
            this._value = defaultValue;
        }
    }
}