using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Minimal port of a Hugging Face tokenizer.json with model.type=BPE, a Replace(" " -> "▁") normalizer,
// a Metaspace pre-tokenizer (prepend "always", split) and byte_fallback (Gemma-style vocab, e.g. bekko).
// Encode() returns <bos> ids <eos>. Loaded once at engine initialization; per-call cost is the BPE merge loop
// over each word plus a small per-word cache, never a vocab scan.
internal sealed class BpeTokenizerLite
{
    private const char Metaspace = '\u2581';
    private const int WordCacheMax = 4096;

    private readonly Dictionary<string, int> _vocab;
    private readonly struct MergeRule
    {
        internal readonly int Rank;
        internal readonly int Merged;
        internal MergeRule(int rank, int merged) { Rank = rank; Merged = merged; }
    }

    private readonly Dictionary<long, MergeRule> _mergeRanks;
    private readonly int[] _byteIds;
    private readonly int _unkId;
    private readonly int _bosId;
    private readonly int _eosId;
    private readonly object _cacheLock = new object();
    private readonly Dictionary<string, int[]> _wordCache = new Dictionary<string, int[]>(StringComparer.Ordinal);
    // Added tokens are matched on the raw text before normalization (all are normalized=false in supported files),
    // keyed by first char and sorted longest-first, so the scan stays O(text length x candidates per char).
    private readonly Dictionary<char, AddedToken[]> _addedByFirstChar;

    private sealed class AddedToken
    {
        internal string Content;
        internal int Id;
        internal bool LStrip;
        internal bool RStrip;
    }

    internal int PadId { get; }

    private BpeTokenizerLite(Dictionary<string, int> vocab, Dictionary<long, MergeRule> mergeRanks, int[] byteIds, int unkId, int bosId, int eosId, int padId, Dictionary<char, AddedToken[]> addedByFirstChar)
    {
        _addedByFirstChar = addedByFirstChar;
        _vocab = vocab;
        _mergeRanks = mergeRanks;
        _byteIds = byteIds;
        _unkId = unkId;
        _bosId = bosId;
        _eosId = eosId;
        PadId = padId;
    }

    // Returns null when the file is not a supported BPE tokenizer, so the caller can try another tokenizer kind.
    internal static BpeTokenizerLite TryLoad(string tokenizerJsonPath)
    {
        JObject root = JObject.Parse(File.ReadAllText(tokenizerJsonPath, Encoding.UTF8));
        JObject model = root["model"] as JObject;
        if (model == null || !string.Equals(model["type"]?.Value<string>(), "BPE", StringComparison.Ordinal))
            return null;
        if (!(model["vocab"] is JObject vocabJson) || !(model["merges"] is JArray mergesJson))
            return null;
        if (!IsMetaspacePipeline(root))
            throw new InvalidOperationException("Unsupported BPE tokenizer pipeline (expected Replace/Metaspace).");

        var vocab = new Dictionary<string, int>(vocabJson.Count, StringComparer.Ordinal);
        foreach (JProperty p in vocabJson.Properties())
            vocab[p.Name] = p.Value.Value<int>();
        var addedLists = new Dictionary<char, List<AddedToken>>();
        if (root["added_tokens"] is JArray added)
        {
            foreach (JToken t in added)
            {
                string content = t?["content"]?.Value<string>();
                if (string.IsNullOrEmpty(content)) continue;
                int id = t["id"].Value<int>();
                if (!vocab.ContainsKey(content)) vocab[content] = id;
                if (t["normalized"]?.Value<bool>() == true)
                    throw new InvalidOperationException("Unsupported normalized added token: " + content);
                if (!addedLists.TryGetValue(content[0], out List<AddedToken> list)) addedLists[content[0]] = list = new List<AddedToken>();
                list.Add(new AddedToken { Content = content, Id = id, LStrip = t["lstrip"]?.Value<bool>() == true, RStrip = t["rstrip"]?.Value<bool>() == true });
            }
        }
        var addedByFirstChar = new Dictionary<char, AddedToken[]>(addedLists.Count);
        foreach (KeyValuePair<char, List<AddedToken>> kv in addedLists)
            addedByFirstChar[kv.Key] = kv.Value.OrderByDescending(x => x.Content.Length).ToArray();

        var ranks = new Dictionary<long, MergeRule>(mergesJson.Count);
        for (int i = 0; i < mergesJson.Count; i++)
        {
            string left, right;
            JToken m = mergesJson[i];
            if (m is JArray pair && pair.Count == 2)
            {
                left = pair[0].Value<string>();
                right = pair[1].Value<string>();
            }
            else
            {
                string s = m.Value<string>() ?? "";
                int space = s.IndexOf(' ');
                if (space <= 0) continue;
                left = s.Substring(0, space);
                right = s.Substring(space + 1);
            }
            if (!vocab.TryGetValue(left, out int l) || !vocab.TryGetValue(right, out int r) || !vocab.TryGetValue(left + right, out int merged)) continue;
            long key = PairKey(l, r);
            if (!ranks.ContainsKey(key)) ranks[key] = new MergeRule(i, merged);
        }

        bool byteFallback = model["byte_fallback"]?.Value<bool>() ?? false;
        int[] byteIds = null;
        if (byteFallback)
        {
            byteIds = new int[256];
            for (int b = 0; b < 256; b++)
                byteIds[b] = vocab.TryGetValue("<0x" + b.ToString("X2") + ">", out int id) ? id : -1;
        }
        string unkToken = model["unk_token"]?.Value<string>() ?? "<unk>";
        int unkId = vocab.TryGetValue(unkToken, out int u) ? u : 0;
        int bosId = vocab.TryGetValue("<bos>", out int bos) ? bos : 2;
        int eosId = vocab.TryGetValue("<eos>", out int eos) ? eos : 1;
        int padId = vocab.TryGetValue("<pad>", out int pad) ? pad : 0;
        return new BpeTokenizerLite(vocab, ranks, byteIds, unkId, bosId, eosId, padId, addedByFirstChar);
    }

    private static bool IsMetaspacePipeline(JObject root)
    {
        JToken pre = root["pre_tokenizer"];
        string preType = pre?["type"]?.Value<string>() ?? "";
        if (string.Equals(preType, "Metaspace", StringComparison.Ordinal))
            return true;
        // Gemma-style: no pre-tokenizer, the normalizer prepends "▁" and replaces spaces.
        return pre == null || pre.Type == JTokenType.Null;
    }

    private static long PairKey(int left, int right) => ((long)left << 32) | (uint)right;

    internal List<long> Encode(string text, int maxLength, out int[] attentionMask)
    {
        if (maxLength < 8) maxLength = 8;
        var ids = new List<long>(Math.Min(maxLength, 64)) { _bosId };
        int budget = maxLength - 1;
        string raw = text ?? "";
        int segmentStart = 0;
        for (int i = 0; i < raw.Length && ids.Count < budget; i++)
        {
            AddedToken match = MatchAddedToken(raw, i);
            if (match == null) continue;
            int segmentEnd = i;
            if (match.LStrip) while (segmentEnd > segmentStart && char.IsWhiteSpace(raw[segmentEnd - 1])) segmentEnd--;
            EncodeSegment(raw.Substring(segmentStart, segmentEnd - segmentStart), ids, budget);
            if (ids.Count < budget) ids.Add(match.Id);
            i += match.Content.Length;
            if (match.RStrip) while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
            segmentStart = i;
            i--;
        }
        if (segmentStart < raw.Length) EncodeSegment(raw.Substring(segmentStart), ids, budget);
        ids.Add(_eosId);
        attentionMask = new int[ids.Count];
        for (int k = 0; k < attentionMask.Length; k++) attentionMask[k] = 1;
        return ids;
    }

    private AddedToken MatchAddedToken(string text, int at)
    {
        if (!_addedByFirstChar.TryGetValue(text[at], out AddedToken[] candidates)) return null;
        foreach (AddedToken candidate in candidates)
        {
            if (string.CompareOrdinal(text, at, candidate.Content, 0, candidate.Content.Length) == 0) return candidate;
        }
        return null;
    }

    // Replace(" " -> "▁") then Metaspace prepend_scheme=always + split=true: each segment starts with "▁" and splits before every "▁".
    private void EncodeSegment(string segment, List<long> ids, int budget)
    {
        if (segment.Length == 0) return;
        string normalized = segment.Replace(' ', Metaspace);
        if (normalized[0] != Metaspace) normalized = Metaspace + normalized;
        int start = 0;
        for (int i = 1; i <= normalized.Length && ids.Count < budget; i++)
        {
            if (i < normalized.Length && normalized[i] != Metaspace) continue;
            foreach (int id in EncodeWord(normalized.Substring(start, i - start)))
            {
                if (ids.Count >= budget) return;
                ids.Add(id);
            }
            start = i;
        }
    }

    private int[] EncodeWord(string word)
    {
        lock (_cacheLock)
        {
            if (_wordCache.TryGetValue(word, out int[] cached)) return cached;
        }
        int[] result = Merge(word);
        lock (_cacheLock)
        {
            if (_wordCache.Count >= WordCacheMax) _wordCache.Clear();
            _wordCache[word] = result;
        }
        return result;
    }

    private int[] Merge(string word)
    {
        if (_vocab.TryGetValue(word, out int whole))
            return new[] { whole };
        // Initial symbols: one per Unicode scalar; unknown scalars become their UTF-8 bytes (byte_fallback) or <unk>.
        var symbols = new List<int>(word.Length);
        for (int i = 0; i < word.Length; i++)
        {
            int len = char.IsHighSurrogate(word[i]) && i + 1 < word.Length && char.IsLowSurrogate(word[i + 1]) ? 2 : 1;
            string ch = word.Substring(i, len);
            i += len - 1;
            if (_vocab.TryGetValue(ch, out int id))
            {
                symbols.Add(id);
            }
            else if (_byteIds != null)
            {
                foreach (byte b in Encoding.UTF8.GetBytes(ch))
                    symbols.Add(_byteIds[b] >= 0 ? _byteIds[b] : _unkId);
            }
            else if (symbols.Count == 0 || symbols[symbols.Count - 1] != _unkId)
            {
                symbols.Add(_unkId); // fuse_unk
            }
        }
        // Greedy lowest-rank merge, identical to tokenizers' BPE word merge (no dropout).
        while (symbols.Count > 1)
        {
            int bestRank = int.MaxValue, bestAt = -1, merged = -1;
            for (int i = 0; i + 1 < symbols.Count; i++)
            {
                if (_mergeRanks.TryGetValue(PairKey(symbols[i], symbols[i + 1]), out MergeRule rule) && rule.Rank < bestRank)
                {
                    bestRank = rule.Rank;
                    bestAt = i;
                    merged = rule.Merged;
                }
            }
            if (bestAt < 0) break;
            int left = symbols[bestAt], right = symbols[bestAt + 1];
            // Merge every non-overlapping occurrence of this pair, left to right.
            var next = new List<int>(symbols.Count);
            for (int i = 0; i < symbols.Count; i++)
            {
                if (i + 1 < symbols.Count && symbols[i] == left && symbols[i + 1] == right)
                {
                    next.Add(merged);
                    i++;
                }
                else
                {
                    next.Add(symbols[i]);
                }
            }
            symbols = next;
        }
        return symbols.ToArray();
    }
}
