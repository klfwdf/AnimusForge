using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

internal static class Program
{
    private static int _checks;
    private static void Check(bool c, string m) { _checks++; if (!c) throw new Exception("FAIL: " + m); }
    private sealed class Row { public int Day; public string Text; }
    private sealed class Boom { public string Bad { get { throw new InvalidOperationException("serialize boom"); } } }

    private static void Main()
    {
        var source = new Dictionary<string, List<Row>>
        {
            ["a"] = new List<Row> { new Row { Day = 1, Text = "x" } },
            [""] = new List<Row> { new Row { Day = 9, Text = "blank-key" } },
            [" "] = new List<Row> { new Row { Day = 9, Text = "ws-key" } },
            ["empty"] = new List<Row>(),
            ["nulls"] = null,
        };
        var storage = new Dictionary<string, string> { ["stale"] = "old" };
        var errors = new List<string>();
        int written = OwnerJsonStorageCodec.Serialize(source, storage, skipWhitespaceKeys: false, skipEmptyLists: false, null, (k, e) => errors.Add(k));
        Check(written == 3 && storage.ContainsKey("a") && storage.ContainsKey(" ") && storage.ContainsKey("empty") && !storage.ContainsKey("") && !storage.ContainsKey("stale") && !storage.ContainsKey("nulls"), "IsNullOrEmpty key policy, empty lists kept, storage cleared first: " + string.Join(",", storage.Keys));
        written = OwnerJsonStorageCodec.Serialize(source, storage, skipWhitespaceKeys: true, skipEmptyLists: true, null, null);
        Check(written == 1 && storage.Keys.SequenceEqual(new[] { "a" }), "IsNullOrWhiteSpace key policy + skip empty lists");
        written = OwnerJsonStorageCodec.Serialize(source, storage, true, true, list => list.Where(r => r.Day > 5).ToList(), null);
        Check(written == 0 && storage.Count == 0, "transform returning empty skips owner");
        written = OwnerJsonStorageCodec.Serialize(source, storage, false, true, list => list, null);
        Check(written == 2 && storage["a"].Contains("\"Text\":\"x\"") && storage.ContainsKey(" "), "transform pass-through serializes (IsNullOrEmpty policy keeps whitespace owner)");

        var boomSource = new Dictionary<string, List<Boom>> { ["b1"] = new List<Boom> { new Boom() }, ["b2"] = new List<Boom>() };
        var boomStorage = new Dictionary<string, string>();
        errors.Clear();
        written = OwnerJsonStorageCodec.Serialize(boomSource, boomStorage, false, false, null, (k, e) => errors.Add(k + ":" + e.GetType().Name));
        Check(written == 1 && boomStorage.ContainsKey("b2") && errors.Count == 1 && errors[0].StartsWith("b1:"), "serialize failure isolated per owner: " + string.Join(",", errors));

        var loadStorage = new Dictionary<string, string>
        {
            ["A"] = "[{\"Day\":1,\"Text\":\"x\"},{\"Day\":2,\"Text\":\"y\"}]",
            ["B"] = "not json",
            ["C"] = "null",
            ["D"] = "[]",
            [""] = "[{\"Day\":1}]",
            ["E"] = "",
            [" "] = "[{\"Day\":1}]",
        };
        var target = new Dictionary<string, List<Row>> { ["stale"] = new List<Row>() };
        errors.Clear();
        int restored = OwnerJsonStorageCodec.Deserialize(loadStorage, target, null, null, skipWhitespaceKeys: false, (k, e) => errors.Add(k));
        Check(restored == 3 && target.Keys.OrderBy(k => k).SequenceEqual(new[] { " ", "A", "D" }) && target["A"].Count == 2 && target["D"].Count == 0 && !target.ContainsKey("stale") && !target.ContainsKey("C") && errors.SequenceEqual(new[] { "B" }), "deserialize: bad json isolated, null skipped, empty list kept, target cleared: " + string.Join(",", target.Keys) + " errors=" + string.Join(",", errors));
        restored = OwnerJsonStorageCodec.Deserialize(loadStorage, target, k => k.ToLowerInvariant(), list => list.Where(r => r.Day >= 2).ToList(), true, null);
        Check(restored == 1 && target.Keys.SequenceEqual(new[] { "a" }) && target["a"].Single().Day == 2, "sanitize drops rows, empty result not stored, key normalized, whitespace keys skipped");
        Check(OwnerJsonStorageCodec.Deserialize<Row>(null, target, null, null, false, null) == 0 && target.Count == 0, "null storage clears target");
        Console.WriteLine("PASS owner-json-codec checks=" + _checks);
    }

    }
