using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

// The actual RebuildVisualLines/FindBestLineLength/FindPreferredBreak methods are
// extracted by run.py. This seam replaces only font metrics and native widgets.
partial class LayoutReplay
{
    private sealed class VisualLine
    {
        public int StartIndex, EndIndex;
        public string Text = string.Empty;
    }
    private readonly List<VisualLine> _visualLines = new();
    private string _realText = "";
    private bool _layoutDirty;
    private (float X, float Y) Size;
    private readonly bool _legacy;
    private readonly Func<char, float> _advance;
    public long MeasuredCharacters { get; private set; }
    public long MeasureCalls { get; private set; }
    public int LargestProbe { get; private set; }
    public int LineCount => _visualLines.Count;
    public LayoutReplay(bool legacy, Func<char, float> advance) { _legacy = legacy; _advance = advance; }
    private void UpdateMeasurementFont() { }
    private void SyncLineWidgets() { }
    private float MeasureWidth(string value)
    {
        MeasureCalls++;
        MeasuredCharacters += value.Length;
        LargestProbe = Math.Max(LargestProbe, value.Length);
        float width = 0;
        foreach (char c in value) width += _advance(c);
        return width;
    }
    private int FindBestLineLength(string text, int start, int paragraphEnd, float width) =>
        _legacy ? FindLegacyLineLength(text, start, paragraphEnd, width) : FindCurrentLineLength(text, start, paragraphEnd, width);
    // Exact pre-fix fitter from ba3a47e1a, kept as a comparison oracle.
    private int FindLegacyLineLength(string text, int start, int paragraphEnd, float availableWidth)
    {
        int low = 1;
        int high = Math.Max(1, paragraphEnd - start);
        int best = 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (MeasureWidth(text.Substring(start, mid)) <= availableWidth)
            {
                best = mid;
                low = mid + 1;
            }
            else high = mid - 1;
        }
        return best;
    }
    public int Fit(string value, int start, int end, float width) => FindBestLineLength(value, start, end, width);
    public string[] Layout(string value, float width)
    {
        _realText = value; Size = (width, 0);
        MeasuredCharacters = MeasureCalls = 0; LargestProbe = 0;
        RebuildVisualLines();
        return _visualLines.Select(x => x.StartIndex + ":" + x.EndIndex + ":" + x.Text).ToArray();
    }
}

static class Program
{
    private static int _assertions;
    private static void Check(bool condition, string context)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException(context);
    }
    private static float VariableAdvance(char c) => c switch
    {
        '\u0301' => 0, 'i' => 2, 'l' => 3, ' ' => 4, 'W' => 18, '\t' => 24,
        ',' or '.' or ';' or ':' => 6, _ => c > 127 ? 16 : 8
    };
    private static void SameLayout(string value, float width, Func<char, float> advance, string name)
    {
        var old = new LayoutReplay(true, advance); var current = new LayoutReplay(false, advance);
        Check(old.Layout(value, width).SequenceEqual(current.Layout(value, width)), name + " line text/ranges");
    }
    static void Main(string[] args)
    {
        var metrics = new Func<char, float>[] { _ => 16, VariableAdvance, _ => 0 };
        string[] cases = { "", "\n", "\n\n", "hello", "hello\n", "\nhello", "中文规则，保留标点。\n下一段。", "one two; three: four.\n\nlast", new string('规', 1000), new string('\u0301', 1000), "iW iW iii WWW \u0301\t末尾", new string('\n', 60000) };
        foreach (var advance in metrics)
        foreach (string value in cases)
        foreach (float width in new[] { 0f, 32f, 55f, 880f, 4000f })
            SameLayout(value, width, advance, "boundary");
        foreach (var advance in metrics)
        {
            var old = new LayoutReplay(true, advance); var current = new LayoutReplay(false, advance);
            foreach (int length in new[] { 1, 2, 31, 32, 33, 63, 64, 65, 129, 1001 })
            foreach (float width in new[] { -1f, 0f, 1f, 16f, 31f, 32f, 880f, float.PositiveInfinity, float.NaN })
            {
                string text = "prefix" + string.Concat(Enumerable.Repeat("i规\u0301W ", (length + 4) / 5));
                int actual = current.Fit(text, 6, 6 + length, width);
                Check(actual == old.Fit(text, 6, 6 + length, width), "fit offsets/exponential boundary");
                Check(actual >= 1 && actual <= length, "bounded positive progress");
            }
        }
        var random = new Random(261026);
        const string alphabet = "规则测试，。；：、English Wiil 0123.,;:\t\n\u0301";
        for (int iteration = 0; iteration < 1200; iteration++)
        {
            var value = new StringBuilder();
            int length = random.Next(0, 800);
            for (int i = 0; i < length; i++) value.Append(alphabet[random.Next(alphabet.Length)]);
            SameLayout(value.ToString(), random.Next(1, 2400) / 2f, VariableAdvance, "seeded variable-width random " + iteration);
        }
        var performance = new List<object>();
        foreach (int length in new[] { 10000, 30000, 60000 })
        {
            var old = new LayoutReplay(true, _ => 16); var current = new LayoutReplay(false, _ => 16);
            string value = new string('规', length);
            Check(old.Layout(value, 880).SequenceEqual(current.Layout(value, 880)), "stress lines/ranges " + length);
            Check(current.MeasuredCharacters * 10 < old.MeasuredCharacters, "at least 90% less measured text " + length);
            Check(current.LargestProbe <= 64, "probes bounded near visible line " + length);
            performance.Add(new { length, lines = current.LineCount, oldMeasuredCharacters = old.MeasuredCharacters,
                newMeasuredCharacters = current.MeasuredCharacters, oldMeasureCalls = old.MeasureCalls,
                newMeasureCalls = current.MeasureCalls, largestNewProbe = current.LargestProbe });
        }
        // Zero-width prefixes can legitimately fit the full paragraph: doubling must terminate.
        SameLayout(new string('\u0301', 60000), 880, VariableAdvance, "all-zero-width full paragraph");
        var report = new { assertions = _assertions, status = "PASS", scope = "Source-extracted production layout with synthetic font advances; no native widgets, game renderer, clipboard or focus. Counts are algorithm work, not in-game timing.", performance };
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(args[0], json);
        Console.WriteLine(json);
    }
}
