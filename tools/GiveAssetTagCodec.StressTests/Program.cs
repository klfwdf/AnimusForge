using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void Equal<T>(T expected, T actual, string message)
    {
        _assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message + "; expected=" + expected + "; actual=" + actual);
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
private static void AssertSingle(string asset, string quantity)
{
    string raw = "[ACTION:GIVE_ASSET:" + asset + ":" + quantity + "]";
    Test.True(GiveAssetTagCodec.TryParseWhole(raw, out GiveAssetTag tag), "must parse: " + raw);
    Test.Equal(asset, tag.AssetToken, "asset round-trip");
    Test.Equal(quantity, tag.QuantityToken, "quantity round-trip");
    Test.Equal(raw, tag.RawTag, "raw round-trip");
}

private static bool HasAmbiguousTerminator(string asset)
{
    return Regex.IsMatch(asset, ":(?:ALL|[0-9]+)\\]", RegexOptions.IgnoreCase);
}

private static int Main()
{
string[] importantNames =
{
    "[ROT]佛雷甲",
    "[ROT]贵族头饰",
    "[ROT]安达尔马鞍配钢面甲",
    "火焰之剑:北境版",
    "A]B",
    "[A:B]",
    "{#id=weapon}钢剑",
    "盔甲@稀有+3",
    "空 格　全角",
    "emoji🗡️护甲",
    "[ACTION:MOOD:ANNOYED]作为名称",
    "\\\\/|^$*+?.(){}\"'`~!@#%&_=,;<>"
};

foreach (string name in importantNames)
{
    AssertSingle(name, "1");
}
AssertSingle("任何物品", "ALL");
AssertSingle("任何物品", "all");
AssertSingle("物品:含多个:冒号", "007");

for (int code = 32; code <= 126; code++)
{
    char symbol = (char)code;
    AssertSingle("左" + symbol + "右", "1");
}

string first = "[ACTION:GIVE_ASSET:[ROT]佛雷甲:1]";
string second = "[ACTION:GIVE_ASSET:火焰:北境版:2]";
string combined = "前缀 " + first + second + " 后缀";
List<GiveAssetTag> tags = GiveAssetTagCodec.Extract(combined);
Test.Equal(2, tags.Count, "contiguous tags must stay separate");
Test.Equal("[ROT]佛雷甲", tags[0].AssetToken, "first contiguous asset");
Test.Equal("火焰:北境版", tags[1].AssetToken, "second contiguous asset");
Test.Equal("前缀  后缀", GiveAssetTagCodec.StripTags(combined), "strip must leave visible text intact");
Test.Equal("前缀 <[ROT]佛雷甲|1><火焰:北境版|2> 后缀", GiveAssetTagCodec.ReplaceTags(combined, tag => "<" + tag.AssetToken + "|" + tag.QuantityToken + ">"), "replace must preserve order and boundaries");

string malformedThenValid = "[ACTION:GIVE_ASSET:损坏:xyz]" + first;
tags = GiveAssetTagCodec.Extract(malformedThenValid);
Test.Equal(1, tags.Count, "malformed tag must not consume following valid tag");
Test.Equal("[ROT]佛雷甲", tags[0].AssetToken, "following valid tag must survive malformed predecessor");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET::1]", out _), "empty asset must be rejected");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:-1]", out _), "negative syntax must be rejected");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:一]", out _), "non-numeric quantity must be rejected");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:1\n]", out _), "line breaks must be rejected");
tags = GiveAssetTagCodec.Extract("[ACTION:GIVE_ASSET:损坏:1\n" + first);
Test.Equal(1, tags.Count, "line-broken malformed tag must not hide next-line valid tag");
Test.True(GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:0]", out _), "zero is syntactically parseable and must be rejected by quantity policy, not by boundary parsing");
Test.True(GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:999999999999999999999]", out _), "overflow quantity is syntactically parseable and must be rejected by executor policy");

// '[ACTION:GIVE_ASSET:' is the reserved introducer. If it appears after a malformed tag,
// it starts a fresh candidate rather than allowing a cross-tag accidental grant.
string nestedRecovery = "[ACTION:GIVE_ASSET:坏标签" + first;
tags = GiveAssetTagCodec.Extract(nestedRecovery);
Test.Equal(1, tags.Count, "nested introducer must recover the later candidate");
Test.Equal("[ROT]佛雷甲", tags[0].AssetToken, "later candidate after nested introducer");

var random = new Random(20260723);
const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 []:{}()<>!@#$%^&*-_=+;,.?/\\|\"'~`中文测试🗡️";
for (int index = 0; index < 20000; index++)
{
    string asset;
    do
    {
        int length = random.Next(1, 80);
        var builder = new StringBuilder(length);
        for (int charIndex = 0; charIndex < length; charIndex++)
        {
            builder.Append(alphabet[random.Next(alphabet.Length)]);
        }
        asset = builder.ToString();
    }
    while (HasAmbiguousTerminator(asset) || asset.IndexOf(GiveAssetTagCodec.Prefix, StringComparison.OrdinalIgnoreCase) >= 0);

    string quantity = index % 7 == 0 ? "ALL" : (index % 97 + 1).ToString();
    AssertSingle(asset, quantity);
}

var pressure = new StringBuilder(1_500_000);
for (int index = 0; index < 25000; index++)
{
    pressure.Append("正文");
    pressure.Append("[ACTION:GIVE_ASSET:[ROT]物品:");
    pressure.Append(index % 19 + 1);
    pressure.Append("]");
}
var stopwatch = Stopwatch.StartNew();
tags = GiveAssetTagCodec.Extract(pressure.ToString());
string stripped = GiveAssetTagCodec.StripTags(pressure.ToString());
stopwatch.Stop();
Test.Equal(25000, tags.Count, "pressure extraction count");
Test.Equal("正文".Length * 25000, stripped.Length, "pressure stripping result");
Test.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), "postprocess-sized parser pressure run exceeded 10 seconds: " + stopwatch.Elapsed);

string repoRoot = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(repoRoot, "MyBehavior.cs")))
{
    string? parent = Directory.GetParent(repoRoot)?.FullName;
    if (string.IsNullOrWhiteSpace(parent))
    {
        throw new InvalidOperationException("Could not find repository root.");
    }
    repoRoot = parent;
}

string myBehavior = File.ReadAllText(Path.Combine(repoRoot, "MyBehavior.cs"));
string shoutBehavior = File.ReadAllText(Path.Combine(repoRoot, "ShoutBehavior.cs"));
string rewardSystem = File.ReadAllText(Path.Combine(repoRoot, "RewardSystemBehavior.cs"));
Test.True(myBehavior.Contains("GiveAssetTagCodec.TryParseWhole", StringComparison.Ordinal) && myBehavior.Contains("GiveAssetTagCodec.ReplaceTags", StringComparison.Ordinal), "free-conversation parser integration missing");
Test.True(shoutBehavior.Contains("GiveAssetTagCodec.Extract", StringComparison.Ordinal) && shoutBehavior.Contains("GiveAssetTagCodec.StripTags", StringComparison.Ordinal), "scene/courier parser integration missing");
Test.True(rewardSystem.Contains("GiveAssetTagCodec.ReplaceTags", StringComparison.Ordinal) && rewardSystem.Contains("GiveAssetTagCodec.StripTags", StringComparison.Ordinal), "all reward execution parser integration missing");

Console.WriteLine("PASS assertions=" + Test.Assertions + " fuzz=20000 pressureTags=25000 elapsedMs=" + stopwatch.Elapsed.TotalMilliseconds.ToString("F2"));
return 0;
}
}
