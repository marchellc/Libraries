using Common.Pooling.Pools;
using Common.Utilities;

using System;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class StringExtensions
    {
        public static readonly char[] MultiLine = ['\r', '\n', '\x85', '\x2028', '\x2029'];

        public static string Compile(this IEnumerable<string> values, string separator = "\n")
            => string.Join(separator, values);

        public static string SubstringPostfix(this string str, int index, int length, string postfix = " ...")
            => str.Substring(index, length) + postfix;

        public static string SubstringPostfix(this string str, int length, string postfix = " ...")
            => str.SubstringPostfix(0, length, postfix);

        public static string GetBeforeIndex(this string str, int index)
        {
            var newStr = "";

            for (int i = 0; i < index; i++)
                newStr += str[i];

            return newStr;
        }

        public static string GetBeforeChar(this string str, char c)
        {
            var index = str.IndexOf(c);

            if (index < 0)
                return str;

            if (index == 0)
                return string.Empty;

            return GetBeforeIndex(str, index);
        }

        public static string[] SplitByLine(this string str)
            => str.Split(MultiLine);

        public static string SnakeCase(this string str)
        {
            if (str is null)
                throw new ArgumentNullException(nameof(str));

            if (str.Length <= 1)
                return str;

            var sb = StringBuilderPool.Shared.Next();

            sb.Append(char.ToLowerInvariant(str[0]));

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                    sb.Append('_').Append(char.ToLowerInvariant(str[i]));
                else
                    sb.Append(str[i]);
            }

            return StringBuilderPool.Shared.StringReturn(sb);
        }

        public static string CamelCase(this string str)
        {
            str = str.Replace("_", "");

            if (str.Length == 0)
                return "null";

            str = RegexUtils.CamelCaseRegex.Replace(str, match => match.Groups[1].Value + match.Groups[2].Value.ToLower() + match.Groups[3].Value);

            return char.ToLower(str[0]) + str.Substring(1);
        }

        public static string PascalCase(this string str)
        {
            str = str.CamelCase();
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static string TitleCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            var words = str.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length <= 0)
                    continue;

                var c = char.ToUpper(words[i][0]);
                var str2 = "";

                if (words[i].Length > 1)
                    str2 = words[i].Substring(1).ToLower();

                words[i] = c + str2;
            }

            return string.Join(" ", words);
        }

        public static string SpaceByLowerCase(this string str)
        {
            var newStr = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsLower(str[i]))
                    newStr += $"{str[i]} ";
                else
                    newStr += str[i];
            }

            return newStr.Trim();
        }

        public static string SpaceByUpperCase(this string str)
        {
            var newStr = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                    newStr += $"{str[i]} ";
                else
                    newStr += str[i];
            }

            return newStr.Trim();
        }
    }
}
