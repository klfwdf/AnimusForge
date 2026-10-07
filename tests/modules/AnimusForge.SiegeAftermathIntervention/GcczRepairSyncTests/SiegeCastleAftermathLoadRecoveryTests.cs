using System;
using AnimusForge.SiegeAftermathIntervention;

internal static class SiegeCastleAftermathLoadRecoveryTests
{
    internal static void Run(Action<bool, string> check)
    {
        const string decision = SiegeAftermathMenuProfile.SettlementTakenPlayerLeaderMenuId;
        bool CanRestore(string menu = decision, bool castle = true, bool matching = true,
            bool complex = true, bool location = false, bool mission = false,
            bool battle = false, bool captive = false)
            => SiegeCastleAftermathLoadRecoveryPolicy.CanRestoreLocationEncounter(
                menu, castle, matching, complex, location, mission, battle, captive);

        check(CanRestore(), "castle load: missing context can be restored for the saved encounter");
        check(CanRestore(SiegeAftermathMenuProfile.SettlementTakenMenuId), "castle load: native routing menu is supported");
        check(CanRestore(SiegeAftermathMenuProfile.ContextualSummaryMenuId), "castle load: summary recovery does not need to replay mercy");
        check(!CanRestore(location: true), "castle load: repeated recovery preserves the existing encounter and followers");
        check(!CanRestore(castle: false), "castle load: towns and villages remain native");
        check(!CanRestore(matching: false), "castle load: missing or different encounter is never fabricated");
        check(!CanRestore(complex: false), "castle load: missing scene definitions are not fabricated");
        check(!CanRestore(mission: true), "castle load: active mission is never replaced");
        check(!CanRestore(battle: true), "castle load: battle and map-event context remain untouched");
        check(!CanRestore(captive: true), "castle load: captivity remains untouched");
        foreach (string menu in new[] { null, "", "castle", "castle_outside", "town", "encounter",
            "menu_settlement_taken_player_army_member", "AnimusForge_siege_intervention_done" })
            check(!CanRestore(menu), "castle load: no recovery from unrelated menu " + (menu ?? "null"));
        check(SiegeCastleAftermathLoadRecoveryPolicy.IsDiagnosticMenu(decision), "castle trace: decision is observed");
        check(SiegeCastleAftermathLoadRecoveryPolicy.IsDiagnosticMenu(SiegeAftermathMenuProfile.ContextualSummaryMenuId), "castle trace: summary is observed");
        check(SiegeCastleAftermathLoadRecoveryPolicy.IsCastleReturnMenu("castle_outside"), "castle trace: outside return ends observation");
        check(SiegeCastleAftermathLoadRecoveryPolicy.IsCastleReturnMenu("castle"), "castle trace: inside return ends observation");
        check(!SiegeCastleAftermathLoadRecoveryPolicy.IsDiagnosticMenu("encounter"), "castle trace: unrelated encounter ends observation");
    }
}
