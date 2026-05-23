using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Helpers
{
	// Token: 0x0200001C RID: 28
	public static class BarterHelper
	{
		// Token: 0x060000FF RID: 255 RVA: 0x0000CCB8 File Offset: 0x0000AEB8
		private static bool ItemExistsInBarterables(List<Barterable> barterables, ItemBarterable itemBarterable)
		{
			return barterables.AnyQ(delegate(Barterable x)
			{
				ItemBarterable itemBarterable2;
				return (itemBarterable2 = x as ItemBarterable) != null && itemBarterable2.ItemRosterElement.EquipmentElement.Item == itemBarterable.ItemRosterElement.EquipmentElement.Item;
			});
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000CCE4 File Offset: 0x0000AEE4
		[return: TupleElementNames(new string[] { "barterable", "count" })]
		public static IEnumerable<ValueTuple<Barterable, int>> GetAutoBalanceBarterablesAdd(BarterData barterData, IFaction factionToBalanceFor, IFaction offerer, Hero offererHero, float fulfillRatio = 1f)
		{
			List<Barterable> offeredBarterables = barterData.GetOfferedBarterables();
			List<Barterable> list = barterData.GetBarterables().WhereQ(delegate(Barterable x)
			{
				ItemBarterable itemBarterable;
				return x.OriginalOwner == offererHero && ((itemBarterable = x as ItemBarterable) == null || !BarterHelper.ItemExistsInBarterables(barterData.GetOfferedBarterables(), itemBarterable));
			}).ToList<Barterable>();
			int num = 0;
			foreach (Barterable barterable in offeredBarterables)
			{
				num += barterable.GetValueForFaction(factionToBalanceFor);
			}
			List<ValueTuple<Barterable, int>> list2 = new List<ValueTuple<Barterable, int>>();
			int num2 = (int)(-fulfillRatio * (float)num);
			bool flag = false;
			while (num2 > 0 && !flag)
			{
				float num3 = 0f;
				Barterable barterable2 = null;
				for (int i = 0; i < list.Count; i++)
				{
					Barterable barterable3 = list[i];
					float num4 = 0f;
					if (!barterable3.IsOffered || barterable3.CurrentAmount < barterable3.MaxAmount)
					{
						int unitValueForFaction = barterable3.GetUnitValueForFaction(factionToBalanceFor);
						int unitValueForFaction2 = barterable3.GetUnitValueForFaction(offerer);
						int num5 = barterable3.MaxAmount - barterable3.CurrentAmount;
						if (barterable3 is GoldBarterable && unitValueForFaction * num5 >= num2)
						{
							barterable2 = barterable3;
							break;
						}
						if (unitValueForFaction > 0)
						{
							if (unitValueForFaction2 >= 0)
							{
								num4 = 10000000f;
							}
							else
							{
								num4 = (float)(-(float)unitValueForFaction) / (float)unitValueForFaction2;
								if (unitValueForFaction > num2)
								{
									num4 = (float)unitValueForFaction / (float)(-(float)unitValueForFaction2 + (unitValueForFaction - num2));
								}
							}
						}
					}
					if (num4 > num3)
					{
						num3 = num4;
						barterable2 = barterable3;
					}
				}
				if (barterable2 == null)
				{
					flag = true;
				}
				else
				{
					int unitValueForFaction3 = barterable2.GetUnitValueForFaction(factionToBalanceFor);
					int num6 = barterable2.MaxAmount;
					if (barterable2.IsOffered)
					{
						num6 -= barterable2.CurrentAmount;
					}
					int num7 = MathF.Min(MathF.Ceiling((float)num2 / (float)unitValueForFaction3), num6);
					list2.Add(new ValueTuple<Barterable, int>(barterable2, num7));
					list.Remove(barterable2);
					num2 -= num7 * unitValueForFaction3;
				}
			}
			return list2;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000CED4 File Offset: 0x0000B0D4
		[return: TupleElementNames(new string[] { "barterable", "count" })]
		public static IEnumerable<ValueTuple<Barterable, int>> GetAutoBalanceBarterablesToRemove(BarterData barterData, IFaction factionToBalanceFor, IFaction offerer, Hero offererHero)
		{
			List<Barterable> offeredBarterables = barterData.GetOfferedBarterables();
			int num = 0;
			foreach (Barterable barterable in offeredBarterables)
			{
				num += barterable.GetValueForFaction(factionToBalanceFor);
			}
			List<ValueTuple<Barterable, int>> list = new List<ValueTuple<Barterable, int>>();
			int num2 = num;
			bool flag = false;
			while (num2 > 0 && !flag)
			{
				float num3 = 0f;
				Barterable barterable2 = null;
				for (int i = 0; i < offeredBarterables.Count; i++)
				{
					Barterable barterable3 = offeredBarterables[i];
					float num4 = 0f;
					if (barterable3.CurrentAmount > 0)
					{
						int unitValueForFaction = barterable3.GetUnitValueForFaction(factionToBalanceFor);
						int unitValueForFaction2 = barterable3.GetUnitValueForFaction(offerer);
						if (unitValueForFaction > 0)
						{
							if (unitValueForFaction2 >= 0)
							{
								num4 = -10000f;
							}
							else
							{
								num4 = (float)(-(float)unitValueForFaction2) / (float)unitValueForFaction;
							}
						}
					}
					if (num4 > num3)
					{
						num3 = num4;
						barterable2 = barterable3;
					}
				}
				if (barterable2 == null)
				{
					flag = true;
				}
				else
				{
					int unitValueForFaction3 = barterable2.GetUnitValueForFaction(factionToBalanceFor);
					int currentAmount = barterable2.CurrentAmount;
					int num5 = MathF.Min(MathF.Ceiling((float)num2 / (float)unitValueForFaction3), currentAmount);
					list.Add(new ValueTuple<Barterable, int>(barterable2, num5));
					offeredBarterables.Remove(barterable2);
					num2 -= num5 * unitValueForFaction3;
				}
			}
			return list;
		}
	}
}
