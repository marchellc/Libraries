using System.Text.RegularExpressions;

namespace Common.Utilities
{
    public static class RegexUtils
    {
        public static readonly Regex CamelCaseRegex = new Regex("([A-Z])([A-Z]+)($|[A-Z])", RegexOptions.Compiled);
    }
}