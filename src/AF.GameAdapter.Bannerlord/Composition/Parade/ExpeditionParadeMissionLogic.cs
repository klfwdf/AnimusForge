using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class ExpeditionParadeMissionLogic : MissionLogic
{
    private sealed class Marcher
    {
        internal Agent Agent;
        internal Agent Mount;
        internal int Row, Column;
        internal bool BannerBearer, Exited;
        internal Vec3 LastPosition;
        internal float StillSeconds;
    }
    private readonly ExpeditionParadeRequest _request;
    private readonly List<Marcher> _marchers = new List<Marcher>();
    private readonly List<Marcher> _owned = new List<Marcher>();
    private ExpeditionParadeRoutePlanner _planner;
    private IEnumerator<bool> _planning;
    private ExpeditionParadeRoute _route;
    private ExpeditionParadeFormationSpec _formation;
    private ItemObject _bannerItem;
    private int _nextFormation;
    private float _elapsed, _stepTimer, _headDistance, _settleTime;
    internal bool Finished { get; private set; }

    internal ExpeditionParadeMissionLogic(ExpeditionParadeRequest request) { _request = request; }
    public override void OnMissionTick(float dt)
    {
        if (Finished) return;
        try
        {
            if (Mission.MainAgent == null || Mission.Scene == null) return;
            if (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Duel)
            { Stop("Settlement mission entered combat.", false); return; }
            if (Mission.Mode == MissionMode.Conversation) return;
            _elapsed += dt;
            if (_elapsed > 1200f) { Stop("Parade time budget exceeded.", false); return; }
            if (_planner == null)
            {
                _planner = new ExpeditionParadeRoutePlanner(Mission.Scene, _request.Formations, _request.Type);
                _planning = _planner.Plan();
                ExpeditionParadeBehavior.Notify(ExpeditionParadeBehavior.T("正在检查阅兵路线与城门通行空间……", "Checking the parade route and gate clearance..."));
            }
            if (_route == null)
            {
                var clock = Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < 6)
                {
                    if (_planning.MoveNext()) continue;
                    _planning.Dispose(); _planning = null;
                    _route = _planner.Result;
                    if (_route == null) { Stop(_planner.Failure, false); return; }
                    _bannerItem = ResolveBannerItem();
                    if (_bannerItem == null) { Stop("No usable banner item is available.", false); return; }
                    break;
                }
                return;
            }
            if (_formation == null)
            {
                if (_nextFormation >= _request.Formations.Count) { Stop("completed", true); return; }
                if (Mission.CurrentTime < 4f) return;
                SpawnBlock(_request.Formations[_nextFormation++]);
            }
            _stepTimer += dt;
            if (_stepTimer < 0.2f) return;
            float step = Math.Min(0.5f, _stepTimer); _stepTimer = 0;
            March(step);
        }
        catch (Exception ex) { Stop("Mission error: " + ex, false); }
    }

    private void SpawnBlock(ExpeditionParadeFormationSpec spec)
    {
        // The conservative scene budget includes mounts and a reserve for native spawns.
        int units = spec.Troops.Count * (spec.Mounted ? 2 : 1);
        if (Mission.Agents.Count + units + ExpeditionParadeRules.AgentReserve > 512)
            throw new InvalidOperationException("Not enough scene agent capacity for the next formation.");
        _formation = spec;
        _headDistance = (spec.Rows - 1) * RowSpacing + 2f;
        _settleTime = 3f;
        _marchers.Clear();
        for (int i = 0; i < spec.Troops.Count; i++)
        {
            CharacterObject troop = spec.Troops[i];
            int row = i / spec.Columns, col = i % spec.Columns;
            Vec3 position = _route.At(_headDistance - row * RowSpacing, (col - (spec.Columns - 1) / 2f) * ColumnSpacing, out Vec2 facing);
            if (!ExpeditionParadeRoutePlanner.TryGround(Mission.Scene, position, out position))
                throw new InvalidOperationException("Assembly position left the navigation mesh.");
            var build = new AgentBuildData(troop)
                .TroopOrigin(new SimpleAgentOrigin(troop, -1, spec.Banner, default(UniqueTroopDescriptor)))
                .Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(troop.Race, "_settlement"))
                .Team(Mission.PlayerTeam ?? Mission.MainAgent.Team)
                .InitialPosition(in position).InitialDirection(facing).Controller(AgentControllerType.AI)
                .CivilianEquipment(false).NoHorses(!spec.Mounted);
            if (spec.Banner != null) build.Banner(spec.Banner);
            bool bearer = row == 0;
            if (bearer)
            {
                build.BannerItem(_bannerItem);
                ItemObject replacement = MissionGameModels.Current?.BattleBannerBearersModel?.GetBannerBearerReplacementWeapon(troop);
                if (replacement != null) build.BannerReplacementWeaponItem(replacement);
            }
            Agent agent = Mission.SpawnAgent(build, false);
            if (agent == null) throw new InvalidOperationException("Troop spawn returned null.");
            var marcher = new Marcher { Agent = agent, Mount = agent.MountAgent, Row = row, Column = col, BannerBearer = bearer, LastPosition = agent.Position };
            _marchers.Add(marcher); // Track immediately so any later failure can clean up.
            _owned.Add(marcher);
            agent.SetMortalityState(Agent.MortalityState.Invulnerable);
            if (marcher.Mount != null) marcher.Mount.SetMortalityState(Agent.MortalityState.Invulnerable);
            if (spec.Mounted && marcher.Mount == null) throw new InvalidOperationException("A mounted parade troop spawned without its mount.");
            agent.SetWatchState(Agent.WatchState.Patrolling);
            agent.SetMaximumSpeedLimit(spec.Mounted ? 1.65f : 1.35f, false);
            marcher.Mount?.SetMaximumSpeedLimit(1.65f, false);
            if (bearer) WieldBanner(agent);
            Command(marcher, _headDistance);
        }
        ExpeditionParadeBehavior.Log("block=" + _nextFormation + " source=" + spec.SourceId + " kind=" + spec.Kind + " troops=" + spec.Troops.Count + " columns=" + spec.Columns);
    }

    private float ColumnSpacing => _formation.Mounted ? 1.65f : 0.9f;
    private float RowSpacing => _formation.Mounted ? 2.15f : 1.05f;

    private void March(float dt)
    {
        if (_formation == null || Finished) return;
        bool catchingUp = false;
        foreach (Marcher marcher in _marchers.Where(x => !x.Exited))
        {
            if (!marcher.Agent.IsActive()) { Stop("A display troop left the mission unexpectedly.", false); return; }
            float distance = _headDistance - marcher.Row * RowSpacing;
            Vec3 target = _route.At(distance, (marcher.Column - (_formation.Columns - 1) / 2f) * ColumnSpacing, out _);
            float error = (marcher.Agent.Position - target).AsVec2.Length;
            if (distance >= _route.Length - 0.2f && error < 1.5f)
            {
                marcher.Exited = true;
                marcher.Agent.FadeOut(false, true);
                if (marcher.Mount?.IsActive() == true) marcher.Mount.FadeOut(false, true);
                continue;
            }
            if (error > (_formation.Mounted ? 2.5f : 1.8f)) catchingUp = true;
            float moved = (marcher.Agent.Position - marcher.LastPosition).AsVec2.Length;
            marcher.StillSeconds = error > 1.2f && moved < 0.04f ? marcher.StillSeconds + dt : 0;
            marcher.LastPosition = marcher.Agent.Position;
            if (marcher.StillSeconds > 25f) { Stop("Formation blocked for 25 seconds; no teleport or column compression was applied.", false); return; }
            // Reissue the same validated target; never skip a stuck soldier past the gate.
            Command(marcher, _headDistance);
            if (marcher.BannerBearer) WieldBanner(marcher.Agent);
        }
        if (_marchers.All(x => x.Exited))
        {
            // The whole block has crossed the exit zone before the next block is released.
            _formation = null;
            return;
        }
        _settleTime -= dt;
        if (_settleTime <= 0 && !catchingUp) _headDistance += dt * (_formation.Mounted ? 1.1f : 0.95f);
    }

    private void Command(Marcher marcher, float head)
    {
        Vec3 target = _route.At(head - marcher.Row * RowSpacing, (marcher.Column - (_formation.Columns - 1) / 2f) * ColumnSpacing, out Vec2 facing);
        if (!ExpeditionParadeRoutePlanner.TryGround(Mission.Scene, target, out target))
            throw new InvalidOperationException("March target is no longer navigable.");
        WorldPosition position = new WorldPosition(Mission.Scene, target);
        marcher.Agent.SetScriptedPositionAndDirection(ref position, facing.RotationInRadians, false,
            Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
    }

    private static void WieldBanner(Agent agent)
    {
        if (agent.GetPrimaryWieldedItemIndex() != EquipmentIndex.ExtraWeaponSlot && agent.GetOffhandWieldedItemIndex() != EquipmentIndex.ExtraWeaponSlot)
            agent.TryToWieldWeaponInSlot(EquipmentIndex.ExtraWeaponSlot, Agent.WeaponWieldActionType.InstantAfterPickUp, false);
    }
    private static ItemObject ResolveBannerItem()
    {
        ItemObject player = Hero.MainHero?.BannerItem.Item;
        if (player?.IsBannerItem == true && player.BannerComponent != null) return player;
        return Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
            .Where(i => i.IsBannerItem && i.BannerComponent != null).OrderBy(i => i.StringId, StringComparer.Ordinal).FirstOrDefault();
    }

    private void Stop(string reason, bool success)
    {
        if (Finished) return;
        Finished = true;
        Cleanup();
        ExpeditionParadeBehavior.Log("finish settlement=" + _request.SettlementId + " type=" + _request.Type + " success=" + success + " reason=" + reason);
        ExpeditionParadeBehavior.Notify(success
            ? ExpeditionParadeBehavior.T("阅兵结束。", "The parade is complete.")
            : ExpeditionParadeBehavior.T("阅兵停止：路线或队列未能通过安全检查，详情见阅兵日志。", "Parade stopped: route or formation validation failed. See the parade log."));
    }
    private void Cleanup()
    {
        _planning?.Dispose(); _planning = null;
        foreach (Marcher marcher in _owned)
        {
            try { if (marcher.Agent.IsActive()) marcher.Agent.FadeOut(true, true); }
            catch (Exception ex) { ExpeditionParadeBehavior.Log("cleanup troop: " + ex.Message); }
            try { if (marcher.Mount?.IsActive() == true) marcher.Mount.FadeOut(true, true); }
            catch (Exception ex) { ExpeditionParadeBehavior.Log("cleanup mount: " + ex.Message); }
        }
        _marchers.Clear();
        _owned.Clear();
    }
    protected override void OnEndMission() { Finished = true; Cleanup(); base.OnEndMission(); }
    public override void OnRemoveBehavior() { Finished = true; Cleanup(); base.OnRemoveBehavior(); }
}
