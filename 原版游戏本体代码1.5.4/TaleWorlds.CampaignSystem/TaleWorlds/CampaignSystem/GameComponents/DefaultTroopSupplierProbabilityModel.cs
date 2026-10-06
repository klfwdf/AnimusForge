using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTroopSupplierProbabilityModel : TroopSupplierProbabilityModel
{
	public override void EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(MapEventParty battleParty, FlattenedTroopRoster priorityTroops, bool includePlayer, int sizeOfSide, bool forcePriorityTroops, List<(FlattenedTroopRosterElement, MapEventParty, float)> priorityList)
	{
		UnitSpawnPrioritizations unitSpawnPrioritizations = UnitSpawnPrioritizations.HighLevel;
		bool flag = PlayerEncounter.Battle?.IsSiegeAmbush ?? false;
		if (battleParty.Party == PartyBase.MainParty && !flag)
		{
			unitSpawnPrioritizations = Game.Current.UnitSpawnPrioritization;
		}
		if (includePlayer)
		{
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list2 = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list3 = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list4 = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			int[] array = new int[8];
			for (int i = 0; i < 8; i++)
			{
				array[i] = 0;
			}
			int num = 0;
			foreach (FlattenedTroopRosterElement troop in battleParty.Troops)
			{
				if (CanTroopJoinBattle(troop, includePlayer))
				{
					int key = 0;
					switch (unitSpawnPrioritizations)
					{
					case UnitSpawnPrioritizations.Default:
						key = -num;
						break;
					case UnitSpawnPrioritizations.HighLevel:
						key = troop.Troop.Level;
						break;
					case UnitSpawnPrioritizations.LowLevel:
						key = -troop.Troop.Level;
						break;
					case UnitSpawnPrioritizations.Homogeneous:
					{
						int formationClass = (int)troop.Troop.GetFormationClass();
						key = -array[formationClass];
						array[formationClass]++;
						break;
					}
					}
					bool isHero = troop.Troop.IsHero;
					if (isHero && troop.Troop.IsPlayerCharacter)
					{
						key = int.MaxValue;
					}
					bool flag2 = false;
					if (priorityTroops != null)
					{
						foreach (FlattenedTroopRosterElement priorityTroop in priorityTroops)
						{
							if (priorityTroop.Troop == troop.Troop)
							{
								flag2 = true;
								break;
							}
						}
					}
					if (flag2)
					{
						priorityTroops.Remove(priorityTroops.FindIndexOfCharacter(troop.Troop));
						if (isHero)
						{
							list3.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
						}
						else
						{
							list4.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
						}
					}
					else if (isHero)
					{
						list2.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
					}
					else
					{
						list.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
					}
				}
				num++;
			}
			if (unitSpawnPrioritizations == UnitSpawnPrioritizations.Homogeneous)
			{
				for (int j = 0; j < list.Count; j++)
				{
					KeyValuePair<int, FlattenedTroopRosterElement> keyValuePair = list[j];
					int formationClass2 = (int)keyValuePair.Value.Troop.GetFormationClass();
					int key2 = keyValuePair.Key * list.Count / array[formationClass2];
					list[j] = new KeyValuePair<int, FlattenedTroopRosterElement>(key2, keyValuePair.Value);
				}
			}
			list3 = list3.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			list4 = list4.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			list2 = list2.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			list = list.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			for (int num2 = 0; num2 < list3.Count; num2++)
			{
				priorityList.Add((list3[num2].Value, battleParty, 3f + (float)(num2 + 1) / (float)list3.Count));
			}
			for (int num3 = 0; num3 < list4.Count; num3++)
			{
				priorityList.Add((list4[num3].Value, battleParty, 2f + (float)(num3 + 1) / (float)list4.Count));
			}
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				priorityList.Add((list2[num4].Value, battleParty, 1f + (float)(num4 + 1) / (float)list2.Count));
			}
			for (int num5 = 0; num5 < list.Count; num5++)
			{
				priorityList.Add((list[num5].Value, battleParty, (float)(num5 + 1) / (float)list.Count));
			}
			return;
		}
		foreach (FlattenedTroopRosterElement troop2 in battleParty.Troops)
		{
			if (CanTroopJoinBattle(troop2, includePlayer))
			{
				priorityList.Add((troop2, battleParty, 2.1474836E+09f));
			}
		}
	}

	private bool CanTroopJoinBattle(FlattenedTroopRosterElement troopRoster, bool includePlayer)
	{
		if (!troopRoster.IsWounded && !troopRoster.IsRouted && !troopRoster.IsKilled)
		{
			if (!includePlayer)
			{
				return !troopRoster.Troop.IsPlayerCharacter;
			}
			return true;
		}
		return false;
	}
}
