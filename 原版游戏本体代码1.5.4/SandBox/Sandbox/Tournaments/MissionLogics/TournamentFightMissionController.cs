using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Tournaments.MissionLogics;

public class TournamentFightMissionController : MissionLogic, ITournamentGameBehavior
{
	private readonly CharacterObject _defaultWeaponTemplatesIdTeamSizeOne = MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_one_participant_set_v1");

	private readonly CharacterObject _defaultWeaponTemplatesIdTeamSizeTwo = MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_two_participant_set_v1");

	private readonly CharacterObject _defaultWeaponTemplatesIdTeamSizeFour = MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_four_participant_set_v1");

	private TournamentMatch _match;

	private bool _isLastRound;

	private BasicMissionTimer _endTimer;

	private BasicMissionTimer _cheerTimer;

	private List<GameEntity> _spawnPoints;

	private bool _isSimulated;

	private bool _forceEndMatch;

	private bool _cheerStarted;

	private CultureObject _culture;

	private List<TournamentParticipant> _aliveParticipants;

	private List<TournamentTeam> _aliveTeams;

	private List<Agent> _currentTournamentAgents;

	private List<Agent> _currentTournamentMountAgents;

	private const float XpShareForKill = 0.5f;

	private const float XpShareForDamage = 0.5f;

	public TournamentFightMissionController(CultureObject culture)
	{
		_match = null;
		_culture = culture;
		_cheerStarted = false;
		_currentTournamentAgents = new List<Agent>();
		_currentTournamentMountAgents = new List<Agent>();
	}

	public override void OnBehaviorInitialize()
	{
		base.Mission.CanAgentRout_AdditionalCondition += CanAgentRout;
	}

	public override void AfterStart()
	{
		TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
		_spawnPoints = new List<GameEntity>();
		for (int i = 0; i < 4; i++)
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_arena_" + (i + 1));
			if (gameEntity != null)
			{
				_spawnPoints.Add(gameEntity);
			}
		}
		if (_spawnPoints.Count < 4)
		{
			_spawnPoints = base.Mission.Scene.FindEntitiesWithTag("sp_arena").ToList();
		}
	}

	public void PrepareForMatch()
	{
		List<Equipment> teamWeaponEquipmentList = GetTeamWeaponEquipmentList(_match.Teams.First().Participants.Count());
		foreach (TournamentTeam team in _match.Teams)
		{
			int num = 0;
			foreach (TournamentParticipant participant in team.Participants)
			{
				participant.MatchEquipment = teamWeaponEquipmentList[num].Clone();
				AddRandomClothes(_culture, participant);
				num++;
			}
		}
	}

	public void StartMatch(TournamentMatch match, bool isLastRound)
	{
		_cheerStarted = false;
		_match = match;
		_isLastRound = isLastRound;
		PrepareForMatch();
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		List<Team> list = new List<Team>();
		int count = _spawnPoints.Count;
		int num = 0;
		foreach (TournamentTeam team2 in _match.Teams)
		{
			BattleSideEnum side = ((!team2.IsPlayerTeam) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			Team team = base.Mission.Teams.Add(side, team2.TeamColor, uint.MaxValue, team2.TeamBanner);
			GameEntity spawnPoint = _spawnPoints[num % count];
			foreach (TournamentParticipant participant in team2.Participants)
			{
				if (participant.Character.IsPlayerCharacter)
				{
					SpawnTournamentParticipant(spawnPoint, participant, team);
					break;
				}
			}
			foreach (TournamentParticipant participant2 in team2.Participants)
			{
				if (!participant2.Character.IsPlayerCharacter)
				{
					SpawnTournamentParticipant(spawnPoint, participant2, team);
				}
			}
			num++;
			list.Add(team);
		}
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				list[i].SetIsEnemyOf(list[j], isEnemyOf: true);
			}
		}
		_aliveParticipants = _match.Participants.ToList();
		_aliveTeams = _match.Teams.ToList();
	}

	protected override void OnEndMission()
	{
		base.Mission.CanAgentRout_AdditionalCondition -= CanAgentRout;
	}

	private void SpawnTournamentParticipant(GameEntity spawnPoint, TournamentParticipant participant, Team team)
	{
		MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
		globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		SpawnAgentWithRandomItems(participant, team, globalFrame);
	}

	private List<Equipment> GetTeamWeaponEquipmentList(int teamSize)
	{
		List<Equipment> list = new List<Equipment>();
		CultureObject culture = PlayerEncounter.EncounterSettlement.Culture;
		MBReadOnlyList<CharacterObject> mBReadOnlyList = teamSize switch
		{
			2 => culture.TournamentTeamTemplatesForTwoParticipant, 
			4 => culture.TournamentTeamTemplatesForFourParticipant, 
			_ => culture.TournamentTeamTemplatesForOneParticipant, 
		};
		CharacterObject characterObject = ((mBReadOnlyList.Count <= 0) ? (teamSize switch
		{
			2 => _defaultWeaponTemplatesIdTeamSizeTwo, 
			4 => _defaultWeaponTemplatesIdTeamSizeFour, 
			_ => _defaultWeaponTemplatesIdTeamSizeOne, 
		}) : mBReadOnlyList[MBRandom.RandomInt(mBReadOnlyList.Count)]);
		foreach (Equipment battleEquipment in characterObject.BattleEquipments)
		{
			Equipment equipment = new Equipment();
			equipment.FillFrom(battleEquipment);
			list.Add(equipment);
		}
		return list;
	}

	public void SkipMatch(TournamentMatch match)
	{
		_match = match;
		PrepareForMatch();
		Simulate();
	}

	public bool IsMatchEnded()
	{
		if (_isSimulated || _match == null)
		{
			return true;
		}
		if ((_endTimer != null && _endTimer.ElapsedTime > 6f) || _forceEndMatch)
		{
			_forceEndMatch = false;
			_endTimer = null;
			return true;
		}
		if (_cheerTimer != null && !_cheerStarted && _cheerTimer.ElapsedTime > 1f)
		{
			OnMatchResultsReady();
			_cheerTimer = null;
			_cheerStarted = true;
			AgentVictoryLogic missionBehavior = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
			foreach (Agent currentTournamentAgent in _currentTournamentAgents)
			{
				if (currentTournamentAgent.IsAIControlled)
				{
					missionBehavior.SetTimersOfVictoryReactionsOnTournamentVictoryForAgent(currentTournamentAgent, 1f, 3f);
				}
			}
			return false;
		}
		if (_endTimer == null && !CheckIfIsThereAnyEnemies())
		{
			_endTimer = new BasicMissionTimer();
			if (!_cheerStarted)
			{
				_cheerTimer = new BasicMissionTimer();
			}
		}
		return false;
	}

	public void OnMatchResultsReady()
	{
		if (_match.IsPlayerParticipating())
		{
			if (_match.IsPlayerWinner())
			{
				if (_isLastRound)
				{
					if (_match.QualificationMode == TournamentGame.QualificationMode.IndividualScore)
					{
						MBInformationManager.AddQuickInformation(new TextObject("{=Jn0k20c3}Round is over, you survived the final round of the tournament."));
					}
					else
					{
						MBInformationManager.AddQuickInformation(new TextObject("{=wOqOQuJl}Round is over, your team survived the final round of the tournament."));
					}
				}
				else if (_match.QualificationMode == TournamentGame.QualificationMode.IndividualScore)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=uytwdSVH}Round is over, you are qualified for the next stage of the tournament."));
				}
				else
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=fkOYvnVG}Round is over, your team is qualified for the next stage of the tournament."));
				}
			}
			else if (_match.QualificationMode == TournamentGame.QualificationMode.IndividualScore)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=lcVauEKV}Round is over, you are disqualified from the tournament."));
			}
			else
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=MLyBN51z}Round is over, your team is disqualified from the tournament."));
			}
		}
		else
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=UBd0dEPp}Match is over"));
		}
	}

	public void OnMatchEnded()
	{
		SandBoxHelpers.MissionHelper.FadeOutAgents(_currentTournamentAgents.Where((Agent x) => x.IsActive()), hideInstantly: true, hideMount: false);
		SandBoxHelpers.MissionHelper.FadeOutAgents(_currentTournamentMountAgents.Where((Agent x) => x.IsActive()), hideInstantly: true, hideMount: false);
		base.Mission.ClearCorpses(isMissionReset: false);
		base.Mission.Teams.Clear();
		base.Mission.RemoveSpawnedItemsAndMissiles();
		_match = null;
		_endTimer = null;
		_cheerTimer = null;
		_isSimulated = false;
		_currentTournamentAgents.Clear();
		_currentTournamentMountAgents.Clear();
	}

	private void SpawnAgentWithRandomItems(TournamentParticipant participant, Team team, MatrixFrame frame)
	{
		frame.Strafe((float)MBRandom.RandomInt(-2, 2) * 1f);
		frame.Advance((float)MBRandom.RandomInt(0, 2) * 1f);
		CharacterObject character = participant.Character;
		AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, participant.Descriptor)).Team(team).InitialPosition(in frame.origin).InitialDirection(frame.rotation.f.AsVec2.Normalized())
			.Equipment(participant.MatchEquipment)
			.ClothingColor1(team.Color)
			.Banner(team.Banner)
			.Controller((!character.IsPlayerCharacter) ? AgentControllerType.AI : AgentControllerType.Player);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		if (character.IsPlayerCharacter)
		{
			agent.Health = character.HeroObject.HitPoints;
			base.Mission.PlayerTeam = team;
		}
		else
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		agent.WieldInitialWeapons();
		_currentTournamentAgents.Add(agent);
		if (agent.HasMount)
		{
			_currentTournamentMountAgents.Add(agent.MountAgent);
		}
	}

	private void AddRandomClothes(CultureObject culture, TournamentParticipant participant)
	{
		Equipment participantArmor = Campaign.Current.Models.TournamentModel.GetParticipantArmor(participant.Character);
		for (int i = 5; i < 10; i++)
		{
			EquipmentElement equipmentFromSlot = participantArmor.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item != null)
			{
				participant.MatchEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
			}
		}
	}

	private bool CheckIfTeamIsDead(TournamentTeam affectedParticipantTeam)
	{
		bool result = true;
		foreach (TournamentParticipant aliveParticipant in _aliveParticipants)
		{
			if (aliveParticipant.Team == affectedParticipantTeam)
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private void AddScoreToRemainingTeams()
	{
		foreach (TournamentTeam aliveTeam in _aliveTeams)
		{
			foreach (TournamentParticipant participant in aliveTeam.Participants)
			{
				participant.AddScore(1);
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (!IsMatchEnded() && affectedAgent.IsHuman)
		{
			TournamentParticipant participant = _match.GetParticipant(affectedAgent.Origin.UniqueSeed);
			_aliveParticipants.Remove(participant);
			_currentTournamentAgents.Remove(affectedAgent);
			if (CheckIfTeamIsDead(participant.Team))
			{
				_aliveTeams.Remove(participant.Team);
				AddScoreToRemainingTeams();
			}
		}
	}

	public bool CanAgentRout(Agent agent)
	{
		return false;
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent == null)
		{
			return;
		}
		if (affectorAgent.IsMount && affectorAgent.RiderAgent != null)
		{
			affectorAgent = affectorAgent.RiderAgent;
		}
		if (affectorAgent.Character != null && affectedAgent.Character != null)
		{
			float num = blow.InflictedDamage;
			if (num > affectedAgent.HealthLimit)
			{
				num = affectedAgent.HealthLimit;
			}
			float num2 = num / affectedAgent.HealthLimit;
			EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, blow.AttackType, 0.5f * num2, num, collisionData.IsSneakAttack);
		}
	}

	private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
	{
		CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
		CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;
		if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
		{
			bool isHorseCharge = affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision;
			SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Tournament, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damageAmount, affectedAgent.Health < 1f, isSiegeEngineHit: false, isHorseCharge, isSneakAttack);
		}
	}

	public bool CheckIfIsThereAnyEnemies()
	{
		Team team = null;
		foreach (Agent currentTournamentAgent in _currentTournamentAgents)
		{
			if (currentTournamentAgent.IsHuman && currentTournamentAgent.IsActive() && currentTournamentAgent.Team != null)
			{
				if (team == null)
				{
					team = currentTournamentAgent.Team;
				}
				else if (team != currentTournamentAgent.Team)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void Simulate()
	{
		_isSimulated = false;
		if (_currentTournamentAgents.Count == 0)
		{
			_aliveParticipants = _match.Participants.ToList();
			_aliveTeams = _match.Teams.ToList();
		}
		TournamentParticipant tournamentParticipant = _aliveParticipants.FirstOrDefault((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
		if (tournamentParticipant != null)
		{
			TournamentTeam team = tournamentParticipant.Team;
			foreach (TournamentParticipant participant in team.Participants)
			{
				participant.ResetScore();
				_aliveParticipants.Remove(participant);
			}
			_aliveTeams.Remove(team);
			AddScoreToRemainingTeams();
		}
		Dictionary<TournamentParticipant, Tuple<float, float>> dictionary = new Dictionary<TournamentParticipant, Tuple<float, float>>();
		foreach (TournamentParticipant aliveParticipant in _aliveParticipants)
		{
			aliveParticipant.Character.GetSimulationAttackPower(out var attackPoints, out var defencePoints, aliveParticipant.MatchEquipment);
			dictionary.Add(aliveParticipant, new Tuple<float, float>(attackPoints, defencePoints));
		}
		int num = 0;
		while (_aliveParticipants.Count > 1 && _aliveTeams.Count > 1)
		{
			num++;
			num %= _aliveParticipants.Count;
			TournamentParticipant tournamentParticipant2 = _aliveParticipants[num];
			int num2;
			TournamentParticipant tournamentParticipant3;
			do
			{
				num2 = MBRandom.RandomInt(_aliveParticipants.Count);
				tournamentParticipant3 = _aliveParticipants[num2];
			}
			while (tournamentParticipant2 == tournamentParticipant3 || tournamentParticipant2.Team == tournamentParticipant3.Team);
			if (dictionary[tournamentParticipant3].Item2 - dictionary[tournamentParticipant2].Item1 > 0f)
			{
				dictionary[tournamentParticipant3] = new Tuple<float, float>(dictionary[tournamentParticipant3].Item1, dictionary[tournamentParticipant3].Item2 - dictionary[tournamentParticipant2].Item1);
				continue;
			}
			dictionary.Remove(tournamentParticipant3);
			_aliveParticipants.Remove(tournamentParticipant3);
			if (CheckIfTeamIsDead(tournamentParticipant3.Team))
			{
				_aliveTeams.Remove(tournamentParticipant3.Team);
				AddScoreToRemainingTeams();
			}
			if (num2 < num)
			{
				num--;
			}
		}
		_isSimulated = true;
	}

	private bool IsThereAnyPlayerAgent()
	{
		if (base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive())
		{
			return true;
		}
		return _currentTournamentAgents.Any((Agent agent) => agent.IsPlayerControlled);
	}

	private void SkipMatch()
	{
		Mission.Current.GetMissionBehavior<TournamentBehavior>().SkipMatch();
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		InquiryData result = null;
		canPlayerLeave = true;
		if (_match != null)
		{
			if (_match.IsPlayerParticipating())
			{
				MBTextManager.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.EncyclopediaLinkWithName);
				if (IsThereAnyPlayerAgent())
				{
					if (base.Mission.IsPlayerCloseToAnEnemy())
					{
						canPlayerLeave = false;
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat"));
					}
					else if (CheckIfIsThereAnyEnemies())
					{
						result = new InquiryData(GameTexts.FindText("str_tournament").ToString(), GameTexts.FindText("str_tournament_forfeit_game").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), SkipMatch, null);
					}
					else
					{
						_forceEndMatch = true;
						canPlayerLeave = false;
					}
				}
				else if (CheckIfIsThereAnyEnemies())
				{
					result = new InquiryData(GameTexts.FindText("str_tournament").ToString(), GameTexts.FindText("str_tournament_skip").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), SkipMatch, null);
				}
				else
				{
					_forceEndMatch = true;
					canPlayerLeave = false;
				}
			}
			else if (CheckIfIsThereAnyEnemies())
			{
				result = new InquiryData(GameTexts.FindText("str_tournament").ToString(), GameTexts.FindText("str_tournament_skip").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), SkipMatch, null);
			}
			else
			{
				_forceEndMatch = true;
				canPlayerLeave = false;
			}
		}
		return result;
	}
}
