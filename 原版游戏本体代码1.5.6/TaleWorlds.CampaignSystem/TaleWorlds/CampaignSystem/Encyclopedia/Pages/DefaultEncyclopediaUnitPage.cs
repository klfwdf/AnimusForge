using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(CharacterObject) })]
public class DefaultEncyclopediaUnitPage : EncyclopediaPage
{
	private class EncyclopediaListUnitTierComparer : EncyclopediaListUnitComparer
	{
		private static Func<CharacterObject, CharacterObject, int> _comparison = (CharacterObject c1, CharacterObject c2) => c1.Tier.CompareTo(c2.Tier);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareUnits(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is CharacterObject { Tier: var tier })
			{
				return tier.ToString();
			}
			Debug.FailedAssert("Unable to get the tier of a non-character object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaUnitPage.cs", "GetComparedValueText", 175);
			return "";
		}
	}

	private class EncyclopediaListUnitLevelComparer : EncyclopediaListUnitComparer
	{
		private static Func<CharacterObject, CharacterObject, int> _comparison = (CharacterObject c1, CharacterObject c2) => c1.Level.CompareTo(c2.Level);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareUnits(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is CharacterObject { Level: var level })
			{
				return level.ToString();
			}
			Debug.FailedAssert("Unable to get the level of a non-character object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaUnitPage.cs", "GetComparedValueText", 196);
			return "";
		}
	}

	public abstract class EncyclopediaListUnitComparer : EncyclopediaListItemComparerBase
	{
		public int CompareUnits(EncyclopediaListItem x, EncyclopediaListItem y, Func<CharacterObject, CharacterObject, int> comparison)
		{
			if (x.Object is CharacterObject arg && y.Object is CharacterObject arg2)
			{
				int num = comparison(arg, arg2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Both objects should be character objects.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaUnitPage.cs", "CompareUnits", 211);
			return 0;
		}
	}

	public DefaultEncyclopediaUnitPage()
	{
		base.HomePageOrderIndex = 300;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		foreach (CharacterObject character in CharacterObject.All)
		{
			if (IsValidEncyclopediaItem(character))
			{
				yield return new EncyclopediaListItem(character, character.Name.ToString(), "", character.StringId, GetIdentifier(typeof(CharacterObject)), playerCanSeeValues: true, delegate
				{
					InformationManager.ShowTooltip(typeof(CharacterObject), character);
				});
			}
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
		List<EncyclopediaFilterItem> typeFilterItems = GetTypeFilterItems();
		list.Add(new EncyclopediaFilterGroup(typeFilterItems, new TextObject("{=zMMqgxb1}Type")));
		List<EncyclopediaFilterItem> occupationFilterItems = GetOccupationFilterItems();
		list.Add(new EncyclopediaFilterGroup(occupationFilterItems, new TextObject("{=GZxFIeiJ}Occupation")));
		List<EncyclopediaFilterItem> cultureFilterItems = GetCultureFilterItems();
		list.Add(new EncyclopediaFilterGroup(cultureFilterItems, GameTexts.FindText("str_culture")));
		List<EncyclopediaFilterItem> outlawFilterItems = GetOutlawFilterItems();
		list.Add(new EncyclopediaFilterGroup(outlawFilterItems, GameTexts.FindText("str_outlaw")));
		return list;
	}

	protected virtual List<EncyclopediaFilterItem> GetTypeFilterItems()
	{
		return new List<EncyclopediaFilterItem>
		{
			new EncyclopediaFilterItem(new TextObject("{=1Bm1Wk1v}Infantry"), (object s) => ((CharacterObject)s).IsInfantry),
			new EncyclopediaFilterItem(new TextObject("{=bIiBytSB}Archers"), (object s) => ((CharacterObject)s).IsRanged && !((CharacterObject)s).IsMounted),
			new EncyclopediaFilterItem(new TextObject("{=YVGtcLHF}Cavalry"), (object s) => ((CharacterObject)s).IsMounted && !((CharacterObject)s).IsRanged),
			new EncyclopediaFilterItem(new TextObject("{=I1CMeL9R}Mounted Archers"), (object s) => ((CharacterObject)s).IsRanged && ((CharacterObject)s).IsMounted)
		};
	}

	protected virtual List<EncyclopediaFilterItem> GetOccupationFilterItems()
	{
		return new List<EncyclopediaFilterItem>
		{
			new EncyclopediaFilterItem(GameTexts.FindText("str_occupation", "Soldier"), (object s) => ((CharacterObject)s).Occupation == Occupation.Soldier),
			new EncyclopediaFilterItem(GameTexts.FindText("str_occupation", "Mercenary"), (object s) => ((CharacterObject)s).Occupation == Occupation.Mercenary),
			new EncyclopediaFilterItem(GameTexts.FindText("str_occupation", "Bandit"), (object s) => ((CharacterObject)s).Occupation == Occupation.Bandit)
		};
	}

	protected virtual List<EncyclopediaFilterItem> GetCultureFilterItems()
	{
		List<EncyclopediaFilterItem> list = new List<EncyclopediaFilterItem>();
		foreach (CultureObject culture in (from f in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
			where f.IsMainCulture
			orderby f.Name.ToString()
			select f).ToList())
		{
			if (!culture.IsBandit && culture.StringId != "neutral_culture")
			{
				list.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((CharacterObject)c).Culture == culture));
			}
		}
		return list;
	}

	protected virtual List<EncyclopediaFilterItem> GetOutlawFilterItems()
	{
		List<EncyclopediaFilterItem> list = new List<EncyclopediaFilterItem>();
		foreach (CultureObject culture in (from x in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
			orderby !x.IsMainCulture descending
			select x).ThenBy((CultureObject f) => f.Name.ToString()).ToList())
		{
			if (culture.IsBandit)
			{
				list.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((CharacterObject)c).Culture == culture));
			}
		}
		return list;
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(new TextObject("{=cc1d7mkq}Tier"), new EncyclopediaListUnitTierComparer()),
			new EncyclopediaSortController(GameTexts.FindText("str_level_tag"), new EncyclopediaListUnitLevelComparer())
		};
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaUnitPage";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_encyclopedia_troops");
	}

	public override TextObject GetDescriptionText()
	{
		return GameTexts.FindText("str_unit_description");
	}

	public override string GetStringID()
	{
		return "EncyclopediaUnit";
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		if (o is CharacterObject { IsTemplate: false } characterObject && characterObject != null && !characterObject.HiddenInEncyclopedia && characterObject?.HeroObject == null)
		{
			if (characterObject.Occupation != Occupation.Soldier && characterObject.Occupation != Occupation.Mercenary && characterObject.Occupation != Occupation.Bandit && characterObject.Occupation != Occupation.Gangster && characterObject.Occupation != Occupation.CaravanGuard)
			{
				if (characterObject.Occupation == Occupation.Villager)
				{
					return characterObject.UpgradeTargets.Length != 0;
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
