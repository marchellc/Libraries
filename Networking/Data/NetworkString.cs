namespace Networking.Data
{
    public struct NetworkString
    {
        public readonly bool IsEmpty;
        public readonly bool IsNull;
        public readonly bool IsNullOrEmpty;

        public readonly string Value;

        public NetworkString(bool isEmpty, bool isNull, string value)
        {
            IsEmpty = isEmpty;
            IsNull = isNull;
            IsNullOrEmpty = isEmpty || isNull;

            Value = value;
        }

        public string GetValue(string defaultValue = "")
        {
            if (IsNullOrEmpty)
                return defaultValue;

            return Value;
        }
    }
}