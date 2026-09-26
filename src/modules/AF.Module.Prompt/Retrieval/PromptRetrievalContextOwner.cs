using System;
using System.Threading;

namespace AnimusForge;

internal sealed class PromptRetrievalContextSlot<T>
{
    private readonly Func<T> _read;
    private readonly Action<T> _write;

    internal PromptRetrievalContextSlot(Func<T> read, Action<T> write)
    {
        _read = read;
        _write = write;
    }

    internal T Value
    {
        get => _read();
        set => _write(value);
    }
}

// One ambient value is copied on write and restored to the parent on scope exit.
internal static class PromptRetrievalContextOwner
{
    internal sealed class Context
    {
        internal string Semantic;
        internal string Kingdom;
        internal string Hero;
        internal string Character;
        internal string Troop;
        internal string UnnamedRank;
        internal int AgentIndex = -1;
        internal object Eligibility;
        internal object LatestEntities;

        internal Context Clone() => (Context)MemberwiseClone();
    }

    private static readonly AsyncLocal<Context> Ambient = new AsyncLocal<Context>();
    private static readonly Context Empty = new Context();

    private static Context Current => Ambient.Value ?? Empty;

    private static void Update(Action<Context> update)
    {
        Context replacement = Current.Clone();
        update(replacement);
        Ambient.Value = replacement;
    }

    internal static PromptRetrievalContextSlot<T> CreateSlot<T>(Func<Context, T> read, Action<Context, T> write) =>
        new PromptRetrievalContextSlot<T>(() => read(Current), value => Update(context => write(context, value)));

    internal static readonly PromptRetrievalContextSlot<string> Semantic = CreateSlot(context => context.Semantic, (context, value) => context.Semantic = value);
    internal static readonly PromptRetrievalContextSlot<string> Kingdom = CreateSlot(context => context.Kingdom, (context, value) => context.Kingdom = value);
    internal static readonly PromptRetrievalContextSlot<string> Hero = CreateSlot(context => context.Hero, (context, value) => context.Hero = value);
    internal static readonly PromptRetrievalContextSlot<string> Character = CreateSlot(context => context.Character, (context, value) => context.Character = value);
    internal static readonly PromptRetrievalContextSlot<string> Troop = CreateSlot(context => context.Troop, (context, value) => context.Troop = value);
    internal static readonly PromptRetrievalContextSlot<string> UnnamedRank = CreateSlot(context => context.UnnamedRank, (context, value) => context.UnnamedRank = value);
    internal static readonly PromptRetrievalContextSlot<int> AgentIndex = CreateSlot(context => context.AgentIndex, (context, value) => context.AgentIndex = value);
    internal static readonly PromptRetrievalContextSlot<object> Eligibility = CreateSlot(context => context.Eligibility, (context, value) => context.Eligibility = value);

    internal static IDisposable BeginScope(Func<object, object, object> mergeLatest = null)
    {
        Context previous = Ambient.Value;
        Ambient.Value = Current.Clone();
        return new Scope(previous, mergeLatest);
    }

    private sealed class Scope : IDisposable
    {
        private Context _previous;
        private readonly Func<object, object, object> _mergeLatest;
        private int _disposed;

        internal Scope(Context previous, Func<object, object, object> mergeLatest)
        {
            _previous = previous;
            _mergeLatest = mergeLatest;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Context result = _previous;
            object latest = Ambient.Value?.LatestEntities;
            if (_mergeLatest != null && latest != null && !ReferenceEquals(latest, _previous?.LatestEntities))
            {
                try
                {
                    result = (_previous ?? Empty).Clone();
                    result.LatestEntities = _mergeLatest(_previous?.LatestEntities, latest);
                }
                catch
                {
                    // Scope restoration is mandatory even if optional mention continuation fails.
                    result = _previous;
                }
            }
            Ambient.Value = result;
            _previous = null;
        }
    }
}
