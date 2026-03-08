using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200015F RID: 351
	public class DefaultTroopSupplierProbabilityModel : TroopSupplierProbabilityModel
	{
		// Token: 0x06001ABE RID: 6846 RVA: 0x0008929C File Offset: 0x0008749C
		public override void EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(MapEventParty battleParty, FlattenedTroopRoster priorityTroops, bool includePlayer, int sizeOfSide, bool forcePriorityTroops, List<ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>> priorityList)
		{
			UnitSpawnPrioritizations unitSpawnPrioritizations = UnitSpawnPrioritizations.HighLevel;
			MapEvent battle = PlayerEncounter.Battle;
			bool flag = battle != null && battle.IsSiegeAmbush;
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
				int num = 0;
				foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in battleParty.Troops)
				{
					if (this.CanTroopJoinBattle(flattenedTroopRosterElement, includePlayer))
					{
						int key = 0;
						switch (unitSpawnPrioritizations)
						{
						case UnitSpawnPrioritizations.Default:
							key = num;
							break;
						case UnitSpawnPrioritizations.HighLevel:
							key = flattenedTroopRosterElement.Troop.Level;
							break;
						case UnitSpawnPrioritizations.LowLevel:
							key = -flattenedTroopRosterElement.Troop.Level;
							break;
						}
						bool isHero = flattenedTroopRosterElement.Troop.IsHero;
						if (isHero && flattenedTroopRosterElement.Troop.IsPlayerCharacter)
						{
							key = int.MaxValue;
						}
						bool flag2 = false;
						if (priorityTroops != null)
						{
							foreach (FlattenedTroopRosterElement flattenedTroopRosterElement2 in priorityTroops)
							{
								if (flattenedTroopRosterElement2.Troop == flattenedTroopRosterElement.Troop)
								{
									flag2 = true;
									break;
								}
							}
						}
						if (flag2)
						{
							if (isHero)
							{
								list3.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, flattenedTroopRosterElement));
							}
							else
							{
								list4.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, flattenedTroopRosterElement));
							}
						}
						else if (isHero)
						{
							list2.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, flattenedTroopRosterElement));
						}
						else
						{
							list.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, flattenedTroopRosterElement));
						}
					}
					num++;
				}
				list3 = list3.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList<KeyValuePair<int, FlattenedTroopRosterElement>>();
				list4 = list4.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList<KeyValuePair<int, FlattenedTroopRosterElement>>();
				list2 = list2.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList<KeyValuePair<int, FlattenedTroopRosterElement>>();
				list = list.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList<KeyValuePair<int, FlattenedTroopRosterElement>>();
				for (int i = 0; i < list3.Count; i++)
				{
					priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(list3[i].Value, battleParty, 3f + (float)(i + 1) / (float)list3.Count));
				}
				for (int j = 0; j < list4.Count; j++)
				{
					priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(list4[j].Value, battleParty, 2f + (float)(j + 1) / (float)list4.Count));
				}
				for (int k = 0; k < list2.Count; k++)
				{
					priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(list2[k].Value, battleParty, 1f + (float)(k + 1) / (float)list2.Count));
				}
				for (int l = 0; l < list.Count; l++)
				{
					priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(list[l].Value, battleParty, (float)(l + 1) / (float)list.Count));
				}
				return;
			}
			foreach (FlattenedTroopRosterElement flattenedTroopRosterElement3 in battleParty.Troops)
			{
				if (this.CanTroopJoinBattle(flattenedTroopRosterElement3, includePlayer))
				{
					priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(flattenedTroopRosterElement3, battleParty, 2.1474836E+09f));
				}
			}
		}

		// Token: 0x06001ABF RID: 6847 RVA: 0x000896A0 File Offset: 0x000878A0
		private bool CanTroopJoinBattle(FlattenedTroopRosterElement troopRoster, bool includePlayer)
		{
			return !troopRoster.IsWounded && !troopRoster.IsRouted && !troopRoster.IsKilled && (includePlayer || !troopRoster.Troop.IsPlayerCharacter);
		}
	}
}
