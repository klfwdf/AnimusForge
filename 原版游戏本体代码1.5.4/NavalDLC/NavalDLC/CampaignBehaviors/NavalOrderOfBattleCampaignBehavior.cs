using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace NavalDLC.CampaignBehaviors;

public class NavalOrderOfBattleCampaignBehavior : CampaignBehaviorBase
{
	public class NavalOrderOfBattleFormationData
	{
		[SaveableField(1)]
		public readonly Hero Captain;

		[SaveableField(2)]
		public readonly Ship Ship;

		[SaveableField(3)]
		public readonly DeploymentFormationClass FormationClass;

		[SaveableField(4)]
		public readonly Dictionary<FormationFilterType, bool> Filters;

		public NavalOrderOfBattleFormationData(Hero captain, Ship ship, DeploymentFormationClass formationClass, Dictionary<FormationFilterType, bool> filters)
		{
			Captain = captain;
			Ship = ship;
			FormationClass = formationClass;
			Filters = new Dictionary<FormationFilterType, bool>();
			foreach (FormationFilterType key in filters.Keys)
			{
				Filters.Add(key, filters[key]);
			}
		}
	}

	private List<NavalOrderOfBattleFormationData> _navalBattleFormationInfos;

	private List<NavalOrderOfBattleFormationData> _navalBattleArmyFormationInfos;

	public NavalOrderOfBattleCampaignBehavior()
	{
		_navalBattleFormationInfos = new List<NavalOrderOfBattleFormationData>();
		_navalBattleArmyFormationInfos = new List<NavalOrderOfBattleFormationData>();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnShipDestroyedEvent.AddNonSerializedListener(this, OnShipDestroyed);
		CampaignEvents.OnHeroUnregisteredEvent.AddNonSerializedListener(this, OnHeroUnregistered);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_navalBattleFormationInfos", ref _navalBattleFormationInfos);
		dataStore.SyncData("_navalArmyBattleFormationInfos", ref _navalBattleArmyFormationInfos);
	}

	public NavalOrderOfBattleFormationData GetFormationDataAtIndex(int formationIndex, bool isInArmy)
	{
		if (isInArmy)
		{
			if (_navalBattleArmyFormationInfos.Count > formationIndex)
			{
				return _navalBattleArmyFormationInfos[formationIndex];
			}
			return null;
		}
		if (_navalBattleFormationInfos.Count > formationIndex)
		{
			return _navalBattleFormationInfos[formationIndex];
		}
		return null;
	}

	public void SetFormationInfos(List<NavalOrderOfBattleFormationData> formationInfos, bool isInArmy)
	{
		if (isInArmy)
		{
			_navalBattleArmyFormationInfos = new List<NavalOrderOfBattleFormationData>(formationInfos);
		}
		else
		{
			_navalBattleFormationInfos = new List<NavalOrderOfBattleFormationData>(formationInfos);
		}
	}

	private void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		for (int num = _navalBattleFormationInfos.Count - 1; num >= 0; num--)
		{
			if (_navalBattleFormationInfos[num].Ship == ship)
			{
				_navalBattleFormationInfos.RemoveAt(num);
			}
		}
		for (int num2 = _navalBattleArmyFormationInfos.Count - 1; num2 >= 0; num2--)
		{
			if (_navalBattleArmyFormationInfos[num2].Ship == ship)
			{
				_navalBattleArmyFormationInfos.RemoveAt(num2);
			}
		}
	}

	private void OnHeroUnregistered(Hero hero)
	{
		for (int num = _navalBattleFormationInfos.Count - 1; num >= 0; num--)
		{
			NavalOrderOfBattleFormationData navalOrderOfBattleFormationData = _navalBattleFormationInfos[num];
			if (navalOrderOfBattleFormationData.Captain == hero)
			{
				_navalBattleFormationInfos[num] = new NavalOrderOfBattleFormationData(null, navalOrderOfBattleFormationData.Ship, navalOrderOfBattleFormationData.FormationClass, navalOrderOfBattleFormationData.Filters);
			}
		}
		for (int num2 = _navalBattleArmyFormationInfos.Count - 1; num2 >= 0; num2--)
		{
			NavalOrderOfBattleFormationData navalOrderOfBattleFormationData2 = _navalBattleArmyFormationInfos[num2];
			if (navalOrderOfBattleFormationData2.Captain == hero)
			{
				_navalBattleArmyFormationInfos[num2] = new NavalOrderOfBattleFormationData(null, navalOrderOfBattleFormationData2.Ship, navalOrderOfBattleFormationData2.FormationClass, navalOrderOfBattleFormationData2.Filters);
			}
		}
	}
}
