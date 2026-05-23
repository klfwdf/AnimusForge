using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(Hero) })]
public class DefaultEncyclopediaHeroPage : EncyclopediaPage
{
	private class EncyclopediaListHeroAgeComparer : EncyclopediaListHeroComparer
	{
		private static Func<Hero, Hero, int> _comparison = (Hero h1, Hero h2) => h1.Age.CompareTo(h2.Age);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareHeroes(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Hero hero)
			{
				if (!CanPlayerSeeValuesOf(hero))
				{
					return _missingValue.ToString();
				}
				return ((int)hero.Age).ToString();
			}
			Debug.FailedAssert("Unable to get the age of a non-hero object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaHeroPage.cs", "GetComparedValueText", 179);
			return "";
		}
	}

	private class EncyclopediaListHeroRelationComparer : EncyclopediaListHeroComparer
	{
		private static Func<Hero, Hero, int> _comparison = (Hero h1, Hero h2) => h1.GetRelationWithPlayer().CompareTo(h2.GetRelationWithPlayer());

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareHeroes(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is Hero hero)
			{
				if (!CanPlayerSeeValuesOf(hero))
				{
					return _missingValue.ToString();
				}
				int num = (int)hero.GetRelationWithPlayer();
				MBTextManager.SetTextVariable("NUMBER", num);
				if (num <= 0)
				{
					return num.ToString();
				}
				return GameTexts.FindText("str_plus_with_number").ToString();
			}
			Debug.FailedAssert("Unable to get the relation between a non-hero object and the player.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaHeroPage.cs", "GetComparedValueText", 209);
			return "";
		}
	}

	public abstract class EncyclopediaListHeroComparer : EncyclopediaListItemComparerBase
	{
		protected delegate bool HeroVisibilityComparerDelegate(Hero h1, Hero h2, out int comparisonResult);

		protected bool CompareVisibility(Hero h1, Hero h2, out int comparisonResult)
		{
			bool flag = CanPlayerSeeValuesOf(h1);
			bool flag2 = CanPlayerSeeValuesOf(h2);
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

		protected int CompareHeroes(EncyclopediaListItem x, EncyclopediaListItem y, Func<Hero, Hero, int> comparison)
		{
			if (x.Object is Hero hero && y.Object is Hero hero2)
			{
				if (CompareVisibility(hero, hero2, out var comparisonResult))
				{
					if (comparisonResult == 0)
					{
						return ResolveEquality(x, y);
					}
					return comparisonResult * (base.IsAscending ? 1 : (-1));
				}
				int num = comparison(hero, hero2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Both objects should be heroes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaHeroPage.cs", "CompareHeroes", 258);
			return 0;
		}
	}

	public DefaultEncyclopediaHeroPage()
	{
		base.HomePageOrderIndex = 200;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		int comingOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		TextObject heroName = new TextObject("{=TauRjAud}{NAME} of the {FACTION}");
		_ = string.Empty;
		foreach (Hero hero in Hero.AllAliveHeroes)
		{
			if (IsValidEncyclopediaItem(hero) && !hero.IsNotable && hero.Age >= (float)comingOfAge)
			{
				string name;
				if (hero.Clan != null)
				{
					heroName.SetTextVariable("NAME", hero.FirstName ?? hero.Name);
					heroName.SetTextVariable("FACTION", hero.Clan?.Name ?? TextObject.GetEmpty());
					name = heroName.ToString();
				}
				else
				{
					name = hero.Name.ToString();
				}
				yield return new EncyclopediaListItem(hero, name, "", hero.StringId, GetIdentifier(typeof(Hero)), CanPlayerSeeValuesOf(hero), delegate
				{
					InformationManager.ShowTooltip(typeof(Hero), hero, false);
				});
			}
		}
		foreach (Hero hero2 in Hero.DeadOrDisabledHeroes)
		{
			if (!IsValidEncyclopediaItem(hero2) || hero2.IsNotable || !(hero2.Age >= (float)comingOfAge))
			{
				continue;
			}
			if (hero2.Clan != null)
			{
				heroName.SetTextVariable("NAME", hero2.FirstName ?? hero2.Name);
				heroName.SetTextVariable("FACTION", hero2.Clan?.Name ?? TextObject.GetEmpty());
				yield return new EncyclopediaListItem(hero2, heroName.ToString(), "", hero2.StringId, GetIdentifier(typeof(Hero)), CanPlayerSeeValuesOf(hero2), delegate
				{
					InformationManager.ShowTooltip(typeof(Hero), hero2, false);
				});
			}
			else
			{
				yield return new EncyclopediaListItem(hero2, hero2.Name.ToString(), "", hero2.StringId, GetIdentifier(typeof(Hero)), CanPlayerSeeValuesOf(hero2), delegate
				{
					InformationManager.ShowTooltip(typeof(Hero), hero2, false);
				});
			}
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
		List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
		list2.Add(new EncyclopediaFilterItem(new TextObject("{=5xi0t1dD}Met Before"), (object h) => ((Hero)h).HasMet));
		list.Add(new EncyclopediaFilterGroup(list2, new TextObject("{=BlidMNGT}Relation")));
		List<EncyclopediaFilterItem> list3 = new List<EncyclopediaFilterItem>();
		list3.Add(new EncyclopediaFilterItem(new TextObject("{=oAb4NqO5}Male"), (object h) => !((Hero)h).IsFemale));
		list3.Add(new EncyclopediaFilterItem(new TextObject("{=2YUUGQvG}Female"), (object h) => ((Hero)h).IsFemale));
		list.Add(new EncyclopediaFilterGroup(list3, new TextObject("{=fGFMqlGz}Gender")));
		List<EncyclopediaFilterItem> list4 = new List<EncyclopediaFilterItem>();
		list4.Add(new EncyclopediaFilterItem(new TextObject("{=uvjOVy5P}Dead"), (object h) => !((Hero)h).IsAlive));
		list4.Add(new EncyclopediaFilterItem(new TextObject("{=3TmLIou4}Alive"), (object h) => ((Hero)h).IsAlive));
		list.Add(new EncyclopediaFilterGroup(list4, new TextObject("{=DXczLzml}Status")));
		List<EncyclopediaFilterItem> list5 = new List<EncyclopediaFilterItem>();
		foreach (CultureObject culture in (from f in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
			where f.IsMainCulture
			orderby f.Name.ToString()
			select f).ToList())
		{
			if (culture.StringId != "neutral_culture" && !culture.IsBandit)
			{
				list5.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((Hero)c).Culture == culture));
			}
		}
		list.Add(new EncyclopediaFilterGroup(list5, GameTexts.FindText("str_culture")));
		List<EncyclopediaFilterItem> list6 = new List<EncyclopediaFilterItem>();
		list6.Add(new EncyclopediaFilterItem(new TextObject("{=b9ty57rJ}Faction Leader"), (object h) => ((Hero)h).IsKingdomLeader || ((Hero)h).IsClanLeader));
		list6.Add(new EncyclopediaFilterItem(new TextObject("{=4vleNtxb}Lord/Lady"), (object h) => ((Hero)h).IsLord));
		list6.Add(new EncyclopediaFilterItem(new TextObject("{=vmMqs3Ck}Noble"), (object h) => ((Hero)h).Clan?.IsNoble ?? false));
		list6.Add(new EncyclopediaFilterItem(new TextObject("{=FLa5OuyK}Wanderer"), (object h) => ((Hero)h).IsWanderer));
		list.Add(new EncyclopediaFilterGroup(list6, new TextObject("{=GZxFIeiJ}Occupation")));
		List<EncyclopediaFilterItem> list7 = new List<EncyclopediaFilterItem>();
		list7.Add(new EncyclopediaFilterItem(new TextObject("{=qIAgh9VL}Not Married"), (object h) => ((Hero)h).Spouse == null));
		list7.Add(new EncyclopediaFilterItem(new TextObject("{=xeawD38S}Married"), (object h) => ((Hero)h).Spouse != null));
		list.Add(new EncyclopediaFilterGroup(list7, new TextObject("{=PMio7set}Marital Status")));
		return list;
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(new TextObject("{=jaaQijQs}Age"), new EncyclopediaListHeroAgeComparer()),
			new EncyclopediaSortController(new TextObject("{=BlidMNGT}Relation"), new EncyclopediaListHeroRelationComparer())
		};
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaHeroPage";
	}

	public override string GetStringID()
	{
		return "EncyclopediaHero";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_encyclopedia_heroes");
	}

	public override TextObject GetDescriptionText()
	{
		return GameTexts.FindText("str_hero_description");
	}

	public override MBObjectBase GetObject(string typeName, string stringID)
	{
		return Campaign.Current.CampaignObjectManager.Find<Hero>(stringID);
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		if (o is Hero { IsTemplate: false, IsReady: not false } hero)
		{
			IFaction mapFaction = hero.MapFaction;
			if ((mapFaction == null || !mapFaction.IsBanditFaction) && !hero.CharacterObject.HiddenInEncyclopedia)
			{
				return !hero.HiddenInEncyclopedia;
			}
		}
		return false;
	}

	private static bool CanPlayerSeeValuesOf(Hero hero)
	{
		return Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(hero);
	}
}
