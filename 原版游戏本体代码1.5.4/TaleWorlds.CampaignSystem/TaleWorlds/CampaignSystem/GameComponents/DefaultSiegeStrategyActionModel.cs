using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSiegeStrategyActionModel : SiegeStrategyActionModel
{
	private List<(SiegeEngineType, int)> _prepareAssaultEngineList;

	private List<(SiegeEngineType, int)> _breachWallsEngineList;

	private List<(SiegeEngineType, int)> _wearOutDefendersEngineList;

	private List<(SiegeEngineType, int)> _prepareAgainstAssaultEngineList;

	private List<(SiegeEngineType, int)> _counterBombardmentEngineList;

	public override void GetLogicalActionForStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		siegeAction = SiegeAction.Hold;
		siegeEngineType = null;
		deploymentIndex = -1;
		reserveIndex = -1;
		SiegeStrategy siegeStrategy = side.SiegeStrategy;
		if (siegeStrategy == DefaultSiegeStrategies.Custom)
		{
			return;
		}
		if (siegeStrategy == DefaultSiegeStrategies.PreserveStrength)
		{
			GetLogicalActionForPreserveStrengthStrategy(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex);
		}
		else if (side.BattleSide == BattleSideEnum.Attacker)
		{
			if (siegeStrategy == DefaultSiegeStrategies.PrepareAssault)
			{
				GetLogicalActionForPrepareAssaultStrategy(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex);
			}
			else if (siegeStrategy == DefaultSiegeStrategies.BreachWalls)
			{
				GetLogicalActionForBreachWallsStrategy(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex);
			}
			else if (siegeStrategy == DefaultSiegeStrategies.WearOutDefenders)
			{
				GetLogicalActionForWearOutDefendersStrategy(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex);
			}
		}
		else if (side.BattleSide == BattleSideEnum.Defender)
		{
			if (siegeStrategy == DefaultSiegeStrategies.PrepareAgainstAssault)
			{
				GetLogicalActionForPrepareAgainstAssaultStrategy(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex);
			}
			else if (siegeStrategy == DefaultSiegeStrategies.CounterBombardment)
			{
				GetLogicalActionForCounterBombardmentStrategy(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex);
			}
		}
	}

	private bool CheckIfStrategyListSatisfied(ISiegeEventSide side, List<(SiegeEngineType, int)> engineList)
	{
		SiegeEvent.SiegeEnginesContainer siegeEngines = side.SiegeEngines;
		foreach (var engine in engineList)
		{
			siegeEngines.DeployedSiegeEngineTypesCount.TryGetValue(engine.Item1, out var value);
			if (value != engine.Item2)
			{
				return false;
			}
		}
		return true;
	}

	private void GetLogicalActionToCompleteEngineList(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex, List<(SiegeEngineType, int)> engineList)
	{
		siegeAction = SiegeAction.Hold;
		siegeEngineType = null;
		deploymentIndex = -1;
		reserveIndex = -1;
		if (CheckIfStrategyListSatisfied(side, engineList))
		{
			return;
		}
		SiegeEvent.SiegeEnginesContainer siegeEngines = side.SiegeEngines;
		int num = -1;
		int num2 = -1;
		foreach (KeyValuePair<SiegeEngineType, int> item2 in siegeEngines.DeployedSiegeEngineTypesCount)
		{
			int num3 = -1;
			foreach (var engine in engineList)
			{
				if (item2.Key == engine.Item1)
				{
					num3 = engine.Item2;
					break;
				}
			}
			if (num3 < 0 || num3 < item2.Value)
			{
				SiegeEngineType excessSiegeEngineType = item2.Key;
				Func<SiegeEvent.SiegeEngineConstructionProgress, bool> predicate = (SiegeEvent.SiegeEngineConstructionProgress engine) => engine != null && engine.SiegeEngine == excessSiegeEngineType && engine.IsActive && engine.Hitpoints > 0f;
				if (num2 == -1 && excessSiegeEngineType.IsRanged)
				{
					num2 = siegeEngines.DeployedRangedSiegeEngines.FindIndex(predicate);
				}
				else if (num == -1 && !excessSiegeEngineType.IsRanged)
				{
					num = siegeEngines.DeployedMeleeSiegeEngines.FindIndex(predicate);
				}
			}
		}
		int num4 = siegeEngines.DeployedMeleeSiegeEngines.FindIndex((SiegeEvent.SiegeEngineConstructionProgress engine) => engine == null);
		int num5 = siegeEngines.DeployedRangedSiegeEngines.FindIndex((SiegeEvent.SiegeEngineConstructionProgress engine) => engine == null);
		if (num4 == -1 && num5 == -1 && num == -1 && num2 == -1)
		{
			return;
		}
		int num6 = ((num5 != -1) ? num5 : num2);
		int num7 = ((num4 != -1) ? num4 : num);
		if (!siegeEngines.ReservedSiegeEngines.IsEmpty())
		{
			foreach (var engine2 in engineList)
			{
				siegeEngines.DeployedSiegeEngineTypesCount.TryGetValue(engine2.Item1, out var value);
				if (value >= engine2.Item2)
				{
					continue;
				}
				var (slackEngineType, _) = engine2;
				siegeEngines.ReservedSiegeEngineTypesCount.TryGetValue(slackEngineType, out var value2);
				if (value2 <= 0)
				{
					continue;
				}
				int num8 = (slackEngineType.IsRanged ? num6 : num7);
				if (num8 != -1)
				{
					siegeAction = SiegeAction.DeploySiegeEngineFromReserve;
					siegeEngineType = slackEngineType;
					reserveIndex = siegeEngines.ReservedSiegeEngines.FindIndex((SiegeEvent.SiegeEngineConstructionProgress reservedEngine) => reservedEngine.SiegeEngine == slackEngineType);
					deploymentIndex = num8;
					return;
				}
			}
		}
		if (side.BattleSide == BattleSideEnum.Defender || (side as BesiegerCamp).IsPreparationComplete)
		{
			bool flag = false;
			foreach (SiegeEvent.SiegeEngineConstructionProgress deployedSiegeEngine in siegeEngines.DeployedSiegeEngines)
			{
				if (deployedSiegeEngine.Progress < 1f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (var engine3 in engineList)
				{
					siegeEngines.DeployedSiegeEngineTypesCount.TryGetValue(engine3.Item1, out var value3);
					if (value3 < engine3.Item2)
					{
						SiegeEngineType item = engine3.Item1;
						int num9 = (item.IsRanged ? num6 : num7);
						if (num9 != -1)
						{
							siegeAction = SiegeAction.ConstructNewSiegeEngine;
							siegeEngineType = item;
							deploymentIndex = num9;
							return;
						}
					}
				}
			}
		}
		if (num4 != -1)
		{
			int num10 = siegeEngines.ReservedSiegeEngines.FindIndex((SiegeEvent.SiegeEngineConstructionProgress engine) => !engine.SiegeEngine.IsRanged);
			if (num10 != -1)
			{
				siegeAction = SiegeAction.DeploySiegeEngineFromReserve;
				siegeEngineType = siegeEngines.ReservedSiegeEngines[num10].SiegeEngine;
				reserveIndex = num10;
				deploymentIndex = num4;
				return;
			}
		}
		if (num5 != -1)
		{
			int num11 = siegeEngines.ReservedSiegeEngines.FindIndex((SiegeEvent.SiegeEngineConstructionProgress engine) => engine.SiegeEngine.IsRanged);
			if (num11 != -1)
			{
				siegeAction = SiegeAction.DeploySiegeEngineFromReserve;
				siegeEngineType = siegeEngines.ReservedSiegeEngines[num11].SiegeEngine;
				reserveIndex = num11;
				deploymentIndex = num5;
			}
		}
	}

	private void GetLogicalActionForPreserveStrengthStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		SiegeEvent.SiegeEnginesContainer siegeEngines = side.SiegeEngines;
		for (int i = 0; i < side.SiegeEngines.DeployedRangedSiegeEngines.Length; i++)
		{
			SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress = siegeEngines.DeployedRangedSiegeEngines[i];
			if (siegeEngineConstructionProgress != null && siegeEngineConstructionProgress.IsActive && siegeEngineConstructionProgress.Hitpoints > 0f)
			{
				siegeAction = SiegeAction.MoveSiegeEngineToReserve;
				siegeEngineType = siegeEngineConstructionProgress.SiegeEngine;
				deploymentIndex = i;
				reserveIndex = -1;
				return;
			}
		}
		for (int j = 0; j < side.SiegeEngines.DeployedMeleeSiegeEngines.Length; j++)
		{
			SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress2 = siegeEngines.DeployedMeleeSiegeEngines[j];
			if (siegeEngineConstructionProgress2 != null && siegeEngineConstructionProgress2.IsActive && siegeEngineConstructionProgress2.Hitpoints > 0f)
			{
				siegeAction = SiegeAction.MoveSiegeEngineToReserve;
				siegeEngineType = siegeEngineConstructionProgress2.SiegeEngine;
				deploymentIndex = j;
				reserveIndex = -1;
				return;
			}
		}
		siegeAction = SiegeAction.Hold;
		siegeEngineType = null;
		deploymentIndex = -1;
		reserveIndex = -1;
	}

	private void GetLogicalActionForPrepareAssaultStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		if (_prepareAssaultEngineList == null)
		{
			_prepareAssaultEngineList = new List<(SiegeEngineType, int)>
			{
				(DefaultSiegeEngineTypes.Ram, 1),
				(DefaultSiegeEngineTypes.SiegeTower, 2),
				(DefaultSiegeEngineTypes.Ballista, 2),
				(DefaultSiegeEngineTypes.Onager, 2)
			};
		}
		GetLogicalActionToCompleteEngineList(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex, _prepareAssaultEngineList);
	}

	private void GetLogicalActionForBreachWallsStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		if (_breachWallsEngineList == null)
		{
			_breachWallsEngineList = new List<(SiegeEngineType, int)>
			{
				(DefaultSiegeEngineTypes.Ram, 1),
				(DefaultSiegeEngineTypes.SiegeTower, 1),
				(DefaultSiegeEngineTypes.Onager, 4)
			};
		}
		GetLogicalActionToCompleteEngineList(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex, _breachWallsEngineList);
	}

	private void GetLogicalActionForWearOutDefendersStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		if (_wearOutDefendersEngineList == null)
		{
			_wearOutDefendersEngineList = new List<(SiegeEngineType, int)>
			{
				(DefaultSiegeEngineTypes.Ram, 1),
				(DefaultSiegeEngineTypes.SiegeTower, 1),
				(DefaultSiegeEngineTypes.Trebuchet, 4)
			};
		}
		GetLogicalActionToCompleteEngineList(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex, _wearOutDefendersEngineList);
	}

	private void GetLogicalActionForPrepareAgainstAssaultStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		if (_prepareAgainstAssaultEngineList == null)
		{
			_prepareAgainstAssaultEngineList = new List<(SiegeEngineType, int)>
			{
				(DefaultSiegeEngineTypes.FireCatapult, 1),
				(DefaultSiegeEngineTypes.Catapult, 2),
				(DefaultSiegeEngineTypes.FireBallista, 1)
			};
		}
		GetLogicalActionToCompleteEngineList(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex, _prepareAgainstAssaultEngineList);
	}

	private void GetLogicalActionForCounterBombardmentStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex)
	{
		if (_counterBombardmentEngineList == null)
		{
			_counterBombardmentEngineList = new List<(SiegeEngineType, int)>
			{
				(DefaultSiegeEngineTypes.FireCatapult, 2),
				(DefaultSiegeEngineTypes.Catapult, 2)
			};
		}
		GetLogicalActionToCompleteEngineList(side, out siegeAction, out siegeEngineType, out deploymentIndex, out reserveIndex, _counterBombardmentEngineList);
	}
}
