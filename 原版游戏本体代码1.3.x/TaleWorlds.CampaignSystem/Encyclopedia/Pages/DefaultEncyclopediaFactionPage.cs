using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages
{
	// Token: 0x0200017E RID: 382
	[EncyclopediaModel(new Type[] { typeof(Kingdom) })]
	public class DefaultEncyclopediaFactionPage : EncyclopediaPage
	{
		// Token: 0x06001B5D RID: 7005 RVA: 0x0008BEDF File Offset: 0x0008A0DF
		public DefaultEncyclopediaFactionPage()
		{
			base.HomePageOrderIndex = 400;
		}

		// Token: 0x06001B5E RID: 7006 RVA: 0x0008BEF2 File Offset: 0x0008A0F2
		public override string GetViewFullyQualifiedName()
		{
			return "EncyclopediaFactionPage";
		}

		// Token: 0x06001B5F RID: 7007 RVA: 0x0008BEF9 File Offset: 0x0008A0F9
		public override TextObject GetName()
		{
			return GameTexts.FindText("str_kingdoms_group", null);
		}

		// Token: 0x06001B60 RID: 7008 RVA: 0x0008BF06 File Offset: 0x0008A106
		public override TextObject GetDescriptionText()
		{
			return GameTexts.FindText("str_faction_description", null);
		}

		// Token: 0x06001B61 RID: 7009 RVA: 0x0008BF13 File Offset: 0x0008A113
		public override string GetStringID()
		{
			return "EncyclopediaKingdom";
		}

		// Token: 0x06001B62 RID: 7010 RVA: 0x0008BF1A File Offset: 0x0008A11A
		public override MBObjectBase GetObject(string typeName, string stringID)
		{
			return Campaign.Current.CampaignObjectManager.Find<Kingdom>(stringID);
		}

		// Token: 0x06001B63 RID: 7011 RVA: 0x0008BF2C File Offset: 0x0008A12C
		public override bool IsValidEncyclopediaItem(object o)
		{
			IFaction faction = o as IFaction;
			return faction != null && !faction.IsBanditFaction;
		}

		// Token: 0x06001B64 RID: 7012 RVA: 0x0008BF4E File Offset: 0x0008A14E
		protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
		{
			using (List<Kingdom>.Enumerator enumerator = Kingdom.All.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Kingdom kingdom = enumerator.Current;
					if (this.IsValidEncyclopediaItem(kingdom))
					{
						yield return new EncyclopediaListItem(kingdom, kingdom.Name.ToString(), "", kingdom.StringId, base.GetIdentifier(typeof(Kingdom)), true, delegate()
						{
							InformationManager.ShowTooltip(typeof(Kingdom), new object[] { kingdom });
						});
					}
				}
			}
			List<Kingdom>.Enumerator enumerator = default(List<Kingdom>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06001B65 RID: 7013 RVA: 0x0008BF60 File Offset: 0x0008A160
		protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
		{
			List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
			List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
			list2 = new List<EncyclopediaFilterItem>();
			list2.Add(new EncyclopediaFilterItem(new TextObject("{=lEHjxPTs}Ally", null), (object f) => DiplomacyHelper.IsSameFactionAndNotEliminated((IFaction)f, Hero.MainHero.MapFaction)));
			list2.Add(new EncyclopediaFilterItem(new TextObject("{=sPmQz21k}Enemy", null), (object f) => FactionManager.IsAtWarAgainstFaction((IFaction)f, Hero.MainHero.MapFaction) && !((IFaction)f).IsBanditFaction));
			list2.Add(new EncyclopediaFilterItem(new TextObject("{=3PzgpFGq}Neutral", null), (object f) => FactionManager.IsNeutralWithFaction((IFaction)f, Hero.MainHero.MapFaction)));
			list.Add(new EncyclopediaFilterGroup(list2, new TextObject("{=L7wn49Uz}Diplomacy", null)));
			return list;
		}

		// Token: 0x06001B66 RID: 7014 RVA: 0x0008C034 File Offset: 0x0008A234
		protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
		{
			return new List<EncyclopediaSortController>
			{
				new EncyclopediaSortController(GameTexts.FindText("str_total_strength", null), new DefaultEncyclopediaFactionPage.EncyclopediaListKingdomTotalStrengthComparer()),
				new EncyclopediaSortController(GameTexts.FindText("str_fiefs", null), new DefaultEncyclopediaFactionPage.EncyclopediaListKingdomFiefsComparer()),
				new EncyclopediaSortController(GameTexts.FindText("str_clans", null), new DefaultEncyclopediaFactionPage.EncyclopediaListKingdomClanComparer())
			};
		}

		// Token: 0x020005BE RID: 1470
		private class EncyclopediaListKingdomTotalStrengthComparer : DefaultEncyclopediaFactionPage.EncyclopediaListKingdomComparer
		{
			// Token: 0x06004E4C RID: 20044 RVA: 0x0017CCF3 File Offset: 0x0017AEF3
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareKingdoms(x, y, DefaultEncyclopediaFactionPage.EncyclopediaListKingdomTotalStrengthComparer._comparison);
			}

			// Token: 0x06004E4D RID: 20045 RVA: 0x0017CD04 File Offset: 0x0017AF04
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				Kingdom kingdom;
				if ((kingdom = item.Object as Kingdom) != null)
				{
					return ((int)kingdom.CurrentTotalStrength).ToString();
				}
				Debug.FailedAssert("Unable to get the total strength of a non-kingdom object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "GetComparedValueText", 107);
				return "";
			}

			// Token: 0x0400180E RID: 6158
			private static Func<Kingdom, Kingdom, int> _comparison = (Kingdom k1, Kingdom k2) => k1.CurrentTotalStrength.CompareTo(k2.CurrentTotalStrength);
		}

		// Token: 0x020005BF RID: 1471
		private class EncyclopediaListKingdomFiefsComparer : DefaultEncyclopediaFactionPage.EncyclopediaListKingdomComparer
		{
			// Token: 0x06004E50 RID: 20048 RVA: 0x0017CD6A File Offset: 0x0017AF6A
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareKingdoms(x, y, DefaultEncyclopediaFactionPage.EncyclopediaListKingdomFiefsComparer._comparison);
			}

			// Token: 0x06004E51 RID: 20049 RVA: 0x0017CD7C File Offset: 0x0017AF7C
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				Kingdom kingdom;
				if ((kingdom = item.Object as Kingdom) != null)
				{
					return kingdom.Fiefs.Count.ToString();
				}
				Debug.FailedAssert("Unable to get the fief count from a non-kingdom object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "GetComparedValueText", 128);
				return "";
			}

			// Token: 0x0400180F RID: 6159
			private static Func<Kingdom, Kingdom, int> _comparison = (Kingdom k1, Kingdom k2) => k1.Fiefs.Count.CompareTo(k2.Fiefs.Count);
		}

		// Token: 0x020005C0 RID: 1472
		private class EncyclopediaListKingdomClanComparer : DefaultEncyclopediaFactionPage.EncyclopediaListKingdomComparer
		{
			// Token: 0x06004E54 RID: 20052 RVA: 0x0017CDE9 File Offset: 0x0017AFE9
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareKingdoms(x, y, DefaultEncyclopediaFactionPage.EncyclopediaListKingdomClanComparer._comparison);
			}

			// Token: 0x06004E55 RID: 20053 RVA: 0x0017CDF8 File Offset: 0x0017AFF8
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				Kingdom kingdom;
				if ((kingdom = item.Object as Kingdom) != null)
				{
					return kingdom.Clans.Count.ToString();
				}
				Debug.FailedAssert("Unable to get the clan count from a non-kingdom object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "GetComparedValueText", 149);
				return "";
			}

			// Token: 0x04001810 RID: 6160
			private static Func<Kingdom, Kingdom, int> _comparison = (Kingdom k1, Kingdom k2) => k1.Clans.Count.CompareTo(k2.Clans.Count);
		}

		// Token: 0x020005C1 RID: 1473
		public abstract class EncyclopediaListKingdomComparer : EncyclopediaListItemComparerBase
		{
			// Token: 0x06004E58 RID: 20056 RVA: 0x0017CE68 File Offset: 0x0017B068
			public int CompareKingdoms(EncyclopediaListItem x, EncyclopediaListItem y, Func<Kingdom, Kingdom, int> comparison)
			{
				Kingdom arg;
				Kingdom arg2;
				if ((arg = x.Object as Kingdom) == null || (arg2 = y.Object as Kingdom) == null)
				{
					Debug.FailedAssert("Both objects should be kingdoms.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaFactionPage.cs", "CompareKingdoms", 164);
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
