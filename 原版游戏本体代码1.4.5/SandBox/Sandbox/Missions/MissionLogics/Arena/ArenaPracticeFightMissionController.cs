using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena;

public class ArenaPracticeFightMissionController : MissionLogic
{
	private const int AIParticipantCount = 30;

	private const int MaxAliveAgentCount = 6;

	private const int MaxSpawnInterval = 14;

	private const int MinSpawnDistanceSquared = 144;

	private const int TotalStageCount = 3;

	private const int PracticeFightTroopTierLimit = 3;

	public int TeleportTime = 5;

	private Settlement _settlement;

	private int _spawnedOpponentAgentCount;

	private int _aliveOpponentCount;

	private float _nextSpawnTime;

	private List<MatrixFrame> _initialSpawnFrames;

	private List<MatrixFrame> _spawnFrames;

	private List<Team> _AIParticipantTeams;

	private List<Agent> _participantAgents;

	private Team _tournamentMasterTeam;

	private BasicMissionTimer _teleportTimer;

	private List<CharacterObject> _participantCharacters;

	private const float XpShareForKill = 0.5f;

	private const float XpShareForDamage = 0.5f;

	private int AISpawnIndex => _spawnedOpponentAgentCount;

	public int RemainingOpponentCountFromLastPractice { get; private set; }

	public bool IsPlayerPracticing { get; private set; }

	public int OpponentCountBeatenByPlayer { get; private set; }

	public int RemainingOpponentCount => 30 - _spawnedOpponentAgentCount + _aliveOpponentCount;

	public bool IsPlayerSurvived { get; private set; }

	public bool AfterPractice { get; set; }

	public override void AfterStart()
	{
		_settlement = PlayerEncounter.LocationEncounter.Settlement;
		InitializeTeams();
		GameEntity item = base.Mission.Scene.FindEntityWithTag("tournament_practice") ?? base.Mission.Scene.FindEntityWithTag("tournament_fight");
		List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("arena_set").ToList();
		list.Remove(item);
		foreach (GameEntity item2 in list)
		{
			item2.Remove(88);
		}
		_initialSpawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena")
			select e.GetGlobalFrame()).ToList();
		_spawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena_respawn")
			select e.GetGlobalFrame()).ToList();
		for (int num = 0; num < _initialSpawnFrames.Count; num++)
		{
			MatrixFrame value = _initialSpawnFrames[num];
			value.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			_initialSpawnFrames[num] = value;
		}
		for (int num2 = 0; num2 < _spawnFrames.Count; num2++)
		{
			MatrixFrame value2 = _spawnFrames[num2];
			value2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			_spawnFrames[num2] = value2;
		}
		IsPlayerPracticing = false;
		_participantAgents = new List<Agent>();
		StartPractice();
		MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		SandBoxHelpers.MissionHelper.SpawnPlayer(civilianEquipment: true, noHorses: true);
		missionBehavior.SpawnLocationCharacters();
	}

	private void SpawnPlayerNearTournamentMaster()
	{
		GameEntity entity = base.Mission.Scene.FindEntityWithTag("sp_player_near_arena_master");
		base.Mission.SpawnAgent(new AgentBuildData(CharacterObject.PlayerCharacter).Team(base.Mission.PlayerTeam).InitialFrameFromSpawnPointEntity(entity).NoHorses(noHorses: true)
			.CivilianEquipment(civilianEquipment: true)
			.TroopOrigin(new SimpleAgentOrigin(CharacterObject.PlayerCharacter))
			.Controller(AgentControllerType.Player));
		Mission.Current.SetMissionMode(MissionMode.StartUp, atStart: false);
	}

	private Agent SpawnArenaAgent(Team team, MatrixFrame frame)
	{
		CharacterObject characterObject;
		int spawnIndex;
		if (team == base.Mission.PlayerTeam)
		{
			characterObject = CharacterObject.PlayerCharacter;
			spawnIndex = 0;
		}
		else
		{
			characterObject = _participantCharacters[AISpawnIndex];
			spawnIndex = AISpawnIndex;
		}
		Equipment equipment = new Equipment();
		AddRandomWeapons(equipment, spawnIndex);
		AddRandomClothes(characterObject, equipment);
		Agent agent = base.Mission.SpawnAgent(new AgentBuildData(characterObject).Team(team).InitialPosition(in frame.origin).InitialDirection(frame.rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.Equipment(equipment)
			.TroopOrigin(new SimpleAgentOrigin(characterObject))
			.Controller((characterObject != CharacterObject.PlayerCharacter) ? AgentControllerType.AI : AgentControllerType.Player));
		agent.FadeIn();
		if (characterObject != CharacterObject.PlayerCharacter)
		{
			_aliveOpponentCount++;
			_spawnedOpponentAgentCount++;
		}
		if (agent.IsAIControlled)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		return agent;
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

	private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData attackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
	{
		CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
		CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;
		if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
		{
			bool flag = affectorAgent.MountAgent != null;
			bool isHorseCharge = flag && attackType == AgentAttackType.Collision;
			SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus, lastShotDifficulty, attackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.PracticeFight, flag, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damageAmount, affectedAgent.Health < 1f, isSiegeEngineHit: false, isHorseCharge, isSneakAttack);
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_aliveOpponentCount < 6 && _spawnedOpponentAgentCount < 30 && (_aliveOpponentCount == 2 || _nextSpawnTime < base.Mission.CurrentTime))
		{
			Team team = SelectRandomAiTeam();
			Agent item = SpawnArenaAgent(team, GetSpawnFrame(considerPlayerDistance: true, isInitialSpawn: false));
			_participantAgents.Add(item);
			_nextSpawnTime = base.Mission.CurrentTime + 14f - (float)_spawnedOpponentAgentCount / 3f;
			if (_spawnedOpponentAgentCount == 30 && !IsPlayerPracticing)
			{
				_spawnedOpponentAgentCount = 0;
			}
		}
		if (_teleportTimer == null && IsPlayerPracticing && CheckPracticeEndedForPlayer())
		{
			_teleportTimer = new BasicMissionTimer();
			IsPlayerSurvived = base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive();
			if (IsPlayerSurvived)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=seyti8xR}Victory!"), 0, null, null, "event:/ui/mission/arena_victory");
			}
			AfterPractice = true;
		}
		if (_teleportTimer != null && _teleportTimer.ElapsedTime > (float)TeleportTime)
		{
			_teleportTimer = null;
			RemainingOpponentCountFromLastPractice = RemainingOpponentCount;
			IsPlayerPracticing = false;
			StartPractice();
			SpawnPlayerNearTournamentMaster();
			Agent agent = base.Mission.Agents.FirstOrDefault((Agent x) => x.Character != null && ((CharacterObject)x.Character).Occupation == Occupation.ArenaMaster);
			MissionConversationLogic.Current.StartConversation(agent, setActionsInstantly: true);
		}
	}

	private Team SelectRandomAiTeam()
	{
		Team team = null;
		foreach (Team aIParticipantTeam in _AIParticipantTeams)
		{
			if (!aIParticipantTeam.HasBots)
			{
				team = aIParticipantTeam;
				break;
			}
		}
		if (team == null)
		{
			team = _AIParticipantTeams[MBRandom.RandomInt(_AIParticipantTeams.Count - 1) + 1];
		}
		return team;
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectedAgent != null && affectedAgent.IsHuman)
		{
			if (affectedAgent != Agent.Main)
			{
				_aliveOpponentCount--;
			}
			if (affectorAgent != null && affectorAgent.IsHuman && affectorAgent == Agent.Main && affectedAgent != Agent.Main)
			{
				OpponentCountBeatenByPlayer++;
			}
		}
		if (_participantAgents.Contains(affectedAgent))
		{
			_participantAgents.Remove(affectedAgent);
		}
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		return false;
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (!IsPlayerPracticing)
		{
			return null;
		}
		return new InquiryData(new TextObject("{=zv49qE35}Practice Fight").ToString(), GameTexts.FindText("str_give_up_fight").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), base.Mission.OnEndMissionResult, null);
	}

	public void StartPlayerPractice()
	{
		IsPlayerPracticing = true;
		AfterPractice = false;
		StartPractice();
	}

	private void StartPractice()
	{
		InitializeParticipantCharacters();
		SandBoxHelpers.MissionHelper.FadeOutAgents(base.Mission.Agents.Where((Agent agent2) => _participantAgents.Contains(agent2) || agent2.IsMount || agent2.IsPlayerControlled), hideInstantly: true, hideMount: false);
		_spawnedOpponentAgentCount = 0;
		_aliveOpponentCount = 0;
		_participantAgents.Clear();
		Mission.Current.ClearCorpses(isMissionReset: false);
		base.Mission.RemoveSpawnedItemsAndMissiles();
		ArrangePlayerTeamEnmity();
		if (IsPlayerPracticing)
		{
			Agent agent = SpawnArenaAgent(base.Mission.PlayerTeam, GetSpawnFrame(considerPlayerDistance: false, isInitialSpawn: true));
			agent.WieldInitialWeapons();
			OpponentCountBeatenByPlayer = 0;
			_participantAgents.Add(agent);
		}
		int count = _AIParticipantTeams.Count;
		int num = 0;
		while (_spawnedOpponentAgentCount < 6)
		{
			_participantAgents.Add(SpawnArenaAgent(_AIParticipantTeams[num % count], GetSpawnFrame(considerPlayerDistance: false, isInitialSpawn: true)));
			num++;
		}
		_nextSpawnTime = base.Mission.CurrentTime + 14f;
	}

	private bool CheckPracticeEndedForPlayer()
	{
		if (base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive())
		{
			return RemainingOpponentCount == 0;
		}
		return true;
	}

	private void AddRandomWeapons(Equipment equipment, int spawnIndex)
	{
		int num = 1 + spawnIndex * 3 / 30;
		List<Equipment> list = (Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + num + "_" + _settlement.MapFaction.Culture.StringId) ?? Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + num + "_empire")).BattleEquipments.ToList();
		int index = MBRandom.RandomInt(list.Count);
		for (int i = 0; i <= 3; i++)
		{
			EquipmentElement equipmentFromSlot = list[index].GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item != null)
			{
				equipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
			}
		}
	}

	private void AddRandomClothes(CharacterObject troop, Equipment equipment)
	{
		Equipment participantArmor = Campaign.Current.Models.TournamentModel.GetParticipantArmor(troop);
		for (int i = 0; i < 12; i++)
		{
			if (i > 4 && i != 10 && i != 11)
			{
				EquipmentElement equipmentFromSlot = participantArmor.GetEquipmentFromSlot((EquipmentIndex)i);
				if (equipmentFromSlot.Item != null)
				{
					equipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
				}
			}
		}
	}

	private void InitializeTeams()
	{
		_AIParticipantTeams = new List<Team>();
		base.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2);
		base.Mission.PlayerTeam = base.Mission.DefenderTeam;
		_tournamentMasterTeam = base.Mission.Teams.Add(BattleSideEnum.None, _settlement.MapFaction.Color, _settlement.MapFaction.Color2);
		while (_AIParticipantTeams.Count < 6)
		{
			_AIParticipantTeams.Add(base.Mission.Teams.Add(BattleSideEnum.Attacker));
		}
		for (int i = 0; i < _AIParticipantTeams.Count; i++)
		{
			_AIParticipantTeams[i].SetIsEnemyOf(_tournamentMasterTeam, isEnemyOf: false);
			for (int j = i + 1; j < _AIParticipantTeams.Count; j++)
			{
				_AIParticipantTeams[i].SetIsEnemyOf(_AIParticipantTeams[j], isEnemyOf: true);
			}
		}
	}

	private void InitializeParticipantCharacters()
	{
		List<CharacterObject> participantCharacters = GetParticipantCharacters(_settlement);
		_participantCharacters = participantCharacters.OrderBy((CharacterObject x) => x.Level).ToList();
	}

	public static List<CharacterObject> GetParticipantCharacters(Settlement settlement)
	{
		int num = 30;
		List<CharacterObject> list = new List<CharacterObject>();
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (list.Count < num && settlement.Town.GarrisonParty != null)
		{
			foreach (TroopRosterElement item in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
			{
				int num5 = num - list.Count;
				if (!list.Contains(item.Character) && item.Character.Tier == 3 && (float)num5 * 0.4f > (float)num2)
				{
					list.Add(item.Character);
					num2++;
				}
				else if (!list.Contains(item.Character) && item.Character.Tier == 4 && (float)num5 * 0.4f > (float)num3)
				{
					list.Add(item.Character);
					num3++;
				}
				else if (!list.Contains(item.Character) && item.Character.Tier == 5 && (float)num5 * 0.2f > (float)num4)
				{
					list.Add(item.Character);
					num4++;
				}
				if (list.Count >= num)
				{
					break;
				}
			}
		}
		if (list.Count < num)
		{
			List<CharacterObject> list2 = new List<CharacterObject>();
			GetUpgradeTargets(((settlement != null) ? settlement.Culture : Game.Current.ObjectManager.GetObject<CultureObject>("empire")).BasicTroop, ref list2);
			int num6 = num - list.Count;
			foreach (CharacterObject item2 in list2)
			{
				if (!list.Contains(item2) && item2.Tier == 3 && (float)num6 * 0.4f > (float)num2)
				{
					list.Add(item2);
					num2++;
				}
				else if (!list.Contains(item2) && item2.Tier == 4 && (float)num6 * 0.4f > (float)num3)
				{
					list.Add(item2);
					num3++;
				}
				else if (!list.Contains(item2) && item2.Tier == 5 && (float)num6 * 0.2f > (float)num4)
				{
					list.Add(item2);
					num4++;
				}
				if (list.Count >= num)
				{
					break;
				}
			}
			while (list.Count < num)
			{
				for (int i = 0; i < list2.Count; i++)
				{
					if (list.Count >= num)
					{
						break;
					}
					list.Add(list2[i]);
				}
			}
		}
		return list;
	}

	private static void GetUpgradeTargets(CharacterObject troop, ref List<CharacterObject> list)
	{
		if (!list.Contains(troop) && troop.Tier >= 3)
		{
			list.Add(troop);
		}
		CharacterObject[] upgradeTargets = troop.UpgradeTargets;
		for (int i = 0; i < upgradeTargets.Length; i++)
		{
			GetUpgradeTargets(upgradeTargets[i], ref list);
		}
	}

	private void ArrangePlayerTeamEnmity()
	{
		foreach (Team aIParticipantTeam in _AIParticipantTeams)
		{
			aIParticipantTeam.SetIsEnemyOf(base.Mission.PlayerTeam, IsPlayerPracticing);
		}
	}

	private Team GetStrongestTeamExceptPlayerTeam()
	{
		Team result = null;
		int num = -1;
		foreach (Team aIParticipantTeam in _AIParticipantTeams)
		{
			int num2 = CalculateTeamPower(aIParticipantTeam);
			if (num2 > num)
			{
				result = aIParticipantTeam;
				num = num2;
			}
		}
		return result;
	}

	private int CalculateTeamPower(Team team)
	{
		int num = 0;
		foreach (Agent activeAgent in team.ActiveAgents)
		{
			num += activeAgent.Character.Level * activeAgent.KillCount + (int)MathF.Sqrt(activeAgent.Health);
		}
		return num;
	}

	private MatrixFrame GetSpawnFrame(bool considerPlayerDistance, bool isInitialSpawn)
	{
		List<MatrixFrame> list = ((isInitialSpawn || _spawnFrames.IsEmpty()) ? _initialSpawnFrames : _spawnFrames);
		if (list.Count == 1)
		{
			Debug.FailedAssert("Spawn point count is wrong! Arena practice spawn point set should be used in arena scenes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Arena\\ArenaPracticeFightMissionController.cs", "GetSpawnFrame", 616);
			return list[0];
		}
		MatrixFrame result;
		if (considerPlayerDistance && Agent.Main != null && Agent.Main.IsActive())
		{
			int num = MBRandom.RandomInt(list.Count);
			result = list[num];
			float num2 = float.MinValue;
			for (int i = num + 1; i < num + list.Count; i++)
			{
				MatrixFrame matrixFrame = list[i % list.Count];
				float num3 = CalculateLocationScore(matrixFrame);
				if (num3 >= 100f)
				{
					result = matrixFrame;
					break;
				}
				if (num3 > num2)
				{
					result = matrixFrame;
					num2 = num3;
				}
			}
		}
		else
		{
			int num4 = _spawnedOpponentAgentCount;
			if (IsPlayerPracticing && Agent.Main != null)
			{
				num4++;
			}
			result = list[num4 % list.Count];
		}
		return result;
	}

	private float CalculateLocationScore(MatrixFrame matrixFrame)
	{
		float num = 100f;
		float num2 = 0.25f;
		float num3 = 0.75f;
		if (matrixFrame.origin.DistanceSquared(Agent.Main.Position) < 144f)
		{
			num *= num2;
		}
		for (int i = 0; i < _participantAgents.Count; i++)
		{
			if (_participantAgents[i].Position.DistanceSquared(matrixFrame.origin) < 144f)
			{
				num *= num3;
			}
		}
		return num;
	}
}
