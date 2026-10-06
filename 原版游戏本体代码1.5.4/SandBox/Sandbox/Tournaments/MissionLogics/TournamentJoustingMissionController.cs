using System.Collections.Generic;
using System.Linq;
using SandBox.Tournaments.AgentControllers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics;

public class TournamentJoustingMissionController : MissionLogic, ITournamentGameBehavior
{
	public delegate void JoustingEventDelegate(Agent affectedAgent, Agent affectorAgent);

	public delegate void JoustingAgentStateChangedEventDelegate(Agent agent, JoustingAgentController.JoustingAgentState state);

	private Team _winnerTeam;

	public List<GameEntity> RegionBoxList;

	public List<GameEntity> RegionExitBoxList;

	public List<MatrixFrame> CornerBackStartList;

	public List<GameEntity> CornerStartList;

	public List<MatrixFrame> CornerMiddleList;

	public List<MatrixFrame> CornerFinishList;

	public bool IsSwordDuelStarted;

	private TournamentMatch _match;

	private BasicMissionTimer _endTimer;

	private bool _isSimulated;

	private CultureObject _culture;

	private readonly Equipment _joustingEquipment;

	public event JoustingEventDelegate VictoryAchieved;

	public event JoustingEventDelegate PointGanied;

	public event JoustingEventDelegate Disqualified;

	public event JoustingEventDelegate Unconscious;

	public event JoustingAgentStateChangedEventDelegate AgentStateChanged;

	public TournamentJoustingMissionController(CultureObject culture)
	{
		_culture = culture;
		_match = null;
		RegionBoxList = new List<GameEntity>(2);
		RegionExitBoxList = new List<GameEntity>(2);
		CornerBackStartList = new List<MatrixFrame>();
		CornerStartList = new List<GameEntity>(2);
		CornerMiddleList = new List<MatrixFrame>();
		CornerFinishList = new List<MatrixFrame>();
		IsSwordDuelStarted = false;
		_joustingEquipment = new Equipment();
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("charger")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("horse_harness_e")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("vlandia_lance_2_t4")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("leather_round_shield")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("desert_lamellar")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("nasal_helmet_with_mail")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("reinforced_mail_mitten")));
		_joustingEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("leather_cavalier_boots")));
	}

	public override void AfterStart()
	{
		TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_jousting"));
		for (int i = 0; i < 2; i++)
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_jousting_back_" + i);
			GameEntity item = base.Mission.Scene.FindEntityWithTag("sp_jousting_start_" + i);
			GameEntity gameEntity2 = base.Mission.Scene.FindEntityWithTag("sp_jousting_middle_" + i);
			GameEntity gameEntity3 = base.Mission.Scene.FindEntityWithTag("sp_jousting_finish_" + i);
			CornerBackStartList.Add(gameEntity.GetGlobalFrame());
			CornerStartList.Add(item);
			CornerMiddleList.Add(gameEntity2.GetGlobalFrame());
			CornerFinishList.Add(gameEntity3.GetGlobalFrame());
		}
		GameEntity item2 = base.Mission.Scene.FindEntityWithName("region_box_0");
		RegionBoxList.Add(item2);
		GameEntity item3 = base.Mission.Scene.FindEntityWithName("region_box_1");
		RegionBoxList.Add(item3);
		GameEntity item4 = base.Mission.Scene.FindEntityWithName("region_end_box_0");
		RegionExitBoxList.Add(item4);
		GameEntity item5 = base.Mission.Scene.FindEntityWithName("region_end_box_1");
		RegionExitBoxList.Add(item5);
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
	}

	public void StartMatch(TournamentMatch match, bool isLastRound)
	{
		_match = match;
		int num = 0;
		foreach (TournamentTeam team2 in _match.Teams)
		{
			Team team = base.Mission.Teams.Add(BattleSideEnum.None);
			foreach (TournamentParticipant participant in team2.Participants)
			{
				participant.MatchEquipment = _joustingEquipment.Clone();
				SetItemsAndSpawnCharacter(participant, team, num);
			}
			num++;
		}
		List<Team> list = base.Mission.Teams.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				list[i].SetIsEnemyOf(list[j], isEnemyOf: true);
			}
		}
		base.Mission.Scene.SetAbilityOfFacesWithId(1, isEnabled: false);
		base.Mission.Scene.SetAbilityOfFacesWithId(2, isEnabled: false);
	}

	public void SkipMatch(TournamentMatch match)
	{
		_match = match;
		Simulate();
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
		if (_endTimer == null && _winnerTeam != null)
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

	private void Simulate()
	{
		_isSimulated = false;
		List<TournamentParticipant> participants = _match.Participants.ToList();
		while (participants.Count > 1 && participants.Any((TournamentParticipant x) => x.Team != participants[0].Team) && !participants.Any((TournamentParticipant x) => x.Score >= 3))
		{
			TournamentParticipant tournamentParticipant = participants[MBRandom.RandomInt(participants.Count)];
			TournamentParticipant tournamentParticipant2 = participants[MBRandom.RandomInt(participants.Count)];
			while (tournamentParticipant == tournamentParticipant2 || tournamentParticipant.Team == tournamentParticipant2.Team)
			{
				tournamentParticipant2 = participants[MBRandom.RandomInt(participants.Count)];
			}
			tournamentParticipant.AddScore(1);
		}
		_isSimulated = true;
	}

	private void SetItemsAndSpawnCharacter(TournamentParticipant participant, Team team, int cornerIndex)
	{
		AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(participant.Character, -1, null, participant.Descriptor)).Team(team).InitialFrameFromSpawnPointEntity(CornerStartList[cornerIndex]).Equipment(participant.MatchEquipment)
			.Controller((!participant.Character.IsPlayerCharacter) ? AgentControllerType.AI : AgentControllerType.Player);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.Health = agent.HealthLimit;
		AddJoustingAgentController(agent);
		agent.GetController<JoustingAgentController>().CurrentCornerIndex = cornerIndex;
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

	private void AddJoustingAgentController(Agent agent)
	{
		agent.AddController(typeof(JoustingAgentController));
	}

	public bool IsAgentInTheTrack(Agent agent, bool inCurrentTrack = true)
	{
		bool result = false;
		if (agent != null)
		{
			JoustingAgentController controller = agent.GetController<JoustingAgentController>();
			int index = (inCurrentTrack ? controller.CurrentCornerIndex : (1 - controller.CurrentCornerIndex));
			result = RegionBoxList[index].CheckPointWithOrientedBoundingBox(agent.Position);
		}
		return result;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (base.Mission.IsMissionEnding)
		{
			return;
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			agent.GetController<JoustingAgentController>()?.UpdateState();
		}
		CheckStartOfSwordDuel();
	}

	private void CheckStartOfSwordDuel()
	{
		if (base.Mission.IsMissionEnding)
		{
			return;
		}
		if (!IsSwordDuelStarted)
		{
			if (base.Mission.Agents.Count <= 0 || base.Mission.Agents.Count((Agent a) => a.IsMount) >= 2)
			{
				return;
			}
			IsSwordDuelStarted = true;
			RemoveBarriers();
			base.Mission.Scene.SetAbilityOfFacesWithId(2, isEnabled: true);
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent.IsHuman)
					{
						JoustingAgentController controller = agent.GetController<JoustingAgentController>();
						controller.State = JoustingAgentController.JoustingAgentState.SwordDuel;
						controller.PrepareAgentToSwordDuel();
					}
				}
				return;
			}
		}
		foreach (Agent agent2 in base.Mission.Agents)
		{
			if (!agent2.IsHuman)
			{
				continue;
			}
			JoustingAgentController controller2 = agent2.GetController<JoustingAgentController>();
			controller2.State = JoustingAgentController.JoustingAgentState.SwordDuel;
			if (controller2.PrepareEquipmentsAfterDismount && agent2.MountAgent == null)
			{
				CharacterObject obj = (CharacterObject)agent2.Character;
				controller2.PrepareEquipmentsForSwordDuel();
				agent2.DisableScriptedMovement();
				if (obj == CharacterObject.PlayerCharacter)
				{
					agent2.Controller = AgentControllerType.Player;
				}
			}
		}
	}

	private void RemoveBarriers()
	{
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("jousting_barrier").ToList())
		{
			item.Remove(95);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		if (base.Mission.IsMissionEnding || IsSwordDuelStarted || !affectedAgent.IsHuman || affectorAgent == null || !affectorAgent.IsHuman || affectedAgent == affectorAgent)
		{
			return;
		}
		JoustingAgentController controller = affectorAgent.GetController<JoustingAgentController>();
		JoustingAgentController controller2 = affectedAgent.GetController<JoustingAgentController>();
		if (IsAgentInTheTrack(affectorAgent) && controller2.IsRiding() && controller.IsRiding())
		{
			_match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(1);
			controller.Score++;
			if (controller.Score >= 3)
			{
				_winnerTeam = affectorAgent.Team;
				this.VictoryAchieved?.Invoke(affectorAgent, affectedAgent);
			}
			else
			{
				this.PointGanied?.Invoke(affectorAgent, affectedAgent);
			}
		}
		else
		{
			_match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(-100);
			_winnerTeam = affectedAgent.Team;
			MBTextManager.SetTextVariable("OPPONENT_GENDER", affectorAgent.Character.IsFemale ? "0" : "1");
			this.Disqualified?.Invoke(affectorAgent, affectedAgent);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (base.Mission.IsMissionEnding || !affectedAgent.IsHuman || affectorAgent == null || !affectorAgent.IsHuman || affectedAgent == affectorAgent)
		{
			return;
		}
		if (IsAgentInTheTrack(affectorAgent) || IsSwordDuelStarted)
		{
			_match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(100);
			_winnerTeam = affectorAgent.Team;
			if (this.Unconscious != null)
			{
				this.Unconscious(affectorAgent, affectedAgent);
			}
			return;
		}
		_match.GetParticipant(affectorAgent.Origin.UniqueSeed).AddScore(-100);
		_winnerTeam = affectedAgent.Team;
		MBTextManager.SetTextVariable("OPPONENT_GENDER", affectorAgent.Character.IsFemale ? "0" : "1");
		if (this.Disqualified != null)
		{
			this.Disqualified(affectorAgent, affectedAgent);
		}
	}

	public void OnJoustingAgentStateChanged(Agent agent, JoustingAgentController.JoustingAgentState state)
	{
		if (this.AgentStateChanged != null)
		{
			this.AgentStateChanged(agent, state);
		}
	}
}
