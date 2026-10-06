using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge.Illustrator.Core
{
    // UTF-16 length is conservative for providers counting Unicode characters.
    // Operates once per request; no model calls, game objects or persistent state.
    internal static class ImagePromptBudget
    {
        internal const int MaximumCharacters = 32000;
        private static readonly Regex Headers = new Regex(@"【([^】\r\n]{1,100})】|(?m:^\[[^\]\r\n]{1,100}\])",
            RegexOptions.CultureInvariant);
        private static readonly HashSet<string> DirectorSections = new HashSet<string>(StringComparer.Ordinal)
        {
            "人物与镜头", "人物镜头", "角色与镜头", "人物与构图",
            "场景空间", "场景环境", "环境空间", "空间与环境",
            "光线与色彩", "光影与色彩", "光线与光影", "光照与色彩", "光影色彩",
            "空间关系", "空间透视", "透视与空间", "透视关系", "空间与景深"
        };

        private sealed class Part
        {
            internal string Text;
            internal bool Flexible;
            internal int Floor => Math.Min(256, Text.Length);
        }

        internal static string FitMain(string main, int reservedCharacters, int limit)
        {
            main = main ?? string.Empty;
            if (limit <= 0 || limit > MaximumCharacters) limit = MaximumCharacters;
            if (reservedCharacters < 0) throw new ArgumentOutOfRangeException(nameof(reservedCharacters));
            if ((long)main.Length + reservedCharacters <= limit) return main;
            var parts = Split(main);
            long fixedSize = reservedCharacters;
            int maximum = 0;
            foreach (var part in parts)
            {
                if (!part.Flexible) fixedSize += part.Text.Length;
                else maximum = Math.Max(maximum, part.Text.Length);
            }
            long minimum = fixedSize;
            foreach (var part in parts) if (part.Flexible) minimum += part.Floor;
            if (minimum > limit || maximum == 0) throw TooLong(limit);

            // Water-fill the director sections so the last scene/light/spatial
            // sections cannot disappear behind a very long first section.
            int low = 0, high = maximum;
            while (low < high)
            {
                int cap = low + (high - low + 1) / 2;
                long size = fixedSize;
                foreach (var part in parts)
                    if (part.Flexible) size += Math.Max(part.Floor, Math.Min(part.Text.Length, cap));
                if (size <= limit) low = cap;
                else high = cap - 1;
            }
            var result = new StringBuilder(Math.Max(0, limit - reservedCharacters));
            foreach (var part in parts)
            {
                int quota = part.Flexible ? Math.Max(part.Floor, Math.Min(part.Text.Length, low)) : part.Text.Length;
                result.Append(part.Text.Length <= quota ? part.Text : CompleteSentences(part.Text, quota, limit));
            }
            var fitted = result.ToString();
            EnsureFits((long)fitted.Length + reservedCharacters, limit);
            return fitted;
        }

        internal static void EnsureFits(long length, int limit)
        {
            if (length > limit) throw TooLong(limit);
        }

        private static List<Part> Split(string text)
        {
            var result = new List<Part>();
            var matches = Headers.Matches(text);
            int position = 0;
            bool authoritative = false;
            for (int index = 0; index < matches.Count; index++)
            {
                Match header = matches[index];
                if (header.Index > position)
                    result.Add(new Part { Text = text.Substring(position, header.Index - position) });
                result.Add(new Part { Text = header.Value });
                string name = header.Groups[1].Value;
                if (name == "不可改写的核心事实" || name == "画面呈现规范") authoritative = true;
                int end = index + 1 < matches.Count ? matches[index + 1].Index : text.Length;
                result.Add(new Part { Text = text.Substring(header.Index + header.Length, end - header.Index - header.Length),
                    Flexible = !authoritative && DirectorSections.Contains(name) });
                position = end;
            }
            if (position < text.Length) result.Add(new Part { Text = text.Substring(position) });
            return result;
        }

        private static string CompleteSentences(string text, int quota, int limit)
        {
            // Reserve one newline to keep the following heading distinct. No
            // cuts inside a sentence, reference label, number or surrogate pair.
            for (int i = Math.Min(text.Length - 1, quota - 2); i >= 63; i--)
            {
                char c = text[i];
                bool boundary = c == '。' || c == '！' || c == '？' || c == '；'
                    || c == '\n' || ((c == '.' || c == '!' || c == '?' || c == ';')
                        && i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]));
                if (boundary) return text.Substring(0, i + 1) + "\n";
            }
            throw TooLong(limit);
        }

        private static InvalidDataException TooLong(int limit) => new InvalidDataException(
            "生图提示词超过" + limit + "字符预算，无法在保留事实、参考图约束和完整段落的前提下缩短；请求未发送。请缩短自定义画风/要求或减少本次素材。");
    }
}
