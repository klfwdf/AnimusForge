using System;
using System.Collections.Generic;
using System.Threading;

namespace AnimusForge;

// A receipt for one current interaction only. Nothing here is serialized into a save.
// Capture scopes surround synchronous owner-thread writes, never a network operation.
internal sealed class ConversationRewriteTurn
{
    private static readonly AsyncLocal<ConversationRewriteTurn> Current = new AsyncLocal<ConversationRewriteTurn>();
    private readonly int _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
    private readonly List<IWriteReceipt> _writes = new List<IWriteReceipt>();
    private readonly Dictionary<object, Dictionary<string, DictionaryValueReceipt>> _dictionaryWrites = new Dictionary<object, Dictionary<string, DictionaryValueReceipt>>();
    private int _completed, _invalidated, _rewritten;
    private bool _hasPlayerSpeech, _hasNpcReply;
    private string _invalidReason = "rewrite_turn_invalidated";

    internal ConversationRewriteTurn(string playerText) { PlayerText = playerText ?? ""; }
    internal string PlayerText { get; }
    internal bool IsCompleted => Volatile.Read(ref _completed) != 0 && Volatile.Read(ref _invalidated) == 0 && Volatile.Read(ref _rewritten) == 0;
    internal static bool IsCapturing => Current.Value != null && Current.Value.CanCapture;
    private bool IsOwnerThread => Thread.CurrentThread.ManagedThreadId == _ownerThreadId;
    private bool CanCapture => IsOwnerThread && Volatile.Read(ref _completed) == 0 && Volatile.Read(ref _invalidated) == 0 && Volatile.Read(ref _rewritten) == 0;

    internal static IDisposable CaptureScope(ConversationRewriteTurn turn)
    {
        var previous = Current.Value;
        Current.Value = turn;
        if (turn != null && !turn.IsOwnerThread) turn.Invalidate("rewrite_wrong_thread");
        return new Scope(previous);
    }

    internal void Complete()
    {
        if (!IsOwnerThread) { Invalidate("rewrite_wrong_thread"); return; }
        if (!_hasPlayerSpeech || !_hasNpcReply) { Invalidate("rewrite_incomplete_turn"); return; }
        if (Volatile.Read(ref _invalidated) == 0) Volatile.Write(ref _completed, 1);
    }

    // Cancellation can originate on a worker; only this flag crosses threads.
    internal void Invalidate(string reason = "rewrite_turn_invalidated")
    {
        _invalidReason = string.IsNullOrWhiteSpace(reason) ? "rewrite_turn_invalidated" : reason;
        Interlocked.Exchange(ref _invalidated, 1);
    }

    internal bool CanRewrite(out string reason)
    {
        if (!IsOwnerThread) { reason = "rewrite_wrong_thread"; return false; }
        if (Volatile.Read(ref _invalidated) != 0) { reason = _invalidReason; return false; }
        if (Volatile.Read(ref _rewritten) != 0) { reason = "rewrite_already_applied"; return false; }
        if (!IsCompleted) { reason = "rewrite_turn_pending"; return false; }
        if (_writes.Count == 0) { reason = "rewrite_no_records"; return false; }
        foreach (IWriteReceipt write in _writes)
            if (!write.Validate()) { reason = "rewrite_source_changed"; return false; }
        reason = "";
        return true;
    }

    internal bool TryRewrite(out string reason)
    {
        // Every source is checked before any mutation. Owner-thread callers cannot
        // interleave compression, another completion, a history edit or save/load here.
        if (!CanRewrite(out reason)) return false;
        for (int i = _writes.Count - 1; i >= 0; i--) _writes[i].Remove();
        Volatile.Write(ref _rewritten, 1);
        _writes.Clear();
        _dictionaryWrites.Clear();
        return true;
    }

    internal static void CaptureListWrite<T>(Func<List<T>> currentList, T item, Func<bool> unchanged = null, Action afterRemove = null, bool isPlayerSpeech = false, bool isNpcReply = false) where T : class
    {
        var turn = Current.Value;
        if (turn == null || !turn.CanCapture || item == null) return;
        turn._hasPlayerSpeech |= isPlayerSpeech;
        turn._hasNpcReply |= isNpcReply;
        turn._writes.Add(new ListWriteReceipt<T>(currentList, item, unchanged, afterRemove));
    }

    internal static void CaptureDictionaryWrite(Dictionary<string, string> dictionary, string key, bool hadPrevious, string previous, string current)
    {
        var turn = Current.Value;
        if (turn == null || !turn.CanCapture) return;
        if (!turn._dictionaryWrites.TryGetValue(dictionary, out var keys))
            turn._dictionaryWrites[dictionary] = keys = new Dictionary<string, DictionaryValueReceipt>(dictionary.Comparer);
        if (keys.TryGetValue(key, out var receipt)) { receipt.Current = current; return; }
        keys[key] = receipt = new DictionaryValueReceipt(dictionary, key, hadPrevious, previous, current);
        turn._writes.Add(receipt);
    }

    private interface IWriteReceipt { bool Validate(); void Remove(); }
    private sealed class ListWriteReceipt<T> : IWriteReceipt where T : class
    {
        private readonly Func<List<T>> _currentList;
        private readonly T _item;
        private readonly Func<bool> _unchanged;
        private readonly Action _afterRemove;
        internal ListWriteReceipt(Func<List<T>> currentList, T item, Func<bool> unchanged, Action afterRemove)
        { _currentList = currentList; _item = item; _unchanged = unchanged; _afterRemove = afterRemove; }
        public bool Validate()
        {
            List<T> list = _currentList();
            if (list == null || (_unchanged != null && !_unchanged())) return false;
            int matches = 0;
            for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], _item)) matches++;
            return matches == 1;
        }
        public void Remove()
        {
            List<T> list = _currentList();
            // Reference comparison is essential for repeated identical speech strings.
            for (int i = list.Count - 1; i >= 0; i--)
                if (ReferenceEquals(list[i], _item)) { list.RemoveAt(i); _afterRemove?.Invoke(); return; }
        }
    }
    private sealed class DictionaryValueReceipt : IWriteReceipt
    {
        private readonly Dictionary<string, string> _dictionary;
        private readonly string _key, _previous;
        private readonly bool _hadPrevious;
        internal string Current;
        internal DictionaryValueReceipt(Dictionary<string, string> dictionary, string key, bool hadPrevious, string previous, string current)
        { _dictionary = dictionary; _key = key; _hadPrevious = hadPrevious; _previous = previous; Current = current; }
        public bool Validate() => _dictionary.TryGetValue(_key, out var value) && ReferenceEquals(value, Current);
        public void Remove() { if (_hadPrevious) _dictionary[_key] = _previous; else _dictionary.Remove(_key); }
    }
    private sealed class Scope : IDisposable
    {
        private readonly ConversationRewriteTurn _previous;
        private bool _disposed;
        internal Scope(ConversationRewriteTurn previous) { _previous = previous; }
        public void Dispose() { if (!_disposed) { Current.Value = _previous; _disposed = true; } }
    }
}
