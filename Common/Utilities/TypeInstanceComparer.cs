namespace Common.Utilities
{
    public static class TypeInstanceComparer
    {
        public static bool IsEqualTo(this object instance, object otherInstance, bool countNull = true)
        {
            if (instance is null && otherInstance is null)
                return countNull;

            if ((instance is null && otherInstance != null) || (instance != null && otherInstance is null))
                return countNull;

            return instance == otherInstance;
        }
    }
}