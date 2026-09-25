using System;

namespace AnimusForge.Refactor.Modules;

// Owns two selection stages and their delayed opening tickets; TW UI/Mission calls stay in the host.
internal sealed class MilitaryExerciseSessionOwner<TSelection, TRuntime>
    where TSelection : class where TRuntime : class
{
    private readonly Func<TSelection, bool> _isSecondStage;
    private readonly Func<TRuntime, bool> _isSettled;

    internal MilitaryExerciseSessionOwner(Func<TSelection, bool> isSecondStage,
        Func<TRuntime, bool> isSettled)
    {
        _isSecondStage = isSecondStage;
        _isSettled = isSettled;
    }

    internal TSelection Selection { get; set; }
    internal TRuntime Runtime { get; set; }
    internal bool IsOpening { get; set; }
    internal bool SecondQueued { get; private set; }
    internal bool BattleQueued { get; private set; }
    internal bool HasActiveRuntime => Runtime != null && !_isSettled(Runtime);
    internal bool IsCurrentSelection(TSelection selection) => ReferenceEquals(Selection, selection);

    private float _secondDueAt;
    private float _battleDueAt;

    internal void QueueSecond(float now, float delaySeconds)
    {
        SecondQueued = true;
        _secondDueAt = now + delaySeconds;
    }

    internal bool IsSecondDue(float now)
    {
        if (!SecondQueued) return false;
        if (Selection == null || !_isSecondStage(Selection))
        {
            SecondQueued = false;
            return false;
        }
        return now >= _secondDueAt;
    }

    internal bool BeginSecond()
    {
        if (!SecondQueued || Selection == null || !_isSecondStage(Selection)) return false;
        SecondQueued = false;
        IsOpening = true;
        return true;
    }

    internal void QueueBattle(float now, float delaySeconds)
    {
        BattleQueued = true;
        _battleDueAt = now + delaySeconds;
    }

    internal bool IsBattleDue(float now)
    {
        if (!BattleQueued) return false;
        if (!HasActiveRuntime)
        {
            BattleQueued = false;
            return false;
        }
        return now >= _battleDueAt;
    }

    internal bool BeginBattle()
    {
        if (!BattleQueued || !HasActiveRuntime) return false;
        BattleQueued = false;
        IsOpening = true;
        return true;
    }

    internal void ResetSelection()
    {
        Selection = null;
        IsOpening = false;
        SecondQueued = false;
        BattleQueued = false;
    }

    internal void ReleaseRuntime(TRuntime runtime)
    {
        if (ReferenceEquals(Runtime, runtime)) Runtime = null;
        IsOpening = false;
        BattleQueued = false;
    }

    internal bool NeedsEngineTick(bool harmonyPatched, bool knownDummyParties,
        bool fallbackScanRequested)
        => !harmonyPatched || SecondQueued || BattleQueued || HasActiveRuntime
            || knownDummyParties || fallbackScanRequested;
}
