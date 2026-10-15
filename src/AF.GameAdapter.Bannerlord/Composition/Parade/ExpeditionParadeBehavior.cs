using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed partial class ExpeditionParadeBehavior : CampaignBehaviorBase
{
    internal const string RuleId = "expedition_parade";
    private const string SaveKey = "_afExpeditionParade_v1";
    private readonly Dictionary<string, ExpeditionParadeWarRecord> _wars = new Dictionary<string, ExpeditionParadeWarRecord>();
    private readonly Dictionary<string, ExpeditionParadeVictoryRecord> _victories = new Dictionary<string, ExpeditionParadeVictoryRecord>();
    private readonly Dictionary<string, int> _peaceTerms = new Dictionary<string, int>();
    private ExpeditionParadeRequest _pending;
    private Mission _transitionFrom;
    private ExpeditionParadeMissionLogic _active;
    private DateTime _queuedAt;
    internal static ExpeditionParadeBehavior Current => Campaign.Current?.GetCampaignBehavior<ExpeditionParadeBehavior>();
    internal static string T(string zh, string en) => GcczLocalizedResourceLoader.UseChineseInterface ? zh : en;
    internal static void Log(string message) => Logger.Log("ExpeditionParade", message);
    internal static void Notify(string message) => InformationManager.DisplayMessage(new InformationMessage(message));
    private static string Pair(IFaction a, IFaction b) => string.CompareOrdinal(a.StringId, b.StringId) <= 0 ? a.StringId + "|" + b.StringId : b.StringId + "|" + a.StringId;

    public override void RegisterEvents()
    {
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeace);
        CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, Prune);
    }

    public override void SyncData(IDataStore dataStore)
    {
        var storage = new Dictionary<string, string>();
        if (dataStore.IsSaving)
        {
            foreach (var pair in _wars) storage["war:" + pair.Key] = JsonConvert.SerializeObject(pair.Value);
            foreach (var pair in _victories) storage["victory:" + pair.Key] = JsonConvert.SerializeObject(pair.Value);
            storage = CampaignSaveChunkHelper.FlattenStringDictionary(storage, SaveKey, "ExpeditionParade");
        }
        dataStore.SyncData(SaveKey, ref storage);
        if (!dataStore.IsLoading) return;
        _wars.Clear(); _victories.Clear(); _peaceTerms.Clear(); _actionPermits.Clear(); _pending = null; _active = null; _transitionFrom = null;
        foreach (var pair in CampaignSaveChunkHelper.RestoreStringDictionary(storage, "ExpeditionParade") ?? new Dictionary<string, string>())
        {
            try
            {
                if (pair.Key.StartsWith("war:", StringComparison.Ordinal))
                {
                    var record = JsonConvert.DeserializeObject<ExpeditionParadeWarRecord>(pair.Value);
                    if (record != null) _wars[pair.Key.Substring(4)] = record;
                }
                else if (pair.Key.StartsWith("victory:", StringComparison.Ordinal))
                {
                    var record = JsonConvert.DeserializeObject<ExpeditionParadeVictoryRecord>(pair.Value);
                    if (record != null) _victories[pair.Key.Substring(8)] = record;
                }
            }
            catch (Exception ex) { Log("load rejected " + pair.Key + ": " + ex.Message); }
        }
    }

    private void OnWarDeclared(IFaction first, IFaction second, DeclareWarAction.DeclareWarDetail detail)
    {
        if (first == null || second == null || first.IsBanditFaction || second.IsBanditFaction) return;
        _wars[Pair(first, second)] = new ExpeditionParadeWarRecord {
            FirstFactionId = first.StringId, SecondFactionId = second.StringId, DeclarationDay = CampaignTime.Now.ToDays,
            FirstOpeningFiefs = first.Fiefs.Count, SecondOpeningFiefs = second.Fiefs.Count, HasExactOpeningSnapshot = true
        };
    }

    internal static void CapturePendingPeaceTerms(IFaction first, IFaction second, int tribute)
    {
        if (first != null && second != null && Current != null)
            Current._peaceTerms[Pair(first, second)] = string.CompareOrdinal(first.StringId, second.StringId) <= 0 ? tribute : -tribute;
    }

    internal static void ClearPendingPeaceTerms(IFaction first, IFaction second)
    {
        if (first != null && second != null) Current?._peaceTerms.Remove(Pair(first, second));
    }

    private void OnPeace(IFaction first, IFaction second, MakePeaceAction.MakePeaceDetail detail)
    {
        if (first == null || second == null) return;
        string key = Pair(first, second);
        _wars.TryGetValue(key, out var war);
        _wars.Remove(key);
        bool capturedTerms = _peaceTerms.TryGetValue(key, out int tribute);
        _peaceTerms.Remove(key);
        if (string.CompareOrdinal(first.StringId, second.StringId) > 0) tribute = -tribute;
        int firstOpening = war?.FirstFactionId == first.StringId ? war.FirstOpeningFiefs : war?.SecondOpeningFiefs ?? first.Fiefs.Count;
        int secondOpening = war?.FirstFactionId == first.StringId ? war.SecondOpeningFiefs : war?.FirstOpeningFiefs ?? second.Fiefs.Count;
        int winner = ExpeditionParadeRules.Winner(capturedTerms ? tribute : 0, war?.HasExactOpeningSnapshot == true,
            first.Fiefs.Count - firstOpening, second.Fiefs.Count - secondOpening);
        IFaction playerFaction = Hero.MainHero?.MapFaction;
        IFaction winning = winner > 0 ? first : winner < 0 ? second : null;
        // Record only a victory involving the player's faction at peace time.
        if (winning == null || winning != playerFaction) return;
        double now = CampaignTime.Now.ToDays;
        var record = new ExpeditionParadeVictoryRecord {
            Id = Guid.NewGuid().ToString("N"), WinningFactionId = winning.StringId,
            LosingFactionId = winning == first ? second.StringId : first.StringId,
            PeaceDay = now, ExpiresDay = now + ExpeditionParadeRules.WindowDays,
            Basis = capturedTerms && tribute != 0 ? "tribute_received" : "relative_fief_gain"
        };
        _victories[record.Id] = record;
        Log("victory window opened winner=" + record.WinningFactionId + " basis=" + record.Basis);
    }

    private void Prune()
    {
        double now = CampaignTime.Now.ToDays;
        foreach (string key in _victories.Where(p => !ExpeditionParadeRules.InWindow(now, p.Value.PeaceDay)).Select(p => p.Key).ToList()) _victories.Remove(key);
    }

    internal ExpeditionParadeAvailability GetAvailability(Hero governor, bool ignoreOwnQueue = false)
    {
        var result = GetSettlementAvailability(ignoreOwnQueue);
        Settlement place = result.Settlement;
        if (place == null) return result;
        // The retained AI channel still requires the actual local governor. Menu requests do not.
        if (governor == null || governor == Hero.MainHero || place.Town?.Governor != governor || governor.CurrentSettlement != place)
        {
            result.ExpeditionAvailable = result.VictoryAvailable = false;
            result.ExpeditionReason = result.VictoryReason = T("请与本地总督交谈，或使用大地图城镇／城堡菜单中的“举行阅兵”。", "Speak to the local governor, or use Hold a parade in the world-map town/castle menu.");
        }
        return result;
    }

    private ExpeditionParadeAvailability GetSettlementAvailability(bool ignoreOwnQueue = false)
    {
        var result = new ExpeditionParadeAvailability();
        Settlement place = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
        string unavailable = T("只能在有权管理且安全的己方城镇或城堡举行阅兵。", "A parade requires a safe town or castle you are authorized to govern.");
        if (place == null || (!place.IsTown && !place.IsCastle)
            || Hero.MainHero?.IsPrisoner != false || MobileParty.MainParty == null
            || place.IsUnderSiege || MobileParty.MainParty.MapEvent != null
            || !(place.OwnerClan == Clan.PlayerClan || (place.MapFaction == Hero.MainHero.MapFaction && place.MapFaction.Leader == Hero.MainHero)))
        { result.ExpeditionReason = result.VictoryReason = unavailable; return result; }
        result.Settlement = place;
        if ((!ignoreOwnQueue && _pending != null) || (_active != null && !_active.Finished))
        { result.ExpeditionReason = result.VictoryReason = T("已有阅兵正在准备或进行。", "A parade is already queued or running."); return result; }
        double now = CampaignTime.Now.ToDays;
        IFaction faction = Hero.MainHero.MapFaction;
        result.ExpeditionAvailable = faction.FactionsAtWarWith.Any(enemy => !enemy.IsBanditFaction
            && ExpeditionParadeRules.InWindow(now, faction.GetStanceWith(enemy).WarStartDate.ToDays));
        result.VictoryAvailable = _victories.Values.Any(v => v.WinningFactionId == faction.StringId && ExpeditionParadeRules.InWindow(now, v.PeaceDay));
        result.ExpeditionReason = result.ExpeditionAvailable ? T("开战后14天内。", "Within 14 days of war declaration.") : T("当前没有开战14天内的战争。", "No active war was declared within the last 14 days.");
        result.VictoryReason = result.VictoryAvailable ? T("优势结束战争后14天内。", "Within 14 days of a favorable peace.") : T("当前没有可核实的14天内战争胜利。", "No verified favorable peace within the last 14 days.");
        return result;
    }

    private bool TryQueue(Hero governor, ExpeditionParadeType type, out string message, bool fromSettlementMenu = false)
    {
        var available = fromSettlementMenu ? GetSettlementAvailability() : GetAvailability(governor);
        bool allowed = type == ExpeditionParadeType.Expedition ? available.ExpeditionAvailable : available.VictoryAvailable;
        if (!allowed) { message = type == ExpeditionParadeType.Expedition ? available.ExpeditionReason : available.VictoryReason; return false; }
        var formations = ExpeditionParadeRoster.Capture(available.Settlement);
        if (formations.Count == 0) { message = T("没有可参加阅兵的健康士兵。", "There are no healthy soldiers available for the parade."); return false; }
        _pending = new ExpeditionParadeRequest {
            Type = type, SettlementId = available.Settlement.StringId, GovernorHeroId = governor?.StringId ?? "",
            FromSettlementMenu = fromSettlementMenu,
            QueuedDay = CampaignTime.Now.ToDays, Formations = formations, DisplayTroopCount = formations.Sum(f => f.Troops.Count)
        };
        _queuedAt = DateTime.UtcNow;
        message = fromSettlementMenu
            ? T("阅兵已安排，正在进入城内集结并检查路线。", "Parade arranged. Entering the settlement to muster troops and check the route.")
            : T("阅兵已安排，结束交谈后前往城内集结。", "Parade arranged. The muster will begin after this conversation.");
        Log("queued type=" + type + " settlement=" + _pending.SettlementId + " troops=" + _pending.DisplayTroopCount);
        return true;
    }

    private void OnMissionStarted(IMission mission)
    {
        if (mission is Mission current && current.GetMissionBehavior<ExpeditionParadePendingMissionLogic>() == null)
            current.AddMissionBehavior(new ExpeditionParadePendingMissionLogic(this));
    }
    private void Tick(float dt) => ProcessPending();

    internal void ProcessPending(bool startImmediately = false)
    {
        if (_pending == null || (!startImmediately && DateTime.UtcNow - _queuedAt < TimeSpan.FromSeconds(1))) return;
        if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true || Campaign.Current?.ConversationManager?.IsConversationFlowActive == true) return;
        try
        {
            Settlement place = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
            if (place?.StringId != _pending.SettlementId || place.IsUnderSiege || Hero.MainHero?.IsPrisoner == true)
            { CancelPending(T("阅兵取消：地点或安全条件已改变。", "Parade cancelled: the location or safety conditions changed.")); return; }
            var available = _pending.FromSettlementMenu ? GetSettlementAvailability(ignoreOwnQueue: true)
                : GetAvailability(place.Town?.Governor, ignoreOwnQueue: true);
            if ((!_pending.FromSettlementMenu && place.Town?.Governor?.StringId != _pending.GovernorHeroId)
                || !(_pending.Type == ExpeditionParadeType.Expedition ? available.ExpeditionAvailable : available.VictoryAvailable))
            { CancelPending(T("阅兵取消：总督、权限或14天有效期已改变。", "Parade cancelled: the governor, authorization or 14-day window changed.")); return; }
            if (DateTime.UtcNow - _queuedAt > TimeSpan.FromMinutes(5))
            { CancelPending(T("阅兵取消：场景准备超时，请重新安排。", "Parade cancelled: scene preparation timed out. Please arrange it again.")); return; }
            Mission mission = Mission.Current;
            if (_transitionFrom != null && ReferenceEquals(mission, _transitionFrom)) return;
            if (mission != null && CampaignMission.Current?.Location?.StringId == "center")
            {
                var request = _pending;
                _pending = null; _transitionFrom = null;
                // Capture again at execution time; the queue does not reserve campaign troops.
                request.Formations = ExpeditionParadeRoster.Capture(place);
                request.DisplayTroopCount = request.Formations.Sum(f => f.Troops.Count);
                if (request.DisplayTroopCount == 0)
                { Notify(T("阅兵取消：已没有健康士兵。", "Parade cancelled: no healthy soldiers remain.")); return; }
                _active = new ExpeditionParadeMissionLogic(request);
                mission.AddMissionBehavior(_active);
                return;
            }
            Location center = LocationComplex.Current?.GetLocationWithId("center");
            if (center == null || PlayerEncounter.LocationEncounter == null)
            { CancelPending(T("阅兵取消：当前场景没有城内入口。", "Parade cancelled: no settlement center is available.")); return; }
            if (mission != null)
            {
                if (CampaignMission.Current?.Location == null) return;
                Campaign.Current.GameMenuManager.NextLocation = center;
                Campaign.Current.GameMenuManager.PreviousLocation = CampaignMission.Current.Location;
                _transitionFrom = mission;
                mission.EndMission();
            }
            else
            {
                _transitionFrom = null;
                var menus = Campaign.Current.GameMenuManager;
                menus.NextLocation = center;
                menus.PreviousLocation = null;
                try
                {
                    if (PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(center, null, null, null) == null)
                        CancelPending(T("阅兵场景未能打开。", "The parade scene could not be opened."));
                }
                finally { menus.NextLocation = null; menus.PreviousLocation = null; }
            }
        }
        catch (Exception ex) { Log("pending failed: " + ex); CancelPending(T("阅兵准备失败，详情已写入日志。", "Parade preparation failed; see the log for details.")); }
    }

    private void CancelPending(string reason) { _pending = null; _transitionFrom = null; Notify(reason); Log(reason); }
}

internal sealed class ExpeditionParadePendingMissionLogic : MissionLogic
{
    private readonly ExpeditionParadeBehavior _owner;
    internal ExpeditionParadePendingMissionLogic(ExpeditionParadeBehavior owner) { _owner = owner; }
    public override void OnMissionTick(float dt) { _owner.ProcessPending(); }
}
