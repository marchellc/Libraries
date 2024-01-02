﻿using Common.Pooling.Pools;
using Common.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.Extensions
{
    public static class StringExtensions
    {
        public static readonly char[] MultiLine = ['\r', '\n', '\x85', '\x2028', '\x2029'];

        public static bool TryPeekIndex(this string str, int index, out char value)
        {
            if (index >= str.Length)
            {
                value = default;
                return false;
            }

            value = str[index];
            return true;
        }

        public static string[] SplitLines(this string line)
            => Regex.Split(line, RegexUtils.NewLinesRegexString);

        public static bool HasHtmlTags(this string text, out IList<int> openIndexes, out IList<int> closeIndexes)
        {
            openIndexes = Regex.Matches(text, "<").Cast<Match>().Select(m => m.Index).ToList();
            closeIndexes = Regex.Matches(text, ">").Cast<Match>().Select(m => m.Index).ToList();

            return openIndexes.Any() || closeIndexes.Any();
        }

        public static string RemoveHtmlTags(this string text, IList<int> openTagIndexes = null, IList<int> closeTagIndexes = null)
        {
            openTagIndexes ??= Regex.Matches(text, "<").Cast<Match>().Select(m => m.Index).ToList();
            closeTagIndexes ??= Regex.Matches(text, ">").Cast<Match>().Select(m => m.Index).ToList();

            if (closeTagIndexes.Count > 0)
            {
                var sb = StringBuilderPool.Shared.Next();
                var previousIndex = 0;

                foreach (int closeTagIndex in closeTagIndexes)
                {
                    var openTagsSubset = openTagIndexes.Where(x => x >= previousIndex && x < closeTagIndex);

                    if (openTagsSubset.Count() > 0 && closeTagIndex - openTagsSubset.Max() > 1)
                        sb.Append(text.Substring(previousIndex, openTagsSubset.Max() - previousIndex));
                    else
                        sb.Append(text.Substring(previousIndex, closeTagIndex - previousIndex + 1));

                    previousIndex = closeTagIndex + 1;
                }

                if (closeTagIndexes.Max() < text.Length)
                    sb.Append(text.Substring(closeTagIndexes.Max() + 1));

                return StringBuilderPool.Shared.StringReturn(sb);
            }
            else
            {
                return text;
            }
        }

        public static string Remove(this string value, IEnumerable<char> toRemove)
        {
            foreach (var c in toRemove)
                value = value.Replace($"{c}", "");

            return value;
        }

        public static string Remove(this string value, IEnumerable<string> toRemove)
        {
            foreach (var c in toRemove)
                value = value.Replace(c, "");

            return value;
        }

        public static string Remove(this string value, params char[] toRemove)
        {
            foreach (var c in toRemove)
                value = value.Replace($"{c}", "");

            return value;
        }

        public static string Remove(this string value, params string[] toRemove)
        {
            foreach (var str in toRemove)
                value = value.Replace(str, "");

            return value;
        }

        public static string ReplaceWithMap(this string value, params KeyValuePair<string, string>[] stringMap)
            => value.ReplaceWithMap(stringMap.ToDictionary());

        public static string ReplaceWithMap(this string value, params KeyValuePair<char, string>[] charMap)
            => value.ReplaceWithMap(charMap.ToDictionary());

        public static string ReplaceWithMap(this string value, params KeyValuePair<char, char>[] charMap)
            => value.ReplaceWithMap(charMap.ToDictionary());

        public static string ReplaceWithMap(this string value, IDictionary<char, string> charMap)
        {
            foreach (var pair in charMap)
                value = value.Replace(pair.Key.ToString(), pair.Value);

            return value;
        }

        public static string ReplaceWithMap(this string value, IDictionary<char, char> charMap)
        {
            foreach (var pair in charMap)
                value = value.Replace(pair.Key, pair.Value);

            return value;
        }

        public static string ReplaceWithMap(this string value, IDictionary<string, string> stringMap)
        {
            foreach (var pair in stringMap)
                value = value.Replace(pair.Key, pair.Value);

            return value;
        }

        public static bool IsSimilar(this string source, string target, double minScore = 0.9)
            => source.GetSimilarity(target) >= minScore;

        public static double GetSimilarity(this string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return 0.0;

            if (source == target)
                return 1.0;

            var stepsToSame = GetLevenshteinDistance(source, target);

            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        public static int GetLevenshteinDistance(this string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return 0;

            if (source == target)
                return source.Length;

            var sourceWordCount = source.Length;
            var targetWordCount = target.Length;

            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            var distance = new int[sourceWordCount + 1, targetWordCount + 1];

            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;
            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }

        public static string FilterWhiteSpaces(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var builder = StringBuilderPool.Shared.Next();

            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (i == 0 || c != ' ' || (c == ' ' && input[i - 1] != ' '))
                    builder.Append(c);
            }

            return StringBuilderPool.Shared.StringReturn(builder);
        }

        public static int GetStableHashCode(this string str)
        {
            var seed = 23;

            for (int i = 0; i < str.Length; i++)
                seed = (seed * 31) + (int)str[i];

            return seed;
        }

        public static bool TrySplit(this string line, char splitChar, bool removeEmptyOrWhitespace, int? length, out string[] splits)
        {
            splits = line.Split(splitChar).Select(str => str.Trim()).ToArray();

            if (removeEmptyOrWhitespace)
                splits = splits.Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();

            if (length.HasValue && splits.Length != length)
                return false;

            return splits.Any();
        }

        public static bool TrySplit(this string line, char[] splitChars, bool removeEmptyOrWhitespace, int? length, out string[] splits)
        {
            splits = line.Split(splitChars).Select(str => str.Trim()).ToArray();

            if (removeEmptyOrWhitespace)
                splits = splits.Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();

            if (length.HasValue && splits.Length != length)
                return false;

            return splits.Any();
        }

        public static bool ParseBool(this string input, bool def = false)
            => input.TryParseBool(out var b) ? b : def;

        public static bool TryParseBool(this string input, out bool result)
        {
            if (bool.TryParse(input, out result))
                return true;

            if (input == "y" || input == "yes" || input == "1")
            {
                result = true;
                return true;
            }

            if (input == "n" || input == "no" || input == "0")
            {
                result = false;
                return true;
            }

            result = false;
            return true;
        }

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
                if (i == 0)
                {
                    newStr += str[i];
                    continue;
                }

                if ((i + 1).IsValidIndex(str.Length) && char.IsLower(str[i + 1]))
                {
                    newStr += $" {str[i]}{str[i + 1]}";
                    i += 1;
                    continue;
                }
            }

            return newStr.Trim();
        }

        public static string SpaceByUpperCase(this string str)
            => Regex.Replace(str, RegexUtils.PascalCaseRegexString, "$1 ", RegexOptions.Compiled);
    }
}
