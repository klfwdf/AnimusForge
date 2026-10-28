using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Tournaments.AgentControllers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics;

public class TournamentArcheryMissionController : MissionLogic, ITournamentGameBehavior
{
	private readonly List<ArcheryTournamentAgentController> _agentControllers;

	private TournamentMatch _match;

	private BasicMissionTimer _endTimer;

	private List<GameEntity> _spawnPoints;

	private bool _isSimulated;

	private CultureObject _culture;

	private List<DestructableComponent> _targets;

	public List<GameEntity> ShootingPositions;

	private readonly Equipment _archeryEquipment;

	public IEnumerable<ArcheryTournamentAgentController> AgentControllers => _agentControllers;

	public TournamentArcheryMissionController(CultureObject culture)
	{
		_culture = culture;
		ShootingPositions = new List<GameEntity>();
		_agentControllers = new List<ArcheryTournamentAgentController>();
		_archeryEquipment = new Equipment();
		_archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("nordic_shortbow")));
		_archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("blunt_arrows")));
		_archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("desert_lamellar")));
		_archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("reinforced_mail_mitten")));
		_archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("leather_cavalier_boots")));
	}

	public override void AfterStart()
	{
		TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_archery"));
		_spawnPoints = base.Mission.Scene.FindEntitiesWithTag("sp_arena").ToList();
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		_targets = (from x in base.Mission.ActiveMissionObjects.FindAllWithType<DestructableComponent>()
			where x.GameEntity.HasTag("archery_target")
			select x).ToList();
		foreach (DestructableComponent target in _targets)
		{
			target.OnDestroyed += OnTargetDestroyed;
		}
	}

	public void StartMatch(TournamentMatch match, bool isLastRound)
	{
		_match = match;
		ResetTargets();
		int count = _spawnPoints.Count;
		int num = 0;
		int num2 = 0;
		foreach (TournamentTeam team2 in _match.Teams)
		{
			Team team = base.Mission.Teams.Add(BattleSideEnum.None, MissionAgentHandler.GetRandomTournamentTeamColor(num2));
			foreach (TournamentParticipant participant in team2.Participants)
			{
				participant.MatchEquipment = _archeryEquipment.Clone();
				MatrixFrame globalFrame = _spawnPoints[num % count].GetGlobalFrame();
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				SetItemsAndSpawnCharacter(participant, team, globalFrame);
				num++;
			}
			num2++;
		}
	}

	public void SkipMatch(TournamentMatch match)
	{
		_match = match;
		Simulate();
	}

	private void Simulate()
	{
		_isSimulated = false;
		List<TournamentParticipant> list = _match.Participants.ToList();
		int num = _targets.Count;
		while (num > 0)
		{
			foreach (TournamentParticipant item in list)
			{
				if (num == 0)
				{
					break;
				}
				if (MBRandom.RandomFloat < GetDeadliness(item))
				{
					item.AddScore(1);
					num--;
				}
			}
		}
		_isSimulated = true;
	}

	public bool IsMatchEnded()
	{
		if (_isSimulated || _match == null)
		{
			return true;
		}
		if (_endTimer != null && _endTimer.ElapsedTime > 6f)
		{
			_endTimer = null;
			return true;
		}
		if (_endTimer == null && (!IsThereAnyTargetLeft() || !IsThereAnyArrowLeft()))
		{
			_endTimer = new BasicMissionTimer();
		}
		return false;
	}

	public void OnMatchEnded()
	{
		SandBoxHelpers.MissionHelper.FadeOutAgents(base.Mission.Agents, hideInstantly: true, hideMount: false);
		base.Mission.ClearCorpses(isMissionReset: false);
		base.Mission.Teams.Clear();
		base.Mission.RemoveSpawnedItemsAndMissiles();
		_match = null;
		_endTimer = null;
		_isSimulated = false;
	}

	private void ResetTargets()
	{
		foreach (DestructableComponent target in _targets)
		{
			target.Reset();
		}
	}

	private void SetItemsAndSpawnCharacter(TournamentParticipant participant, Team team, MatrixFrame frame)
	{
		AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(participant.Character, -1, null, participant.Descriptor)).Team(team).Equipment(participant.MatchEquipment).InitialPosition(in frame.origin)
			.InitialDirection(frame.rotation.f.AsVec2.Normalized())
			.Controller((!participant.Character.IsPlayerCharacter) ? AgentControllerType.AI : AgentControllerType.Player);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.Health = agent.HealthLimit;
		ArcheryTournamentAgentController archeryTournamentAgentController = agent.AddController(typeof(ArcheryTournamentAgentController)) as ArcheryTournamentAgentController;
		archeryTournamentAgentController.SetTargets(_targets);
		_agentControllers.Add(archeryTournamentAgentController);
		if (participant.Character.IsPlayerCharacter)
		{
			agent.WieldInitialWeapons();
			base.Mission.PlayerTeam = team;
		}
		else
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
	}

	public void OnTargetDestroyed(DestructableComponent destroyedComponent, Agent destroyerAgent, in MissionWeapon attackerWeapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		foreach (ArcheryTournamentAgentController agentController in AgentControllers)
		{
			agentController.OnTargetHit(destroyerAgent, destroyedComponent);
			_match.GetParticipant(destroyerAgent.Origin.UniqueSeed).AddScore(1);
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (IsMatchEnded())
		{
			return;
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			agent.GetController<ArcheryTournamentAgentController>()?.OnTick();
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.Mission.EndMission();
	}

	private bool IsThereAnyTargetLeft()
	{
		return _targets.Any((DestructableComponent e) => !e.IsDestroyed);
	}

	private bool IsThereAnyArrowLeft()
	{
		return base.Mission.Agents.Any((Agent agent) => agent.Equipment.GetAmmoAmount(EquipmentIndex.WeaponItemBeginSlot) > 0);
	}

	private float GetDeadliness(TournamentParticipant participant)
	{
		return 0.01f + (float)participant.Character.GetSkillValue(DefaultSkills.Bow) / 300f * 0.19f;
	}
}
