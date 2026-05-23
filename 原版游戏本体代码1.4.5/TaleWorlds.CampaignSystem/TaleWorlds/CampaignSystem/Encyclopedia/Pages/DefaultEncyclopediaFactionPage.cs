using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(Kingdom) })]
public class DefaultEncyclopediaFactionPage : EncyclopediaPage
{
	private class EncyclopediaListKingdomTotalStrengthComparer : EncyclopediaListKingdomComparer
	{
		private static Func<Kingdom, Kingdom, int> _comparison = (Kingdom k1, Kingdom k2) => k1.CurrentTotalStrength.CompareTo(k2.CurrentTotalStrength);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareKingdoms(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Kingdom kingdom)
			{
				return ((int)kingdom.CurrentTotalStrength).ToString();
			}
			Debug.FailedAssert("Unable to get the total strength of a non-kingdom object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "GetComparedValueText", 107);
			return "";
		}
	}

	private class EncyclopediaListKingdomFiefsComparer : EncyclopediaListKingdomComparer
	{
		private static Func<Kingdom, Kingdom, int> _comparison = (Kingdom k1, Kingdom k2) => k1.Fiefs.Count.CompareTo(k2.Fiefs.Count);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareKingdoms(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Kingdom kingdom)
			{
				return kingdom.Fiefs.Count.ToString();
			}
			Debug.FailedAssert("Unable to get the fief count from a non-kingdom object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "GetComparedValueText", 128);
			return "";
		}
	}

	private class EncyclopediaListKingdomClanComparer : EncyclopediaListKingdomComparer
	{
		private static Func<Kingdom, Kingdom, int> _comparison = (Kingdom k1, Kingdom k2) => k1.Clans.Count.CompareTo(k2.Clans.Count);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareKingdoms(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Kingdom kingdom)
			{
				return kingdom.Clans.Count.ToString();
			}
			Debug.FailedAssert("Unable to get the clan count from a non-kingdom object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "GetComparedValueText", 149);
			return "";
		}
	}

	public abstract class EncyclopediaListKingdomComparer : EncyclopediaListItemComparerBase
	{
		public int CompareKingdoms(EncyclopediaListItem x, EncyclopediaListItem y, Func<Kingdom, Kingdom, int> comparison)
		{
			if (x.Object is Kingdom arg && y.Object is Kingdom arg2)
			{
				int num = comparison(arg, arg2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Both objects should be kingdoms.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "CompareKingdoms", 164);
			return 0;
		}
	}

	public DefaultEncyclopediaFactionPage()
	{
		base.HomePageOrderIndex = 400;
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaFactionPage";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_kingdoms_group");
	}

	public override TextObject GetDescriptionText()
	{
		return GameTexts.FindText("str_faction_description");
	}

	public override string GetStringID()
	{
		return "EncyclopediaKingdom";
	}

	public override MBObjectBase GetObject(string typeName, string stringID)
	{
		return Campaign.Current.CampaignObjectManager.Find<Kingdom>(stringID);
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		if (o is IFaction faction)
		{
			return !faction.IsBanditFaction;
		}
		return false;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		foreach (Kingdom kingdom in Kingdom.All)
		{
			if (IsValidEncyclopediaItem(kingdom))
			{
				yield return new EncyclopediaListItem(kingdom, kingdom.Name.ToString(), "", kingdom.StringId, GetIdentifier(typeof(Kingdom)), playerCanSeeValues: true, delegate
				{
					InformationManager.ShowTooltip(typeof(Kingdom), kingdom);
				});
			}
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
		List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
		list.Add(new EncyclopediaFilterGroup(new List<EncyclopediaFilterItem>
		{
			new EncyclopediaFilterItem(new TextObject("{=lEHjxPTs}Ally"), (object f) => DiplomacyHelper.IsSameFactionAndNotEliminated((IFaction)f, Hero.MainHero.MapFaction)),
			new EncyclopediaFilterItem(new TextObject("{=sPmQz21k}Enemy"), (object f) => FactionManager.IsAtWarAgainstFaction((IFaction)f, Hero.MainHero.MapFaction) && !((IFaction)f).IsBanditFaction),
			new EncyclopediaFilterItem(new TextObject("{=3PzgpFGq}Neutral"), (object f) => FactionManager.IsNeutralWithFaction((IFaction)f, Hero.MainHero.MapFaction))
		}, new TextObject("{=L7wn49Uz}Diplomacy")));
		return list;
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(GameTexts.FindText("str_total_strength"), new EncyclopediaListKingdomTotalStrengthComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_fiefs"), new EncyclopediaListKingdomFiefsComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_clans"), new EncyclopediaListKingdomClanComparer())
		};
	}
}
