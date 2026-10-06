using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Helpers;

public static class BarterHelper
{
	private static bool ItemExistsInBarterables(List<Barterable> barterables, ItemBarterable itemBarterable)
	{
		return barterables.AnyQ((Barterable x) => x is ItemBarterable { ItemRosterElement: { EquipmentElement: var equipmentElement } } && equipmentElement.Item == itemBarterable.ItemRosterElement.EquipmentElement.Item);
	}

	public static IEnumerable<(Barterable barterable, int count)> GetAutoBalanceBarterablesAdd(BarterData barterData, IFaction factionToBalanceFor, IFaction offerer, Hero offererHero, float fulfillRatio = 1f)
	{
		List<Barterable> offeredBarterables = barterData.GetOfferedBarterables();
		List<Barterable> list = barterData.GetBarterables().WhereQ((Barterable x) => x.OriginalOwner == offererHero && (!(x is ItemBarterable itemBarterable) || !ItemExistsInBarterables(barterData.GetOfferedBarterables(), itemBarterable))).ToList();
		int num = 0;
		foreach (Barterable item in offeredBarterables)
		{
			num += item.GetValueForFaction(factionToBalanceFor);
		}
		List<(Barterable, int)> list2 = new List<(Barterable, int)>();
		int num2 = (int)((0f - fulfillRatio) * (float)num);
		bool flag = false;
		while (num2 > 0 && !flag)
		{
			float num3 = 0f;
			Barterable barterable = null;
			for (int num4 = 0; num4 < list.Count; num4++)
			{
				Barterable barterable2 = list[num4];
				float num5 = 0f;
				if (!barterable2.IsOffered || barterable2.CurrentAmount < barterable2.MaxAmount)
				{
					int unitValueForFaction = barterable2.GetUnitValueForFaction(factionToBalanceFor);
					int unitValueForFaction2 = barterable2.GetUnitValueForFaction(offerer);
					int num6 = barterable2.MaxAmount - barterable2.CurrentAmount;
					if (barterable2 is GoldBarterable && unitValueForFaction * num6 >= num2)
					{
						barterable = barterable2;
						break;
					}
					if (unitValueForFaction > 0)
					{
						if (unitValueForFaction2 >= 0)
						{
							num5 = 10000000f;
						}
						else
						{
							num5 = (float)(-unitValueForFaction) / (float)unitValueForFaction2;
							if (unitValueForFaction > num2)
							{
								num5 = (float)unitValueForFaction / (float)(-unitValueForFaction2 + (unitValueForFaction - num2));
							}
						}
					}
				}
				if (num5 > num3)
				{
					num3 = num5;
					barterable = barterable2;
				}
			}
			if (barterable == null)
			{
				flag = true;
				continue;
			}
			int unitValueForFaction3 = barterable.GetUnitValueForFaction(factionToBalanceFor);
			int num7 = barterable.MaxAmount;
			if (barterable.IsOffered)
			{
				num7 -= barterable.CurrentAmount;
			}
			int num8 = MathF.Min(MathF.Ceiling((float)num2 / (float)unitValueForFaction3), num7);
			list2.Add((barterable, num8));
			list.Remove(barterable);
			num2 -= num8 * unitValueForFaction3;
		}
		return list2;
	}

	public static IEnumerable<(Barterable barterable, int count)> GetAutoBalanceBarterablesToRemove(BarterData barterData, IFaction factionToBalanceFor, IFaction offerer, Hero offererHero)
	{
		List<Barterable> offeredBarterables = barterData.GetOfferedBarterables();
		int num = 0;
		foreach (Barterable item in offeredBarterables)
		{
			num += item.GetValueForFaction(factionToBalanceFor);
		}
		List<(Barterable, int)> list = new List<(Barterable, int)>();
		int num2 = num;
		bool flag = false;
		while (num2 > 0 && !flag)
		{
			float num3 = 0f;
			Barterable barterable = null;
			for (int i = 0; i < offeredBarterables.Count; i++)
			{
				Barterable barterable2 = offeredBarterables[i];
				float num4 = 0f;
				if (barterable2.CurrentAmount > 0)
				{
					int unitValueForFaction = barterable2.GetUnitValueForFaction(factionToBalanceFor);
					int unitValueForFaction2 = barterable2.GetUnitValueForFaction(offerer);
					if (unitValueForFaction > 0)
					{
						num4 = ((unitValueForFaction2 < 0) ? ((float)(-unitValueForFaction2) / (float)unitValueForFaction) : (-10000f));
					}
				}
				if (num4 > num3)
				{
					num3 = num4;
					barterable = barterable2;
				}
			}
			if (barterable == null)
			{
				flag = true;
				continue;
			}
			int unitValueForFaction3 = barterable.GetUnitValueForFaction(factionToBalanceFor);
			int currentAmount = barterable.CurrentAmount;
			int num5 = MathF.Min(MathF.Ceiling((float)num2 / (float)unitValueForFaction3), currentAmount);
			list.Add((barterable, num5));
			offeredBarterables.Remove(barterable);
			num2 -= num5 * unitValueForFaction3;
		}
		return list;
	}
}
