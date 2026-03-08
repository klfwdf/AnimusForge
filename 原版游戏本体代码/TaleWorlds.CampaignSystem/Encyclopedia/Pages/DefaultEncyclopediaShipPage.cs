using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages
{
	// Token: 0x02000181 RID: 385
	[EncyclopediaModel(new Type[] { typeof(ShipHull) })]
	public class DefaultEncyclopediaShipPage : EncyclopediaPage
	{
		// Token: 0x06001B7C RID: 7036 RVA: 0x0008C8C0 File Offset: 0x0008AAC0
		public DefaultEncyclopediaShipPage()
		{
			base.HomePageOrderIndex = 200;
		}

		// Token: 0x06001B7D RID: 7037 RVA: 0x0008C8D4 File Offset: 0x0008AAD4
		public override bool IsRelevant()
		{
			MBObjectManager instance = MBObjectManager.Instance;
			if (instance == null)
			{
				return false;
			}
			MBReadOnlyList<ShipHull> objectTypeList = instance.GetObjectTypeList<ShipHull>();
			int? num = ((objectTypeList != null) ? new int?(objectTypeList.Count) : null);
			int num2 = 0;
			return (num.GetValueOrDefault() > num2) & (num != null);
		}

		// Token: 0x06001B7E RID: 7038 RVA: 0x0008C91F File Offset: 0x0008AB1F
		protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
		{
			List<ShipHull> list = new List<ShipHull>();
			foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
			{
				if (cultureObject.IsMainCulture && cultureObject.AvailableShipHulls.Count > 0)
				{
					list.AddRange(cultureObject.AvailableShipHulls);
				}
			}
			MBReadOnlyList<ShipHull> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<ShipHull>();
			list.AddRange(from x in objectTypeList
				where x.StringId == "fishing_ship" || x.StringId == "southern_fishing_ship"
				select x);
			using (IEnumerator<ShipHull> enumerator2 = list.Distinct<ShipHull>().GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					ShipHull shipHull = enumerator2.Current;
					TextObject textObject = null;
					if (this.IsValidEncyclopediaItem(shipHull))
					{
						textObject = shipHull.Name;
					}
					string name = ((textObject != null) ? textObject.ToString() : null) ?? string.Empty;
					yield return new EncyclopediaListItem(shipHull, name, "", shipHull.StringId, base.GetIdentifier(typeof(ShipHull)), DefaultEncyclopediaShipPage.CanPlayerSeeValuesOf(shipHull), delegate()
					{
						InformationManager.ShowTooltip(typeof(ShipHull), new object[] { shipHull });
					});
				}
			}
			IEnumerator<ShipHull> enumerator2 = null;
			yield break;
			yield break;
		}

		// Token: 0x06001B7F RID: 7039 RVA: 0x0008C930 File Offset: 0x0008AB30
		protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
		{
			List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
			List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
			using (List<CultureObject>.Enumerator enumerator = (from x in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
				orderby !x.IsMainCulture descending
				select x).ThenBy((CultureObject f) => f.Name.ToString()).ToList<CultureObject>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CultureObject culture = enumerator.Current;
					if (culture.StringId != "neutral_culture" && culture.CanHaveSettlement && culture.IsMainCulture)
					{
						list2.Add(new EncyclopediaFilterItem(culture.Name, (object c) => culture.AvailableShipHulls.Contains((ShipHull)c)));
					}
				}
			}
			list.Add(new EncyclopediaFilterGroup(list2, GameTexts.FindText("str_culture", null)));
			List<EncyclopediaFilterItem> list3 = new List<EncyclopediaFilterItem>();
			list3.Add(new EncyclopediaFilterItem(GameTexts.FindText("str_ship_type", "heavy"), delegate(object s)
			{
				ShipHull shipHull;
				return (shipHull = s as ShipHull) != null && shipHull.Type == ShipHull.ShipType.Heavy;
			}));
			list3.Add(new EncyclopediaFilterItem(GameTexts.FindText("str_ship_type", "medium"), delegate(object s)
			{
				ShipHull shipHull;
				return (shipHull = s as ShipHull) != null && shipHull.Type == ShipHull.ShipType.Medium;
			}));
			list3.Add(new EncyclopediaFilterItem(GameTexts.FindText("str_ship_type", "light"), delegate(object s)
			{
				ShipHull shipHull;
				return (shipHull = s as ShipHull) != null && shipHull.Type == ShipHull.ShipType.Light;
			}));
			List<EncyclopediaFilterItem> filters = list3;
			list.Add(new EncyclopediaFilterGroup(filters, new TextObject("{=sqdzHOPe}Class", null)));
			List<EncyclopediaFilterItem> filters2 = new List<EncyclopediaFilterItem>
			{
				new EncyclopediaFilterItem(new TextObject("{=bXJLb0BE}Hybrid", null), delegate(object s)
				{
					ShipHull shipHull;
					MissionShipObject @object;
					return (shipHull = s as ShipHull) != null && (@object = MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId)) != null && this.HasSailOfType(@object, SailType.Square) && this.HasSailOfType(@object, SailType.Lateen);
				}),
				new EncyclopediaFilterItem(new TextObject("{=kNxD2oer}Lateen", null), delegate(object s)
				{
					ShipHull shipHull;
					return (shipHull = s as ShipHull) != null && this.HasSailOfType(MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId), SailType.Lateen);
				}),
				new EncyclopediaFilterItem(new TextObject("{=E3tCWX7w}Square", null), delegate(object s)
				{
					ShipHull shipHull;
					return (shipHull = s as ShipHull) != null && this.HasSailOfType(MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId), SailType.Square);
				})
			};
			list.Add(new EncyclopediaFilterGroup(filters2, new TextObject("{=UIb3IW3f}Sail Type", null)));
			return list;
		}

		// Token: 0x06001B80 RID: 7040 RVA: 0x0008CBA8 File Offset: 0x0008ADA8
		private bool HasSailOfType(MissionShipObject ship, SailType sailType)
		{
			return ship != null && ship.HasSails && ship.Sails.Any((ShipSail x) => x.Type == sailType);
		}

		// Token: 0x06001B81 RID: 7041 RVA: 0x0008CBE8 File Offset: 0x0008ADE8
		protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
		{
			return new List<EncyclopediaSortController>
			{
				new EncyclopediaSortController(new TextObject("{=sqdzHOPe}Class", null), new DefaultEncyclopediaShipPage.EncyclopediaListShipClassComparer()),
				new EncyclopediaSortController(new TextObject("{=UbZL2BJQ}Hitpoints", null), new DefaultEncyclopediaShipPage.EncyclopediaListShipHealthComparer()),
				new EncyclopediaSortController(new TextObject("{=FQ2m5e5E}Slots", null), new DefaultEncyclopediaShipPage.EncyclopediaListShipSlotCountComparer())
			};
		}

		// Token: 0x06001B82 RID: 7042 RVA: 0x0008CC4B File Offset: 0x0008AE4B
		public override string GetViewFullyQualifiedName()
		{
			return "EncyclopediaShipPage";
		}

		// Token: 0x06001B83 RID: 7043 RVA: 0x0008CC52 File Offset: 0x0008AE52
		public override string GetStringID()
		{
			return "EncyclopediaShip";
		}

		// Token: 0x06001B84 RID: 7044 RVA: 0x0008CC59 File Offset: 0x0008AE59
		public override TextObject GetName()
		{
			return GameTexts.FindText("str_encyclopedia_ships", null);
		}

		// Token: 0x06001B85 RID: 7045 RVA: 0x0008CC66 File Offset: 0x0008AE66
		public override MBObjectBase GetObject(string typeName, string stringID)
		{
			return MBObjectManager.Instance.GetObject<ShipHull>(stringID);
		}

		// Token: 0x06001B86 RID: 7046 RVA: 0x0008CC74 File Offset: 0x0008AE74
		public override bool IsValidEncyclopediaItem(object o)
		{
			ShipHull shipHull;
			return (shipHull = o as ShipHull) != null && shipHull.IsReady && shipHull.IsInitialized;
		}

		// Token: 0x06001B87 RID: 7047 RVA: 0x0008CC9D File Offset: 0x0008AE9D
		private static bool CanPlayerSeeValuesOf(ShipHull shipHull)
		{
			return true;
		}

		// Token: 0x020005D8 RID: 1496
		private class EncyclopediaListShipClassComparer : DefaultEncyclopediaShipPage.EncyclopediaListShipComparer
		{
			// Token: 0x06004EC6 RID: 20166 RVA: 0x0017E3DF File Offset: 0x0017C5DF
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareShips(x, y, DefaultEncyclopediaShipPage.EncyclopediaListShipClassComparer._comparison);
			}

			// Token: 0x06004EC7 RID: 20167 RVA: 0x0017E3F0 File Offset: 0x0017C5F0
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				ShipHull shipHull;
				if ((shipHull = item.Object as ShipHull) == null)
				{
					Debug.FailedAssert("Unable to get the class of a ship object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "GetComparedValueText", 165);
					return "";
				}
				if (!DefaultEncyclopediaShipPage.CanPlayerSeeValuesOf(shipHull))
				{
					return this._missingValue.ToString();
				}
				return shipHull.Type.ToString();
			}

			// Token: 0x04001842 RID: 6210
			private static Func<ShipHull, ShipHull, int> _comparison = (ShipHull s1, ShipHull s2) => s1.Type.CompareTo(s2.Type);
		}

		// Token: 0x020005D9 RID: 1497
		private class EncyclopediaListShipSlotCountComparer : DefaultEncyclopediaShipPage.EncyclopediaListShipComparer
		{
			// Token: 0x06004ECA RID: 20170 RVA: 0x0017E472 File Offset: 0x0017C672
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareShips(x, y, DefaultEncyclopediaShipPage.EncyclopediaListShipSlotCountComparer._comparison);
			}

			// Token: 0x06004ECB RID: 20171 RVA: 0x0017E484 File Offset: 0x0017C684
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				ShipHull shipHull;
				if ((shipHull = item.Object as ShipHull) == null)
				{
					Debug.FailedAssert("Unable to get the availableSlotCount of a ship object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "GetComparedValueText", 193);
					return "";
				}
				if (!DefaultEncyclopediaShipPage.CanPlayerSeeValuesOf(shipHull))
				{
					return this._missingValue.ToString();
				}
				return shipHull.AvailableSlots.Count.ToString();
			}

			// Token: 0x04001843 RID: 6211
			private static Func<ShipHull, ShipHull, int> _comparison = (ShipHull s1, ShipHull s2) => s1.AvailableSlots.Count.CompareTo(s2.AvailableSlots.Count);
		}

		// Token: 0x020005DA RID: 1498
		private class EncyclopediaListShipHealthComparer : DefaultEncyclopediaShipPage.EncyclopediaListShipComparer
		{
			// Token: 0x06004ECE RID: 20174 RVA: 0x0017E505 File Offset: 0x0017C705
			public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
			{
				return base.CompareShips(x, y, DefaultEncyclopediaShipPage.EncyclopediaListShipHealthComparer._comparison);
			}

			// Token: 0x06004ECF RID: 20175 RVA: 0x0017E514 File Offset: 0x0017C714
			public override string GetComparedValueText(EncyclopediaListItem item)
			{
				ShipHull shipHull;
				if ((shipHull = item.Object as ShipHull) == null)
				{
					Debug.FailedAssert("Unable to get the hitPoints between a ship object and the player.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "GetComparedValueText", 223);
					return "";
				}
				if (!DefaultEncyclopediaShipPage.CanPlayerSeeValuesOf(shipHull))
				{
					return this._missingValue.ToString();
				}
				int maxHitPoints = shipHull.MaxHitPoints;
				MBTextManager.SetTextVariable("NUMBER", maxHitPoints);
				return maxHitPoints.ToString();
			}

			// Token: 0x04001844 RID: 6212
			private static Func<ShipHull, ShipHull, int> _comparison = (ShipHull s1, ShipHull s2) => s1.MaxHitPoints.CompareTo(s2.MaxHitPoints);
		}

		// Token: 0x020005DB RID: 1499
		public abstract class EncyclopediaListShipComparer : EncyclopediaListItemComparerBase
		{
			// Token: 0x06004ED2 RID: 20178 RVA: 0x0017E59C File Offset: 0x0017C79C
			protected bool CompareVisibility(ShipHull s1, ShipHull s2, out int comparisonResult)
			{
				bool flag = DefaultEncyclopediaShipPage.CanPlayerSeeValuesOf(s1);
				bool flag2 = DefaultEncyclopediaShipPage.CanPlayerSeeValuesOf(s2);
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

			// Token: 0x06004ED3 RID: 20179 RVA: 0x0017E5EC File Offset: 0x0017C7EC
			protected int CompareShips(EncyclopediaListItem x, EncyclopediaListItem y, Func<ShipHull, ShipHull, int> comparison)
			{
				ShipHull shipHull;
				ShipHull shipHull2;
				if ((shipHull = x.Object as ShipHull) == null || (shipHull2 = y.Object as ShipHull) == null)
				{
					Debug.FailedAssert("Both objects should be shipHull.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "CompareShips", 272);
					return 0;
				}
				int num;
				if (this.CompareVisibility(shipHull, shipHull2, out num))
				{
					if (num == 0)
					{
						return base.ResolveEquality(x, y);
					}
					return num * (base.IsAscending ? 1 : (-1));
				}
				else
				{
					int num2 = comparison(shipHull, shipHull2) * (base.IsAscending ? 1 : (-1));
					if (num2 == 0)
					{
						return base.ResolveEquality(x, y);
					}
					return num2;
				}
			}

			// Token: 0x020008B5 RID: 2229
			// (Invoke) Token: 0x06006808 RID: 26632
			protected delegate bool ShipVisibilityComparerDelegate(ShipHull s1, ShipHull s2, out int comparisonResult);
		}
	}
}
