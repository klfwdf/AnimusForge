using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public abstract class TacticComponent
{
	public static readonly int MoveHornSoundIndex;

	public static readonly int AttackHornSoundIndex;

	public static readonly int RetreatHornSoundIndex;

	protected int _AIControlledFormationCount;

	protected bool IsTacticReapplyNeeded;

	private bool _areFormationsCreated;

	protected Formation _mainInfantry;

	protected Formation _archers;

	protected Formation _leftCavalry;

	protected Formation _rightCavalry;

	protected Formation _rangedCavalry;

	private float _hornCooldownExpireTime;

	private const float HornCooldownTime = 10f;

	public Team Team { get; protected set; }

	protected MBList<Formation> FormationsIncludingSpecialAndEmpty => Team.FormationsIncludingSpecialAndEmpty;

	protected MBList<Formation> FormationsIncludingEmpty => Team.FormationsIncludingEmpty;

	protected bool AreFormationsCreated
	{
		get
		{
			if (_areFormationsCreated)
			{
				return true;
			}
			if (FormationsIncludingEmpty.AnyQ((Formation f) => f.CountOfUnits > 0))
			{
				_areFormationsCreated = true;
				ManageFormationCounts();
				CheckAndSetAvailableFormationsChanged();
				IsTacticReapplyNeeded = true;
				return true;
			}
			return false;
		}
	}

	protected TacticComponent(Team team)
	{
		Team = team;
	}

	static TacticComponent()
	{
		MoveHornSoundIndex = SoundEvent.GetEventIdFromString("event:/ui/mission/horns/move");
		AttackHornSoundIndex = SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack");
		RetreatHornSoundIndex = SoundEvent.GetEventIdFromString("event:/ui/mission/horns/retreat");
	}

	protected internal virtual void OnCancel()
	{
	}

	protected internal virtual void OnApply()
	{
		IsTacticReapplyNeeded = true;
	}

	public virtual void TickOccasionally()
	{
		TeamAIComponent teamAI = Team.TeamAI;
		if (teamAI.GetIsFirstTacticChosen)
		{
			teamAI.OnTacticAppliedForFirstTime();
		}
		if (Mission.Current.IsMissionEnding)
		{
			StopUsingAllMachines();
			if (teamAI.HasStrategicAreas)
			{
				teamAI.RemoveAllStrategicAreas();
			}
		}
	}

	private static void GetUnitCountByAttackType(Formation formation, out int unitCount, out int rangedCount, out int semiRangedCount, out int nonRangedCount)
	{
		FormationQuerySystem querySystem = formation.QuerySystem;
		unitCount = formation.CountOfUnits;
		rangedCount = (int)(querySystem.RangedUnitRatio * (float)unitCount);
		semiRangedCount = 0;
		nonRangedCount = unitCount - rangedCount;
	}

	protected static float GetFormationGroupEffectivenessOverOrder(IEnumerable<Formation> formationGroup, OrderType orderType, IOrderable targetObject = null)
	{
		GetUnitCountByAttackType(formationGroup.FirstOrDefault(), out var unitCount, out var rangedCount, out var semiRangedCount, out var nonRangedCount);
		switch (orderType)
		{
		case OrderType.PointDefence:
		{
			float num3 = ((float)nonRangedCount * 0.1f + (float)semiRangedCount * 0.3f + (float)rangedCount * 1f) / (float)unitCount;
			int num4 = (targetObject as IPointDefendable).DefencePoints.Count() * 3;
			float num5 = TaleWorlds.Library.MathF.Min((float)unitCount * 1f / (float)num4, 1f);
			return num3 * num5;
		}
		case OrderType.Use:
		{
			float num = ((float)nonRangedCount * 1f + (float)semiRangedCount * 0.9f + (float)rangedCount * 0.3f) / (float)unitCount;
			int maxUserCount = (targetObject as UsableMachine).MaxUserCount;
			float num2 = TaleWorlds.Library.MathF.Min((float)unitCount * 1f / (float)maxUserCount, 1f);
			return num * num2;
		}
		case OrderType.Charge:
			return ((float)nonRangedCount * 1f + (float)semiRangedCount * 0.9f + (float)rangedCount * 0.3f) / (float)unitCount;
		default:
			return 1f;
		}
	}

	protected static float GetFormationEffectivenessOverOrder(Formation formation, OrderType orderType, IOrderable targetObject = null)
	{
		GetUnitCountByAttackType(formation, out var unitCount, out var rangedCount, out var semiRangedCount, out var nonRangedCount);
		switch (orderType)
		{
		case OrderType.PointDefence:
		{
			float num = ((float)nonRangedCount * 0.1f + (float)semiRangedCount * 0.3f + (float)rangedCount * 1f) / (float)unitCount;
			int num2 = (targetObject as IPointDefendable).DefencePoints.Count() * 3;
			float num3 = TaleWorlds.Library.MathF.Min((float)unitCount * 1f / (float)num2, 1f);
			return num * num3;
		}
		case OrderType.Use:
		{
			float minDistanceSquared = float.MaxValue;
			formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				minDistanceSquared = TaleWorlds.Library.MathF.Min(agent.Position.DistanceSquared((targetObject as UsableMachine).GameEntity.GlobalPosition), minDistanceSquared);
			});
			return 1f / MBMath.ClampFloat(minDistanceSquared, 1f, float.MaxValue);
		}
		case OrderType.Charge:
			return ((float)nonRangedCount * 1f + (float)semiRangedCount * 0.9f + (float)rangedCount * 0.3f) / (float)unitCount;
		default:
			return 1f;
		}
	}

	[Conditional("DEBUG")]
	protected internal virtual void DebugTick(float dt)
	{
	}

	private static int GetAvailableUnitCount(Formation formation, IEnumerable<(Formation, UsableMachine, int)> appliedCombinations)
	{
		int num = appliedCombinations.Where(((Formation, UsableMachine, int) c) => c.Item1 == formation).Sum(((Formation, UsableMachine, int) c) => c.Item3);
		int num2 = 0;
		return formation.CountOfUnits - (num + num2);
	}

	private static int GetVacantSlotCount(UsableMachine weapon, IEnumerable<(Formation, UsableMachine, int)> appliedCombinations)
	{
		int num = appliedCombinations.Where(((Formation, UsableMachine, int) c) => c.Item2 == weapon).Sum(((Formation, UsableMachine, int) c) => c.Item3);
		return weapon.MaxUserCount - num;
	}

	protected List<Formation> ConsolidateFormations(List<Formation> formationsToBeConsolidated, int neededCount)
	{
		if (formationsToBeConsolidated.Count <= neededCount || neededCount <= 0)
		{
			return formationsToBeConsolidated;
		}
		List<Formation> list = formationsToBeConsolidated.OrderByDescending((Formation f) => f.CountOfUnits + ((!f.IsAIControlled) ? 10000 : 0)).ToList();
		List<Formation> list2 = list.Take(neededCount).Reverse().ToList();
		list.RemoveRange(0, neededCount);
		Queue<Formation> queue = new Queue<Formation>(list2);
		List<Formation> list3 = new List<Formation>();
		foreach (Formation item in list)
		{
			if (!item.IsAIControlled)
			{
				list3.Add(item);
				continue;
			}
			if (queue.IsEmpty())
			{
				queue = new Queue<Formation>(list2);
			}
			Formation target = queue.Dequeue();
			item.TransferUnits(target, item.CountOfUnits);
		}
		return list2.Concat(list3).ToList();
	}

	protected static float CalculateNotEngagingTacticalAdvantage(TeamQuerySystem team)
	{
		float num = team.CavalryRatio + team.RangedCavalryRatio;
		float num2 = team.EnemyCavalryRatio + team.EnemyRangedCavalryRatio;
		return TaleWorlds.Library.MathF.Pow(MBMath.ClampFloat((num > 0f) ? (num2 / num) : 1.5f, 1f, 1.5f), 1.5f * TaleWorlds.Library.MathF.Max(num, num2));
	}

	protected void SplitFormationClassIntoGivenNumber(Func<Formation, bool> formationClass, int count)
	{
		List<Formation> list = new List<Formation>();
		List<int> list2 = new List<int>();
		int num = 0;
		List<int> list3 = new List<int>();
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (Formation item2 in Team.FormationsIncludingEmpty)
		{
			if (item2.CountOfUnits == 0)
			{
				list.Add(item2);
				num3++;
			}
			else
			{
				if (!formationClass(item2))
				{
					continue;
				}
				list.Add(item2);
				if (item2.IsAIOwned)
				{
					list2.Add(num3);
					num += item2.CountOfUnits;
					if (item2.IsConvenientForTransfer)
					{
						list3.Add(num3);
						num2 += item2.CountOfUnits;
					}
				}
				else
				{
					num4++;
				}
				num3++;
			}
		}
		int num5 = count - num4;
		int count2 = list3.Count;
		int count3 = list2.Count;
		if (count3 <= 0)
		{
			return;
		}
		List<int> list4 = new List<int>();
		if (num5 > count3 && count2 > 0)
		{
			int num6 = num5 - count3;
			_ = (float)num / (float)num5;
			double num7 = Math.Ceiling((float)num2 / (float)(list3.Count + num6));
			List<int> list5 = new List<int>();
			int num8 = 0;
			for (num3 = 0; num3 < count3; num3++)
			{
				if (list[list2[num3]].IsConvenientForTransfer || (double)list[list2[num3]].CountOfUnits <= num7)
				{
					list5.Add(num3);
					num8 += list[list2[num3]].CountOfUnits;
				}
			}
			double num9 = Math.Ceiling((float)num8 / (float)(list5.Count + num6));
			List<int> list6 = new List<int>();
			for (num3 = 0; num3 < list.Count; num3++)
			{
				if (list[num3].CountOfUnits == 0 && list6.Count < num6)
				{
					list6.Add(num3);
				}
			}
			for (num3 = 0; num3 < count2; num3++)
			{
				Formation formation = list[list3[num3]];
				int num10 = (int)((double)formation.CountOfUnits - num9);
				int num11 = 0;
				while (num10 >= 1 && num11 < list5.Count)
				{
					Formation formation2 = list[list5[num11]];
					if (formation != formation2)
					{
						int num12 = (int)(num9 - (double)formation2.CountOfUnits);
						if (num12 >= 1)
						{
							int num13 = TaleWorlds.Library.MathF.Min(num10, num12);
							formation.TransferUnits(formation2, num13);
							if (!list4.Contains(list3[num3]))
							{
								list4.Add(list3[num3]);
							}
							if (!list4.Contains(list5[num11]))
							{
								list4.Add(list5[num11]);
							}
							num10 -= num13;
						}
					}
					num11++;
				}
				if (num10 < 1)
				{
					continue;
				}
				int num14 = 0;
				while (num10 >= 1 && num14 < list6.Count)
				{
					Formation formation3 = list[list6[num14]];
					int num15 = (int)(num7 - (double)formation3.CountOfUnits);
					if (num15 >= 1)
					{
						int num16 = TaleWorlds.Library.MathF.Min(num10, num15);
						formation.TransferUnits(formation3, num16);
						if (!list4.Contains(list3[num3]))
						{
							list4.Add(list3[num3]);
						}
						if (!list4.Contains(list6[num14]))
						{
							list4.Add(list6[num14]);
						}
						num10 -= num16;
					}
					num14++;
				}
			}
		}
		else if (num5 < count3 && count3 != 1)
		{
			if (num5 < 1)
			{
				num5 = 1;
			}
			float num17 = (float)num / (float)num5;
			int num18 = 0;
			List<int> list7 = new List<int>();
			int num19 = 0;
			int num20 = 0;
			for (num3 = 0; num3 < list2.Count; num3++)
			{
				Formation formation4 = list[list2[num3]];
				bool flag = false;
				if (!formation4.IsConvenientForTransfer)
				{
					num18++;
					if ((float)formation4.CountOfUnits < num17)
					{
						flag = true;
					}
					else
					{
						num20++;
					}
				}
				else
				{
					flag = true;
				}
				if (flag)
				{
					list7.Add(list2[num3]);
					num19 += formation4.CountOfUnits;
				}
			}
			if (num5 < 1)
			{
				num5 = 1;
			}
			else if (num5 < num18)
			{
				num5 = num18;
			}
			float num21 = (float)num19 / (float)(num5 - num20);
			List<int> list8 = new List<int>();
			int num22 = count3 - num5;
			while (list8.Count < num22 && count2 > 0)
			{
				int num23 = int.MaxValue;
				int item = -1;
				for (num3 = 0; num3 < list3.Count; num3++)
				{
					Formation formation5 = list[list3[num3]];
					if (formation5.CountOfUnits < num23)
					{
						num23 = formation5.CountOfUnits;
						item = list3[num3];
					}
				}
				list3.Remove(item);
				list8.Add(item);
			}
			for (num3 = 0; num3 < list8.Count + list3.Count; num3++)
			{
				bool num24 = num3 < list8.Count;
				int num25 = ((!num24) ? list3[num3 - list8.Count] : list8[num3]);
				Formation formation6 = list[num25];
				int num26 = ((!num24) ? ((int)((float)formation6.CountOfUnits - num21)) : formation6.CountOfUnits);
				int num27 = 0;
				while (num26 >= 1 && num27 < list7.Count)
				{
					Formation formation7 = list[list7[num27]];
					if (formation6 != formation7 && formation7.CountOfUnits != 0)
					{
						int num28 = (int)Math.Ceiling(num21 - (float)formation7.CountOfUnits);
						if (num28 >= 1)
						{
							int num29 = TaleWorlds.Library.MathF.Min(num26, num28);
							formation6.TransferUnits(formation7, num29);
							if (!list4.Contains(num25))
							{
								list4.Add(num25);
							}
							if (!list4.Contains(list7[num27]))
							{
								list4.Add(list7[num27]);
							}
							num26 -= num29;
						}
					}
					num27++;
				}
			}
		}
		if (num5 <= 1 || list4.Count <= 1)
		{
			return;
		}
		List<Formation> list9 = new List<Formation>();
		for (num3 = 0; num3 < list4.Count; num3++)
		{
			Formation formation8 = list[list4[num3]];
			if (formation8.CountOfUnits > 0)
			{
				formation8.AI.Side = FormationAI.BehaviorSide.BehaviorSideNotSet;
				formation8.AI.IsMainFormation = false;
				formation8.AI.ResetBehaviorWeights();
				SetDefaultBehaviorWeights(formation8);
				list9.Add(formation8);
			}
		}
		IsTacticReapplyNeeded = true;
	}

	protected virtual bool CheckAndSetAvailableFormationsChanged()
	{
		return false;
	}

	public void ResetTactic()
	{
		ManageFormationCounts();
		CheckAndSetAvailableFormationsChanged();
		IsTacticReapplyNeeded = true;
	}

	protected void AssignTacticFormations1121()
	{
		ManageFormationCounts(1, 1, 2, 1);
		_mainInfantry = ChooseAndSortByPriority(FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.IsMainFormation = true;
			_mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
		}
		_archers = ChooseAndSortByPriority(FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
		List<Formation> list = ChooseAndSortByPriority(FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsCavalryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower);
		if (list.Count > 0)
		{
			_leftCavalry = list[0];
			_leftCavalry.AI.Side = FormationAI.BehaviorSide.Left;
			if (list.Count > 1)
			{
				_rightCavalry = list[1];
				_rightCavalry.AI.Side = FormationAI.BehaviorSide.Right;
			}
			else
			{
				_rightCavalry = null;
			}
		}
		else
		{
			_leftCavalry = null;
			_rightCavalry = null;
		}
		_rangedCavalry = ChooseAndSortByPriority(FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedCavalryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
	}

	protected static List<Formation> ChooseAndSortByPriority(IEnumerable<Formation> formations, Func<Formation, bool> isEligible, Func<Formation, bool> isPrioritized, Func<Formation, float> score)
	{
		formations = formations.Where(isEligible);
		IOrderedEnumerable<Formation> orderedEnumerable = formations.Where(isPrioritized).OrderByDescending(score);
		IOrderedEnumerable<Formation> second = formations.Except(orderedEnumerable).OrderByDescending(score);
		return orderedEnumerable.Concat(second).ToList();
	}

	protected virtual void ManageFormationCounts()
	{
	}

	protected void ManageFormationCounts(int infantryCount, int rangedCount, int cavalryCount, int rangedCavalryCount)
	{
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsInfantryFormation, infantryCount);
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsRangedFormation, rangedCount);
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsCavalryFormation, cavalryCount);
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsRangedCavalryFormation, rangedCavalryCount);
	}

	protected virtual void StopUsingAllMachines()
	{
		if (!(Team.TeamAI is TeamAISiegeComponent teamAISiegeComponent))
		{
			return;
		}
		foreach (SiegeWeapon sceneSiegeWeapon in teamAISiegeComponent.SceneSiegeWeapons)
		{
			if (sceneSiegeWeapon.Side != Team.Side)
			{
				continue;
			}
			sceneSiegeWeapon.SetForcedUse(value: false);
			for (int num = sceneSiegeWeapon.UserFormations.Count - 1; num >= 0; num--)
			{
				Formation formation = sceneSiegeWeapon.UserFormations[num];
				if (formation.Team == Team)
				{
					formation.StopUsingMachine(sceneSiegeWeapon);
				}
			}
		}
	}

	protected void StopUsingAllRangedSiegeWeapons()
	{
		foreach (Formation item in FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits <= 0)
			{
				continue;
			}
			for (int num = item.Detachments.Count - 1; num >= 0; num--)
			{
				if (item.Detachments[num] is RangedSiegeWeapon rangedSiegeWeapon)
				{
					item.StopUsingMachine(rangedSiegeWeapon);
					rangedSiegeWeapon.SetForcedUse(value: false);
				}
			}
		}
	}

	protected void SoundTacticalHorn(int soundCode)
	{
		float currentTime = Mission.Current.CurrentTime;
		if (currentTime > _hornCooldownExpireTime)
		{
			_hornCooldownExpireTime = currentTime + 10f;
			SoundEvent.PlaySound2D(soundCode);
		}
	}

	public static void SetDefaultBehaviorWeights(Formation f)
	{
		f.AI.SetBehaviorWeight<BehaviorCharge>(1f);
		f.AI.SetBehaviorWeight<BehaviorPullBack>(1f);
		f.AI.SetBehaviorWeight<BehaviorStop>(1f);
		f.AI.SetBehaviorWeight<BehaviorReserve>(1f);
	}

	protected internal virtual float GetTacticWeight()
	{
		return 0f;
	}

	protected bool CheckAndDetermineFormation(ref Formation formation, Func<Formation, bool> isEligible)
	{
		if (formation == null || formation.CountOfUnits == 0 || !isEligible(formation))
		{
			List<Formation> list = FormationsIncludingEmpty.Where(isEligible).ToList();
			if (list.Count > 0)
			{
				formation = TaleWorlds.Core.Extensions.MaxBy(list, (Formation f) => f.CountOfUnits);
				IsTacticReapplyNeeded = true;
				return true;
			}
			if (formation != null)
			{
				formation = null;
				IsTacticReapplyNeeded = true;
			}
			return false;
		}
		return true;
	}

	protected internal virtual bool ResetTacticalPositions()
	{
		return false;
	}
}
