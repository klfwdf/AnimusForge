using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

internal static class Program
{
    private static int _assertions;
    private static void Check(bool value, string message)
    { _assertions++; if (!value) throw new InvalidOperationException(message); }

    private static void Main()
    {
        foreach (Fault fault in new[] { Fault.None, Fault.Before, Fault.NoOp, Fault.After, Fault.Unreadable })
        {
            foreach (string worldAction in new[] { "peace", "alliance", "trade" })
            {
                Engine.Reset(fault, war: worldAction == "peace");
                var worldPort = WorldDiplomacyBehavior.NewOfferPort();
                WorldDiplomacyOfferActionReceipt receipt = worldAction switch
                {
                    "peace" => worldPort.ExecutePeace("player", "npc", new() { DailyTribute = 50, DurationDays = 100 }),
                    "alliance" => worldPort.ExecuteAlliance("player", "npc"),
                    _ => worldPort.ExecuteTrade("player", "npc")
                };
                Check(receipt.Known == (fault != Fault.Unreadable), "real world port retains read certainty: " + worldAction);
                Check(receipt.Applied == (fault is Fault.None or Fault.After) && Engine.Calls == 1,
                    "real world port attempts once and confirms effect: " + worldAction + " " + fault);
                if (!receipt.Known) Check(receipt.Message.Contains("无法确认"), "unknown is not reported as known no-op");
            }
            foreach (string action in new[] { "alliance", "break", "trade", "cancel", "independent" })
            {
                Engine.Reset(fault, war: action == "independent", allied: action == "break", trading: action == "cancel");
                if (action == "independent") Clan.PlayerClan.Kingdom = null!;
                string status = Execute(action);
                string expected = fault == Fault.Unreadable ? "UnknownAfterStart"
                    : fault is Fault.Before or Fault.NoOp ? "ActionNotApplied" : "Applied";
                Check(status == expected, action + ": " + fault + " actual=" + status);
                Check(Engine.Calls == 1, action + " attempts exactly once");
                if (expected == "Applied")
                {
                    Check(Execute(action) != "Applied" && Engine.Calls == 1, action + " retry observes state and never repeats effect");
                    if (action == "independent") Check(Engine.Registrations == 1, "register confirmed peace once");
                }
            }
            Engine.Reset(fault, war: true);
            var source = new PeaceSource();
            DiplomacyOralMakePeaceApplication.Execute(ref source, "player:npc:50:100");
            bool applied = fault is Fault.None or Fault.After;
            Check(source.Receipt.IsApplied == applied && source.Notifications == (applied ? 1 : 0),
                "real peace adapter and Application only publish confirmed receipt " + fault);
            if (applied)
            {
                Check(source.Receipt.AppliedDailyTribute == 50 && source.Receipt.AppliedDurationDays == 100, "measured tribute and installments");
                DiplomacyOralMakePeaceApplication.Execute(ref source, "player:npc:50:100");
                Check(source.Notifications == 1 && Engine.Calls == 1, "peace retry does not repeat publication/action");
            }
            Engine.Reset(fault);
            var town = new Settlement { Owner = Engine.Player.RulingClan };
            var terms = new WorldDiplomacyPeaceTerms { CessionSettlementId = "town" };
            var cession = BannerlordWorldDiplomacyCessionGameActionPort.Apply(terms, Engine.Player, Engine.Npc,
                Engine.Player, Engine.Npc, town, _ => { });
            Check(cession.Applied == applied && cession.Known == (fault != Fault.Unreadable), "actual cession receipt " + fault);
            Check(Engine.Calls == 1, "cession attempts once");
        }
        Engine.Reset(Fault.BetweenPeaceAndTerms, war: true);
        var partialSource = new PeaceSource();
        DiplomacyOralMakePeaceApplication.Execute(ref partialSource, "player:npc:50:100");
        Check(partialSource.Receipt.Status == WorldDiplomacyMakePeaceExecutionStatus.PartiallyApplied
            && partialSource.Notifications == 1 && !partialSource.Receipt.IsApplied && partialSource.Receipt.AppliedDailyTribute == 0,
            "peace-before-tribute failure publishes only partial peace");
        Check(Engine.Registrations == 1, "partial peace still protects actual peace");
        Engine.Reset(Fault.None, war: true);
        var zero = DiplomacyPeaceTermsService.ApplyPeace(Engine.Player, Engine.Npc, 0, 123, "test");
        Check(zero.Complete && zero.ActualDurationDays == 123, "zero tribute retains explicit installments; do not use remaining payment count");
        Engine.Stance.Tribute = -50;
        var wrongDirection = DiplomacyPeaceTermsService.ConfirmPeace(Engine.Player, Engine.Npc, 50, 123, "test");
        Check(wrongDirection.PeaceApplied && !wrongDirection.Complete && wrongDirection.ActualDailyTribute == -50,
            "opposite tribute direction cannot match requested terms");
        Engine.Reset(Fault.None);
        var invalidCession = BannerlordWorldDiplomacyCessionGameActionPort.Apply(new() { CessionSettlementId = "town" },
            Engine.Player, Engine.Npc, Engine.Player, Engine.Npc, null!, _ => { });
        Check(invalidCession.Requested && !invalidCession.Complete && Engine.Calls == 0, "invalid requested cession is failure, not empty success");
        var absentCession = BannerlordWorldDiplomacyCessionGameActionPort.Apply(new(), Engine.Player, Engine.Npc, null!, null!, null!, _ => { });
        Check(!absentCession.Requested && absentCession.Complete, "no cession requested is complete without game effect");
        foreach (Fault fault in new[] { Fault.None, Fault.Before, Fault.After, Fault.Unreadable })
        {
            Engine.Reset(fault); int relation = 95;
            var measured = DiplomacyRelationEffect.Apply(() => { Engine.Read(); return relation; },
                () => Engine.Apply(() => relation = Math.Clamp(relation + 10, -100, 100)));
            Check(measured.IsKnown == (fault != Fault.Unreadable), "relation receipt read knowledge " + fault);
            Check(measured.AppliedDelta == (fault is Fault.None or Fault.After ? 5 : 0), "relation clamp and observer exception measured " + fault);
        }
        Console.WriteLine($"Diplomacy real effect receipt tests passed ({_assertions} assertions).");
    }
    private static string Execute(string action) => action switch
    {
        "alliance" => new BannerlordWorldDiplomacyFormAllianceGameActionPort().Execute(new("player", "npc", "speaker", "100")).Status.ToString(),
        "break" => new BannerlordWorldDiplomacyBreakAllianceGameActionPort().Execute(new("player", "npc", "speaker")).Status.ToString(),
        "trade" => new BannerlordWorldDiplomacyMakeTradeGameActionPort().Execute(new("player", "npc", "speaker", "100")).Status.ToString(),
        "cancel" => new BannerlordWorldDiplomacyCancelTradeGameActionPort().Execute(new("player", "npc", "speaker")).Status.ToString(),
        _ => new BannerlordWorldDiplomacyIndependentClanPeaceGameActionPort().Execute(new("player-clan", "npc", "speaker")).Status.ToString()
    };
    private struct PeaceSource : IDiplomacyOralMakePeaceSource
    {
        internal WorldDiplomacyMakePeaceExecutionReceipt Receipt;
        internal int Notifications;
        public DiplomacyOralRoyalSnapshot Capture() => new(true, "player", false, true, true, "npc", "speaker", true);
        public WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command)
            => Receipt = new BannerlordWorldDiplomacyMakePeaceGameActionPort().Execute(command);
        public bool TryResolveAppliedEndpoints(string payer, string receiver, out string first, out string second)
        { first = payer; second = receiver; return true; }
        public void NotifyResolved(WorldDiplomacyMakePeaceExecutionReceipt receipt) => Notifications++;
        public void Log(string message) { }
    }
}
