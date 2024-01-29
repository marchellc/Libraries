namespace Common.Utilities
{
    public static class HardwareId
    {
        private static string hwId;

        public static string Id
        {
            get
            {
                if (string.IsNullOrWhiteSpace(hwId))
                    hwId = libc.hwid.HwId.Generate();

                return hwId;
            }
        }
    }
}