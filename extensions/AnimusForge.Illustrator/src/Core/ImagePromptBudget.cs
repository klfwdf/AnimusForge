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
        private static readonly Regex Headers = new Regex(@"(?m:^\[\[(?<protocol>AFI_PROTOCOL_(?:SUBJECT|SCENE|LIGHT|SPATIAL))\]\])|【(?<name>[^】\r\n]{1,100})】|(?m:^\[[^\]\r\n]{1,100}\])",
            RegexOptions.CultureInvariant);
        private static readonly HashSet<string> DirectorSections = new HashSet<string>(StringComparer.Ordinal)
        {
            IllustratorText.Raw("Residual_ImagePromptBudget_9d74adf88df4"), IllustratorText.Raw("Residual_ImagePromptBudget_2a8ae329881c"), IllustratorText.Raw("Residual_ImagePromptBudget_3733c837db22"), IllustratorText.Raw("Residual_ImagePromptBudget_067a21c2cea5"),
            IllustratorText.Raw("Residual_ImagePromptBudget_6b05b94fef37"), IllustratorText.Raw("Residual_ImagePromptBudget_fe33ab426448"), IllustratorText.Raw("Residual_ImagePromptBudget_b31fac91d9ec"), IllustratorText.Raw("Residual_ImagePromptBudget_7f003b063fac"),
            IllustratorText.Raw("Residual_ImagePromptBudget_c03fde28bf42"), IllustratorText.Raw("Residual_ImagePromptBudget_7e5c54d063ea"), IllustratorText.Raw("Residual_ImagePromptBudget_a8e4d458fea9"), IllustratorText.Raw("Residual_ImagePromptBudget_8024c01bbd87"), IllustratorText.Raw("Residual_ImagePromptBudget_5f5cece52e49"),
            IllustratorText.Raw("Residual_ImagePromptBudget_59419c80e872"), IllustratorText.Raw("Residual_ImagePromptBudget_7648a5db94f6"), IllustratorText.Raw("Residual_ImagePromptBudget_2aaf40f10949"), IllustratorText.Raw("Residual_ImagePromptBudget_c4719f13a055"), IllustratorText.Raw("Residual_ImagePromptBudget_2941ee1c6fd2")
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
                string name = header.Groups["name"].Value;
                if (name == IllustratorText.Raw("Residual_ImagePromptBudget_5960cf16ee62") || name == IllustratorText.Raw("Residual_ImagePromptBudget_780da068d119")) authoritative = true;
                int end = index + 1 < matches.Count ? matches[index + 1].Index : text.Length;
                result.Add(new Part { Text = text.Substring(header.Index + header.Length, end - header.Index - header.Length),
                    Flexible = !authoritative && (header.Groups["protocol"].Success || DirectorSections.Contains(name)) });
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
            IllustratorText.Raw("Residual_ImagePromptBudget_ebbc6f54b09c") + limit + IllustratorText.Raw("Residual_ImagePromptBudget_624ff0057c84"));
    }
}
