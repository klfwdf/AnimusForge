using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(Settlement) })]
public class DefaultEncyclopediaSettlementPage : EncyclopediaPage
{
	private class EncyclopediaListSettlementGarrisonComparer : EncyclopediaListSettlementComparer
	{
		private static int GarrisonComparison(Town t1, Town t2)
		{
			int num = ((t1.GarrisonParty != null) ? t1.GarrisonParty.MemberRoster.TotalManCount : 0);
			int value = ((t2.GarrisonParty != null) ? t2.GarrisonParty.MemberRoster.TotalManCount : 0);
			return num.CompareTo(value);
		}

		protected override bool CompareVisibility(Settlement s1, Settlement s2, out int comparisonResult)
		{
			if (s1.IsTown && s2.IsTown)
			{
				if (s1.Town.GarrisonParty == null && s2.Town.GarrisonParty == null)
				{
					comparisonResult = 0;
					return true;
				}
				if (s1.Town.GarrisonParty == null)
				{
					comparisonResult = (base.IsAscending ? 2 : (-2));
					return true;
				}
				if (s2.Town.GarrisonParty == null)
				{
					comparisonResult = (base.IsAscending ? (-2) : 2);
					return true;
				}
			}
			return base.CompareVisibility(s1, s2, out comparisonResult);
		}

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareFiefs(x, y, CompareVisibility, GarrisonComparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Settlement settlement)
			{
				if (settlement.IsVillage)
				{
					return _emptyValue.ToString();
				}
				if (!CanPlayerSeeValuesOf(settlement))
				{
					return _missingValue.ToString();
				}
				return settlement.Town.GarrisonParty?.MemberRoster.TotalManCount.ToString() ?? 0.ToString();
			}
			Debug.FailedAssert("Unable to get the garrison of a non-settlement object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "GetComparedValueText", 163);
			return "";
		}
	}

	private class EncyclopediaListSettlementFoodComparer : EncyclopediaListSettlementComparer
	{
		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareFiefs(x, y, CompareVisibility, FoodComparison);
		}

		private static int FoodComparison(Town t1, Town t2)
		{
			return t1.FoodStocks.CompareTo(t2.FoodStocks);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Settlement settlement)
			{
				if (settlement.IsVillage)
				{
					return _emptyValue.ToString();
				}
				if (!CanPlayerSeeValuesOf(settlement))
				{
					return _missingValue.ToString();
				}
				return ((int)settlement.Town.FoodStocks).ToString();
			}
			Debug.FailedAssert("Unable to get the food stocks of a non-settlement object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "GetComparedValueText", 198);
			return "";
		}
	}

	private class EncyclopediaListSettlementSecurityComparer : EncyclopediaListSettlementComparer
	{
		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareFiefs(x, y, CompareVisibility, SecurityComparison);
		}

		private static int SecurityComparison(Town t1, Town t2)
		{
			return t1.Security.CompareTo(t2.Security);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Settlement settlement)
			{
				if (settlement.IsVillage)
				{
					return _emptyValue.ToString();
				}
				if (!CanPlayerSeeValuesOf(settlement))
				{
					return _missingValue.ToString();
				}
				return ((int)settlement.Town.Security).ToString();
			}
			Debug.FailedAssert("Unable to get the security of a non-settlement object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "GetComparedValueText", 233);
			return "";
		}
	}

	private class EncyclopediaListSettlementLoyaltyComparer : EncyclopediaListSettlementComparer
	{
		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareFiefs(x, y, CompareVisibility, LoyaltyComparison);
		}

		private static int LoyaltyComparison(Town t1, Town t2)
		{
			return t1.Loyalty.CompareTo(t2.Loyalty);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Settlement settlement)
			{
				if (settlement.IsVillage)
				{
					return _emptyValue.ToString();
				}
				if (!CanPlayerSeeValuesOf(settlement))
				{
					return _missingValue.ToString();
				}
				return ((int)settlement.Town.Loyalty).ToString();
			}
			Debug.FailedAssert("Unable to get the loyalty of a non-settlement object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "GetComparedValueText", 268);
			return "";
		}
	}

	private class EncyclopediaListSettlementMilitiaComparer : EncyclopediaListSettlementComparer
	{
		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareSettlements(x, y, CompareVisibility, MilitiaComparison);
		}

		private static int MilitiaComparison(Settlement t1, Settlement t2)
		{
			return t1.Militia.CompareTo(t2.Militia);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Settlement settlement)
			{
				if (!CanPlayerSeeValuesOf(settlement))
				{
					return _missingValue.ToString();
				}
				return ((int)settlement.Militia).ToString();
			}
			Debug.FailedAssert("Unable to get the militia of a non-settlement object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "GetComparedValueText", 299);
			return "";
		}
	}

	private class EncyclopediaListSettlementProsperityComparer : EncyclopediaListSettlementComparer
	{
		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareFiefs(x, y, CompareVisibility, ProsperityComparison);
		}

		private static int ProsperityComparison(Town t1, Town t2)
		{
			return t1.Prosperity.CompareTo(t2.Prosperity);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Settlement settlement)
			{
				if (settlement.IsVillage)
				{
					return _emptyValue.ToString();
				}
				if (!CanPlayerSeeValuesOf(settlement))
				{
					return _missingValue.ToString();
				}
				return ((int)settlement.Town.Prosperity).ToString();
			}
			Debug.FailedAssert("Unable to get the prosperity of a non-settlement object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "GetComparedValueText", 334);
			return "";
		}
	}

	public abstract class EncyclopediaListSettlementComparer : EncyclopediaListItemComparerBase
	{
		protected delegate bool SettlementVisibilityComparerDelegate(Settlement s1, Settlement s2, out int comparisonResult);

		protected virtual bool CompareVisibility(Settlement s1, Settlement s2, out int comparisonResult)
		{
			bool flag = CanPlayerSeeValuesOf(s1);
			bool flag2 = CanPlayerSeeValuesOf(s2);
			if (!flag && !flag2)
			{
				comparisonResult = 0;
				return true;
			}
			if (!flag)
			{
				comparisonResult = (base.IsAscending ? 1 : (-1));
				return true;
			}
			if (!flag2)
			{
				comparisonResult = ((!base.IsAscending) ? 1 : (-1));
				return true;
			}
			comparisonResult = 0;
			return false;
		}

		protected int CompareSettlements(EncyclopediaListItem x, EncyclopediaListItem y, SettlementVisibilityComparerDelegate visibilityComparison, Func<Settlement, Settlement, int> comparison)
		{
			if (x.Object is Settlement settlement && y.Object is Settlement settlement2)
			{
				if (visibilityComparison(settlement, settlement2, out var comparisonResult))
				{
					if (comparisonResult == 0)
					{
						return ResolveEquality(x, y);
					}
					return comparisonResult * (base.IsAscending ? 1 : (-1));
				}
				int num = comparison(settlement, settlement2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Both objects should be settlements.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "CompareSettlements", 383);
			return 0;
		}

		protected int CompareFiefs(EncyclopediaListItem x, EncyclopediaListItem y, SettlementVisibilityComparerDelegate visibilityComparison, Func<Town, Town, int> comparison)
		{
			if (x.Object is Settlement settlement && y.Object is Settlement settlement2)
			{
				int num = settlement.IsVillage.CompareTo(settlement2.IsVillage);
				if (num != 0)
				{
					return num;
				}
				if (settlement.IsVillage && settlement2.IsVillage)
				{
					return ResolveEquality(x, y);
				}
				if (visibilityComparison(settlement, settlement2, out var comparisonResult))
				{
					if (comparisonResult == 0)
					{
						return ResolveEquality(x, y);
					}
					return comparisonResult * (base.IsAscending ? 1 : (-1));
				}
				num = comparison(settlement.Town, settlement2.Town) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Unable to compare loyalty of non-fief (castle or town) objects.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaSettlementPage.cs", "CompareFiefs", 407);
			return 0;
		}
	}

	public DefaultEncyclopediaSettlementPage()
	{
		base.HomePageOrderIndex = 100;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		foreach (Settlement settlement in Settlement.All)
		{
			if (IsValidEncyclopediaItem(settlement))
			{
				yield return new EncyclopediaListItem(settlement, settlement.Name.ToString(), "", settlement.StringId, GetIdentifier(typeof(Settlement)), CanPlayerSeeValuesOf(settlement), delegate
				{
					InformationManager.ShowTooltip(typeof(Settlement), settlement);
				});
			}
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
		List<EncyclopediaFilterItem> filters = new List<EncyclopediaFilterItem>
		{
			new EncyclopediaFilterItem(new TextObject("{=bOTQ7Pta}Town"), (object s) => ((Settlement)s).IsTown),
			new EncyclopediaFilterItem(new TextObject("{=sVXa3zFx}Castle"), (object s) => ((Settlement)s).IsCastle),
			new EncyclopediaFilterItem(new TextObject("{=Ua6CNLBZ}Village"), (object s) => ((Settlement)s).IsVillage)
		};
		list.Add(new EncyclopediaFilterGroup(filters, new TextObject("{=zMMqgxb1}Type")));
		List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
		foreach (CultureObject culture in (from f in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
			where f.IsMainCulture
			orderby f.Name.ToString()
			select f).ToList())
		{
			if (culture.StringId != "neutral_culture" && culture.CanHaveSettlement)
			{
				list2.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((Settlement)c).Culture == culture));
			}
		}
		list.Add(new EncyclopediaFilterGroup(list2, GameTexts.FindText("str_culture")));
		return list;
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(GameTexts.FindText("str_garrison"), new EncyclopediaListSettlementGarrisonComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_food"), new EncyclopediaListSettlementFoodComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_security"), new EncyclopediaListSettlementSecurityComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_loyalty"), new EncyclopediaListSettlementLoyaltyComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_militia"), new EncyclopediaListSettlementMilitiaComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_prosperity"), new EncyclopediaListSettlementProsperityComparer())
		};
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaSettlementPage";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_settlements");
	}

	public override TextObject GetDescriptionText()
	{
		return GameTexts.FindText("str_settlement_description");
	}

	public override string GetStringID()
	{
		return "EncyclopediaSettlement";
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		if (o is Settlement settlement)
		{
			if (!settlement.IsFortification)
			{
				return settlement.IsVillage;
			}
			return true;
		}
		return false;
	}

	private static bool CanPlayerSeeValuesOf(Settlement settlement)
	{
		return Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(settlement);
	}
}
