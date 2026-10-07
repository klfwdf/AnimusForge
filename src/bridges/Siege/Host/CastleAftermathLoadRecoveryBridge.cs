using System;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>Load-finished/main-thread adapter. Does not settle, finish or restart any encounter.</summary>
internal static class CastleAftermathLoadRecoveryBridge
{
    private static Campaign _loadedCampaign;
    private static PlayerEncounter _loadedEncounter;
    private static Settlement _loadedCastle;
    private static int _remainingDiagnosticEntries;

    internal static void Reset()
    {
        _loadedCampaign = null;
        _loadedEncounter = null;
        _loadedCastle = null;
        _remainingDiagnosticEntries = 0;
    }

    internal static void OnGameLoadFinished()
    {
        Reset();
        try
        {
            Campaign campaign = Campaign.Current;
            string menuId = campaign?.CurrentMenuContext?.GameMenu?.StringId;
            MobileParty mainParty = MobileParty.MainParty;
            if (campaign == null || mainParty == null || Mission.Current != null
                || !SiegeAftermathMenuProfile.IsNativeOrContextualSummaryMenuId(menuId))
            {
                return;
            }
            Settlement castle = Settlement.CurrentSettlement;
            if (castle?.IsCastle != true)
            {
                return;
            }
            _loadedCampaign = campaign;
            _loadedEncounter = PlayerEncounter.Current;
            _loadedCastle = castle;
            _remainingDiagnosticEntries = 24;
            Log("load_finished", menuId);

            bool matchingEncounter = _loadedEncounter != null
                && PlayerEncounter.EncounterSettlement == castle
                && PlayerEncounter.EncounteredParty == castle.Party
                && (mainParty.CurrentSettlement == null || mainParty.CurrentSettlement == castle);
            bool canRestore = SiegeCastleAftermathLoadRecoveryPolicy.CanRestoreLocationEncounter(
                menuId,
                castle.IsCastle,
                matchingEncounter,
                castle.LocationComplex != null,
                PlayerEncounter.LocationEncounter != null,
                Mission.Current != null,
                PlayerEncounterCompat.GetBattleSafe() != null || mainParty.MapEvent != null,
                Hero.MainHero?.IsPrisoner == true);
            if (!canRestore)
            {
                Log("location_preserved", menuId);
                return;
            }

            // Native OnLoad creates this only while InsideSettlement (or in a waiting village).
            // A saved castle aftermath menu can be outside. Rebuild only the missing wrapper;
            // never call EnterSettlement, StartSettlementEncounter, Finish or ApplyAftermath.
            PlayerEncounter.LocationEncounter = new CastleEncounter(castle);
            Log("location_restored", menuId);
        }
        catch (Exception ex)
        {
            Log("load_recovery_failed", null, ex);
            Reset();
        }
    }

    internal static bool BeginMenuTrace(string menuId)
    {
        if (_loadedCampaign == null)
        {
            return false;
        }
        if (_loadedCampaign != Campaign.Current || Mission.Current != null
            || !SiegeCastleAftermathLoadRecoveryPolicy.IsDiagnosticMenu(menuId))
        {
            Reset();
            return false;
        }
        if (_loadedEncounter != PlayerEncounter.Current)
        {
            Log("encounter_changed_before_menu", menuId);
            Reset();
            return false;
        }
        Log("menu_init_begin", menuId);
        return true;
    }

    internal static void EndMenuTrace(string menuId, Exception error)
    {
        Log(error == null ? "menu_init_returned" : "menu_init_failed", menuId, error);
        if (error == null && SiegeCastleAftermathLoadRecoveryPolicy.IsCastleReturnMenu(menuId))
        {
            Reset();
        }
    }

    internal static bool BeginMercyTrace()
    {
        if (_loadedCampaign == null || _loadedCampaign != Campaign.Current
            || _loadedEncounter != PlayerEncounter.Current || Mission.Current != null
            || PlayerEncounter.EncounterSettlement != _loadedCastle)
        {
            return false;
        }
        Log("native_mercy_begin", SiegeAftermathMenuProfile.SettlementTakenPlayerLeaderMenuId);
        return true;
    }

    internal static void EndMercyTrace(Exception error)
    {
        Log(error == null ? "native_mercy_returned" : "native_mercy_failed", null, error);
    }

    private static void Log(string phase, string menuId, Exception error = null)
    {
        if (_loadedCampaign == null || _remainingDiagnosticEntries <= 0)
        {
            return;
        }
        _remainingDiagnosticEntries--;
        try
        {
            Settlement current = PlayerEncounter.EncounterSettlement;
            LocationEncounter location = PlayerEncounter.LocationEncounter;
            Logger.LogImmediate("CastleAftermathLoad",
                "Phase=" + phase
                + ", Menu=" + (menuId ?? Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "null")
                + ", LoadedCastle=" + (_loadedCastle?.StringId ?? "null")
                + ", EncounterCastle=" + (current?.StringId ?? "null")
                + ", EncounterMatches=" + (_loadedEncounter == PlayerEncounter.Current)
                + ", LocationCastle=" + (location?.Settlement?.StringId ?? "null")
                + ", InsideSettlement=" + PlayerEncounter.InsideSettlement
                + ", Battle=" + (PlayerEncounterCompat.GetBattleSafe() != null)
                + ", MainPartyMapEvent=" + (MobileParty.MainParty?.MapEvent != null)
                + ", OwnerClan=" + (current?.OwnerClan?.StringId ?? "null")
                + ", OwnerLeader=" + (current?.OwnerClan?.Leader?.StringId ?? "null")
                + (error == null ? "" : ", Error=" + error));
        }
        catch (Exception diagnosticError)
        {
            // Diagnostics must not replace the original menu exception or alter its propagation.
            Logger.LogImmediate("CastleAftermathLoad", "Phase=" + phase
                + ", DiagnosticError=" + diagnosticError.GetType().Name
                + (error == null ? "" : ", OriginalError=" + error));
        }
    }
}
