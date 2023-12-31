namespace Networking.Data
{
    public struct NetworkString
    {
        public readonly bool isEmpty;
        public readonly bool isNull;
        public readonly bool isNullOrEmpty;

        public readonly string value;

        public NetworkString(bool isEmpty, bool isNull, string value)
        {
            this.isEmpty = isEmpty;
            this.isNull = isNull;
            this.isNullOrEmpty = isEmpty || isNull;
            this.value = value;
        }

        public string GetValue(string defaultValue = "")
        {
            if (isNullOrEmpty)
                return defaultValue;

            return value;
        }
    }
}