using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MissionBattleSideSpawnContext
{
	private readonly IBattleMissionAgentSpawnLogic _spawnLogic;

	private readonly BattleSideEnum _side;

	private readonly IMissionTroopSupplier _troopSupplier;

	private BannerBearerLogic _bannerBearerLogic;

	private readonly MBArrayList<Formation> _spawnedFormations;

	private bool _spawnWithHorses;

	private float _reinforcementBatchPriority;

	private int _reinforcementQuotaRequirement;

	private int _reinforcementBatchSize;

	private int _reinforcementsSpawnedInLastBatch;

	private int _numSpawnedTroops;

	private readonly List<IAgentOriginBase> _reservedTroops = new List<IAgentOriginBase>();

	private List<(Team team, List<IAgentOriginBase> origins)> _troopOriginsToSpawnPerTeam;

	private readonly (int currentTroopIndex, int troopCount)[] _reinforcementSpawnedUnitCountPerFormation;

	private readonly Dictionary<IAgentOriginBase, int> _reinforcementTroopFormationAssignments;

	public bool TroopSpawnActive { get; private set; }

	public bool IsPlayerSide { get; }

	public bool ReinforcementSpawnActive { get; private set; }

	public bool SpawnWithHorses => _spawnWithHorses;

	public bool ReinforcementsNotifiedOnLastBatch { get; private set; }

	public int NumberOfActiveTroops => _numSpawnedTroops - _troopSupplier.NumRemovedTroops;

	public int ReinforcementQuotaRequirement => _reinforcementQuotaRequirement;

	public int ReinforcementsSpawnedInLastBatch => _reinforcementsSpawnedInLastBatch;

	public float ReinforcementBatchSize => _reinforcementBatchSize;

	public bool HasReservedTroops => _reservedTroops.Count > 0;

	public bool HasSpawnableReinforcements
	{
		get
		{
			if (ReinforcementSpawnActive && HasReservedTroops)
			{
				return ReinforcementBatchSize > 0f;
			}
			return false;
		}
	}

	public bool ForceSpawnPlayerMounted { get; private set; }

	public float ReinforcementBatchPriority => _reinforcementBatchPriority;

	public int ReservedTroopsCount => _reservedTroops.Count;

	public int GetNumberOfPlayerControllableTroops()
	{
		return _troopSupplier.GetNumberOfPlayerControllableTroops();
	}

	public MissionBattleSideSpawnContext(IBattleMissionAgentSpawnLogic spawnLogic, BattleSideEnum side, IMissionTroopSupplier troopSupplier, bool isPlayerSide, bool forceSpawnPlayerMounted = true)
	{
		_spawnLogic = spawnLogic;
		_side = side;
		_spawnWithHorses = true;
		_spawnedFormations = new MBArrayList<Formation>();
		_troopSupplier = troopSupplier;
		_reinforcementQuotaRequirement = 0;
		_reinforcementBatchSize = 0;
		_reinforcementSpawnedUnitCountPerFormation = new(int, int)[8];
		_reinforcementTroopFormationAssignments = new Dictionary<IAgentOriginBase, int>();
		IsPlayerSide = isPlayerSide;
		ReinforcementsNotifiedOnLastBatch = false;
		ForceSpawnPlayerMounted = forceSpawnPlayerMounted;
	}

	public int TryReinforcementSpawn()
	{
		int num = 0;
		if (ReinforcementSpawnActive && TroopSpawnActive && _reservedTroops.Count > 0)
		{
			int num2 = DefaultBattleMissionAgentSpawnLogic.MaxNumberOfAgentsForMission - _spawnLogic.NumberOfAgents;
			int reservedTroopQuota = GetReservedTroopQuota(0);
			if (num2 >= reservedTroopQuota)
			{
				num = SpawnTroops(1, isReinforcement: true);
				if (num > 0)
				{
					_reinforcementQuotaRequirement -= reservedTroopQuota;
					if (_reservedTroops.Count >= _reinforcementBatchSize)
					{
						_reinforcementQuotaRequirement += GetReservedTroopQuota(_reinforcementBatchSize - 1);
					}
					_reinforcementBatchPriority /= 2f;
				}
			}
		}
		_reinforcementsSpawnedInLastBatch += num;
		return num;
	}

	public void GetTeamFormationsSpawnData(out MBList<(Team team, MissionFormationSpawnData[] formationSpawnData)> teamFormationsSpawnData)
	{
		Mission mission = Mission.Current;
		teamFormationsSpawnData = new MBList<(Team, MissionFormationSpawnData[])>();
		foreach (Team item2 in mission.Teams.Where((Team t) => t.Side == _side && t == mission.PlayerTeam).Concat(mission.Teams.Where((Team t) => t.Side == _side && t != mission.PlayerTeam)))
		{
			if (item2.Side == _side)
			{
				MissionFormationSpawnData[] array = new MissionFormationSpawnData[11];
				for (int num = 0; num < array.Length; num++)
				{
					array[num].FootTroopCount = 0;
					array[num].MountedTroopCount = 0;
				}
				teamFormationsSpawnData.Add((item2, array));
			}
		}
		bool isPlayerSide = _side == mission.PlayerTeam.Side;
		Dictionary<Team, List<IAgentOriginBase>> dictionary = new Dictionary<Team, List<IAgentOriginBase>>();
		foreach (IAgentOriginBase reservedTroop in _reservedTroops)
		{
			Team agentTeam = Mission.GetAgentTeam(reservedTroop, isPlayerSide);
			if (!dictionary.TryGetValue(agentTeam, out var value))
			{
				value = (dictionary[agentTeam] = new List<IAgentOriginBase>());
			}
			value.Add(reservedTroop);
		}
		foreach (KeyValuePair<Team, List<IAgentOriginBase>> item3 in dictionary)
		{
			Team troopTeam = item3.Key;
			List<IAgentOriginBase> value2 = item3.Value;
			MissionFormationSpawnData[] item = teamFormationsSpawnData.FirstOrDefault(((Team team, MissionFormationSpawnData[] formationSpawnData) tf) => tf.team == troopTeam).formationSpawnData;
			foreach (var (agentOriginBase, num2) in MissionGameModels.Current.BattleSpawnModel.GetInitialSpawnAssignments(_side, value2))
			{
				if (agentOriginBase.Troop.HasMount() && SpawnWithHorses)
				{
					item[num2].MountedTroopCount++;
				}
				else
				{
					item[num2].FootTroopCount++;
				}
			}
		}
	}

	public void ReserveTroops(int number)
	{
		if (number > 0 && _troopSupplier.AnyTroopRemainsToBeSupplied)
		{
			_reservedTroops.AddRange(_troopSupplier.SupplyTroops(number));
		}
	}

	public BasicCharacterObject GetGeneralCharacter()
	{
		return _troopSupplier.GetGeneralCharacter();
	}

	public bool CheckReinforcementBatch()
	{
		MissionSpawnPhase missionSpawnPhase = null;
		missionSpawnPhase = ((_side != BattleSideEnum.Defender) ? _spawnLogic.AttackerActivePhase : _spawnLogic.DefenderActivePhase);
		_reinforcementsSpawnedInLastBatch = 0;
		ReinforcementsNotifiedOnLastBatch = false;
		int val = 0;
		MissionSpawnSettings spawnSettings = _spawnLogic.SpawnSettings;
		switch (spawnSettings.ReinforcementTroopsSpawnMethod)
		{
		case MissionSpawnSettings.ReinforcementSpawnMethod.Balanced:
			val = ComputeBalancedBatch(missionSpawnPhase);
			break;
		case MissionSpawnSettings.ReinforcementSpawnMethod.Wave:
			val = ComputeWaveBatch(missionSpawnPhase);
			break;
		case MissionSpawnSettings.ReinforcementSpawnMethod.Fixed:
			val = ComputeFixedBatch(missionSpawnPhase);
			break;
		}
		val = Math.Min(val, missionSpawnPhase.RemainingSpawnNumber);
		val -= _reservedTroops.Count;
		if (val > 0)
		{
			int count = _reservedTroops.Count;
			ReserveTroops(val);
			if (count < _reinforcementBatchSize)
			{
				int num = Math.Min(_reservedTroops.Count, _reinforcementBatchSize);
				for (int i = count; i < num; i++)
				{
					_reinforcementQuotaRequirement += GetReservedTroopQuota(i);
				}
			}
		}
		_reinforcementBatchPriority = _reservedTroops.Count;
		bool flag = false;
		flag = ((spawnSettings.ReinforcementTroopsSpawnMethod != MissionSpawnSettings.ReinforcementSpawnMethod.Wave) ? (_reservedTroops.Count > 0 && (_reservedTroops.Count >= _reinforcementBatchSize || missionSpawnPhase.RemainingSpawnNumber <= _reinforcementBatchSize)) : (_reservedTroops.Count > 0));
		ReinforcementSpawnActive = flag;
		if (ReinforcementSpawnActive)
		{
			ResetReinforcementSpawnedUnitCountsPerFormation();
			foreach (Team team in Mission.Current.Teams)
			{
				if (team.Side == _side)
				{
					_spawnLogic.DeploymentPlan.UpdateReinforcementPlan(team);
				}
			}
		}
		return ReinforcementSpawnActive;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroops()
	{
		return _troopSupplier.GetAllTroops();
	}

	public int SpawnTroops(int number, bool isReinforcement)
	{
		if (number <= 0)
		{
			return 0;
		}
		List<IAgentOriginBase> list = new List<IAgentOriginBase>();
		int num = TaleWorlds.Library.MathF.Min(_reservedTroops.Count, number);
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				IAgentOriginBase item = _reservedTroops[i];
				list.Add(item);
			}
			_reservedTroops.RemoveRange(0, num);
		}
		int numberToAllocate = number - num;
		list.AddRange(_troopSupplier.SupplyTroops(numberToAllocate));
		Mission current = Mission.Current;
		if (_troopOriginsToSpawnPerTeam == null)
		{
			_troopOriginsToSpawnPerTeam = new List<(Team, List<IAgentOriginBase>)>();
			foreach (Team team in current.Teams)
			{
				bool flag = team.Side == current.PlayerTeam.Side;
				if ((IsPlayerSide && flag) || (!IsPlayerSide && !flag))
				{
					_troopOriginsToSpawnPerTeam.Add((team, new List<IAgentOriginBase>()));
				}
			}
		}
		else
		{
			foreach (var item2 in _troopOriginsToSpawnPerTeam)
			{
				item2.origins.Clear();
			}
		}
		int num2 = 0;
		foreach (IAgentOriginBase item3 in list)
		{
			Team agentTeam = Mission.GetAgentTeam(item3, IsPlayerSide);
			foreach (var item4 in _troopOriginsToSpawnPerTeam)
			{
				if (agentTeam == item4.team)
				{
					num2++;
					item4.origins.Add(item3);
				}
			}
		}
		int num3 = 0;
		List<IAgentOriginBase> list2 = new List<IAgentOriginBase>();
		foreach (var item5 in _troopOriginsToSpawnPerTeam)
		{
			if (item5.origins.IsEmpty())
			{
				continue;
			}
			int num4 = 0;
			List<(IAgentOriginBase, int)> list3 = null;
			if (isReinforcement)
			{
				list3 = new List<(IAgentOriginBase, int)>();
				foreach (IAgentOriginBase item6 in item5.origins)
				{
					_reinforcementTroopFormationAssignments.TryGetValue(item6, out var value);
					list3.Add((item6, value));
				}
			}
			else
			{
				list3 = MissionGameModels.Current.BattleSpawnModel.GetInitialSpawnAssignments(_side, item5.origins);
			}
			for (int num5 = 7; num5 >= 0; num5--)
			{
				int num6 = 0;
				int num7 = 0;
				list2.Clear();
				IAgentOriginBase agentOriginBase = null;
				foreach (var (agentOriginBase2, num8) in list3)
				{
					if (num5 != num8)
					{
						continue;
					}
					if (agentOriginBase2.Troop == Game.Current.PlayerTroop)
					{
						agentOriginBase = agentOriginBase2;
						continue;
					}
					if (agentOriginBase2.Troop.HasMount())
					{
						num6++;
					}
					else
					{
						num7++;
					}
					list2.Add(agentOriginBase2);
				}
				if (agentOriginBase != null)
				{
					if (agentOriginBase.Troop.HasMount())
					{
						num6++;
					}
					else
					{
						num7++;
					}
					list2.Add(agentOriginBase);
				}
				int count = list2.Count;
				if (count > 0)
				{
					bool isMounted = _spawnWithHorses && DefaultMissionDeploymentPlan.HasSignificantMountedTroops(num7, num6);
					int num9 = 0;
					int num10 = count;
					if (ReinforcementSpawnActive)
					{
						num9 = _reinforcementSpawnedUnitCountPerFormation[num5].currentTroopIndex;
						num10 = _reinforcementSpawnedUnitCountPerFormation[num5].troopCount;
					}
					Formation formation = item5.team.GetFormation((FormationClass)num5);
					if (!formation.HasBeenPositioned)
					{
						formation.BeginSpawn(num10, isMounted);
						current.SetFormationPositioningFromDeploymentPlan(formation, isReinforcement);
						_spawnedFormations.Add(formation);
					}
					foreach (IAgentOriginBase item7 in list2)
					{
						if (!item7.Troop.IsHero && _bannerBearerLogic != null && current.Mode != MissionMode.Deployment && _bannerBearerLogic.GetMissingBannerCount(formation) > 0)
						{
							_bannerBearerLogic.SpawnBannerBearer(item7, IsPlayerSide, formation, _spawnWithHorses, isReinforcement, num10, num9, isAlarmed: true, wieldInitialWeapons: true, null, null, null, current.IsSallyOutBattle);
						}
						else
						{
							bool spawnWithHorse = (item7.Troop.IsPlayerCharacter && ForceSpawnPlayerMounted) || _spawnWithHorses;
							current.SpawnTroop(item7, IsPlayerSide, hasFormation: true, spawnWithHorse, isReinforcement, num10, num9, isAlarmed: true, wieldInitialWeapons: true, null, null, null, null, formation.FormationIndex, current.IsSallyOutBattle);
						}
						_numSpawnedTroops++;
						num9++;
						num4++;
					}
					if (ReinforcementSpawnActive)
					{
						_reinforcementSpawnedUnitCountPerFormation[num5].currentTroopIndex = num9;
					}
				}
			}
			if (num4 > 0)
			{
				item5.team.QuerySystem.Expire();
			}
			num3 += num4;
			foreach (Formation item8 in item5.team.FormationsIncludingEmpty)
			{
				if (item8.CountOfUnits > 0 && item8.IsSpawning)
				{
					item8.EndSpawn();
				}
			}
		}
		return num3;
	}

	public void SetSpawnWithHorses(bool spawnWithHorses)
	{
		_spawnWithHorses = spawnWithHorses;
	}

	private int ComputeBalancedBatch(MissionSpawnPhase activePhase)
	{
		int result = 0;
		if (activePhase != null && activePhase.RemainingSpawnNumber > 0)
		{
			MissionSpawnSettings spawnSettings = _spawnLogic.SpawnSettings;
			int reinforcementBatchSize = _reinforcementBatchSize;
			_reinforcementBatchSize = (int)((float)_spawnLogic.BattleSize * spawnSettings.ReinforcementBatchPercentage);
			if (reinforcementBatchSize != _reinforcementBatchSize)
			{
				UpdateReinforcementQuotaRequirement(reinforcementBatchSize);
			}
			int num = activePhase.TotalSpawnNumber - activePhase.InitialSpawnedNumber;
			result = TaleWorlds.Library.MathF.Max(1, _reservedTroops.Count + (int)((float)num * spawnSettings.DesiredReinforcementPercentage));
			result = TaleWorlds.Library.MathF.Min(result, activePhase.InitialSpawnedNumber - NumberOfActiveTroops);
		}
		return result;
	}

	private int ComputeFixedBatch(MissionSpawnPhase activePhase)
	{
		int result = 0;
		if (activePhase != null && activePhase.RemainingSpawnNumber > 0)
		{
			MissionSpawnSettings spawnSettings = _spawnLogic.SpawnSettings;
			float num = ((_side == BattleSideEnum.Defender) ? spawnSettings.DefenderReinforcementBatchPercentage : spawnSettings.AttackerReinforcementBatchPercentage);
			int reinforcementBatchSize = _reinforcementBatchSize;
			_reinforcementBatchSize = (int)((float)_spawnLogic.TotalSpawnNumber * num);
			if (reinforcementBatchSize != _reinforcementBatchSize)
			{
				UpdateReinforcementQuotaRequirement(reinforcementBatchSize);
			}
			result = TaleWorlds.Library.MathF.Max(1, _reinforcementBatchSize);
		}
		return result;
	}

	private int ComputeWaveBatch(MissionSpawnPhase activePhase)
	{
		int result = 0;
		if (activePhase != null && activePhase.RemainingSpawnNumber > 0 && _reservedTroops.IsEmpty())
		{
			MissionSpawnSettings spawnSettings = _spawnLogic.SpawnSettings;
			int reinforcementBatchSize = _reinforcementBatchSize;
			int num = (_reinforcementBatchSize = (int)Math.Max(1f, (float)activePhase.InitialSpawnedNumber * spawnSettings.ReinforcementWavePercentage));
			if (reinforcementBatchSize != _reinforcementBatchSize)
			{
				UpdateReinforcementQuotaRequirement(reinforcementBatchSize);
			}
			if (activePhase.InitialSpawnedNumber - activePhase.NumberActiveTroops >= num)
			{
				result = num;
			}
		}
		return result;
	}

	public void SetBannerBearerLogic(BannerBearerLogic bannerBearerLogic)
	{
		_bannerBearerLogic = bannerBearerLogic;
	}

	private void UpdateReinforcementQuotaRequirement(int previousBatchSize)
	{
		if (_reinforcementBatchSize < previousBatchSize)
		{
			for (int num = TaleWorlds.Library.MathF.Min(_reservedTroops.Count - 1, previousBatchSize - 1); num >= _reinforcementBatchSize; num--)
			{
				_reinforcementQuotaRequirement -= GetReservedTroopQuota(num);
			}
		}
		else if (_reinforcementBatchSize > previousBatchSize)
		{
			int num2 = TaleWorlds.Library.MathF.Min(_reservedTroops.Count - 1, _reinforcementBatchSize - 1);
			for (int i = previousBatchSize; i <= num2; i++)
			{
				_reinforcementQuotaRequirement += GetReservedTroopQuota(i);
			}
		}
	}

	public void SetReinforcementsNotifiedOnLastBatch(bool value)
	{
		ReinforcementsNotifiedOnLastBatch = value;
	}

	private void ResetReinforcementSpawnedUnitCountsPerFormation()
	{
		for (int i = 0; i < 8; i++)
		{
			_reinforcementSpawnedUnitCountPerFormation[i].currentTroopIndex = 0;
			_reinforcementSpawnedUnitCountPerFormation[i].troopCount = 0;
		}
		_reinforcementTroopFormationAssignments.Clear();
		foreach (var reinforcementAssignment in MissionGameModels.Current.BattleSpawnModel.GetReinforcementAssignments(_side, _reservedTroops))
		{
			int item = reinforcementAssignment.formationIndex;
			_reinforcementTroopFormationAssignments.Add(reinforcementAssignment.origin, reinforcementAssignment.formationIndex);
			_reinforcementSpawnedUnitCountPerFormation[item].troopCount++;
		}
	}

	public void SetSpawnTroops(bool spawnTroops)
	{
		TroopSpawnActive = spawnTroops;
	}

	private int GetReservedTroopQuota(int index)
	{
		if (!_spawnWithHorses || !_reservedTroops[index].Troop.IsMounted)
		{
			return 1;
		}
		return 2;
	}

	public void OnInitialSpawnOver()
	{
		foreach (Formation spawnedFormation in _spawnedFormations)
		{
			spawnedFormation.EndSpawn();
		}
	}
}
