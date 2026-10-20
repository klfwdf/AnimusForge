using System.Reflection;
using AnimusForge;

internal static class MaintenanceSafetyReplay
{
    private static readonly FieldInfo Cursor = typeof(WorldDiplomacyOrchestration)
        .GetField("_playerRoundCursor", BindingFlags.Instance | BindingFlags.NonPublic)!;

    internal static void Run()
    {
        CheckSlice(2, 1, false);
        foreach (int size in new[] { 0, 1, 2, 3, 8, 9, 17 })
        foreach (int start in new[] { 0, 1, 2, 7, int.MaxValue - 1, int.MaxValue, int.MinValue })
        foreach (bool alternate in new[] { false, true })
            CheckSlice(size, start, alternate);
        CheckHourlyBudgetAndContinuation();
        CheckActiveRoundClosure();
    }

    private static WorldDiplomacyRound AddRound(WorldDiplomacyOrchestration owner, int index, bool conclude)
    {
        var round = PlayerLifecycleReplay.Case("maintenance_" + index);
        round.RootDocumentId = round.RoundId + "_source";
        round.NpcInitiatedPlayerEvent = false;
        if (conclude) round.Participants[0].State = "withdrawn";
        owner.CurrentStorage.ConcurrentRounds.Add(round);
        owner.CurrentStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = round.RootDocumentId, RoundId = round.RoundId,
            AuthorKingdomId = "a", TargetKingdomId = "p", IsReadyForPublication = true,
            RoundParticipation = conclude ? "withdraw" : "continue", RoundStatus = conclude ? "resolved" : "continue"
        });
        return round;
    }

    private static void CheckSlice(int size, int start, bool alternate)
    {
        var (_, owner) = ConcurrentOralMigrationReplay.Fixture();
        owner.CurrentStorage.PlayerLifecycleSchemaVersion = 1;
        owner.CurrentStorage.NextPlayerAgendaDay = int.MaxValue;
        var rounds = Enumerable.Range(0, size).Select(i => AddRound(owner, i, !alternate || i % 2 == 0)).ToArray();
        int count = size + 1;
        int first = start < 0 ? 0 : start % count;
        var selected = Enumerable.Range(0, Math.Min(8, count)).Select(i => (first + i) % count)
            .Where(i => i > 0).Select(i => rounds[i - 1]).ToHashSet();
        Cursor.SetValue(owner, start);
        owner.ProcessRoundLifecycle();
        foreach (var round in rounds)
        {
            bool shouldClose = selected.Contains(round) && round.Participants[0].State == "withdrawn";
            Test.True((round.State == "closed") == shouldClose,
                $"mutation-safe slice: size={size}, cursor={start}, alternate={alternate}, round={round.RoundId}");
        }
        Test.True((int)Cursor.GetValue(owner)! >= 0 && (int)Cursor.GetValue(owner)! < count,
            "maintenance cursor stays bounded without integer overflow");
        Test.True(owner.CurrentStorage.CompletedRounds.Select(x => x.RoundId).Distinct().Count()
            == owner.CurrentStorage.CompletedRounds.Count, "each closed round is archived exactly once");
    }

    private static void CheckHourlyBudgetAndContinuation()
    {
        var (host, owner) = ConcurrentOralMigrationReplay.Fixture();
        owner.CurrentStorage.PlayerLifecycleSchemaVersion = 1;
        owner.CurrentStorage.NextPlayerAgendaDay = int.MaxValue;
        for (int i = 0; i < 20; i++) AddRound(owner, i, true);
        owner.ProcessRoundLifecycle();
        Test.True(owner.CurrentStorage.CompletedRounds.Count == 7, "first eight slots include the active-round slot");
        owner.ProcessRoundLifecycle();
        Test.True(owner.CurrentStorage.CompletedRounds.Count == 7, "repeated same-hour calls do not repeat maintenance");
        for (int i = 0; i < 6 && owner.CurrentStorage.ConcurrentRounds.Count > 0; i++)
        {
            int before = owner.CurrentStorage.CompletedRounds.Count;
            host.CurrentDayValue++;
            owner.ProcessRoundLifecycle();
            Test.True(owner.CurrentStorage.CompletedRounds.Count - before <= 8, "each later slice remains bounded to eight rounds");
        }
        Test.True(owner.CurrentStorage.ConcurrentRounds.Count == 0 && owner.CurrentStorage.CompletedRounds.Count == 20,
            "shrinking collections eventually finish all eligible rounds without skips or duplicate closure");
    }

    private static void CheckActiveRoundClosure()
    {
        var (_, owner) = ConcurrentOralMigrationReplay.Fixture();
        owner.CurrentStorage.PlayerLifecycleSchemaVersion = 1;
        owner.CurrentStorage.NextPlayerAgendaDay = int.MaxValue;
        var active = AddRound(owner, 0, true);
        owner.CurrentStorage.ConcurrentRounds.Remove(active);
        owner.CurrentStorage.ActiveRound = active;
        AddRound(owner, 1, true);
        AddRound(owner, 2, true);
        owner.ProcessRoundLifecycle();
        Test.True(owner.CurrentStorage.ActiveRound == null && owner.CurrentStorage.ConcurrentRounds.Count == 0,
            "closing the active slot and concurrent slots in one slice keeps every snapshot entry valid");
        Test.True(owner.CurrentStorage.CompletedRounds.Count == 3, "active and concurrent slots archive exactly once");
    }
}
