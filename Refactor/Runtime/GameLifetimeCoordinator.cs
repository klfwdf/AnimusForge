using System;

namespace AnimusForge.Refactor.Runtime;

// Game identity, not Game.Current labels, decides whether an end callback belongs to this lifetime.
// Begin/End/Stop are serialized by the real engine main-thread adapter.
internal sealed class GameLifetimeCoordinator
{
    private object _game;
    private readonly Action<string> _advance;
    private readonly Action<string> _retire;
    internal GameLifetimeCoordinator(Action<string> advance, Action<string> retire)
    { _advance = advance ?? throw new ArgumentNullException(nameof(advance)); _retire = retire ?? throw new ArgumentNullException(nameof(retire)); }
    internal bool IsCurrent(object game) => game != null && ReferenceEquals(_game, game);

    internal bool Begin(object game)
    {
        if (game == null || IsCurrent(game)) return false;
        bool replacing = _game != null;
        _game = game;
        _advance("campaign_begin");
        if (replacing) _retire("campaign_replaced");
        return true;
    }

    internal bool End(object game)
    {
        if (!IsCurrent(game)) return false;
        _game = null;
        _advance("campaign_end");
        _retire("campaign_end");
        return true;
    }

    internal bool Stop()
    {
        return _game != null && End(_game);
    }
}
