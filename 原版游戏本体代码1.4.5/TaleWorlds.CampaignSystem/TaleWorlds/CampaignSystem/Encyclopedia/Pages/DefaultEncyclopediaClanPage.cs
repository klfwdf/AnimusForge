using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(Clan) })]
public class DefaultEncyclopediaClanPage : EncyclopediaPage
{
	private class EncyclopediaListClanWealthComparer : EncyclopediaListClanComparer
	{
		private static Func<Clan, Clan, int> _comparison = (Clan c1, Clan c2) => c1.Gold.CompareTo(c2.Gold);

		private string GetClanWealthStatusText(Clan _clan)
		{
			string empty = string.Empty;
			if (_clan.Leader.Gold < 15000)
			{
				return new TextObject("{=SixPXaNh}Very Poor").ToString();
			}
			if (_clan.Leader.Gold < 45000)
			{
				return new TextObject("{=poorWealthStatus}Poor").ToString();
			}
			if (_clan.Leader.Gold < 135000)
			{
				return new TextObject("{=averageWealthStatus}Average").ToString();
			}
			if (_clan.Leader.Gold < 405000)
			{
				return new TextObject("{=UbRqC0Yz}Rich").ToString();
			}
			return new TextObject("{=oJmRg2ms}Very Rich").ToString();
		}

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareClans(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Clan clan)
			{
				return GetClanWealthStatusText(clan);
			}
			Debug.FailedAssert("Unable to get the gold of a non-clan object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaClanPage.cs", "GetComparedValueText", 157);
			return "";
		}
	}

	private class EncyclopediaListClanTierComparer : EncyclopediaListClanComparer
	{
		private static Func<Clan, Clan, int> _comparison = (Clan c1, Clan c2) => c1.Tier.CompareTo(c2.Tier);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareClans(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Clan { Tier: var tier })
			{
				return tier.ToString();
			}
			Debug.FailedAssert("Unable to get the tier of a non-clan object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaClanPage.cs", "GetComparedValueText", 178);
			return "";
		}
	}

	private class EncyclopediaListClanStrengthComparer : EncyclopediaListClanComparer
	{
		private static Func<Clan, Clan, int> _comparison = (Clan c1, Clan c2) => c1.CurrentTotalStrength.CompareTo(c2.CurrentTotalStrength);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareClans(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Clan clan)
			{
				return ((int)clan.CurrentTotalStrength).ToString();
			}
			Debug.FailedAssert("Unable to get the strength of a non-clan object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaClanPage.cs", "GetComparedValueText", 199);
			return "";
		}
	}

	private class EncyclopediaListClanFiefComparer : EncyclopediaListClanComparer
	{
		private static Func<Clan, Clan, int> _comparison = (Clan c1, Clan c2) => c1.Fiefs.Count.CompareTo(c2.Fiefs.Count);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareClans(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Clan clan)
			{
				return clan.Fiefs.Count.ToString();
			}
			Debug.FailedAssert("Unable to get the fief count of a non-clan object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaClanPage.cs", "GetComparedValueText", 220);
			return "";
		}
	}

	private class EncyclopediaListClanMemberComparer : EncyclopediaListClanComparer
	{
		private static Func<Clan, Clan, int> _comparison = (Clan c1, Clan c2) => c1.Heroes.Count.CompareTo(c2.Heroes.Count);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareClans(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Clan clan)
			{
				return clan.Heroes.Count.ToString();
			}
			Debug.FailedAssert("Unable to get members of a non-clan object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaClanPage.cs", "GetComparedValueText", 241);
			return "";
		}
	}

	public abstract class EncyclopediaListClanComparer : EncyclopediaListItemComparerBase
	{
		public int CompareClans(EncyclopediaListItem x, EncyclopediaListItem y, Func<Clan, Clan, int> comparison)
		{
			if (x.Object is Clan arg && y.Object is Clan arg2)
			{
				int num = comparison(arg, arg2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Both objects should be clans.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaClanPage.cs", "CompareClans", 256);
			return 0;
		}
	}

	public DefaultEncyclopediaClanPage()
	{
		base.HomePageOrderIndex = 500;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		foreach (Clan clan in Clan.NonBanditFactions)
		{
			if (IsValidEncyclopediaItem(clan))
			{
				yield return new EncyclopediaListItem(clan, clan.Name.ToString(), "", clan.StringId, GetIdentifier(typeof(Clan)), playerCanSeeValues: true, delegate
				{
					InformationManager.ShowTooltip(typeof(Clan), clan);
				});
			}
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
		List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
		list2.Add(new EncyclopediaFilterItem(new TextObject("{=QwpHoMJu}Minor"), (object f) => ((IFaction)f).IsMinorFaction));
		list.Add(new EncyclopediaFilterGroup(list2, new TextObject("{=zMMqgxb1}Type")));
		List<EncyclopediaFilterItem> list3 = new List<EncyclopediaFilterItem>();
		list3.Add(new EncyclopediaFilterItem(new TextObject("{=lEHjxPTs}Ally"), (object f) => DiplomacyHelper.IsSameFactionAndNotEliminated((IFaction)f, Hero.MainHero.MapFaction)));
		list3.Add(new EncyclopediaFilterItem(new TextObject("{=sPmQz21k}Enemy"), (object f) => FactionManager.IsAtWarAgainstFaction((IFaction)f, Hero.MainHero.MapFaction) && !((IFaction)f).IsBanditFaction));
		list3.Add(new EncyclopediaFilterItem(new TextObject("{=3PzgpFGq}Neutral"), (object f) => FactionManager.IsNeutralWithFaction((IFaction)f, Hero.MainHero.MapFaction)));
		list.Add(new EncyclopediaFilterGroup(list3, new TextObject("{=L7wn49Uz}Diplomacy")));
		List<EncyclopediaFilterItem> list4 = new List<EncyclopediaFilterItem>();
		list4.Add(new EncyclopediaFilterItem(new TextObject("{=SlubkZ1A}Eliminated"), (object f) => ((IFaction)f).IsEliminated));
		list4.Add(new EncyclopediaFilterItem(new TextObject("{=YRbSBxqT}Active"), (object f) => !((IFaction)f).IsEliminated));
		list.Add(new EncyclopediaFilterGroup(list4, new TextObject("{=DXczLzml}Status")));
		List<EncyclopediaFilterItem> list5 = new List<EncyclopediaFilterItem>();
		foreach (CultureObject culture in (from f in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
			where f.IsMainCulture
			orderby f.Name.ToString()
			select f).ToList())
		{
			if (culture.StringId != "neutral_culture" && !culture.IsBandit)
			{
				list5.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((IFaction)c).Culture == culture));
			}
		}
		list.Add(new EncyclopediaFilterGroup(list5, GameTexts.FindText("str_culture")));
		return list;
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(new TextObject("{=qtII2HbK}Wealth"), new EncyclopediaListClanWealthComparer()),
			new EncyclopediaSortController(new TextObject("{=cc1d7mkq}Tier"), new EncyclopediaListClanTierComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_strength"), new EncyclopediaListClanStrengthComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_fiefs"), new EncyclopediaListClanFiefComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_members"), new EncyclopediaListClanMemberComparer())
		};
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaClanPage";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_clans");
	}

	public override TextObject GetDescriptionText()
	{
		return GameTexts.FindText("str_clan_description");
	}

	public override string GetStringID()
	{
		return "EncyclopediaClan";
	}

	public override MBObjectBase GetObject(string typeName, string stringID)
	{
		return Campaign.Current.CampaignObjectManager.Find<Clan>(stringID);
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		return o is IFaction;
	}
}
