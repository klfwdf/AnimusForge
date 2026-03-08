using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages
{
	// Token: 0x02000182 RID: 386
	[EncyclopediaModel(new Type[] { typeof(CharacterObject) })]
	public class DefaultEncyclopediaUnitPage : EncyclopediaPage
	{
		// Token: 0x06001B8B RID: 7051 RVA: 0x0008CD40 File Offset: 0x0008AF40
		public DefaultEncyclopediaUnitPage()
		{
			base.HomePageOrderIndex = 300;
		}

		// Token: 0x06001B8C RID: 7052 RVA: 0x0008CD53 File Offset: 0x0008AF53
		protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
		{
			using (List<CharacterObject>.Enumerator enumerator = CharacterObject.All.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CharacterObject character = enumerator.Current;
					if (this.IsValidEncyclopediaItem(character))
					{
						yield return new EncyclopediaListItem(character, character.Name.ToString(), "", character.StringId, base.GetIdentifier(typeof(CharacterObject)), true, delegate()
						{
							InformationManager.ShowTooltip(typeof(CharacterObject), new object[] { character });
						});
					}
				}
			}
			List<CharacterObject>.Enumerator enumerator = default(List<CharacterObject>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06001B8D RID: 7053 RVA: 0x0008CD64 File Offset: 0x0008AF64
		protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
		{
			List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
			List<EncyclopediaFilterItem> typeFilterItems = this.GetTypeFilterItems();
			list.Add(new EncyclopediaFilterGroup(typeFilterItems, new TextObject("{=zMMqgxb1}Type", null)));
			List<EncyclopediaFilterItem> occupationFilterItems = this.GetOccupationFilterItems();
			list.Add(new EncyclopediaFilterGroup(occupationFilterItems, new TextObject("{=GZxFIeiJ}Occupation", null)));
			List<EncyclopediaFilterItem> cultureFilterItems = this.GetCultureFilterItems();
			list.Add(new EncyclopediaFilterGroup(cultureFilterItems, GameTexts.FindText("str_culture", null)));
			List<EncyclopediaFilterItem> outlawFilterItems = this.GetOutlawFilterItems();
			list.Add(new EncyclopediaFilterGroup(outlawFilterItems, GameTexts.FindText("str_outlaw", null)));
			return list;
		}

		// Token: 0x06001B8E RID: 7054 RVA: 0x0008CDF0 File Offset: 0x0008AFF0
		protected virtual List<EncyclopediaFilterItem> GetTypeFilterItems()
		{
			List<EncyclopediaFilterItem> list = new List<EncyclopediaFilterItem>();
			list.Add(new EncyclopediaFilterItem(new TextObject("{=1Bm1Wk1v}Infantry", null), (object s) => ((CharacterObject)s).IsInfantry));
			list.Add(new EncyclopediaFilterItem(new TextObject("{=bIiBytSB}Archers", null), (object s) => ((CharacterObject)s).IsRanged && !((CharacterObject)s).IsMounted));
			list.Add(new EncyclopediaFilterItem(new TextObject("{=YVGtcLHF}Cavalry", null), (object s) => ((CharacterObject)s).IsMounted && !((CharacterObject)s).IsRanged));
			list.Add(new EncyclopediaFilterItem(new TextObject("{=I1CMeL9R}Mounted Archers", null), (object s) => ((CharacterObject)s).IsRanged && ((CharacterObject)s).IsMounted));
			return list;
		}

		// Token: 0x06001B8F RID: 7055 RVA: 0x0008CED8 File Offset: 0x0008B0D8
		protected virtual List<EncyclopediaFilterItem> GetOccupationFilterItems()
		{
			List<EncyclopediaFilterItem> list = new List<EncyclopediaFilterItem>();
			list.Add(new EncyclopediaFilterItem(GameTexts.FindText("str_occupation", "Soldier"), (object s) => ((CharacterObject)s).Occupation == Occupation.Soldier));
			list.Add(new EncyclopediaFilterItem(GameTexts.FindText("str_occupation", "Mercenary"), (object s) => ((CharacterObject)s).Occupation == Occupation.Mercenary));
			list.Add(new EncyclopediaFilterItem(GameTexts.FindText("str_occupation", "Bandit"), (object s) => ((CharacterObject)s).Occupation == Occupation.Bandit));
			return list;
		}

		// Token: 0x06001B90 RID: 7056 RVA: 0x0008CF98 File Offset: 0x0008B198
		protected virtual List<EncyclopediaFilterItem> GetCultureFilterItems()
		{
			List<EncyclopediaFilterItem> list = new List<EncyclopediaFilterItem>();
			using (List<CultureObject>.Enumerator enumerator = (from x in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
				orderby !x.IsMainCulture descending
				select x).ThenBy((CultureObject f) => f.Name.ToString()).ToList<CultureObject>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CultureObject culture = enumerator.Current;
					if (!culture.IsBandit && culture.StringId != "neutral_culture")
					{
						list.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((CharacterObject)c).Culture == culture));
					}
				}
			}
			return list;
		}

		// Token: 0x06001B91 RID: 7057 RVA: 0x0008D094 File Offset: 0x0008B294
		protected virtual List<EncyclopediaFilterItem> GetOutlawFilterItems()
		{
			List<EncyclopediaFilterItem> list = new List<EncyclopediaFilterItem>();
			using (List<CultureObject>.Enumerator enumerator = (from x in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
				orderby !x.IsMainCulture descending
				select x).ThenBy((CultureObject f) => f.Name.ToString()).ToList<CultureObject>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CultureObject culture = enumerator.Current;
					if (culture.IsBandit)
					{
						list.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((CharacterObject)c).Culture == culture));
					}
				}
			}
			return list;
		}

		// Token: 0x06001B92 RID: 7058 RVA: 0x0008D178 File Offset: 0x0008B378
		protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
		{
			return new List<EncyclopediaSortController>
			{
				new EncyclopediaSortController(new TextObject("{=cc1d7mkq}Tier", null), new DefaultEncyclopediaUnitPage.EncyclopediaListUnitTierComparer()),
				new EncyclopediaSortController(GameTexts.FindText("str_level_tag", null), new DefaultEncyclopediaUnitPage.EncyclopediaListUnitLevelComparer())
			};
		}

		// Token: 0x06001B93 RID: 7059 RVA: 0x0008D1B5 File Offset: 0x0008B3B5
		public override string GetViewFullyQualifiedName()
		{
			return "EncyclopediaUnitPage";
		}

		// Token: 0x06001B94 RID: 7060 RVA: 0x0008D1BC File Offset: 0x0008B3BC
		public override TextObject GetName()
		{
			return GameTexts.FindText("str_encyclopedia_troops", null);
		}

		// Token: 0x06001B95 RID: 7061 RVA: 0x0008D1C9 File Offset: 0x0008B3C9
		public override TextObject GetDescriptionText()
		{
			return GameTexts.FindText("str_unit_description", null);
		}

		// Token: 0x06001B96 RID: 7062 RVA: 0x0008D1D6 File Offset: 0x0008B3D6
		public override string GetStringID()
		{
			return "EncyclopediaUnit";
		}

		// Token: 0x06001B97 RID: 7063 RVA: 0x0008D1E0 File Offset: 0x0008B3E0
		public override bool IsValidEncyclopediaItem(object o)
		{
			CharacterObject characterObject = o as CharacterObject;
			return characterObject != null && !characterObject.IsTemplate && characterObject != null && !characterObject.HiddenInEncyclopedia && ((characterObject != null) ? characterObject.HeroObject : null) == null && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Mercenary || characterObject.Occupation == Occupation.Bandit || characterObject.Occupation == Occupation.Gangster || characterObject.Occupation == Occupation.CaravanGuard || (characterObject.Occupation == Occupation.Villager && characterObject.UpgradeTargets.Length != 0));
		}

		// Token: 0x020005E1 RID: 1505
		private class EncyclopediaListUnitTierComparer : DefaultEncyclopediaUnitPage.EncyclopediaListUnitComparer
		{
			// Token: 0x06004EEC RID: 20204 RVA: 0x0017EA73 File Offset: 0x0017CC73
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareUnits(x, y, DefaultEncyclopediaUnitPage.EncyclopediaListUnitTierComparer._comparison);
			}

			// Token: 0x06004EED RID: 20205 RVA: 0x0017EA84 File Offset: 0x0017CC84
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				CharacterObject characterObject;
				if ((characterObject = item.Object as CharacterObject) != null)
				{
					return characterObject.Tier.ToString();
				}
				Debug.FailedAssert("Unable to get the tier of a non-character object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaUnitPage.cs", "GetComparedValueText", 175);
				return "";
			}

			// Token: 0x04001854 RID: 6228
			private static Func<CharacterObject, CharacterObject, int> _comparison = (CharacterObject c1, CharacterObject c2) => c1.Tier.CompareTo(c2.Tier);
		}

		// Token: 0x020005E2 RID: 1506
		private class EncyclopediaListUnitLevelComparer : DefaultEncyclopediaUnitPage.EncyclopediaListUnitComparer
		{
			// Token: 0x06004EF0 RID: 20208 RVA: 0x0017EAEC File Offset: 0x0017CCEC
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareUnits(x, y, DefaultEncyclopediaUnitPage.EncyclopediaListUnitLevelComparer._comparison);
			}

			// Token: 0x06004EF1 RID: 20209 RVA: 0x0017EAFC File Offset: 0x0017CCFC
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				CharacterObject characterObject;
				if ((characterObject = item.Object as CharacterObject) != null)
				{
					return characterObject.Level.ToString();
				}
				Debug.FailedAssert("Unable to get the level of a non-character object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaUnitPage.cs", "GetComparedValueText", 196);
				return "";
			}

			// Token: 0x04001855 RID: 6229
			private static Func<CharacterObject, CharacterObject, int> _comparison = (CharacterObject c1, CharacterObject c2) => c1.Level.CompareTo(c2.Level);
		}

		// Token: 0x020005E3 RID: 1507
		public abstract class EncyclopediaListUnitComparer : EncyclopediaListItemComparerBase
		{
			// Token: 0x06004EF4 RID: 20212 RVA: 0x0017EB64 File Offset: 0x0017CD64
			public int CompareUnits(EncyclopediaListItem x, EncyclopediaListItem y, Func<CharacterObject, CharacterObject, int> comparison)
			{
				CharacterObject arg;
				CharacterObject arg2;
				if ((arg = x.Object as CharacterObject) == null || (arg2 = y.Object as CharacterObject) == null)
				{
					Debug.FailedAssert("Both objects should be character objects.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaUnitPage.cs", "CompareUnits", 211);
					return 0;
				}
				int num = comparison(arg, arg2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return base.ResolveEquality(x, y);
				}
				return num;
			}
		}
	}
}
