using System;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge;

public sealed partial class FieldCampMissionLogic
{
    private bool _campInitialized;
    private bool _setupDegraded;

    // Main-thread, one attempt after scene load. Cosmetic/setup failures never request a mission exit.
    private void SetUpCamp()
    {
        TryCampSetupStage("layout", () => _layout = FieldCampLayout.Build(_parties, FieldCampSettings.AgentCap, FieldCampSettings.TentCap));
        if (_layout == null)
            _layout = new FieldCampLayout { HalfX = 12f, Back = -12f, Front = 12f, PlayerFacing = Vec2.Forward };
        if (!TryCampSetupStage("site", () => _campFrame = FieldCampSite.Resolve(Mission, _layout, out _campScale)))
            TryCampSetupStage("fallback_site", () => _campFrame = FieldCampSite.ResolveFallback(Mission, _layout));

        int cleared = 0, props = 0, horses = 0;
        TryCampSetupStage("clutter", () => cleared = FieldCampSite.ClearClutter(Mission.Scene, _campFrame, _layout, _campScale));
        // Execution apparatus belongs to the player-confirmed B-key deployment, never camp entry.
        TryCampSetupStage("props", () => props = FieldCampProps.Place(Mission.Scene, _layout, _campFrame, _campScale));
        if (FieldCampProps.HadPlacementFailures) _setupDegraded = true;
        // Always initialize once after the final props, even if a prop/preset failed part-way through.
        TryCampSetupStage("ground_cache", () => FieldCampSite.InitializeGroundCache(Mission.Scene, _campFrame, _layout, _campScale));
        TryCampSetupStage("actors", SpawnEveryone);
        _campInitialized = Mission.MainAgent?.IsActive() == true;
        if (_campInitialized)
        {
            FieldCampBehavior.Instance?.MarkPatrolPlayerReady();
            TryCampSetupStage("horses", () => horses = SpawnHorses());
            TryCampSetupStage("envoys", QueueEnvoys);
        }
        else _setupDegraded = true;
        if (_setupDegraded)
            TryCampSetupStage("notice", () => AnimusForgeQuickInfo.Show(FieldCampText.SetupDegraded()));
        Logger.Log(LogSource, "camp initialization complete player=" + _campInitialized + " degraded=" + _setupDegraded
            + " agents=" + _agents.Count + " prisoners=" + _prisoners.Count + " props=" + props + "/" + _layout.Props.Count
            + " horses=" + horses + " cleared=" + cleared + " scene=" + (Mission.SceneName ?? ""));
    }

    private bool TryCampSetupStage(string stage, Action action)
    {
        try { action(); return true; }
        catch (Exception ex)
        {
            _setupDegraded = true;
            Logger.Log(LogSource, "[WARN] camp setup stage=" + stage + " failed; keeping mission open: " + ex);
            return false;
        }
    }

	private void SpawnEveryone()
	{
		Hero main = Hero.MainHero;
		uint c1 = main?.MapFaction?.Color ?? 4278190335u;
		uint c2 = main?.MapFaction?.Color2 ?? 4278190208u;
		_team = Mission.Teams.Add(BattleSideEnum.Attacker, c1, c2, main?.Clan?.Banner, true, false);
		Mission.PlayerTeam = _team;

		TryCampSetupStage("player", SpawnPlayer);
		TryCampSetupStage("troops", SpawnTroops);
		TryCampSetupStage("companions", SpawnCompanions);
		TryCampSetupStage("lords", SpawnArmyLords);
		TryCampSetupStage("prisoners", SpawnPrisoners);
	}

}
