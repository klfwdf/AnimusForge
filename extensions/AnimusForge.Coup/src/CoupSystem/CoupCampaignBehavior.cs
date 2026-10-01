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
            SetFailure("政变进行中离开了城镇。");
        }
    }

    public override void SyncData(IDataStore store)
    {
        if (store.IsSaving && _saveValid) _saveJson = _session != null ? JsonConvert.SerializeObject(_session) : null;
        store.SyncData("_afCoupSession_v1", ref _saveJson);
        if (!store.IsLoading) return;
        ResetRuntime();
        _session = null;
        _saveValid = true;
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
        if (!_saveValid) { Show("政变存档状态异常，已保留原记录并停用新政变，请查看日志。"); return; }
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
        _selectionOpen = _processing = _dispositionOpen = _retryBlocked = false;
        _nextCheck = 0f;
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption("town", "af_armed_coup", "宣权篡位", MenuCondition, _ => Begin(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_enter_street", "率领政变突击队攻入城镇", args => SceneEntryCondition(args, CoupPhase.Street), _ => EnterSelectedScene(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_enter_hall", "率领政变突击队攻入领主大厅", args => SceneEntryCondition(args, CoupPhase.Hall), _ => EnterSelectedScene(), false, -1);
        starter.AddGameMenuOption("town", "af_coup_resume", "重试政变结算", args =>
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
        if (_session != null && !_session.IsSettled) return false;
        if (!CoupSettings.IsEnabled) return false;
        Settlement town = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        if (town?.IsTown != true || kingdom == null || kingdom.RulingClan == Clan.PlayerClan) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
        args.IsEnabled = CanBegin(town, out string reason);
        args.Tooltip = new TextObject(args.IsEnabled
            ? "按篡位 MCM 的人数要求挑选突击队，攻取大厅并夺取王位；另留1名健康士兵接应。失败将带地叛离并开战。" : reason);
        return true;
    }

    private bool SceneEntryCondition(MenuCallbackArgs args, CoupPhase phase)
    {
        if (_session?.Phase != phase || _selectionOpen || _mission != null || Mission.Current != null) return false;
        Settlement town = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        if (town?.StringId != _session.SettlementId) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
        args.IsEnabled = CanEnterSelectedScene(out string reason);
        args.Tooltip = new TextObject(args.IsEnabled ? "按本次已选名单进场；倒地或撤退将判定政变失败。" : reason);
        return true;
    }

    private bool CanEnterSelectedScene(out string reason)
    {
        reason = "当前没有等待进场的政变突击队。";
        if (!_saveValid || _session?.IsCombatPhase != true || _selectionOpen || _mission != null || Mission.Current != null) return false;
        if (!ValidatePreparedContext(out reason)) return false;
        reason = "政变场景接口未就绪，请检查日志。";
        if (!CoupGuards.MissionProtectionAvailable || !SettlementEntryTroopSelectionBehavior.IsAvailable || !CoupRebellionBridge.IsAvailable) return false;
        Settlement town = Settlement.Find(_session.SettlementId);
        reason = "当前城镇遭遇已改变，无法进入政变场景。";
        if (PlayerEncounter.LocationEncounter?.Settlement != town || Campaign.Current?.IsMainHeroDisguised == true) return false;
        reason = "当前还有城镇冲突或战后处置待完成。";
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
            if (location == null) throw new InvalidOperationException("政变目标场景不存在。");
            // Rebuild from the authoritative session immediately before the native SETS entry.
            QueueCurrentScene();
            if (!_session.IsCombatPhase) return;
            _requeuePending = false;
            Log("enter_scene_requested location=" + location.StringId);
            if (PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, null, null) == null)
                throw new InvalidOperationException("政变场景未能打开。");
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
        if (!CoupSettings.IsEnabled) reason = "宣权篡位已关闭。";
        else if (!_saveValid) reason = "政变存档记录异常，请先处理日志中的错误。";
        else if (!CoupGuards.MissionProtectionAvailable) reason = "政变场景兼容保护未能注册，请检查日志与游戏版本。";
        else if (!SettlementEntryTroopSelectionBehavior.IsAvailable || !CoupRebellionBridge.IsAvailable) reason = "当前 AF 版本的政变接缝不可用，请检查日志。";
        else if (_session != null && !_session.IsSettled) reason = "还有一场政变尚未结算。";
        else if (clan == null || kingdom == null || clan.Leader != Hero.MainHero || clan.IsUnderMercenaryService || clan.IsClanTypeMercenary || kingdom.RulingClan == clan)
            reason = "仅本国正式封臣的家族族长能够发动政变。";
        else if (town?.IsTown != true || town.MapFaction != kingdom) reason = "必须位于本国城镇。";
        else if (Hero.MainHero == null || !Hero.MainHero.IsAlive || Hero.MainHero.IsPrisoner || Hero.MainHero.IsWounded)
            reason = "玩家当前无法参战。";
        else if (king == null || !king.IsAlive || king.IsPrisoner || king.CurrentSettlement != town)
            reason = "本国现任国王必须身在这座城镇且未被俘。";
        else if (Mission.Current != null || town.IsUnderSiege || PlayerEncounterCompat.HasEncounterBattleContext()
            || MobileParty.MainParty?.MapEvent != null || king.PartyBelongedTo?.MapEvent != null)
            reason = "围城、战斗或特殊任务进行中，无法发动政变。";
        else if (SettlementEntryTroopSelectionBehavior.HasPendingFlowForCoup() || CoupGuards.HasBlockingHostFlow())
            reason = "当前还有城镇冲突或战后处置待完成。";
        else if (MobileParty.MainParty?.Army != null) reason = "请先离开军团，再带自己的部队发动政变。";
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
            BattleOptions = CoupSettings.CaptureForNewCoup(),
            EntryRequirements = CoupSettings.CaptureAdmissionForNewCoup(),
            SettlementId = town.StringId, KingdomId = kingdom.StringId, KingId = kingdom.Leader.StringId,
            OriginalRulingClanId = kingdom.RulingClan.StringId, OriginalOwnerClanId = town.OwnerClan.StringId,
            PlayerHealth = Hero.MainHero.HitPoints, KingHealth = Math.Max(1, kingdom.Leader.HitPoints), Phase = CoupPhase.Preparing
        };
        string id = _session.Id;
        long token = _runtimeToken;
        InformationManager.ShowInquiry(new InquiryData("宣权篡位", "目标：" + kingdom.Leader.Name + "\n地点：" + town.Name
            + "\n发动条件：家族等级至少" + _session.EntryRequirements.MinimumClanTier + "级，影响力至少" + _session.EntryRequirements.MinimumInfluence + "（只检查、不扣除；0表示不限制）。"
            + "\n必须实际选中至少" + _session.EntryRequirements.MinimumTroops + "名健康普通士兵，并另留1人接应。"
            + "\n突击队：街道最多" + _session.BattleOptions.StreetAllyLimit + "人，大厅最多" + _session.BattleOptions.HallAllyLimit + "人（不含玩家）。至少留一名士兵接应。"
            + "\n守卫：门口最多" + _session.BattleOptions.GateGuardLimit + "人，大厅最多" + _session.BattleOptions.HallGuardLimit + "人（不含国王），以实际兵源为准。"
            + "\n增援：每波最多" + _session.BattleOptions.DefenderWaveSize + "人，间隔" + _session.BattleOptions.DefenderWaveIntervalSeconds + "秒，同时最多" + _session.BattleOptions.MaxActiveDefenderWaves + "波存活。"
            + "\n本次参数已固定。正式进场后，倒地或撤退都会导致家族带地叛离并与原王国开战。", true, true, "挑选突击队", "取消",
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
        if (limit < minimum) { _session = null; Show("没有足够的健康士兵满足突击队与接应要求。政变已取消。"); return; }
        _selectionOpen = true;
        long token = _runtimeToken;
        CoupTroopSelection.Open(available, limit, "政变突击队", selected =>
        {
            if (!IsCurrentUi(id, token, CoupPhase.Preparing)) return;
            _selectionOpen = false;
            if (!ValidatePreparedContext(out string reason)) { _session = null; Show(reason); return; }
            if (selected.TotalManCount < minimum || selected.TotalManCount > limit || selected.TotalManCount >= HealthyRegulars(MobileParty.MainParty.MemberRoster))
            { _session = null; Show("选兵已失效，必须实际选中至少" + minimum + "名健康普通士兵，并保留一名健康士兵接应。"); return; }
            foreach (TroopRosterElement entry in selected.GetTroopRoster())
            {
                if (entry.Character.IsHero || entry.WoundedNumber != 0 || entry.Number > HealthyCount(MobileParty.MainParty.MemberRoster, entry.Character))
                { _session = null; Show("部队名册已变化，请重新选择。"); return; }
                for (int i = 0; i < entry.Number; i++) AddTroop(entry.Character, MobileParty.MainParty, CoupTroopRole.Ally);
            }
            BuildDefenders(Settlement.Find(_session.SettlementId));
            _session.Phase = CoupPhase.Street;
            _session.Started = true;
            QueueCurrentScene();
            Log("street_selection_complete");
            Show("突击队已登记。点击城镇菜单的“率领政变突击队攻入城镇”进场。");
        }, () => { if (IsCurrentUi(id, token, CoupPhase.Preparing)) { _selectionOpen = false; _session = null; } }, minimum);
    }

    private bool ValidatePreparedContext(out string reason)
    {
        reason = "国王、玩家或城镇状态已改变，政变不能继续。";
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
        reason = "政变发动条件记录不可用。";
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
        if (allies.TotalManCount < 1) { SetFailure("突击队已无可参战士兵，撤出政变。"); return; }
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
        SettlementEntryTroopSelectionBehavior.ClearArmedCoup();
        if (_session?.IsCombatPhase == true) SetFailure("主动撤退或战斗被中断。");
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
        Show("政变暂时中止：" + reason + (Instance._session.HasPoliticalCommit
            ? " 已发生的战役变化已保留；可在城镇菜单重试剩余结算。" : " 已有伤亡保留，未授予胜利或叛离惩罚。"));
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
        if (Campaign.Current != _campaign || _session == null || _session.IsSettled || _processing || _selectionOpen || _dispositionOpen || _retryBlocked
            || (_session.Phase == CoupPhase.Suspended && _session.CasualtiesCommitted && !_session.IsResumable)) return;
        _nextCheck -= dt;
        if (_nextCheck > 0f) return;
        _nextCheck = 0.25f;
        if (Mission.Current != null || !(Game.Current?.GameStateManager?.ActiveState is MapState)) return;
        _endingMission = null;
        // Walking to the next scene needs no time control; leaving town is handled as retreat.
        if (_session.IsCombatPhase)
        {
            if (_requeuePending) { _requeuePending = false; QueueCurrentScene(); }
            return;
        }
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
        if (survivors.Count == 0) { SetFailure("突击队已无可参战士兵，撤出政变。"); return; }
        var roster = TroopRoster.CreateDummyTroopRoster();
        foreach (var troop in survivors) roster.AddToCounts(CharacterObject.Find(troop.CharacterId), 1);
        string id = _session.Id;
        long token = _runtimeToken;
        _selectionOpen = true;
        int limit = Math.Min(_session.BattleOptions.HallAllyLimit, survivors.Count);
        CoupTroopSelection.Open(roster, limit, "攻入领主大厅", selected =>
        {
            if (!IsCurrentUi(id, token, CoupPhase.HallSelection)) return;
            _selectionOpen = false;
            if (selected.TotalManCount > limit) { NotifyTechnicalFailure(null, "大厅选兵超过本次政变人数上限。"); return; }
            foreach (var troop in survivors) troop.HallSelected = false;
            foreach (var entry in selected.GetTroopRoster())
            {
                var matching = survivors.Where(t => t.CharacterId == entry.Character.StringId).OrderByDescending(t => t.Health).Take(entry.Number).ToList();
                if (matching.Count != entry.Number) { NotifyTechnicalFailure(null, "大厅选兵名册不一致。"); return; }
                foreach (var troop in matching) troop.HallSelected = true;
            }
            if (!survivors.Any(t => t.HallSelected)) { SetFailure("放弃攻入大厅。"); return; }
            // Unselected survivors stay outside the hall; they are not casualties and keep Removed=false.
            _session.Phase = CoupPhase.Hall;
            QueueCurrentScene();
            if (_session.Phase == CoupPhase.Hall)
            {
                Log("hall_selection_complete");
                Show("已选定突入大厅的士兵。点击城镇菜单的“率领政变突击队攻入领主大厅”进场。");
            }
        }, () =>
        {
            if (!IsCurrentUi(id, token, CoupPhase.HallSelection)) return;
            _selectionOpen = false;
            SetFailure("放弃攻入领主大厅，撤出政变。");
        });
    }

    private void OpenDisposition()
    {
        string id = _session.Id;
        long token = _runtimeToken;
        _dispositionOpen = true;
        bool canCapture = CoupGuards.CaptivityProtectionAvailable;
        InformationManager.ShowInquiry(new InquiryData("政变胜利：处置旧王", "你已控制领主大厅。接下来将接管王国及本城。\n释放旧王后，其家族可能参与叛乱；扣押会排除其家族的本次叛乱资格。"
            + (canCapture ? "" : "\n当前版本拘押保护不可用，只能释放旧王。"), true, canCapture, "逼其退位并释放", "扣押旧王",
            () => SelectDisposition(id, token, CoupKingDisposition.Release), () => SelectDisposition(id, token, CoupKingDisposition.Capture)), true);
    }
    private void SelectDisposition(string id, long token, CoupKingDisposition disposition)
    {
        if (!IsCurrentUi(id, token, CoupPhase.AwaitingResolution)) return;
        _dispositionOpen = false;
        _session.Disposition = disposition;
    }

    private void CommitVictory()
    {
        if (!_session.IsValid() || !_session.Started || !_session.KingSubdued) throw new InvalidOperationException("政变胜利状态不完整，禁止政治结算。");
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
            if (kingdom.RulingClan != Clan.PlayerClan) ChangeRulingClanAction.Apply(kingdom, Clan.PlayerClan);
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
            if (!owner.TryQueueCoupRebellion(_session.Id, kingdom, out string message)) throw new InvalidOperationException(message);
            _session.RebellionQueued = true;
            Show(message);
        }
        _session.Phase = CoupPhase.Completed;
        Log("completed");
        Show("宣权篡位成功：你已成为" + kingdom.Name + "的统治者，" + town.Name + "归属你的家族。");
    }

    private void CommitFailure()
    {
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
        {
            if (PlayerEncounterCompat.HasEncounterBattleContext()) throw new InvalidOperationException("新的战役战斗已开始，不能自动结束遭遇。");
            if (PlayerEncounter.LocationEncounter != null && PlayerEncounter.LocationEncounter.Settlement?.StringId != _session.SettlementId)
                throw new InvalidOperationException("当前遭遇已改变，不能替其他遭遇执行撤出。");
            if (PlayerEncounter.Current != null) PlayerEncounter.Finish(true);
            if (MobileParty.MainParty?.CurrentSettlement != null) LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            if (MobileParty.MainParty == null || MobileParty.MainParty.CurrentSettlement != null || PlayerEncounter.LocationEncounter != null)
                throw new InvalidOperationException("接应队伍尚未完整退出城镇遭遇。");
            _session.WithdrawalCommitted = true;
        }
        if (!_session.FactsCommitted)
        {
            var owner = CoupRebellionBridge.Instance;
            if (owner == null || !owner.TryRecordCoupOutcome(_session, FindKing(), Settlement.Find(_session.SettlementId), false, false, out _))
                throw new InvalidOperationException("政变失败事实暂时无法写入。");
            _session.FactsCommitted = true;
        }
        Log("failure_committed");
        Show("政变失败，留守部队已接应撤出。你的家族保留原有领地，脱离旧王国并开战。");
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
    private static void Show(string message) { if (!string.IsNullOrEmpty(message)) InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】" + message)); }
    private void Log(string message) => Logger.Log("Coup", "event=" + _session?.Id + " phase=" + _session?.Phase + " town=" + _session?.SettlementId + " " + message);
}
