using System;
using System.Linq;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena;

public class ArenaDuelMissionController : MissionLogic
{
	private CharacterObject _duelCharacter;

	private bool _requireCivilianEquipment;

	private bool _spawnBothSideWithHorses;

	private bool _duelHasEnded;

	private Agent _duelAgent;

	private float _customAgentHealth;

	private BasicMissionTimer _duelEndTimer;

	private MBList<MatrixFrame> _initialSpawnFrames;

	private static Action<CharacterObject> _onDuelEnd;

	public ArenaDuelMissionController(CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBothSideWithHorses, Action<CharacterObject> onDuelEnd, float customAgentHealth)
	{
		_duelCharacter = duelCharacter;
		_requireCivilianEquipment = requireCivilianEquipment;
		_spawnBothSideWithHorses = spawnBothSideWithHorses;
		_customAgentHealth = customAgentHealth;
		_onDuelEnd = onDuelEnd;
	}

	public override void AfterStart()
	{
		_duelHasEnded = false;
		_duelEndTimer = new BasicMissionTimer();
		DeactivateOtherTournamentSets();
		InitializeMissionTeams();
		_initialSpawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena")
			select e.GetGlobalFrame()).ToMBList();
		for (int num = 0; num < _initialSpawnFrames.Count; num++)
		{
			MatrixFrame value = _initialSpawnFrames[num];
			value.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			_initialSpawnFrames[num] = value;
		}
		MatrixFrame randomElement = _initialSpawnFrames.GetRandomElement();
		_initialSpawnFrames.Remove(randomElement);
		MatrixFrame randomElement2 = _initialSpawnFrames.GetRandomElement();
		SpawnAgent(CharacterObject.PlayerCharacter, randomElement);
		_duelAgent = SpawnAgent(_duelCharacter, randomElement2);
		_duelAgent.Defensiveness = 1f;
	}

	private void InitializeMissionTeams()
	{
		base.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, _duelCharacter.Culture.Color, _duelCharacter.Culture.Color2);
		base.Mission.PlayerTeam = base.Mission.Teams.Defender;
	}

	private void DeactivateOtherTournamentSets()
	{
		TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
	}

	private Agent SpawnAgent(CharacterObject character, MatrixFrame spawnFrame)
	{
		AgentBuildData agentBuildData = new AgentBuildData(character);
		agentBuildData.BodyProperties(character.GetBodyPropertiesMax());
		Agent agent = base.Mission.SpawnAgent(agentBuildData.Team((character == CharacterObject.PlayerCharacter) ? base.Mission.PlayerTeam : base.Mission.PlayerEnemyTeam).InitialPosition(in spawnFrame.origin).InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
			.NoHorses(!_spawnBothSideWithHorses)
			.Equipment(_requireCivilianEquipment ? character.FirstCivilianEquipment : character.FirstBattleEquipment)
			.TroopOrigin(new SimpleAgentOrigin(character)));
		agent.FadeIn();
		if (character == CharacterObject.PlayerCharacter)
		{
			agent.Controller = AgentControllerType.Player;
		}
		if (agent.IsAIControlled)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		agent.Health = _customAgentHealth;
		agent.BaseHealthLimit = _customAgentHealth;
		agent.HealthLimit = _customAgentHealth;
		return agent;
	}

	public override void OnMissionTick(float dt)
	{
		if (_duelHasEnded && _duelEndTimer.ElapsedTime > 4f)
		{
			GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4)));
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_duel_has_ended"));
			_duelEndTimer.Reset();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (_onDuelEnd != null)
		{
			_onDuelEnd((affectedAgent == _duelAgent) ? CharacterObject.PlayerCharacter : _duelCharacter);
			_onDuelEnd = null;
			_duelHasEnded = true;
			_duelEndTimer.Reset();
		}
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (!_duelHasEnded)
		{
			canPlayerLeave = false;
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat_duel_ongoing"));
		}
		return null;
	}
}
