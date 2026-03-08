using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages
{
	// Token: 0x0200017F RID: 383
	[EncyclopediaModel(new Type[] { typeof(Hero) })]
	public class DefaultEncyclopediaHeroPage : EncyclopediaPage
	{
		// Token: 0x06001B67 RID: 7015 RVA: 0x0008C097 File Offset: 0x0008A297
		public DefaultEncyclopediaHeroPage()
		{
			base.HomePageOrderIndex = 200;
		}

		// Token: 0x06001B68 RID: 7016 RVA: 0x0008C0AA File Offset: 0x0008A2AA
		protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
		{
			int comingOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
			TextObject heroName = new TextObject("{=TauRjAud}{NAME} of the {FACTION}", null);
			string name = string.Empty;
			using (List<Hero>.Enumerator enumerator = Hero.AllAliveHeroes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Hero hero = enumerator.Current;
					if (this.IsValidEncyclopediaItem(hero) && !hero.IsNotable && hero.Age >= (float)comingOfAge)
					{
						if (hero.Clan != null)
						{
							heroName.SetTextVariable("NAME", hero.FirstName ?? hero.Name);
							TextObject textObject = heroName;
							string tag = "FACTION";
							Clan clan = hero.Clan;
							textObject.SetTextVariable(tag, ((clan != null) ? clan.Name : null) ?? TextObject.GetEmpty());
							name = heroName.ToString();
						}
						else
						{
							name = hero.Name.ToString();
						}
						yield return new EncyclopediaListItem(hero, name, "", hero.StringId, base.GetIdentifier(typeof(Hero)), DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(hero), delegate()
						{
							InformationManager.ShowTooltip(typeof(Hero), new object[] { hero, false });
						});
					}
				}
			}
			List<Hero>.Enumerator enumerator = default(List<Hero>.Enumerator);
			using (List<Hero>.Enumerator enumerator = Hero.DeadOrDisabledHeroes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Hero hero = enumerator.Current;
					if (this.IsValidEncyclopediaItem(hero) && !hero.IsNotable && hero.Age >= (float)comingOfAge)
					{
						if (hero.Clan != null)
						{
							heroName.SetTextVariable("NAME", hero.FirstName ?? hero.Name);
							TextObject textObject2 = heroName;
							string tag2 = "FACTION";
							Clan clan2 = hero.Clan;
							textObject2.SetTextVariable(tag2, ((clan2 != null) ? clan2.Name : null) ?? TextObject.GetEmpty());
							yield return new EncyclopediaListItem(hero, heroName.ToString(), "", hero.StringId, base.GetIdentifier(typeof(Hero)), DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(hero), delegate()
							{
								InformationManager.ShowTooltip(typeof(Hero), new object[] { hero, false });
							});
						}
						else
						{
							yield return new EncyclopediaListItem(hero, hero.Name.ToString(), "", hero.StringId, base.GetIdentifier(typeof(Hero)), DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(hero), delegate()
							{
								InformationManager.ShowTooltip(typeof(Hero), new object[] { hero, false });
							});
						}
					}
				}
			}
			enumerator = default(List<Hero>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06001B69 RID: 7017 RVA: 0x0008C0BC File Offset: 0x0008A2BC
		protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
		{
			List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
			List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
			list2.Add(new EncyclopediaFilterItem(new TextObject("{=5xi0t1dD}Met Before", null), (object h) => ((Hero)h).HasMet));
			list.Add(new EncyclopediaFilterGroup(list2, new TextObject("{=BlidMNGT}Relation", null)));
			List<EncyclopediaFilterItem> list3 = new List<EncyclopediaFilterItem>();
			list3.Add(new EncyclopediaFilterItem(new TextObject("{=oAb4NqO5}Male", null), (object h) => !((Hero)h).IsFemale));
			list3.Add(new EncyclopediaFilterItem(new TextObject("{=2YUUGQvG}Female", null), (object h) => ((Hero)h).IsFemale));
			list.Add(new EncyclopediaFilterGroup(list3, new TextObject("{=fGFMqlGz}Gender", null)));
			List<EncyclopediaFilterItem> list4 = new List<EncyclopediaFilterItem>();
			list4.Add(new EncyclopediaFilterItem(new TextObject("{=uvjOVy5P}Dead", null), (object h) => !((Hero)h).IsAlive));
			list4.Add(new EncyclopediaFilterItem(new TextObject("{=3TmLIou4}Alive", null), (object h) => ((Hero)h).IsAlive));
			list.Add(new EncyclopediaFilterGroup(list4, new TextObject("{=DXczLzml}Status", null)));
			List<EncyclopediaFilterItem> list5 = new List<EncyclopediaFilterItem>();
			using (List<CultureObject>.Enumerator enumerator = (from x in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
				orderby !x.IsMainCulture descending
				select x).ThenBy((CultureObject f) => f.Name.ToString()).ToList<CultureObject>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CultureObject culture = enumerator.Current;
					if (culture.StringId != "neutral_culture" && !culture.IsBandit)
					{
						list5.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((Hero)c).Culture == culture));
					}
				}
			}
			list.Add(new EncyclopediaFilterGroup(list5, GameTexts.FindText("str_culture", null)));
			List<EncyclopediaFilterItem> list6 = new List<EncyclopediaFilterItem>();
			list6.Add(new EncyclopediaFilterItem(new TextObject("{=b9ty57rJ}Faction Leader", null), (object h) => ((Hero)h).IsKingdomLeader || ((Hero)h).IsClanLeader));
			list6.Add(new EncyclopediaFilterItem(new TextObject("{=4vleNtxb}Lord/Lady", null), (object h) => ((Hero)h).IsLord));
			list6.Add(new EncyclopediaFilterItem(new TextObject("{=vmMqs3Ck}Noble", null), delegate(object h)
			{
				Clan clan = ((Hero)h).Clan;
				return clan != null && clan.IsNoble;
			}));
			list6.Add(new EncyclopediaFilterItem(new TextObject("{=FLa5OuyK}Wanderer", null), (object h) => ((Hero)h).IsWanderer));
			list.Add(new EncyclopediaFilterGroup(list6, new TextObject("{=GZxFIeiJ}Occupation", null)));
			List<EncyclopediaFilterItem> list7 = new List<EncyclopediaFilterItem>();
			list7.Add(new EncyclopediaFilterItem(new TextObject("{=qIAgh9VL}Not Married", null), (object h) => ((Hero)h).Spouse == null));
			list7.Add(new EncyclopediaFilterItem(new TextObject("{=xeawD38S}Married", null), (object h) => ((Hero)h).Spouse != null));
			list.Add(new EncyclopediaFilterGroup(list7, new TextObject("{=PMio7set}Marital Status", null)));
			return list;
		}

		// Token: 0x06001B6A RID: 7018 RVA: 0x0008C4C0 File Offset: 0x0008A6C0
		protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
		{
			return new List<EncyclopediaSortController>
			{
				new EncyclopediaSortController(new TextObject("{=jaaQijQs}Age", null), new DefaultEncyclopediaHeroPage.EncyclopediaListHeroAgeComparer()),
				new EncyclopediaSortController(new TextObject("{=BlidMNGT}Relation", null), new DefaultEncyclopediaHeroPage.EncyclopediaListHeroRelationComparer())
			};
		}

		// Token: 0x06001B6B RID: 7019 RVA: 0x0008C4FD File Offset: 0x0008A6FD
		public override string GetViewFullyQualifiedName()
		{
			return "EncyclopediaHeroPage";
		}

		// Token: 0x06001B6C RID: 7020 RVA: 0x0008C504 File Offset: 0x0008A704
		public override string GetStringID()
		{
			return "EncyclopediaHero";
		}

		// Token: 0x06001B6D RID: 7021 RVA: 0x0008C50B File Offset: 0x0008A70B
		public override TextObject GetName()
		{
			return GameTexts.FindText("str_encyclopedia_heroes", null);
		}

		// Token: 0x06001B6E RID: 7022 RVA: 0x0008C518 File Offset: 0x0008A718
		public override TextObject GetDescriptionText()
		{
			return GameTexts.FindText("str_hero_description", null);
		}

		// Token: 0x06001B6F RID: 7023 RVA: 0x0008C525 File Offset: 0x0008A725
		public override MBObjectBase GetObject(string typeName, string stringID)
		{
			return Campaign.Current.CampaignObjectManager.Find<Hero>(stringID);
		}

		// Token: 0x06001B70 RID: 7024 RVA: 0x0008C538 File Offset: 0x0008A738
		public override bool IsValidEncyclopediaItem(object o)
		{
			Hero hero = o as Hero;
			if (hero != null && !hero.IsTemplate && hero.IsReady)
			{
				IFaction mapFaction = hero.MapFaction;
				if ((mapFaction == null || !mapFaction.IsBanditFaction) && !hero.CharacterObject.HiddenInEncyclopedia)
				{
					return !hero.HiddenInEncyclopedia;
				}
			}
			return false;
		}

		// Token: 0x06001B71 RID: 7025 RVA: 0x0008C58E File Offset: 0x0008A78E
		private static bool CanPlayerSeeValuesOf(Hero hero)
		{
			return Campaign.Current.Models.InformationRestrictionModel.DoesPlayerKnowDetailsOf(hero);
		}

		// Token: 0x020005C5 RID: 1477
		private class EncyclopediaListHeroAgeComparer : DefaultEncyclopediaHeroPage.EncyclopediaListHeroComparer
		{
			// Token: 0x06004E6A RID: 20074 RVA: 0x0017D156 File Offset: 0x0017B356
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareHeroes(x, y, DefaultEncyclopediaHeroPage.EncyclopediaListHeroAgeComparer._comparison);
			}

			// Token: 0x06004E6B RID: 20075 RVA: 0x0017D168 File Offset: 0x0017B368
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				Hero hero;
				if ((hero = item.Object as Hero) == null)
				{
					Debug.FailedAssert("Unable to get the age of a non-hero object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaHeroPage.cs", "GetComparedValueText", 179);
					return "";
				}
				if (!DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(hero))
				{
					return this._missingValue.ToString();
				}
				return ((int)hero.Age).ToString();
			}

			// Token: 0x0400181B RID: 6171
			private static Func<Hero, Hero, int> _comparison = (Hero h1, Hero h2) => h1.Age.CompareTo(h2.Age);
		}

		// Token: 0x020005C6 RID: 1478
		private class EncyclopediaListHeroRelationComparer : DefaultEncyclopediaHeroPage.EncyclopediaListHeroComparer
		{
			// Token: 0x06004E6E RID: 20078 RVA: 0x0017D1E5 File Offset: 0x0017B3E5
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareHeroes(x, y, DefaultEncyclopediaHeroPage.EncyclopediaListHeroRelationComparer._comparison);
			}

			// Token: 0x06004E6F RID: 20079 RVA: 0x0017D1F4 File Offset: 0x0017B3F4
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				Hero hero;
				if ((hero = item.Object as Hero) == null)
				{
					Debug.FailedAssert("Unable to get the relation between a non-hero object and the player.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaHeroPage.cs", "GetComparedValueText", 209);
					return "";
				}
				if (!DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(hero))
				{
					return this._missingValue.ToString();
				}
				int num = (int)hero.GetRelationWithPlayer();
				MBTextManager.SetTextVariable("NUMBER", num);
				if (num <= 0)
				{
					return num.ToString();
				}
				return GameTexts.FindText("str_plus_with_number", null).ToString();
			}

			// Token: 0x0400181C RID: 6172
			private static Func<Hero, Hero, int> _comparison = (Hero h1, Hero h2) => h1.GetRelationWithPlayer().CompareTo(h2.GetRelationWithPlayer());
		}

		// Token: 0x020005C7 RID: 1479
		public abstract class EncyclopediaListHeroComparer : EncyclopediaListItemComparerBase
		{
			// Token: 0x06004E72 RID: 20082 RVA: 0x0017D294 File Offset: 0x0017B494
			protected bool CompareVisibility(Hero h1, Hero h2, out int comparisonResult)
			{
				bool flag = DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(h1);
				bool flag2 = DefaultEncyclopediaHeroPage.CanPlayerSeeValuesOf(h2);
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
					comparisonResult = (base.IsAscending ? (-1) : 1);
					return true;
				}
				comparisonResult = 0;
				return false;
			}

			// Token: 0x06004E73 RID: 20083 RVA: 0x0017D2E4 File Offset: 0x0017B4E4
			protected int CompareHeroes(EncyclopediaListItem x, EncyclopediaListItem y, Func<Hero, Hero, int> comparison)
			{
				Hero hero;
				Hero hero2;
				if ((hero = x.Object as Hero) == null || (hero2 = y.Object as Hero) == null)
				{
					Debug.FailedAssert("Both objects should be heroes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaHeroPage.cs", "CompareHeroes", 258);
					return 0;
				}
				int num;
				if (this.CompareVisibility(hero, hero2, out num))
				{
					if (num == 0)
					{
						return base.ResolveEquality(x, y);
					}
					return num * (base.IsAscending ? 1 : (-1));
				}
				else
				{
					int num2 = comparison(hero, hero2) * (base.IsAscending ? 1 : (-1));
					if (num2 == 0)
					{
						return base.ResolveEquality(x, y);
					}
					return num2;
				}
			}

			// Token: 0x020008B0 RID: 2224
			// (Invoke) Token: 0x060067F7 RID: 26615
			protected delegate bool HeroVisibilityComparerDelegate(Hero h1, Hero h2, out int comparisonResult);
		}
	}
}
