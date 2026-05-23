using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Issues.IssueQuestTasks;

public class ArenaDuelQuestTask : QuestTaskBase
{
	private Settlement _settlement;

	private CharacterObject _opponentCharacter;

	private Agent _playerAgent;

	private Agent _opponentAgent;

	private bool _duelStarted;

	private BasicMissionTimer _missionEndTimer;

	public ArenaDuelQuestTask(CharacterObject duelOpponentCharacter, Settlement settlement, Action onSucceededAction, Action onFailedAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction, onFailedAction)
	{
		_opponentCharacter = duelOpponentCharacter;
		_settlement = settlement;
	}

	public void AfterStart(IMission mission)
	{
		if (!Mission.Current.HasMissionBehavior<ArenaDuelMissionBehavior>() || PlayerEncounter.LocationEncounter.Settlement != _settlement)
		{
			return;
		}
		InitializeTeams();
		List<MatrixFrame> list = (from e in Mission.Current.Scene.FindEntitiesWithTag("sp_arena_respawn")
			select e.GetGlobalFrame()).ToList();
		MatrixFrame m = list[MBRandom.RandomInt(list.Count)];
		float num = float.MaxValue;
		MatrixFrame frame = m;
		foreach (MatrixFrame item in list)
		{
			MatrixFrame m2 = item;
			if (m != m2)
			{
				Vec3 origin = m2.origin;
				if (origin.DistanceSquared(m.origin) < num)
				{
					frame = m2;
				}
			}
		}
		m.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		_playerAgent = SpawnArenaAgent(CharacterObject.PlayerCharacter, Mission.Current.PlayerTeam, m);
		_opponentAgent = SpawnArenaAgent(_opponentCharacter, Mission.Current.PlayerEnemyTeam, frame);
	}

	public override void SetReferences()
	{
		CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, AfterStart);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, MissionTick);
	}

	public void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (Hero.MainHero.CurrentSettlement != _settlement)
		{
			return;
		}
		if (_duelStarted)
		{
			if (_opponentAgent.IsActive())
			{
				Finish(FinishStates.Fail);
			}
			else
			{
				Finish(FinishStates.Success);
			}
		}
		else
		{
			OpenArenaDuelMission();
		}
	}

	public void MissionTick(float dt)
	{
		if (Mission.Current.HasMissionBehavior<ArenaDuelMissionBehavior>() && PlayerEncounter.LocationEncounter.Settlement == _settlement && ((_playerAgent != null && !_playerAgent.IsActive()) || (_opponentAgent != null && !_opponentAgent.IsActive())))
		{
			if (_missionEndTimer != null && _missionEndTimer.ElapsedTime > 4f)
			{
				Mission.Current.EndMission();
			}
			else if (_missionEndTimer == null && ((_playerAgent != null && !_playerAgent.IsActive()) || (_opponentAgent != null && !_opponentAgent.IsActive())))
			{
				_missionEndTimer = new BasicMissionTimer();
			}
		}
	}

	private void OpenArenaDuelMission()
	{
		Location locationWithId = _settlement.LocationComplex.GetLocationWithId("arena");
		int upgradeLevel = ((!_settlement.IsTown) ? 1 : _settlement.Town.GetWallLevel());
		SandBoxMissions.OpenArenaDuelMission(locationWithId.GetSceneName(upgradeLevel), locationWithId);
		_duelStarted = true;
	}

	private void InitializeTeams()
	{
		Mission.Current.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2);
		Mission.Current.Teams.Add(BattleSideEnum.Attacker, Hero.MainHero.MapFaction.Color2, Hero.MainHero.MapFaction.Color);
		Mission.Current.PlayerTeam = Mission.Current.DefenderTeam;
	}

	private Agent SpawnArenaAgent(CharacterObject character, Team team, MatrixFrame frame)
	{
		if (team == Mission.Current.PlayerTeam)
		{
			character = CharacterObject.PlayerCharacter;
		}
		Equipment randomElement = _settlement.Culture.DuelPresetEquipmentRoster.AllEquipments.GetRandomElement();
		Agent agent = Mission.Current.SpawnAgent(new AgentBuildData(character).Team(team).ClothingColor1(team.Color).ClothingColor2(team.Color2)
			.InitialPosition(in frame.origin)
			.InitialDirection(frame.rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.Equipment(randomElement)
			.TroopOrigin(new SimpleAgentOrigin(character))
			.Controller((character != CharacterObject.PlayerCharacter) ? AgentControllerType.AI : AgentControllerType.Player));
		if (agent.IsAIControlled)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		return agent;
	}
}
