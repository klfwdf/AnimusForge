using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.CoupSystem;

internal sealed class CoupCampaignBehavior : CampaignBehaviorBase
{
    internal static CoupCampaignBehavior Instance { get; private set; }
    internal static CoupSession CurrentSession => Instance?._session;
    private CoupSession _session;
    private Mission _mission;
    // Kept after OnMissionEnded so native end-of-mission callbacks stay guarded; O(1) checks only.
    private Mission _endingMission;
    private bool _requeuePending;
    private bool _selectionOpen;
    private bool _processing;
    private bool _dispositionOpen;
    private bool _retryBlocked;
    private bool _reportOpen, _coronationOpen;
    private long _dispositionGeneration, _reportGeneration;
    private float _nextCheck;
    private long _runtimeToken;
    private string _saveJson;
    private bool _saveValid = true;
    private Campaign _campaign;

    public override void RegisterEvents()
    {
        Instance = this;
        _campaign = Campaign.Current;
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGame);
        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
    }

    // Leaving the town after the assault was registered is a retreat, not a pause.
    private void OnSettlementLeft(MobileParty party, Settlement settlement)
    {
        if (party != MobileParty.MainParty || _session == null || settlement?.StringId != _session.SettlementId) return;
        if (_session.IsCombatPhase || _session.Phase == CoupPhase.HallSelection)
        {
            SettlementEntryTroopSelectionBehavior.ClearArmedCoup();
            SetFailure(CoupUiText.CombatInterrupted);
        }
    }

    public override void SyncData(IDataStore store)
    {
        if (store.IsSaving)
        {
            if (_saveValid) _saveJson = _session != null ? JsonConvert.SerializeObject(_session) : null;
            CampaignSaveChunkHelper.SaveChunkedString(store, "_afCoupSession_v1", _saveJson, "Coup");
            return;
        }
        if (!store.IsLoading) return;
        ResetRuntime();
        _session = null;
        _saveValid = true;
        _saveJson = CampaignSaveChunkHelper.LoadChunkedString(store, "_afCoupSession_v1", "Coup");
        if (string.IsNullOrEmpty(_saveJson)) return;
        try
        {
            _session = JsonConvert.DeserializeObject<CoupSession>(_saveJson);
            if (_session == null || !_session.IsValid()) throw new InvalidOperationException("Invalid coup save.");
        }
        catch (Exception ex) { _session = null; _saveValid = false; Logger.Log("Coup", "Save rejected; stored evidence retained: " + ex); }
    }

    private void OnNewGame(CampaignGameStarter starter) { ResetRuntime(); _session = null; _saveJson = null; _saveValid = true; }
    private void OnGameLoaded(CampaignGameStarter starter)
    {
        ResetRuntime();
        if (!_saveValid) { Show(CoupUiText.SaveInvalid); return; }
        if (_session == null) return;
        if (_session.Phase == CoupPhase.Preparing) { _session = null; return; }
        // Saves are only possible on the map, between scenes. Keep the committed assault and
        // re-register the pending scene; the host clears its armed entry on load.
        if (_session.IsCombatPhase) _requeuePending = true;
    }

    private void ResetRuntime()
    {
        Instance = this;
        _runtimeToken++;
        _campaign = Campaign.Current;
        _mission = _endingMission = null;
        _requeuePending = false;
        _selectionOpen = _processing = _dispositionOpen = _retryBlocked = _reportOpen = _coronationOpen = false;
        _dispositionGeneration++;
        _reportGeneration++;
        _nextCheck = 0f;
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption("town", "af_armed_coup", CoupUiText.MenuArmedCoup, MenuCondition, _ => Begin(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_retry_war_registration", CoupUiText.MenuRetryWarRegistration, args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return CoupRebellionBridge.Instance?.HasPendingWarRegistration == true;
        }, _ => CoupRebellionBridge.Instance?.RetryPendingWarRegistration(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_enter_street", CoupUiText.MenuEnterStreet, args => SceneEntryCondition(args, CoupPhase.Street), _ => EnterSelectedScene(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_enter_hall", CoupUiText.MenuEnterHall, args => SceneEntryCondition(args, CoupPhase.Hall), _ => EnterSelectedScene(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_result", CoupUiText.MenuVictoryReport, args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return _session?.NeedsVictoryFeedback == true && Mission.Current == null
                && (Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement)?.StringId == _session.SettlementId;
        }, _ => { _retryBlocked = false; _nextCheck = 0f; }, false, -1);
        starter.AddGameMenuOption("town", "af_coup_resume", CoupUiText.MenuResumeSettlement, args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return _session?.Phase == CoupPhase.Suspended && (_session.IsResumable || !_session.CasualtiesCommitted);
        }, _ =>
        {
            if (_session?.Phase != CoupPhase.Suspended) return;
            _retryBlocked = false;
            // A won or lost coup resumes its own settlement; only mid-fight technical stops end neutral.
            if (_session.IsResumable) _session.Phase = _session.ResumePhase;
        }, false, -1);
    }

    private bool MenuCondition(MenuCallbackArgs args)
    {
        if (_session != null && (!_session.IsSettled || _session.NeedsVictoryFeedback)) return false;
        if (!CoupSettings.IsEnabled) return false;
        Settlement town = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        if (town?.IsTown != true || kingdom == null || kingdom.RulingClan == Clan.PlayerClan) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
        args.IsEnabled = CanBegin(town, out string reason);
        args.Tooltip = new TextObject(args.IsEnabled ? CoupUiText.BeginTooltip : reason);
        return true;
    }

    private bool SceneEntryCondition(MenuCallbackArgs args, CoupPhase phase)
    {
        if (_session?.Phase != phase || _selectionOpen || _mission != null || Mission.Current != null) return false;
        Settlement town = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        if (town?.StringId != _session.SettlementId) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
        args.IsEnabled = CanEnterSelectedScene(out string reason);
        args.Tooltip = new TextObject(args.IsEnabled ? CoupUiText.EnterTooltip : reason);
        return true;
    }

    private bool CanEnterSelectedScene(out string reason)
    {
        reason = CoupUiText.NoPendingEntry;
        if (!_saveValid || _session?.IsCombatPhase != true || _selectionOpen || _mission != null || Mission.Current != null) return false;
        if (!ValidatePreparedContext(out reason)) return false;
        reason = CoupUiText.SceneInterfaceUnavailable;
        if (!CoupGuards.MissionProtectionAvailable || !SettlementEntryTroopSelectionBehavior.IsAvailable || !CoupRebellionBridge.IsAvailable) return false;
        Settlement town = Settlement.Find(_session.SettlementId);
        reason = CoupUiText.EncounterChanged;
        if (PlayerEncounter.LocationEncounter?.Settlement != town || Campaign.Current?.IsMainHeroDisguised == true) return false;
        reason = CoupUiText.BlockingTownFlow;
        if (CoupGuards.HasBlockingHostFlow()) return false;
        return CoupSceneBridge.TryValidateScene(town, out reason);
    }

    private void EnterSelectedScene()
    {
        if (!CanEnterSelectedScene(out string reason)) { Show(reason); return; }
        try
        {
            Settlement town = Settlement.Find(_session.SettlementId);
            var location = town.LocationComplex.GetLocationWithId(_session.SceneLocationId);
            if (location == null) throw new InvalidOperationException(CoupUiText.SceneMissing);
            // Rebuild from the authoritative session immediately before the native SETS entry.
            QueueCurrentScene();
            if (!_session.IsCombatPhase) return;
            _requeuePending = false;
            Log("enter_scene_requested location=" + location.StringId);
            if (PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, null, null) == null)
                throw new InvalidOperationException(CoupUiText.SceneOpenFailed);
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "Dedicated scene entry failed: " + ex);
            NotifyTechnicalFailure(_mission, ex.Message);
        }
    }

    private bool CanBegin(Settlement town, out string reason)
    {
        reason = "";
        Clan clan = Clan.PlayerClan;
        Kingdom kingdom = clan?.Kingdom;
        Hero king = kingdom?.Leader;
        if (!CoupSettings.IsEnabled) reason = CoupUiText.CoupDisabled;
        else if (!_saveValid) reason = CoupUiText.SaveRecordInvalid;
        else if (!CoupGuards.MissionProtectionAvailable) reason = CoupUiText.MissionProtectionUnavailable;
        else if (!SettlementEntryTroopSelectionBehavior.IsAvailable || !CoupRebellionBridge.IsAvailable) reason = CoupUiText.SeamUnavailable;
        else if (_session != null && (!_session.IsSettled || _session.NeedsVictoryFeedback)) reason = CoupUiText.PendingCoupFeedback;
        else if (clan == null || kingdom == null || clan.Leader != Hero.MainHero || clan.IsUnderMercenaryService || clan.IsClanTypeMercenary || kingdom.RulingClan == clan)
            reason = CoupUiText.EligibleLeaderOnly;
        else if (town?.IsTown != true || town.MapFaction != kingdom) reason = CoupUiText.OwnTownRequired;
        else if (Hero.MainHero == null || !Hero.MainHero.IsAlive || Hero.MainHero.IsPrisoner || Hero.MainHero.IsWounded)
            reason = CoupUiText.PlayerCannotFight;
        else if (king == null || !king.IsAlive || king.IsPrisoner || king.CurrentSettlement != town)
            reason = CoupUiText.RulerMustBeHere;
        else if (Mission.Current != null || town.IsUnderSiege || PlayerEncounterCompat.HasEncounterBattleContext()
            || MobileParty.MainParty?.MapEvent != null || king.PartyBelongedTo?.MapEvent != null)
            reason = CoupUiText.BlockingBattle;
        else if (SettlementEntryTroopSelectionBehavior.HasPendingFlowForCoup() || CoupGuards.HasBlockingHostFlow())
            reason = CoupUiText.PendingConflict;
        else if (MobileParty.MainParty?.Army != null) reason = CoupUiText.LeaveArmy;
        else if (!CoupSettings.CheckNewCoupRequirements(clan.Tier, clan.Influence, HealthyRegulars(MobileParty.MainParty?.MemberRoster), out reason)) return false;
        else if (!CoupSceneBridge.TryValidateScene(town, out reason)) return false;
        return string.IsNullOrEmpty(reason);
    }

    private void Begin()
    {
        Settlement town = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        if (!CanBegin(town, out string reason)) { Show(reason); return; }
        Kingdom kingdom = Clan.PlayerClan.Kingdom;
        _session = new CoupSession
        {
            VictoryReportAcknowledged = false, CoronationRequested = false,
            BattleOptions = CoupSettings.CaptureForNewCoup(),
            EntryRequirements = CoupSettings.CaptureAdmissionForNewCoup(),
            SettlementId = town.StringId, KingdomId = kingdom.StringId, KingId = kingdom.Leader.StringId,
            OriginalRulingClanId = kingdom.RulingClan.StringId, OriginalOwnerClanId = town.OwnerClan.StringId,
            OriginalKingdomName = kingdom.Name.ToString(), OriginalKingdomShortName = kingdom.InformalName.ToString(),
            PlayerHealth = Hero.MainHero.HitPoints, KingHealth = Math.Max(1, kingdom.Leader.HitPoints), Phase = CoupPhase.Preparing
        };
        string id = _session.Id;
        long token = _runtimeToken;
        InformationManager.ShowInquiry(new InquiryData(CoupUiText.BeginTitle, CoupUiText.BeginBody(kingdom.Leader.Name.ToString(), town.Name.ToString(),
            _session.EntryRequirements.MinimumClanTier, _session.EntryRequirements.MinimumInfluence, _session.EntryRequirements.MinimumTroops,
            _session.BattleOptions.StreetAllyLimit, _session.BattleOptions.HallAllyLimit, _session.BattleOptions.GateGuardLimit,
            _session.BattleOptions.HallGuardLimit, _session.BattleOptions.DefenderWaveSize, _session.BattleOptions.DefenderWaveIntervalSeconds,
            _session.BattleOptions.MaxActiveDefenderWaves), true, true, CoupUiText.BeginConfirm, CoupUiText.Cancel,
            () => { if (IsCurrentUi(id, token, CoupPhase.Preparing)) OpenStreetSelection(id); },
            () => { if (IsCurrentUi(id, token, CoupPhase.Preparing)) _session = null; }), true);
    }

    private void OpenStreetSelection(string id)
    {
        if (_session?.Id != id || _session.Phase != CoupPhase.Preparing) return;
        if (!ValidatePreparedContext(out string preparationReason)) { _session = null; Show(preparationReason); return; }
        var available = SettlementEntryTroopSelectionBehavior.BuildCoupSelectableRoster(MobileParty.MainParty.MemberRoster);
        int limit = Math.Min(_session.BattleOptions.StreetAllyLimit, available.TotalManCount - 1);
        int minimum = _session.EntryRequirements.MinimumTroops;
        if (limit < minimum) { _session = null; Show(CoupUiText.InsufficientTroops); return; }
        _selectionOpen = true;
        long token = _runtimeToken;
        CoupTroopSelection.Open(available, limit, CoupUiText.StreetSelectionTitle, selected =>
        {
            if (!IsCurrentUi(id, token, CoupPhase.Preparing)) return;
            _selectionOpen = false;
            if (!ValidatePreparedContext(out string reason)) { _session = null; Show(reason); return; }
            if (selected.TotalManCount < minimum || selected.TotalManCount > limit || selected.TotalManCount >= HealthyRegulars(MobileParty.MainParty.MemberRoster))
            { _session = null; Show(CoupUiText.SelectionInvalid(minimum)); return; }
            foreach (TroopRosterElement entry in selected.GetTroopRoster())
            {
                if (entry.Character.IsHero || entry.WoundedNumber != 0 || entry.Number > HealthyCount(MobileParty.MainParty.MemberRoster, entry.Character))
                { _session = null; Show(CoupUiText.RosterChanged); return; }
                for (int i = 0; i < entry.Number; i++) AddTroop(entry.Character, MobileParty.MainParty, CoupTroopRole.Ally);
            }
            BuildDefenders(Settlement.Find(_session.SettlementId));
            _session.Phase = CoupPhase.Street;
            _session.Started = true;
            QueueCurrentScene();
            Log("street_selection_complete");
            Show(CoupUiText.StreetRegistered);
        }, () => { if (IsCurrentUi(id, token, CoupPhase.Preparing)) { _selectionOpen = false; _session = null; } }, minimum);
    }

    private bool ValidatePreparedContext(out string reason)
    {
        reason = CoupUiText.ContextChanged;
        if (_session == null || Hero.MainHero?.IsAlive != true || Hero.MainHero.IsPrisoner
            || Clan.PlayerClan?.Leader != Hero.MainHero || Clan.PlayerClan.IsUnderMercenaryService
            || Clan.PlayerClan.IsClanTypeMercenary || MobileParty.MainParty?.Army != null) return false;
        if (!_session.Started && !CoupSettings.IsEnabled) return false;
        if (!CheckSessionAdmission(out string admissionReason))
        { reason = admissionReason; return false; }
        Settlement town = Settlement.Find(_session.SettlementId);
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        // Also used by menu condition refresh: use the kingdom's current leader, no global hero scan.
        Hero king = kingdom?.Leader;
        return town?.IsTown == true && !town.IsUnderSiege && kingdom?.StringId == _session.KingdomId
            && kingdom.RulingClan?.StringId == _session.OriginalRulingClanId && king?.StringId == _session.KingId
            && king?.IsAlive == true && !king.IsPrisoner && king.CurrentSettlement == town
            && town.MapFaction == kingdom && MobileParty.MainParty?.CurrentSettlement == town
            && !PlayerEncounterCompat.HasEncounterBattleContext() && MobileParty.MainParty.MapEvent == null;
    }

    // Admission is frozen at the confirmation window and stops applying after registration.
    private bool CheckSessionAdmission(out string reason)
    {
        reason = CoupUiText.AdmissionUnavailable;
        if (_session == null) return false;
        if (_session.Started) { reason = ""; return true; }
        Clan clan = Clan.PlayerClan;
        if (clan == null || _session.EntryRequirements == null || !_session.EntryRequirements.IsValid() || _session.BattleOptions == null) return false;
        return _session.EntryRequirements.Check(clan.Tier, clan.Influence,
            HealthyRegulars(MobileParty.MainParty?.MemberRoster), _session.BattleOptions.StreetAllyLimit, out reason);
    }

    private void BuildDefenders(Settlement town)
    {
        Hero king = FindKing();
        var sources = new List<MobileParty>();
        void Add(MobileParty party)
        {
            if (party != null && party != MobileParty.MainParty && party.ActualClan != Clan.PlayerClan && !sources.Contains(party)) sources.Add(party);
        }
        Add(king?.PartyBelongedTo);
        Add(town.Town.GarrisonParty);
        Add(town.MilitiaPartyComponent?.MobileParty);
        // Lord parties only: caravans and villagers are not the town's armed defence.
        foreach (MobileParty party in town.Parties)
            if (party.IsLordParty && party.IsActive && party.MapFaction == town.MapFaction && party.CurrentSettlement == town) Add(party);
        int hall = 0, gate = 0;
        foreach (MobileParty party in sources)
        {
            foreach (TroopRosterElement entry in party.MemberRoster.GetTroopRoster().OrderByDescending(e => e.Character.Tier))
            {
                if (entry.Character.IsHero) continue;
                for (int i = 0; i < entry.Number - entry.WoundedNumber; i++)
                {
                    CoupTroopRole role = hall < _session.BattleOptions.HallGuardLimit ? CoupTroopRole.HallGuard
                        : gate < _session.BattleOptions.GateGuardLimit ? CoupTroopRole.GateGuard : CoupTroopRole.StreetDefender;
                    if (role == CoupTroopRole.HallGuard) hall++;
                    if (role == CoupTroopRole.GateGuard) gate++;
                    AddTroop(entry.Character, party, role);
                }
            }
        }
    }

    private void AddTroop(CharacterObject character, MobileParty source, CoupTroopRole role)
    {
        _session.Troops.Add(new CoupTroopRecord { Id = _session.Troops.Count.ToString(), CharacterId = character.StringId, SourcePartyId = source.StringId, Role = role });
    }

    // The single place that arms the host SETS entry: exact location, own allies, own defender records.
    private void QueueCurrentScene()
    {
        bool hall = _session.Phase == CoupPhase.Hall;
        var allies = TroopRoster.CreateDummyTroopRoster();
        foreach (CoupTroopRecord troop in _session.Troops)
        {
            if (troop.Role != CoupTroopRole.Ally || troop.Removed || (hall && !troop.HallSelected)) continue;
            CharacterObject character = CharacterObject.Find(troop.CharacterId);
            if (character != null) allies.AddToCounts(character, 1);
        }
        if (allies.TotalManCount < 1) { SetFailure(CoupUiText.NoAssaultTroops); return; }
        SettlementEntryTroopSelectionBehavior.QueueArmedCoup(_session.SettlementId, _session.SceneLocationId, allies, _session.PendingDefenders(hall), _session.BattleOptions);
    }

    internal static void NotifySetsMissionReady(IMission mission)
    {
        Instance?.OnMissionStarted(mission);
    }

    private void OnMissionStarted(IMission mission)
    {
        if (mission is not Mission concrete || _mission != null || _session?.IsCombatPhase != true) return;
        Settlement town = Settlement.Find(_session.SettlementId);
        if ((Settlement.CurrentSettlement ?? PlayerEncounter.LocationEncounter?.Settlement) != town) return;
        // Only the host logic armed for this phase's exact location joins; any other visit is ordinary.
        if (!SettlementEntryTroopSelectionBehavior.HasArmedSetsLogic(concrete)) return;
        _mission = concrete;
        _endingMission = null;
        _session.SceneEntered = true;
        if (_session.Phase == CoupPhase.Hall) _session.HallEntered = true;
        concrete.AddMissionBehavior(new CoupMissionBehavior(_session, town));
        Log("mission_started");
    }

    private void OnMissionEnded(IMission mission)
    {
        if (!ReferenceEquals(_mission, mission)) return;
        _endingMission = _mission;
        _mission = null;
        _dispositionOpen = false;
        _dispositionGeneration++; // Any scene-owned popup callback is stale after closure.
        SettlementEntryTroopSelectionBehavior.ClearArmedCoup();
        if (_session?.IsCombatPhase == true) SetFailure(CoupUiText.CombatInterrupted);
    }

    internal static void NotifyFollowerCasualty(CharacterObject character, bool killed, string role)
    {
        CoupSession session = Instance?._session;
        if (session == null) return;
        bool hall = session.Phase == CoupPhase.Hall;
        CoupTroopRecord record = session.Troops.FirstOrDefault(troop => troop.Role == CoupTroopRole.Ally && !troop.Removed
            && (!hall || troop.HallSelected) && troop.CharacterId == character?.StringId);
        record?.TryRecordCasualty(killed);
    }

    // recordId is the coup record the host spawned; source party and role come from that record only.
    internal static void NotifyDefenderCasualty(CharacterObject character, bool killed, string recordId)
    {
        if (string.IsNullOrEmpty(recordId)) return;
        CoupTroopRecord record = Instance?._session?.Troops.FirstOrDefault(troop => troop.Role != CoupTroopRole.Ally && troop.Id == recordId);
        if (record == null || record.CharacterId != character?.StringId) return;
        record.TryRecordCasualty(killed);
    }

    internal static bool IsHallObjectiveComplete()
    {
        CoupSession session = CurrentSession;
        return session?.Phase == CoupPhase.Hall && session.KingSubdued
            && !session.Troops.Any(troop => troop.Role == CoupTroopRole.HallGuard && !troop.Removed);
    }

    internal static bool IsMissionActive(Mission mission)
    {
        // Called from per-frame native patches in every mission: reference checks only, no behavior scans.
        CoupCampaignBehavior instance = Instance;
        if (mission == null || instance == null || instance._session == null) return false;
        return ReferenceEquals(instance._mission, mission) || ReferenceEquals(instance._endingMission, mission);
    }
    internal static void NotifyStreetComplete(Mission mission)
    {
        if (!IsMissionActive(mission) || CurrentSession.Phase != CoupPhase.Street) return;
        if (!CurrentSession.TryAdvance(CoupPhase.Street, CoupPhase.HallSelection)) return;
        CurrentSession.GateBreached = true;
        Instance.Log("street_complete");
    }
    internal static void NotifyVictory(Mission mission)
    {
        if (!IsMissionActive(mission) || CurrentSession.Phase != CoupPhase.Hall || !CurrentSession.KingSubdued) return;
        if (!CurrentSession.TryAdvance(CoupPhase.Hall, CoupPhase.AwaitingResolution)) return;
        Instance.Log("king_subdued");
    }
    internal static void NotifyDefeat(Mission mission, string reason)
    {
        if (IsMissionActive(mission)) Instance.SetFailure(reason);
    }
    internal static void NotifyTechnicalFailure(Mission mission, string reason)
    {
        if (Instance?._session == null) return;
        if (mission != null && !IsMissionActive(mission)) return;
        if (Instance._session.Phase != CoupPhase.Suspended) Instance._session.ResumePhase = Instance._session.Phase;
        Instance._session.Phase = CoupPhase.Suspended;
        Instance._session.FailureReason = reason;
        SettlementEntryTroopSelectionBehavior.ClearArmedCoup();
        Instance.Log("suspended: " + reason);
        Show((Instance._session.ResumePhase == CoupPhase.AwaitingResolution ? CoupUiText.ResolutionPendingPrefix : CoupUiText.SuspendedPrefix) + reason + (Instance._session.HasPoliticalCommit
            ? CoupUiText.PoliticalChangesRetained : CoupUiText.CasualtiesRetained));
    }
    private void SetFailure(string reason)
    {
        _session.Phase = CoupPhase.Failed;
        _session.FailureReason = reason;
        Log("defeated: " + reason);
    }

    // Town menus stop campaign time. Drive pending scene/result work from the existing
    // application tick, using real time and an O(1) idle gate rather than Campaign.Tick.
    internal void OnEngineTick(float dt)
    {
        if (Campaign.Current != _campaign || _session == null || (_session.IsSettled && !_session.NeedsVictoryFeedback) || _processing || _selectionOpen || _dispositionOpen || _retryBlocked || _reportOpen || _coronationOpen
            || (_session.Phase == CoupPhase.Suspended && _session.CasualtiesCommitted && !_session.IsResumable)) return;
        _nextCheck -= dt;
        if (_nextCheck > 0f) return;
        _nextCheck = 0.25f;
        if (Mission.Current != null || !(Game.Current?.GameStateManager?.ActiveState is MapState)) return;
        _endingMission = null;
        if (_session.Phase == CoupPhase.Completed)
        {
            if (InformationManager.IsAnyInquiryActive() || MBInformationManager.GetIsAnySceneNotificationActive() == true) return;
            try { PresentVictoryFeedback(); }
            catch (Exception ex)
            {
                _reportOpen = _coronationOpen = false;
                _retryBlocked = true;
                Log("victory_feedback_deferred: " + ex.Message);
                Show(CoupUiText.ReportNotOpened);
            }
            return;
        }
        // Walking to the next scene needs no time control; leaving town is handled as retreat.
        if (_session.IsCombatPhase)
        {
            if (_requeuePending) { _requeuePending = false; QueueCurrentScene(); }
            return;
        }
        if (InformationManager.IsAnyInquiryActive() || MBInformationManager.GetIsAnySceneNotificationActive() == true) return;
        _processing = true;
        try
        {
            // A neutral technical stop only writes the casualties already recorded; resumable ones wait for the menu retry.
            if (_session.Phase == CoupPhase.Suspended)
            {
                if (!_session.IsResumable && !_session.CasualtiesCommitted) CommitCasualties();
                return;
            }
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            if (_session.Phase == CoupPhase.HallSelection) { OpenHallSelection(); return; }
            if (_session.Phase == CoupPhase.AwaitingResolution)
            {
                if (_session.Disposition == CoupKingDisposition.Undecided) { OpenDisposition(); return; }
                CommitVictory();
            }
            else if (_session.Phase == CoupPhase.Failed && !_session.IsSettled) CommitFailure();
        }
        catch (Exception ex) { NotifyTechnicalFailure(null, ex.Message); _retryBlocked = true; Logger.Log("Coup", ex.ToString()); }
        finally { _processing = false; }
    }

    private void OpenHallSelection()
    {
        var survivors = _session.Troops.Where(t => t.Role == CoupTroopRole.Ally && !t.Removed).ToList();
        if (survivors.Count == 0) { SetFailure(CoupUiText.NoAssaultTroops); return; }
        var roster = TroopRoster.CreateDummyTroopRoster();
        foreach (var troop in survivors) roster.AddToCounts(CharacterObject.Find(troop.CharacterId), 1);
        string id = _session.Id;
        long token = _runtimeToken;
        _selectionOpen = true;
        int limit = Math.Min(_session.BattleOptions.HallAllyLimit, survivors.Count);
        CoupTroopSelection.Open(roster, limit, CoupUiText.HallSelectionTitle, selected =>
        {
            if (!IsCurrentUi(id, token, CoupPhase.HallSelection)) return;
            _selectionOpen = false;
            if (selected.TotalManCount > limit) { NotifyTechnicalFailure(null, CoupUiText.HallSelectionLimitExceeded); return; }
            foreach (var troop in survivors) troop.HallSelected = false;
            foreach (var entry in selected.GetTroopRoster())
            {
                var matching = survivors.Where(t => t.CharacterId == entry.Character.StringId).OrderByDescending(t => t.Health).Take(entry.Number).ToList();
                if (matching.Count != entry.Number) { NotifyTechnicalFailure(null, CoupUiText.HallRosterMismatch); return; }
                foreach (var troop in matching) troop.HallSelected = true;
            }
            if (!survivors.Any(t => t.HallSelected)) { SetFailure(CoupUiText.HallSelectionAbandoned); return; }
            // Unselected survivors stay outside the hall; they are not casualties and keep Removed=false.
            _session.Phase = CoupPhase.Hall;
            QueueCurrentScene();
            if (_session.Phase == CoupPhase.Hall)
            {
                Log("hall_selection_complete");
                Show(CoupUiText.HallRegistered);
            }
        }, () =>
        {
            if (!IsCurrentUi(id, token, CoupPhase.HallSelection)) return;
            _selectionOpen = false;
            SetFailure(CoupUiText.HallSelectionCancelled);
        });
    }

    internal static bool TryOpenHallDisposition(Mission mission)
    {
        CoupCampaignBehavior owner = Instance;
        if (owner == null || !ReferenceEquals(owner._mission, mission) || Mission.Current != mission
            || owner._session?.NeedsHallDisposition != true) return false;
        if (owner._dispositionOpen) return true;
        if (InformationManager.IsAnyInquiryActive() || MBInformationManager.GetIsAnySceneNotificationActive() == true) return false;
        owner.OpenDisposition(mission);
        return true;
    }

    private void OpenDisposition(Mission mission = null)
    {
        string id = _session.Id;
        long token = _runtimeToken;
        long generation = ++_dispositionGeneration;
        _dispositionOpen = true;
        bool canCapture = CoupGuards.CaptivityProtectionAvailable;
        var choices = new List<InquiryElement>
        {
            new InquiryElement(CoupKingDisposition.Release, CoupUiText.ReleaseFormerRuler, null),
            new InquiryElement(CoupKingDisposition.Capture, CoupUiText.CaptureFormerRuler, null, canCapture,
                canCapture ? CoupUiText.CaptureExcludesFamily : CoupUiText.CaptureUnavailable)
        };
        string name = FindKing()?.Name?.ToString() ?? CoupUiText.FormerRulerFallback;
        var data = new MultiSelectionInquiryData(CoupUiText.DispositionTitle, CoupUiText.DispositionBody(name),
            choices, true, 1, 1, CoupUiText.DispositionConfirm, CoupUiText.DecideLater,
            selected => SelectDisposition(id, token, generation, mission, selected),
            _ =>
            {
                if (!IsDispositionUiCurrent(id, token, generation, mission)) return;
                _dispositionOpen = false;
                _dispositionGeneration++;
                if (mission != null) Show(CoupUiText.DispositionNotConfirmed);
            });
        try { MBInformationManager.ShowMultiSelectionInquiry(data, true); }
        catch { _dispositionOpen = false; _dispositionGeneration++; throw; }
    }

    private bool IsDispositionUiCurrent(string id, long token, long generation, Mission mission)
    {
        return _dispositionOpen && generation == _dispositionGeneration
            && IsCurrentUi(id, token, CoupPhase.AwaitingResolution) && _session.NeedsHallDisposition
            && (mission == null ? Mission.Current == null
                : ReferenceEquals(_mission, mission) && Mission.Current == mission && !mission.IsMissionEnding);
    }

    private void SelectDisposition(string id, long token, long generation, Mission mission, List<InquiryElement> selected)
    {
        if (!IsDispositionUiCurrent(id, token, generation, mission)) return;
        // The negative/escape callback only dismisses; it is never a Capture shortcut.
        if (selected == null || selected.Count != 1 || !selected[0].IsEnabled
            || !(selected[0].Identifier is CoupKingDisposition disposition)
            || (disposition != CoupKingDisposition.Release && disposition != CoupKingDisposition.Capture)
            || (disposition == CoupKingDisposition.Capture && !CoupGuards.CaptivityProtectionAvailable))
        {
            _dispositionOpen = false;
            _dispositionGeneration++;
            return;
        }
        _dispositionOpen = false;
        _dispositionGeneration++;
        _session.Disposition = disposition;
        if (mission == null) return; // Legacy/map recovery: political effects remain on the map tick.
        try
        {
            var logic = mission.GetMissionBehavior<CoupMissionBehavior>();
            if (logic == null || !logic.CompleteVictoryAndLeave())
                throw new InvalidOperationException(CoupUiText.LeaveNotReady);
        }
        catch (Exception ex)
        {
            NotifyTechnicalFailure(mission, CoupUiText.DispositionLeaveFailed(ex.Message));
            mission.EndMission();
        }
    }

    private void CommitVictory()
    {
        if (!_session.IsValid() || !_session.Started || !_session.KingSubdued || _session.Disposition == CoupKingDisposition.Undecided) throw new InvalidOperationException("政变胜利状态或旧王处置不完整，禁止政治结算。");
        Settlement town = Settlement.Find(_session.SettlementId);
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        Hero king = FindKing();
        if (town == null || kingdom?.StringId != _session.KingdomId || kingdom.IsEliminated || king?.IsAlive != true
            || (kingdom.RulingClan != Clan.PlayerClan && kingdom.RulingClan?.StringId != _session.OriginalRulingClanId)
            || town.MapFaction != kingdom || Clan.PlayerClan.Leader != Hero.MainHero)
            throw new InvalidOperationException("战役身份发生变化，停止自动政治结算。");
        CommitCasualties();
        _session.PoliticalCommitStarted = true;
        if (!_session.RulingClanCommitted)
        {
            // The coup outcome fact is the bulletin story for this throne change.
            MyBehavior.SuppressWorldBulletinRulerChange = true;
            try { if (kingdom.RulingClan != Clan.PlayerClan) ChangeRulingClanAction.Apply(kingdom, Clan.PlayerClan); }
            finally { MyBehavior.SuppressWorldBulletinRulerChange = false; }
            if (kingdom.RulingClan != Clan.PlayerClan) throw new InvalidOperationException("王权转移未生效。");
            _session.RulingClanCommitted = true;
        }
        if (!_session.TownCommitted)
        {
            if (town.OwnerClan != Clan.PlayerClan) ChangeOwnerOfSettlementAction.ApplyByRebellion(Hero.MainHero, town);
            if (town.OwnerClan != Clan.PlayerClan) throw new InvalidOperationException("城镇转移未生效。");
            _session.TownCommitted = true;
        }
        if (!_session.CustodyCommitted)
        {
            king.HitPoints = Math.Max(1, Math.Min(king.HitPoints, (int)_session.KingHealth));
            if (_session.Disposition == CoupKingDisposition.Capture)
            {
                if (!CoupGuards.CaptivityProtectionAvailable) throw new InvalidOperationException("拘押保护不可用，请恢复兼容组件后结算。");
                if (!king.IsPrisoner) TakePrisonerAction.Apply(MobileParty.MainParty.Party, king);
                if (!king.IsPrisoner || king.PartyBelongedToAsPrisoner != MobileParty.MainParty.Party)
                    throw new InvalidOperationException("旧王拘押未生效。");
                CoupCaptivityBehavior.RegisterDetention(_session.Id, king);
            }
            _session.CustodyCommitted = true;
        }
        var owner = CoupRebellionBridge.Instance;
        if (owner == null) throw new InvalidOperationException("AF 事件与叛乱系统不可用。");
        if (!_session.FactsCommitted)
        {
            if (!owner.TryRecordCoupOutcome(_session, king, town, true, _session.Disposition == CoupKingDisposition.Capture, out string message)) throw new InvalidOperationException(message);
            _session.FactsCommitted = true;
        }
        if (!_session.RebellionQueued)
        {
            if (!owner.TryQueueCoupRebellion(_session, kingdom, out string message)) throw new InvalidOperationException(message);
            _session.RebellionQueued = true;
            Show(message);
        }
        _session.Phase = CoupPhase.Completed;
        _session.AftermathPending = false; // This coup flow intentionally does not enter siege aftermath.
        _session.VictoryReportAcknowledged ??= false;
        _session.CoronationRequested ??= false;
        Log("completed");
        Show(CoupUiText.PoliticalSettlementComplete);
    }

    private void PresentVictoryFeedback()
    {
        if (!_session.NeedsVictoryFeedback || !_session.HasConfirmedVictory || !_session.IsValid()) return;
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
        if (_session.CoronationRequested == false)
        {
            // Consume before enqueue: retries/save-load must never submit the same animation twice.
            _session.CoronationRequested = true;
            Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == _session.KingdomId);
            if (kingdom?.Leader == Hero.MainHero && Hero.MainHero?.IsAlive == true
                && MBInformationManager.GetIsAnySceneNotificationActive().HasValue)
            {
                string id = _session.Id;
                long token = _runtimeToken;
                _coronationOpen = true;
                try
                {
                    MBInformationManager.ShowSceneNotification(new CoupBecomeKingSceneNotification(Hero.MainHero, () =>
                    {
                        if (!IsCurrentUi(id, token, CoupPhase.Completed)) return;
                        _coronationOpen = false;
                        _nextCheck = 0f; // Report opens on the next safe map tick, never inside the cutscene callback.
                        Log("coronation_closed");
                    }));
                    Log("coronation_requested");
                    return;
                }
                catch (Exception ex)
                {
                    _coronationOpen = false;
                    Log("coronation_unavailable: " + ex.Message);
                    Show(CoupUiText.CoronationUnavailable);
                }
            }
            else Log("coronation_skipped: ruler changed or scene notification UI unavailable");
        }
        ShowVictoryReport();
    }

    private void ShowVictoryReport()
    {
        if (_reportOpen || !_session.NeedsVictoryFeedback || !_session.HasConfirmedVictory) return;
        string id = _session.Id;
        long token = _runtimeToken;
        long generation = ++_reportGeneration;
        Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == _session.KingdomId);
        Settlement town = Settlement.Find(_session.SettlementId);
        Hero king = FindKing();
        string report = CoupOutcomeReport.BuildPlayerVictory(_session,
            kingdom?.Name?.ToString() ?? _session.OriginalKingdomName ?? "former kingdom",
            kingdom?.Leader?.Name?.ToString() ?? "current ruler unknown",
            town?.Name?.ToString() ?? "target town", town?.OwnerClan?.Name?.ToString() ?? "current owner unknown",
            king?.Name?.ToString() ?? "former ruler", king?.IsPrisoner == true && king.PartyBelongedToAsPrisoner == MobileParty.MainParty?.Party);
        _reportOpen = true;
        try
        {
            InformationManager.ShowInquiry(new InquiryData(CoupUiText.VictoryTitle, report, true, false,
                CoupUiText.ConfirmReturnTown, "", () =>
                {
                    if (!_reportOpen || generation != _reportGeneration || !IsCurrentUi(id, token, CoupPhase.Completed)) return;
                    _reportOpen = false;
                    _reportGeneration++;
                    _session.VictoryReportAcknowledged = true;
                    Log("victory_report_acknowledged");
                    // Do not teleport or hijack an unrelated encounter if the player has since moved.
                    if (Mission.Current == null && !PlayerEncounterCompat.HasEncounterBattleContext()
                        && (Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement)?.StringId == _session.SettlementId)
                    {
                        try { GameMenu.SwitchToMenu("town"); }
                        catch (Exception ex) { Log("return_to_town_deferred: " + ex.Message); }
                    }
                }, () =>
                {
                    if (_reportOpen && generation == _reportGeneration && IsCurrentUi(id, token, CoupPhase.Completed))
                    {
                        _reportOpen = false;
                        _reportGeneration++;
                    }
                }), true);
            Log("victory_report_opened");
        }
        catch { _reportOpen = false; throw; }
    }

    private void CommitFailure()
    {
        if (PlayerEncounterCompat.HasEncounterBattleContext()) throw new InvalidOperationException("新的战役战斗已开始，不能自动结算旧政变。");
        CommitCasualties();
        _session.PoliticalCommitStarted = true;
        if (!_session.DefectionCommitted)
        {
            if (Clan.PlayerClan?.Kingdom?.StringId == _session.KingdomId)
                ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(Clan.PlayerClan, true);
            Kingdom original = Kingdom.All.FirstOrDefault(k => k.StringId == _session.KingdomId);
            if (Clan.PlayerClan?.Kingdom == null && original != null && !original.IsEliminated && !Clan.PlayerClan.IsAtWarWith(original))
                DeclareWarAction.ApplyByRebellion(Clan.PlayerClan, original);
            if (Clan.PlayerClan == null || Clan.PlayerClan.Kingdom != null || (original != null && !original.IsEliminated && !Clan.PlayerClan.IsAtWarWith(original)))
                throw new InvalidOperationException("叛离或宣战结果不一致，停止后续结算。");
            _session.DefectionCommitted = true;
        }
        if (!_session.WithdrawalCommitted)
            CommitWithdrawal();
        if (!_session.FactsCommitted)
        {
            var owner = CoupRebellionBridge.Instance;
            if (owner == null || !owner.TryRecordCoupOutcome(_session, FindKing(), Settlement.Find(_session.SettlementId), false, false, out _))
                throw new InvalidOperationException("政变失败事实暂时无法写入。");
            _session.FactsCommitted = true;
        }
        Log("failure_committed");
        Show(CoupUiText.FailureCommitted);
    }

    private void CommitWithdrawal()
    {
        MobileParty party = MobileParty.MainParty;
        if (party == null) throw new InvalidOperationException("接应队伍不可用，不能确认撤出。");
        var location = PlayerEncounter.LocationEncounter;
        bool inTarget = party.CurrentSettlement?.StringId == _session.SettlementId;
        bool targetLocation = location?.Settlement?.StringId == _session.SettlementId;
        // A delayed retry may run in another town or encounter. Leaving the original town
        // already satisfies withdrawal; never finish or leave the player's new encounter.
        if (!inTarget && !targetLocation)
        {
            _session.WithdrawalCommitted = true;
            Log("withdrawal_already_left_target; current encounter preserved");
            return;
        }
        var encountered = PlayerEncounter.EncounteredParty;
        if ((location != null && !targetLocation) || (party.CurrentSettlement != null && !inTarget)
            || (encountered != null && encountered.Settlement?.StringId != _session.SettlementId)
            || (PlayerEncounter.Current != null && !targetLocation && encountered?.Settlement?.StringId != _session.SettlementId))
            throw new InvalidOperationException("当前遭遇已改变，不能替其他遭遇执行撤出。");
        if (PlayerEncounter.Current != null) PlayerEncounter.Finish(true);
        if (party.CurrentSettlement?.StringId == _session.SettlementId) LeaveSettlementAction.ApplyForParty(party);
        if (party.CurrentSettlement?.StringId == _session.SettlementId
            || PlayerEncounter.LocationEncounter?.Settlement?.StringId == _session.SettlementId)
            throw new InvalidOperationException("接应队伍尚未完整退出城镇遭遇。");
        _session.WithdrawalCommitted = true;
    }

    private void CommitCasualties()
    {
        if (_session.CasualtiesCommitted) return;
        var parties = new Dictionary<string, MobileParty>(StringComparer.Ordinal);
        foreach (MobileParty party in MobileParty.All) if (party?.StringId != null) parties[party.StringId] = party;
        foreach (CoupTroopRecord troop in _session.Troops)
        {
            if (troop.CasualtyCommitted || (!troop.Killed && !troop.Wounded)) continue;
            CharacterObject character = CharacterObject.Find(troop.CharacterId);
            // A disbanded party or an already-depleted stack has nothing left to deduct; retrying
            // would only wedge the session forever, so record the skip and move on.
            if (!parties.TryGetValue(troop.SourcePartyId, out MobileParty source) || character == null
                || HealthyCount(source.MemberRoster, character) < 1)
            {
                Log("casualty_skipped source=" + troop.SourcePartyId + " troop=" + troop.CharacterId);
                troop.CasualtyCommitted = true;
                continue;
            }
            source.MemberRoster.AddToCounts(character, troop.Killed ? -1 : 0, false, troop.Killed ? 0 : 1);
            troop.CasualtyCommitted = true;
        }
        if (Hero.MainHero?.IsAlive == true) Hero.MainHero.HitPoints = Math.Max(1, Math.Min(Hero.MainHero.HitPoints, (int)_session.PlayerHealth));
        _session.CasualtiesCommitted = true;
    }

    private Hero FindKing() => _session == null ? null : Hero.FindFirst(h => h.StringId == _session.KingId);
    private bool IsCurrentUi(string id, long token, CoupPhase phase) => ReferenceEquals(Instance, this)
        && Campaign.Current == _campaign && token == _runtimeToken && _session?.Id == id && _session.Phase == phase;
    private static int HealthyCount(TroopRoster roster, CharacterObject character)
    {
        int index = roster.FindIndexOfTroop(character);
        return index < 0 ? 0 : Math.Max(0, roster.GetElementNumber(index) - roster.GetElementWoundedNumber(index));
    }
    private static int HealthyRegulars(TroopRoster roster)
    {
        if (roster == null) return 0;
        int total = 0;
        for (int i = 0; i < roster.Count; i++)
            if (!roster.GetCharacterAtIndex(i).IsHero)
                total += Math.Max(0, roster.GetElementNumber(i) - roster.GetElementWoundedNumber(i));
        return total;
    }
    private static void Show(string message) { if (!string.IsNullOrEmpty(message)) InformationManager.DisplayMessage(new InformationMessage(CoupUiText.Prefix + message)); }
    private void Log(string message) => Logger.Log("Coup", "event=" + _session?.Id + " phase=" + _session?.Phase + " town=" + _session?.SettlementId + " " + message);
}
