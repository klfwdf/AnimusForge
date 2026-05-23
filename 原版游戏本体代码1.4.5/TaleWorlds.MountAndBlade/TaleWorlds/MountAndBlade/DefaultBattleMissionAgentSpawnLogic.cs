using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefaultBattleMissionAgentSpawnLogic : MissionLogic, IBattleMissionAgentSpawnLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	public delegate void OnPhaseChangedDelegate();

	private const float InterTeamDeploymentGap = 20f;

	private static int _maxNumberOfAgentsForMissionCache;

	private readonly OnPhaseChangedDelegate[] _onPhaseChanged = new OnPhaseChangedDelegate[2];

	private readonly List<MissionSpawnPhase>[] _phases;

	private readonly int[] _numberOfTroopsInTotal;

	private readonly int _battleSize;

	private readonly MissionBattleSideSpawnContext _playerBattleSideSpawnContext;

	private readonly BasicMissionTimer _globalReinforcementSpawnTimer;

	private bool _reinforcementSpawnEnabled = true;

	private bool _spawningReinforcements;

	private ICustomReinforcementSpawnTimer _customReinforcementSpawnTimer;

	private float _globalReinforcementInterval;

	private MissionSpawnSettings _spawnSettings;

	private readonly MissionBattleSideSpawnContext[] _battleSideSpawnContexts;

	private BannerBearerLogic _bannerBearerLogic;

	private DefaultMissionDeploymentPlan _deploymentPlan;

	private List<BattleSideEnum> _sidesWhereSpawnOccured = new List<BattleSideEnum>();

	public static int MaxNumberOfAgentsForMission
	{
		get
		{
			if (_maxNumberOfAgentsForMissionCache == 0)
			{
				_maxNumberOfAgentsForMissionCache = MBAPI.IMBAgent.GetMaximumNumberOfAgents();
			}
			return _maxNumberOfAgentsForMissionCache;
		}
	}

	public static int MaxNumberOfTroopsForMission => MaxNumberOfAgentsForMission / 2;

	public int NumberOfRemainingTroops => (DefenderActivePhase?.RemainingSpawnNumber ?? 0) + (AttackerActivePhase?.RemainingSpawnNumber ?? 0);

	public int NumberOfActiveDefenderTroops => DefenderActivePhase?.NumberActiveTroops ?? 0;

	public int NumberOfActiveAttackerTroops => AttackerActivePhase?.NumberActiveTroops ?? 0;

	public int NumberOfRemainingDefenderTroops => DefenderActivePhase?.RemainingSpawnNumber ?? 0;

	public int NumberOfRemainingAttackerTroops => AttackerActivePhase?.RemainingSpawnNumber ?? 0;

	public BattleSideEnum PlayerSide { get; private set; }

	public int TotalSpawnNumber => (DefenderActivePhase?.TotalSpawnNumber ?? 0) + (AttackerActivePhase?.TotalSpawnNumber ?? 0);

	public int BattleSize => _battleSize;

	public int NumberOfAgents => base.Mission.AllAgents.Count;

	public MissionSpawnPhase DefenderActivePhase => _phases[0].FirstOrDefault();

	public MissionSpawnPhase AttackerActivePhase => _phases[1].FirstOrDefault();

	public ref readonly MissionSpawnSettings SpawnSettings => ref _spawnSettings;

	public IMissionDeploymentPlan DeploymentPlan => _deploymentPlan;

	public bool IsInitialSpawnOver => DefenderActivePhase.InitialSpawnNumber + AttackerActivePhase.InitialSpawnNumber == 0;

	public bool IsDeploymentOver
	{
		get
		{
			if (base.Mission.Mode != MissionMode.Deployment)
			{
				return IsInitialSpawnOver;
			}
			return false;
		}
	}

	public event Action<BattleSideEnum, int> OnReinforcementsSpawned;

	public event Action<BattleSideEnum, int> OnInitialTroopsSpawned;

	public DefaultBattleMissionAgentSpawnLogic(IMissionTroopSupplier[] suppliers, BattleSideEnum playerSide, Mission.BattleSizeType battleSizeType)
	{
		PlayerSide = playerSide;
		switch (battleSizeType)
		{
		case Mission.BattleSizeType.Battle:
			_battleSize = BannerlordConfig.GetRealBattleSize();
			break;
		case Mission.BattleSizeType.Siege:
			_battleSize = BannerlordConfig.GetRealBattleSizeForSiege();
			break;
		case Mission.BattleSizeType.SallyOut:
			_battleSize = BannerlordConfig.GetRealBattleSizeForSallyOut();
			break;
		}
		_battleSize = TaleWorlds.Library.MathF.Min(_battleSize, MaxNumberOfTroopsForMission);
		_globalReinforcementSpawnTimer = new BasicMissionTimer();
		_spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
		_globalReinforcementInterval = _spawnSettings.GlobalReinforcementInterval;
		_battleSideSpawnContexts = new MissionBattleSideSpawnContext[2];
		for (int i = 0; i < 2; i++)
		{
			IMissionTroopSupplier troopSupplier = suppliers[i];
			bool flag = i == (int)playerSide;
			MissionBattleSideSpawnContext missionBattleSideSpawnContext = new MissionBattleSideSpawnContext(this, (BattleSideEnum)i, troopSupplier, flag);
			if (flag)
			{
				_playerBattleSideSpawnContext = missionBattleSideSpawnContext;
			}
			_battleSideSpawnContexts[i] = missionBattleSideSpawnContext;
		}
		_numberOfTroopsInTotal = new int[2];
		_phases = new List<MissionSpawnPhase>[2];
		for (int j = 0; j < 2; j++)
		{
			_phases[j] = new List<MissionSpawnPhase>();
		}
		_reinforcementSpawnEnabled = false;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MissionGameModels.Current.BattleInitializationModel.InitializeModel();
	}

	protected override void OnEndMission()
	{
		MissionGameModels.Current.BattleSpawnModel.OnMissionEnd();
		MissionGameModels.Current.BattleInitializationModel.FinalizeModel();
	}

	public override void AfterStart()
	{
		_bannerBearerLogic = base.Mission.GetMissionBehavior<BannerBearerLogic>();
		if (_bannerBearerLogic != null)
		{
			for (int i = 0; i < 2; i++)
			{
				_battleSideSpawnContexts[i].SetBannerBearerLogic(_bannerBearerLogic);
			}
		}
		MissionGameModels.Current.BattleSpawnModel.OnMissionStart();
	}

	public override void OnMissionTick(float dt)
	{
		if (!GameNetwork.IsClient && !CheckDeployment())
		{
			return;
		}
		PhaseTick();
		if (_reinforcementSpawnEnabled)
		{
			if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer)
			{
				CheckGlobalReinforcementBatch();
			}
			else if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer)
			{
				CheckCustomReinforcementBatch();
			}
		}
		if (_spawningReinforcements)
		{
			CheckReinforcementSpawn();
		}
	}

	public void InitWithSinglePhase(int defenderTotalSpawn, int attackerTotalSpawn, int defenderInitialSpawn, int attackerInitialSpawn, bool spawnDefenders, bool spawnAttackers, in MissionSpawnSettings spawnSettings)
	{
		AddPhase(BattleSideEnum.Defender, defenderTotalSpawn, defenderInitialSpawn);
		AddPhase(BattleSideEnum.Attacker, attackerTotalSpawn, attackerInitialSpawn);
		Init(spawnDefenders, spawnAttackers, in spawnSettings);
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		return _battleSideSpawnContexts[(int)side].GetAllTroops();
	}

	public void SetCustomReinforcementSpawnTimer(ICustomReinforcementSpawnTimer timer)
	{
		_customReinforcementSpawnTimer = timer;
	}

	public void SetSpawnTroops(BattleSideEnum side, bool spawnTroops, bool enforceSpawning = false)
	{
		_battleSideSpawnContexts[(int)side].SetSpawnTroops(spawnTroops);
		if (spawnTroops && enforceSpawning)
		{
			CheckDeployment();
		}
	}

	public void SetSpawnHorses(BattleSideEnum side, bool spawnHorses)
	{
		_battleSideSpawnContexts[(int)side].SetSpawnWithHorses(spawnHorses);
	}

	public void StartSpawner(BattleSideEnum side)
	{
		_battleSideSpawnContexts[(int)side].SetSpawnTroops(spawnTroops: true);
	}

	public void StopSpawner(BattleSideEnum side)
	{
		_battleSideSpawnContexts[(int)side].SetSpawnTroops(spawnTroops: false);
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return _battleSideSpawnContexts[(int)side].TroopSpawnActive;
	}

	public void OnSideDeploymentOver(BattleSideEnum battleSide)
	{
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side == battleSide)
			{
				base.Mission.OnTeamDeployed(team);
			}
		}
		base.Mission.OnBattleSideDeployed(battleSide);
		foreach (Team team2 in base.Mission.Teams)
		{
			if (team2.Side != battleSide)
			{
				continue;
			}
			foreach (Formation item in team2.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					item.QuerySystem.EvaluateAllPreliminaryQueryData();
				}
			}
		}
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		return _playerBattleSideSpawnContext?.GetNumberOfPlayerControllableTroops() ?? 0;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return _globalReinforcementInterval;
	}

	public void SetReinforcementsSpawnEnabled(bool value, bool resetTimers = true)
	{
		if (_reinforcementSpawnEnabled == value)
		{
			return;
		}
		_reinforcementSpawnEnabled = value;
		if (!resetTimers)
		{
			return;
		}
		if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer)
		{
			_globalReinforcementSpawnTimer.Reset();
		}
		else if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer)
		{
			for (int i = 0; i < 2; i++)
			{
				_customReinforcementSpawnTimer.ResetTimer((BattleSideEnum)i);
			}
		}
	}

	public int GetTotalNumberOfTroopsForSide(BattleSideEnum side)
	{
		return _numberOfTroopsInTotal[(int)side];
	}

	public BasicCharacterObject GetGeneralCharacterOfSide(BattleSideEnum side)
	{
		if (side >= BattleSideEnum.Defender && side < BattleSideEnum.NumSides)
		{
			_battleSideSpawnContexts[(int)side].GetGeneralCharacter();
		}
		return null;
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return _battleSideSpawnContexts[(int)side].SpawnWithHorses;
	}

	private bool CheckMinimumBatchQuotaRequirement()
	{
		int num = MaxNumberOfAgentsForMission - NumberOfAgents;
		int num2 = 0;
		for (int i = 0; i < 2; i++)
		{
			num2 += _battleSideSpawnContexts[i].ReinforcementQuotaRequirement;
		}
		return num >= num2;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		if (_phases[(int)side].Count == 1 && _battleSideSpawnContexts[(int)side].NumberOfActiveTroops == 0)
		{
			return GetActivePhaseForSide(side).RemainingSpawnNumber == 0;
		}
		return false;
	}

	public void AddPhaseChangeAction(BattleSideEnum side, OnPhaseChangedDelegate onPhaseChanged)
	{
		ref OnPhaseChangedDelegate reference = ref _onPhaseChanged[(int)side];
		reference = (OnPhaseChangedDelegate)Delegate.Combine(reference, onPhaseChanged);
	}

	private void CheckGlobalReinforcementBatch()
	{
		if (_globalReinforcementSpawnTimer.ElapsedTime >= _globalReinforcementInterval)
		{
			bool flag = false;
			for (int i = 0; i < 2; i++)
			{
				BattleSideEnum battleSide = (BattleSideEnum)i;
				NotifyReinforcementTroopsSpawned(battleSide);
				bool flag2 = _battleSideSpawnContexts[i].CheckReinforcementBatch();
				flag = flag || flag2;
			}
			_spawningReinforcements = flag && CheckMinimumBatchQuotaRequirement();
			_globalReinforcementSpawnTimer.Reset();
		}
	}

	private void CheckCustomReinforcementBatch()
	{
		bool flag = false;
		for (int i = 0; i < 2; i++)
		{
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			if (_customReinforcementSpawnTimer.Check(battleSideEnum))
			{
				flag = true;
				NotifyReinforcementTroopsSpawned(battleSideEnum);
				_battleSideSpawnContexts[i].CheckReinforcementBatch();
			}
		}
		if (flag)
		{
			bool flag2 = false;
			for (int j = 0; j < 2; j++)
			{
				flag2 = flag2 || _battleSideSpawnContexts[j].ReinforcementSpawnActive;
			}
			_spawningReinforcements = flag2 && CheckMinimumBatchQuotaRequirement();
		}
	}

	private void Init(bool spawnDefenders, bool spawnAttackers, in MissionSpawnSettings reinforcementSpawnSettings)
	{
		base.Mission.GetDeploymentPlan<DefaultMissionDeploymentPlan>(out _deploymentPlan);
		List<MissionSpawnPhase>[] phases = _phases;
		for (int i = 0; i < phases.Length; i++)
		{
			if (phases[i].Count <= 0)
			{
				return;
			}
		}
		foreach (Team team in base.Mission.Teams)
		{
			BattleSideEnum side = team.Side;
			bool spawnWithHorses = _battleSideSpawnContexts[(int)side].SpawnWithHorses;
			_deploymentPlan.SetSpawnWithHorses(team, spawnWithHorses);
		}
		_spawnSettings = reinforcementSpawnSettings;
		int num = 0;
		int num2 = 1;
		_globalReinforcementInterval = _spawnSettings.GlobalReinforcementInterval;
		int[] array = new int[2]
		{
			_phases[num].Sum((MissionSpawnPhase p) => p.TotalSpawnNumber),
			_phases[num2].Sum((MissionSpawnPhase p) => p.TotalSpawnNumber)
		};
		int num3 = array.Sum();
		if (_spawnSettings.InitialTroopsSpawnMethod == MissionSpawnSettings.InitialSpawnMethod.BattleSizeAllocating)
		{
			float[] array2 = new float[2]
			{
				(float)array[num] / (float)num3,
				(float)array[num2] / (float)num3
			};
			array2[num] = TaleWorlds.Library.MathF.Min(_spawnSettings.MaximumBattleSideRatio, array2[num] * _spawnSettings.DefenderAdvantageFactor);
			array2[num2] = 1f - array2[num];
			bool num4 = !(array2[num] < array2[num2]);
			int oppositeSide = (int)(num4 ? BattleSideEnum.Attacker : BattleSideEnum.Defender).GetOppositeSide();
			int num5 = (num4 ? 1 : 0);
			if (array2[oppositeSide] > _spawnSettings.MaximumBattleSideRatio)
			{
				array2[oppositeSide] = _spawnSettings.MaximumBattleSideRatio;
				array2[num5] = 1f - _spawnSettings.MaximumBattleSideRatio;
			}
			int[] array3 = new int[2];
			int val = TaleWorlds.Library.MathF.Ceiling(array2[num5] * (float)_battleSize);
			array3[num5] = Math.Min(val, array[num5]);
			array3[oppositeSide] = _battleSize - array3[num5];
			for (int num6 = 0; num6 < 2; num6++)
			{
				foreach (MissionSpawnPhase item in _phases[num6])
				{
					if (item.InitialSpawnNumber > array3[num6])
					{
						int num7 = array3[num6];
						int num8 = item.InitialSpawnNumber - num7;
						item.InitialSpawnNumber = num7;
						item.RemainingSpawnNumber += num8;
					}
				}
			}
		}
		else if (_spawnSettings.InitialTroopsSpawnMethod == MissionSpawnSettings.InitialSpawnMethod.FreeAllocation)
		{
			_phases[num].Max((MissionSpawnPhase p) => p.InitialSpawnNumber);
			_phases[num2].Max((MissionSpawnPhase p) => p.InitialSpawnNumber);
		}
		if (_spawnSettings.ReinforcementTroopsSpawnMethod == MissionSpawnSettings.ReinforcementSpawnMethod.Wave)
		{
			for (int num9 = 0; num9 < 2; num9++)
			{
				foreach (MissionSpawnPhase item2 in _phases[num9])
				{
					int num10 = (int)Math.Max(1f, (float)item2.InitialSpawnNumber * _spawnSettings.ReinforcementWavePercentage);
					if (_spawnSettings.MaximumReinforcementWaveCount > 0)
					{
						int num11 = Math.Min(item2.RemainingSpawnNumber, num10 * _spawnSettings.MaximumReinforcementWaveCount);
						int num12 = Math.Max(0, item2.RemainingSpawnNumber - num11);
						_numberOfTroopsInTotal[num9] -= num12;
						array[num9] -= num12;
						item2.RemainingSpawnNumber = num11;
						item2.TotalSpawnNumber = item2.RemainingSpawnNumber + item2.InitialSpawnNumber;
					}
				}
			}
		}
		base.Mission.SetBattleAgentCount(TaleWorlds.Library.MathF.Min(DefenderActivePhase.InitialSpawnNumber, AttackerActivePhase.InitialSpawnNumber));
		base.Mission.SetInitialAgentCountForSide(BattleSideEnum.Defender, array[num]);
		base.Mission.SetInitialAgentCountForSide(BattleSideEnum.Attacker, array[num2]);
		_battleSideSpawnContexts[num].SetSpawnTroops(spawnDefenders);
		_battleSideSpawnContexts[num2].SetSpawnTroops(spawnAttackers);
	}

	private void AddPhase(BattleSideEnum side, int totalSpawn, int initialSpawn)
	{
		MissionSpawnPhase item = new MissionSpawnPhase
		{
			TotalSpawnNumber = totalSpawn,
			InitialSpawnNumber = initialSpawn,
			RemainingSpawnNumber = totalSpawn - initialSpawn
		};
		_phases[(int)side].Add(item);
		_numberOfTroopsInTotal[(int)side] += totalSpawn;
	}

	private void PhaseTick()
	{
		for (int i = 0; i < 2; i++)
		{
			MissionSpawnPhase activePhaseForSide = GetActivePhaseForSide((BattleSideEnum)i);
			activePhaseForSide.NumberActiveTroops = _battleSideSpawnContexts[i].NumberOfActiveTroops;
			if (activePhaseForSide.NumberActiveTroops != 0 || activePhaseForSide.RemainingSpawnNumber != 0 || _phases[i].Count <= 1)
			{
				continue;
			}
			_phases[i].Remove(activePhaseForSide);
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			if (GetActivePhaseForSide(battleSideEnum) == null)
			{
				continue;
			}
			if (_onPhaseChanged[i] != null)
			{
				_onPhaseChanged[i]();
			}
			foreach (Team team in base.Mission.Teams)
			{
				if (team.Side == battleSideEnum)
				{
					if (_deploymentPlan.IsPlanMade(team))
					{
						_deploymentPlan.ClearAddedTroops(team);
						_deploymentPlan.ClearDeploymentPlan(team);
					}
					if (_deploymentPlan.IsReinforcementPlanMade(team))
					{
						_deploymentPlan.ClearAddedTroops(team, isReinforcement: true);
						_deploymentPlan.ClearReinforcementPlan(team);
					}
				}
			}
			Debug.Print("New spawn phase!", 0, Debug.DebugColor.Green, 64uL);
		}
	}

	private bool CheckDeployment()
	{
		bool isDeploymentOver = IsDeploymentOver;
		if (!isDeploymentOver)
		{
			int troopCount = DefenderActivePhase.InitialSpawnNumber + AttackerActivePhase.InitialSpawnNumber;
			for (int i = 0; i < 2; i++)
			{
				BattleSideEnum battleSideEnum = (BattleSideEnum)i;
				MissionSpawnPhase activePhaseForSide = GetActivePhaseForSide(battleSideEnum);
				if (activePhaseForSide.InitialSpawnNumber <= 0)
				{
					continue;
				}
				if (activePhaseForSide.InitialSpawnNumber > _battleSideSpawnContexts[i].ReservedTroopsCount)
				{
					int number = activePhaseForSide.InitialSpawnNumber - _battleSideSpawnContexts[i].ReservedTroopsCount;
					_battleSideSpawnContexts[i].ReserveTroops(number);
				}
				if (_battleSideSpawnContexts[i].ReservedTroopsCount < activePhaseForSide.InitialSpawnNumber)
				{
					continue;
				}
				bool flag = true;
				int num = 0;
				foreach (Team team in base.Mission.Teams)
				{
					if (team.Side == battleSideEnum)
					{
						flag = flag && _deploymentPlan.IsPlanMade(team) && _deploymentPlan.IsReinforcementPlanMade(team);
						num++;
					}
				}
				if (num <= 0 || flag)
				{
					continue;
				}
				_battleSideSpawnContexts[i].GetTeamFormationsSpawnData(out MBList<(Team, MissionFormationSpawnData[])> teamFormationsSpawnData);
				if (base.Mission.HasSpawnPath)
				{
					MBReadOnlyList<(Team, int, MissionFormationSpawnData[])> mBReadOnlyList = CollectSortedBattleSideTeamsData(teamFormationsSpawnData);
					SpawnPathData initialSpawnPathData = Mission.Current.GetInitialSpawnPathData(battleSideEnum);
					_ = initialSpawnPathData.Path;
					float[] array = new float[mBReadOnlyList.Count];
					for (int j = 0; j < mBReadOnlyList.Count; j++)
					{
						array[j] = GetTeamSpawnPathOffsetRange(initialSpawnPathData, battleSideEnum, mBReadOnlyList[j].Item3);
					}
					Path path = initialSpawnPathData.Path;
					float baseDeploymentOffset = Mission.ComputeSpawnPathDeploymentOffset(troopCount, path);
					ComputeDeploymentBaseOffsets(initialSpawnPathData, baseDeploymentOffset, out var deployingSideBaseOffset, out var opposingSideBaseOffset);
					ComputeTeamDeploymentOffsets(initialSpawnPathData, deployingSideBaseOffset, 20f, array, out var teamDeployOffsets);
					for (int k = 0; k < mBReadOnlyList.Count; k++)
					{
						(Team, int, MissionFormationSpawnData[]) tuple = mBReadOnlyList[k];
						MakeTeamPlans(tuple.Item1, tuple.Item3, teamDeployOffsets[k], opposingSideBaseOffset);
					}
					continue;
				}
				foreach (var item in teamFormationsSpawnData)
				{
					MakeTeamPlans(item.Item1, item.Item2);
				}
			}
			for (int l = 0; l < 2; l++)
			{
				BattleSideEnum battleSideEnum2 = (BattleSideEnum)l;
				int initialSpawnNumber = GetActivePhaseForSide(battleSideEnum2).InitialSpawnNumber;
				if (!_battleSideSpawnContexts[l].TroopSpawnActive)
				{
					continue;
				}
				int reservedTroopsCount = _battleSideSpawnContexts[l].ReservedTroopsCount;
				if (reservedTroopsCount <= 0 || reservedTroopsCount < initialSpawnNumber)
				{
					continue;
				}
				bool flag2 = true;
				int num2 = 0;
				foreach (Team team2 in base.Mission.Teams)
				{
					if (team2.Side == battleSideEnum2)
					{
						flag2 = flag2 && _deploymentPlan.IsPlanMade(team2);
						num2++;
					}
				}
				if (num2 > 0 && flag2)
				{
					_battleSideSpawnContexts[l].SpawnTroops(initialSpawnNumber, isReinforcement: false);
					GetActivePhaseForSide(battleSideEnum2).OnInitialTroopsSpawned();
					_battleSideSpawnContexts[l].OnInitialSpawnOver();
					if (!_sidesWhereSpawnOccured.Contains(battleSideEnum2))
					{
						_sidesWhereSpawnOccured.Add(battleSideEnum2);
					}
					this.OnInitialTroopsSpawned?.Invoke(battleSideEnum2, initialSpawnNumber);
				}
			}
			isDeploymentOver = IsDeploymentOver;
			if (isDeploymentOver)
			{
				foreach (BattleSideEnum item2 in _sidesWhereSpawnOccured)
				{
					OnSideDeploymentOver(item2);
				}
			}
		}
		return isDeploymentOver;
	}

	private float GetTeamSpawnPathOffsetRange(SpawnPathData initialSpawnPath, BattleSideEnum battleSide, MissionFormationSpawnData[] teamFormationsSpawnData)
	{
		float centerFlanksDepth = 0f;
		float leftFlanksDepth = 0f;
		float rightFlanksDepth = 0f;
		float num = 3f;
		bool teamPlanHasAnyFootTroops = teamFormationsSpawnData.Sum((MissionFormationSpawnData data) => data.FootTroopCount) > 0;
		for (int num2 = 0; num2 < teamFormationsSpawnData.Count(); num2++)
		{
			MissionFormationSpawnData missionFormationSpawnData = teamFormationsSpawnData[num2];
			FormationClass formationClass = (FormationClass)num2;
			if (missionFormationSpawnData.NumTroops > 0)
			{
				bool flag = DefaultMissionDeploymentPlan.HasSignificantMountedTroops(missionFormationSpawnData.FootTroopCount, missionFormationSpawnData.MountedTroopCount);
				float item = DefaultDeploymentPlan.GetFormationSpawnWidthAndDepth(formationClass, missionFormationSpawnData.NumTroops, flag, !GetSpawnHorses(battleSide)).Item2;
				ref float flankDepthRef = ref GetFlankDepthRef(DefaultFormationDeploymentPlan.GetFormationDefaultFlankAux(formationClass, missionFormationSpawnData.NumTroops, teamPlanHasAnyFootTroops, flag, GetSpawnHorses(battleSide)), ref centerFlanksDepth, ref leftFlanksDepth, ref rightFlanksDepth);
				if (flankDepthRef > 0.0001f)
				{
					flankDepthRef += num;
				}
				flankDepthRef += item;
			}
			else if ((formationClass == FormationClass.NumberOfRegularFormations || formationClass == FormationClass.Bodyguard) && centerFlanksDepth > 0.0001f)
			{
				centerFlanksDepth += num;
			}
		}
		return TaleWorlds.Library.MathF.Max(centerFlanksDepth, TaleWorlds.Library.MathF.Max(leftFlanksDepth, rightFlanksDepth));
	}

	private void MakeTeamPlans(Team team, MissionFormationSpawnData[] formationsSpawnData, float spawnPathOffset = 0f, float targetOffset = 0f)
	{
		bool spawnHorses = GetSpawnHorses(team.Side);
		if (!_deploymentPlan.IsPlanMade(team))
		{
			for (int i = 0; i < formationsSpawnData.Length; i++)
			{
				if (formationsSpawnData[i].NumTroops > 0)
				{
					_deploymentPlan.AddTroops(team, (FormationClass)i, formationsSpawnData[i].FootTroopCount, formationsSpawnData[i].MountedTroopCount);
				}
			}
			_deploymentPlan.MakeDeploymentPlan(team, spawnPathOffset, targetOffset);
		}
		if (_deploymentPlan.IsReinforcementPlanMade(team))
		{
			return;
		}
		int num = 4;
		int num2 = Math.Max(_battleSize / (2 * num), 1);
		for (int j = 0; j < num; j++)
		{
			if (((FormationClass)j).IsMounted() && spawnHorses)
			{
				_deploymentPlan.AddTroops(team, (FormationClass)j, 0, num2, isReinforcement: true);
			}
			else
			{
				_deploymentPlan.AddTroops(team, (FormationClass)j, num2, 0, isReinforcement: true);
			}
		}
		_deploymentPlan.MakeReinforcementDeploymentPlan(team);
	}

	private void CheckReinforcementSpawn()
	{
		int num = 0;
		int num2 = 1;
		MissionBattleSideSpawnContext missionBattleSideSpawnContext = _battleSideSpawnContexts[num];
		MissionBattleSideSpawnContext missionBattleSideSpawnContext2 = _battleSideSpawnContexts[num2];
		bool flag = missionBattleSideSpawnContext.HasSpawnableReinforcements && ((float)missionBattleSideSpawnContext.ReinforcementsSpawnedInLastBatch < missionBattleSideSpawnContext.ReinforcementBatchSize || missionBattleSideSpawnContext.ReinforcementBatchPriority >= missionBattleSideSpawnContext2.ReinforcementBatchPriority);
		bool flag2 = missionBattleSideSpawnContext2.HasSpawnableReinforcements && ((float)missionBattleSideSpawnContext2.ReinforcementsSpawnedInLastBatch < missionBattleSideSpawnContext2.ReinforcementBatchSize || missionBattleSideSpawnContext2.ReinforcementBatchPriority >= missionBattleSideSpawnContext.ReinforcementBatchPriority);
		int num3 = 0;
		int num4 = 0;
		if (flag && flag2)
		{
			if (missionBattleSideSpawnContext.ReinforcementBatchPriority >= missionBattleSideSpawnContext2.ReinforcementBatchPriority)
			{
				num3 = missionBattleSideSpawnContext.TryReinforcementSpawn();
				DefenderActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
				num3 = missionBattleSideSpawnContext2.TryReinforcementSpawn();
				AttackerActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
			}
			else
			{
				num3 = missionBattleSideSpawnContext2.TryReinforcementSpawn();
				AttackerActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
				num3 = missionBattleSideSpawnContext.TryReinforcementSpawn();
				DefenderActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
			}
		}
		else if (flag)
		{
			num3 = missionBattleSideSpawnContext.TryReinforcementSpawn();
			DefenderActivePhase.RemainingSpawnNumber -= num3;
			num4 += num3;
		}
		else if (flag2)
		{
			num3 = missionBattleSideSpawnContext2.TryReinforcementSpawn();
			AttackerActivePhase.RemainingSpawnNumber -= num3;
			num4 += num3;
		}
		if (num4 > 0)
		{
			for (int i = 0; i < 2; i++)
			{
				NotifyReinforcementTroopsSpawned((BattleSideEnum)i, checkEmptyReserves: true);
			}
		}
	}

	private void NotifyReinforcementTroopsSpawned(BattleSideEnum battleSide, bool checkEmptyReserves = false)
	{
		MissionBattleSideSpawnContext missionBattleSideSpawnContext = _battleSideSpawnContexts[(int)battleSide];
		int reinforcementsSpawnedInLastBatch = missionBattleSideSpawnContext.ReinforcementsSpawnedInLastBatch;
		if (!missionBattleSideSpawnContext.ReinforcementsNotifiedOnLastBatch && reinforcementsSpawnedInLastBatch > 0 && (!checkEmptyReserves || (checkEmptyReserves && !missionBattleSideSpawnContext.HasReservedTroops)))
		{
			this.OnReinforcementsSpawned?.Invoke(battleSide, reinforcementsSpawnedInLastBatch);
			missionBattleSideSpawnContext.SetReinforcementsNotifiedOnLastBatch(value: true);
		}
	}

	private MissionSpawnPhase GetActivePhaseForSide(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Defender:
			return DefenderActivePhase;
		case BattleSideEnum.Attacker:
			return AttackerActivePhase;
		default:
			Debug.FailedAssert("Wrong Side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\DefaultBattleMissionAgentSpawnLogic.cs", "GetActivePhaseForSide", 958);
			return null;
		}
	}

	private MBReadOnlyList<(Team team, int deployingTroopCount, MissionFormationSpawnData[] formationSpawnData)> CollectSortedBattleSideTeamsData(MBList<(Team team, MissionFormationSpawnData[] formationSpawnData)> teamFormationsSpawnData)
	{
		MBList<(Team, int, MissionFormationSpawnData[])> mBList = new MBList<(Team, int, MissionFormationSpawnData[])>();
		foreach (var teamFormationsSpawnDatum in teamFormationsSpawnData)
		{
			int num = 0;
			MissionFormationSpawnData[] item = teamFormationsSpawnDatum.formationSpawnData;
			foreach (MissionFormationSpawnData missionFormationSpawnData in item)
			{
				num += missionFormationSpawnData.NumTroops;
			}
			if (num > 0)
			{
				mBList.Add((teamFormationsSpawnDatum.team, num, teamFormationsSpawnDatum.formationSpawnData));
			}
		}
		mBList.Sort(delegate((Team team, int deployingTroopCount, MissionFormationSpawnData[] formationSpawnData) t1, (Team team, int deployingTroopCount, MissionFormationSpawnData[] formationSpawnData) t2)
		{
			bool flag = t1.team == base.Mission.PlayerTeam || t1.team == base.Mission.PlayerEnemyTeam;
			bool flag2 = t2.team == base.Mission.PlayerTeam || t2.team == base.Mission.PlayerEnemyTeam;
			if (flag && flag2)
			{
				Debug.FailedAssert("There can be only one main team in a battle side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\DefaultBattleMissionAgentSpawnLogic.cs", "CollectSortedBattleSideTeamsData", 989);
				return 0;
			}
			if (!flag && !flag2)
			{
				if (t1.deployingTroopCount > t2.deployingTroopCount)
				{
					return -1;
				}
				if (t1.deployingTroopCount < t2.deployingTroopCount)
				{
					return 1;
				}
				return 0;
			}
			return flag ? 1 : (-1);
		});
		return mBList;
	}

	public static void ComputeDeploymentBaseOffsets(SpawnPathData sideSpawnPathData, float baseDeploymentOffset, out float deployingSideBaseOffset, out float opposingSideBaseOffset)
	{
		float num = 0f - sideSpawnPathData.PivotOffset;
		float num2 = sideSpawnPathData.PathLength - sideSpawnPathData.PivotOffset;
		if (2f * TaleWorlds.Library.MathF.Abs(baseDeploymentOffset) <= sideSpawnPathData.PathLength)
		{
			float num3 = 0f - baseDeploymentOffset;
			float num4 = num - TaleWorlds.Library.MathF.Min(baseDeploymentOffset, num3);
			float num5 = num2 - TaleWorlds.Library.MathF.Max(baseDeploymentOffset, num3);
			float num6 = 0f;
			if (num6 < num4)
			{
				num6 = num4;
			}
			else if (num6 > num5)
			{
				num6 = num5;
			}
			deployingSideBaseOffset = baseDeploymentOffset + num6;
			opposingSideBaseOffset = num3 + num6;
		}
		else if (baseDeploymentOffset <= 0f)
		{
			deployingSideBaseOffset = num;
			opposingSideBaseOffset = num2;
		}
		else
		{
			deployingSideBaseOffset = num2;
			opposingSideBaseOffset = num;
		}
		sideSpawnPathData.ClampPathOffset(ref deployingSideBaseOffset);
		sideSpawnPathData.ClampPathOffset(ref opposingSideBaseOffset);
	}

	public static void ComputeTeamDeploymentOffsets(SpawnPathData spawnPathData, float deploymentBaseOffset, float interTeamGapOffset, float[] teamOffsetRanges, out float[] teamDeployOffsets)
	{
		int num = teamOffsetRanges.Length;
		teamDeployOffsets = new float[num];
		if (!TryPlaceTeamsOneSide(spawnPathData, deploymentBaseOffset, teamOffsetRanges, interTeamGapOffset, searchBackwardFromBase: true, ref teamDeployOffsets))
		{
			float relativePathOffset = 0f - spawnPathData.PathLength;
			spawnPathData.ClampPathOffset(ref relativePathOffset);
			TryPlaceTeamsOneSide(spawnPathData, relativePathOffset, teamOffsetRanges, interTeamGapOffset, searchBackwardFromBase: false, ref teamDeployOffsets);
		}
	}

	private static bool TryPlaceTeamsOneSide(SpawnPathData spawnPathData, float deploymentBaseOffset, float[] teamOffsetRanges, float interTeamGapOffset, bool searchBackwardFromBase, ref float[] teamDeployOffsets)
	{
		int num = teamOffsetRanges.Length;
		for (int i = 0; i < num; i++)
		{
		}
		if (teamDeployOffsets == null)
		{
			teamDeployOffsets = new float[num];
		}
		else
		{
			for (int j = 0; j < teamDeployOffsets.Length; j++)
			{
				teamDeployOffsets[j] = 0f;
			}
		}
		int freeSegmentCount = spawnPathData.FreeSegmentCount;
		if (searchBackwardFromBase)
		{
			float num2 = deploymentBaseOffset;
			for (int k = 0; k < num; k++)
			{
				float num3 = teamOffsetRanges[k];
				if (num3 == 0f)
				{
					teamDeployOffsets[k] = num2;
					continue;
				}
				bool flag = false;
				for (int num4 = freeSegmentCount - 1; num4 >= 0; num4--)
				{
					var (num5, num6) = spawnPathData.GetFreeSegment(num4);
					if (!(num5 >= num2))
					{
						float num7 = num5;
						float num8 = num6;
						if (num8 > num2)
						{
							num8 = num2;
						}
						if (!(num8 - num7 < num3))
						{
							float num9 = num8;
							float num10 = num9 - num3;
							teamDeployOffsets[k] = num9;
							num2 = num10 - interTeamGapOffset;
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}
		float num11 = deploymentBaseOffset;
		for (int num12 = num - 1; num12 >= 0; num12--)
		{
			float num13 = teamOffsetRanges[num12];
			if (num13 == 0f)
			{
				teamDeployOffsets[num12] = num11;
			}
			else
			{
				bool flag2 = false;
				for (int l = 0; l < freeSegmentCount; l++)
				{
					var (num14, num15) = spawnPathData.GetFreeSegment(l);
					if (!(num15 <= num11))
					{
						float num16 = num14;
						if (num16 < num11)
						{
							num16 = num11;
						}
						if (!(num15 - num16 < num13))
						{
							float num17 = num16 + num13;
							teamDeployOffsets[num12] = num17;
							num11 = num17 + interTeamGapOffset;
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					return false;
				}
			}
		}
		return true;
	}

	private static ref float GetFlankDepthRef(FormationDeploymentFlank flank, ref float centerFlanksDepth, ref float leftFlanksDepth, ref float rightFlanksDepth)
	{
		return flank switch
		{
			FormationDeploymentFlank.Left => ref leftFlanksDepth, 
			FormationDeploymentFlank.Right => ref rightFlanksDepth, 
			_ => ref centerFlanksDepth, 
		};
	}
}
