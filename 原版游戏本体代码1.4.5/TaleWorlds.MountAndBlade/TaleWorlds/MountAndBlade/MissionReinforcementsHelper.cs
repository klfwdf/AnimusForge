using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MissionReinforcementsHelper
{
	public enum ReinforcementFormationPriority
	{
		Dominant = 6,
		Common = 5,
		EmptyRepresentativeMatch = 4,
		EmptyNoMatch = 3,
		AlternativeDominant = 2,
		AlternativeCommon = 1,
		Default = 0
	}

	public class ReinforcementFormationPreferenceComparer : IComparer<ReinforcementFormationPriority>
	{
		public int Compare(ReinforcementFormationPriority left, ReinforcementFormationPriority right)
		{
			if (right < left)
			{
				return 1;
			}
			if (right > left)
			{
				return -1;
			}
			return 0;
		}
	}

	public class ReinforcementFormationData
	{
		private uint _initTime;

		private bool _isClassified;

		private int[] _expectedTroopCountPerClass;

		private int _expectedTotalTroopCount;

		private bool[] _troopClasses;

		private FormationClass _representativeClass;

		public ReinforcementFormationData()
		{
			_initTime = 0u;
			_expectedTroopCountPerClass = new int[4];
			_expectedTotalTroopCount = 0;
			_isClassified = false;
			_representativeClass = FormationClass.NumberOfAllFormations;
			_troopClasses = new bool[4];
		}

		public void Initialize(Formation formation, uint initTime)
		{
			int countOfUnits = formation.CountOfUnits;
			_expectedTroopCountPerClass[0] = (int)(formation.QuerySystem.InfantryUnitRatio * (float)countOfUnits);
			_expectedTroopCountPerClass[1] = (int)(formation.QuerySystem.RangedUnitRatio * (float)countOfUnits);
			_expectedTroopCountPerClass[2] = (int)(formation.QuerySystem.CavalryUnitRatio * (float)countOfUnits);
			_expectedTroopCountPerClass[3] = (int)(formation.QuerySystem.RangedCavalryUnitRatio * (float)countOfUnits);
			_expectedTotalTroopCount = countOfUnits;
			_isClassified = false;
			_representativeClass = formation.RepresentativeClass;
			_initTime = initTime;
		}

		public void AddProspectiveTroop(FormationClass troopClass)
		{
			_expectedTroopCountPerClass[(int)troopClass]++;
			_expectedTotalTroopCount++;
			_isClassified = false;
		}

		public bool IsInitialized(uint initTime)
		{
			return initTime == _initTime;
		}

		public ReinforcementFormationPriority GetPriority(FormationClass troopClass)
		{
			if (_expectedTotalTroopCount == 0)
			{
				if (_representativeClass == troopClass)
				{
					return ReinforcementFormationPriority.EmptyRepresentativeMatch;
				}
				return ReinforcementFormationPriority.EmptyNoMatch;
			}
			if (!_isClassified)
			{
				Classify();
			}
			if (HasTroopClass(troopClass, out var isDominant))
			{
				if (!isDominant)
				{
					return ReinforcementFormationPriority.Common;
				}
				return ReinforcementFormationPriority.Dominant;
			}
			FormationClass troopClass2 = troopClass.AlternativeClass();
			if (HasTroopClass(troopClass2, out isDominant))
			{
				if (!isDominant)
				{
					return ReinforcementFormationPriority.AlternativeCommon;
				}
				return ReinforcementFormationPriority.AlternativeDominant;
			}
			return ReinforcementFormationPriority.Default;
		}

		private void Classify()
		{
			if (_expectedTotalTroopCount > 0)
			{
				int num = -1;
				int num2 = 4;
				for (int i = 0; i < num2; i++)
				{
					float num3 = (float)_expectedTroopCountPerClass[i] / (float)_expectedTotalTroopCount;
					_troopClasses[i] = num3 >= 0.25f;
					if (num3 > 0.5f)
					{
						num = i;
						break;
					}
				}
				if (num >= 0)
				{
					ResetClassAssignments();
					_troopClasses[num] = true;
				}
			}
			else
			{
				ResetClassAssignments();
			}
			_isClassified = true;
		}

		private bool HasTroopClass(FormationClass troopClass, out bool isDominant)
		{
			int num = 0;
			for (int i = 0; i < 4; i++)
			{
				if (i == (int)troopClass && _troopClasses[i])
				{
					num++;
				}
			}
			isDominant = num == 1;
			return num >= 1;
		}

		private void ResetClassAssignments()
		{
			int num = 4;
			for (int i = 0; i < num; i++)
			{
				_troopClasses[i] = false;
			}
		}
	}

	private const float DominantClassThreshold = 0.5f;

	private const float CommonClassThreshold = 0.25f;

	private static uint _localInitTime;

	private static ReinforcementFormationData[,] _reinforcementFormationsData;

	public static void OnMissionStart()
	{
		Mission current = Mission.Current;
		_reinforcementFormationsData = new ReinforcementFormationData[current.Teams.Count, 8];
		foreach (Team team in current.Teams)
		{
			for (int i = 0; i < 8; i++)
			{
				_reinforcementFormationsData[team.TeamIndex, i] = new ReinforcementFormationData();
			}
		}
		_localInitTime = 0u;
	}

	public static List<(IAgentOriginBase origin, int formationIndex)> GetReinforcementAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
	{
		_ = Mission.Current;
		_localInitTime++;
		List<(IAgentOriginBase, int)> list = new List<(IAgentOriginBase, int)>();
		TaleWorlds.Library.PriorityQueue<ReinforcementFormationPriority, Formation> priorityQueue = new TaleWorlds.Library.PriorityQueue<ReinforcementFormationPriority, Formation>(new ReinforcementFormationPreferenceComparer());
		foreach (IAgentOriginBase troopOrigin in troopOrigins)
		{
			priorityQueue.Clear();
			FormationClass agentTroopClass = Mission.Current.GetAgentTroopClass(battleSide, troopOrigin.Troop);
			bool isPlayerSide = Mission.Current.PlayerTeam.Side == battleSide;
			Team agentTeam = Mission.GetAgentTeam(troopOrigin, isPlayerSide);
			foreach (Formation item2 in agentTeam.FormationsIncludingEmpty)
			{
				int formationIndex = (int)item2.FormationIndex;
				if (item2.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Retreat)
				{
					ReinforcementFormationData reinforcementFormationData = _reinforcementFormationsData[agentTeam.TeamIndex, formationIndex];
					if (!reinforcementFormationData.IsInitialized(_localInitTime))
					{
						reinforcementFormationData.Initialize(item2, _localInitTime);
					}
					ReinforcementFormationPriority priority = reinforcementFormationData.GetPriority(agentTroopClass);
					if (priorityQueue.IsEmpty() || priority >= priorityQueue.Peek().Key)
					{
						priorityQueue.Enqueue(priority, item2);
					}
				}
			}
			Formation formation = FindBestFormationAmong(priorityQueue);
			if (formation == null)
			{
				formation = agentTeam.GetFormation(agentTroopClass);
			}
			int formationIndex2 = (int)formation.FormationIndex;
			_reinforcementFormationsData[formation.Team.TeamIndex, formationIndex2].AddProspectiveTroop(agentTroopClass);
			(IAgentOriginBase, int) item = (troopOrigin, formationIndex2);
			list.Add(item);
		}
		return list;
	}

	public static void OnMissionEnd()
	{
		_reinforcementFormationsData = null;
	}

	private static Formation FindBestFormationAmong(TaleWorlds.Library.PriorityQueue<ReinforcementFormationPriority, Formation> matchingFormations)
	{
		Formation formation = null;
		float num = float.MinValue;
		if (!matchingFormations.IsEmpty())
		{
			int key = (int)matchingFormations.Peek().Key;
			foreach (KeyValuePair<ReinforcementFormationPriority, Formation> matchingFormation in matchingFormations)
			{
				int key2 = (int)matchingFormation.Key;
				if (key2 < key)
				{
					break;
				}
				Formation value = matchingFormation.Value;
				if (key2 == 3 || key2 == 4)
				{
					if (formation == null || value.FormationIndex < formation.FormationIndex)
					{
						formation = value;
					}
					continue;
				}
				float formationReinforcementScore = GetFormationReinforcementScore(value);
				if (formationReinforcementScore > num)
				{
					num = formationReinforcementScore;
					formation = value;
				}
			}
		}
		return formation;
	}

	private static float GetFormationReinforcementScore(Formation formation)
	{
		Mission current = Mission.Current;
		float num = (float)formation.CountOfUnits / (float)Math.Max(1, formation.Team.ActiveAgents.Count);
		float num2 = TaleWorlds.Library.MathF.Max(0f, 1f - num);
		float num3 = 0f;
		Team team = formation.Team;
		if (current.GetDeploymentPlan<DefaultMissionDeploymentPlan>(out var deploymentPlan) && formation.HasBeenPositioned && deploymentPlan.IsReinforcementPlanMade(team))
		{
			Vec2 asVec = deploymentPlan.GetMeanPosition(team).AsVec2;
			float num4 = formation.CurrentPosition.DistanceSquared(asVec);
			float num5 = TaleWorlds.Library.MathF.Min(1f, num4 / 62500f);
			num3 = TaleWorlds.Library.MathF.Max(0f, 1f - num5);
		}
		return 0.6f * num2 + 0.4f * num3;
	}
}
