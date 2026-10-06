using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(ShipHull) })]
public class DefaultEncyclopediaShipPage : EncyclopediaPage
{
	private class EncyclopediaListShipClassComparer : EncyclopediaListShipComparer
	{
		private static Func<ShipHull, ShipHull, int> _comparison = (ShipHull s1, ShipHull s2) => s1.Type.CompareTo(s2.Type);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareShips(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is ShipHull shipHull)
			{
				if (!CanPlayerSeeValuesOf(shipHull))
				{
					return _missingValue.ToString();
				}
				return shipHull.Type.ToString();
			}
			Debug.FailedAssert("Unable to get the class of a ship object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "GetComparedValueText", 164);
			return "";
		}
	}

	private class EncyclopediaListShipSlotCountComparer : EncyclopediaListShipComparer
	{
		private static Func<ShipHull, ShipHull, int> _comparison = (ShipHull s1, ShipHull s2) => s1.AvailableSlots.Count.CompareTo(s2.AvailableSlots.Count);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareShips(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is ShipHull shipHull)
			{
				if (!CanPlayerSeeValuesOf(shipHull))
				{
					return _missingValue.ToString();
				}
				return shipHull.AvailableSlots.Count.ToString();
			}
			Debug.FailedAssert("Unable to get the availableSlotCount of a ship object.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "GetComparedValueText", 192);
			return "";
		}
	}

	private class EncyclopediaListShipHealthComparer : EncyclopediaListShipComparer
	{
		private static Func<ShipHull, ShipHull, int> _comparison = (ShipHull s1, ShipHull s2) => s1.MaxHitPoints.CompareTo(s2.MaxHitPoints);

		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return CompareShips(x, y, _comparison);
		}

		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			if (item.Object is ShipHull shipHull)
			{
				if (!CanPlayerSeeValuesOf(shipHull))
				{
					return _missingValue.ToString();
				}
				int maxHitPoints = shipHull.MaxHitPoints;
				MBTextManager.SetTextVariable("NUMBER", maxHitPoints);
				return maxHitPoints.ToString();
			}
			Debug.FailedAssert("Unable to get the hitPoints between a ship object and the player.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "GetComparedValueText", 222);
			return "";
		}
	}

	public abstract class EncyclopediaListShipComparer : EncyclopediaListItemComparerBase
	{
		protected delegate bool ShipVisibilityComparerDelegate(ShipHull s1, ShipHull s2, out int comparisonResult);

		protected bool CompareVisibility(ShipHull s1, ShipHull s2, out int comparisonResult)
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

		protected int CompareShips(EncyclopediaListItem x, EncyclopediaListItem y, Func<ShipHull, ShipHull, int> comparison)
		{
			if (x.Object is ShipHull shipHull && y.Object is ShipHull shipHull2)
			{
				if (CompareVisibility(shipHull, shipHull2, out var comparisonResult))
				{
					if (comparisonResult == 0)
					{
						return ResolveEquality(x, y);
					}
					return comparisonResult * (base.IsAscending ? 1 : (-1));
				}
				int num = comparison(shipHull, shipHull2) * (base.IsAscending ? 1 : (-1));
				if (num == 0)
				{
					return ResolveEquality(x, y);
				}
				return num;
			}
			Debug.FailedAssert("Both objects should be shipHull.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\Pages\\DefaultEncyclopediaShipPage.cs", "CompareShips", 271);
			return 0;
		}
	}

	public DefaultEncyclopediaShipPage()
	{
		base.HomePageOrderIndex = 200;
	}

	public override bool IsRelevant()
	{
		MBObjectManager instance = MBObjectManager.Instance;
		if (instance == null)
		{
			return false;
		}
		return instance.GetObjectTypeList<ShipHull>()?.Count > 0;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		List<ShipHull> list = new List<ShipHull>();
		foreach (CultureObject objectType in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
		{
			if (objectType.IsMainCulture && objectType.AvailableShipHulls.Count > 0)
			{
				list.AddRange(objectType.AvailableShipHulls);
			}
		}
		MBReadOnlyList<ShipHull> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<ShipHull>();
		list.AddRange(objectTypeList.Where((ShipHull x) => x.StringId == "fishing_ship" || x.StringId == "southern_fishing_ship"));
		foreach (ShipHull shipHull in list.Distinct())
		{
			TextObject textObject = null;
			if (IsValidEncyclopediaItem(shipHull))
			{
				textObject = shipHull.Name;
			}
			string name = textObject?.ToString() ?? string.Empty;
			yield return new EncyclopediaListItem(shipHull, name, "", shipHull.StringId, GetIdentifier(typeof(ShipHull)), CanPlayerSeeValuesOf(shipHull), delegate
			{
				InformationManager.ShowTooltip(typeof(ShipHull), shipHull);
			});
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		List<EncyclopediaFilterGroup> list = new List<EncyclopediaFilterGroup>();
		List<EncyclopediaFilterItem> list2 = new List<EncyclopediaFilterItem>();
		foreach (CultureObject culture in (from f in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
			where f.IsMainCulture
			orderby f.Name.ToString()
			select f).ToList())
		{
			if (culture.StringId != "neutral_culture" && culture.CanHaveSettlement)
			{
				list2.Add(new EncyclopediaFilterItem(culture.Name, (object c) => culture.AvailableShipHulls.Contains((ShipHull)c)));
			}
		}
		list.Add(new EncyclopediaFilterGroup(list2, GameTexts.FindText("str_culture")));
		List<EncyclopediaFilterItem> filters = new List<EncyclopediaFilterItem>
		{
			new EncyclopediaFilterItem(GameTexts.FindText("str_ship_type", "heavy"), (object s) => s is ShipHull shipHull && shipHull.Type == ShipHull.ShipType.Heavy),
			new EncyclopediaFilterItem(GameTexts.FindText("str_ship_type", "medium"), (object s) => s is ShipHull shipHull && shipHull.Type == ShipHull.ShipType.Medium),
			new EncyclopediaFilterItem(GameTexts.FindText("str_ship_type", "light"), (object s) => s is ShipHull shipHull && shipHull.Type == ShipHull.ShipType.Light)
		};
		list.Add(new EncyclopediaFilterGroup(filters, new TextObject("{=sqdzHOPe}Class")));
		MissionShipObject ship;
		List<EncyclopediaFilterItem> filters2 = new List<EncyclopediaFilterItem>
		{
			new EncyclopediaFilterItem(new TextObject("{=bXJLb0BE}Hybrid"), (object s) => s is ShipHull shipHull && (ship = MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId)) != null && HasSailOfType(ship, SailType.Square) && HasSailOfType(ship, SailType.Lateen)),
			new EncyclopediaFilterItem(new TextObject("{=kNxD2oer}Lateen"), (object s) => s is ShipHull shipHull && HasSailOfType(MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId), SailType.Lateen)),
			new EncyclopediaFilterItem(new TextObject("{=squareSail}Square"), (object s) => s is ShipHull shipHull && HasSailOfType(MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId), SailType.Square))
		};
		list.Add(new EncyclopediaFilterGroup(filters2, new TextObject("{=UIb3IW3f}Sail Type")));
		return list;
	}

	private bool HasSailOfType(MissionShipObject ship, SailType sailType)
	{
		if (ship != null && ship.HasSails)
		{
			return ship.Sails.Any((ShipSail x) => x.Type == sailType);
		}
		return false;
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(new TextObject("{=sqdzHOPe}Class"), new EncyclopediaListShipClassComparer()),
			new EncyclopediaSortController(new TextObject("{=UbZL2BJQ}Hitpoints"), new EncyclopediaListShipHealthComparer()),
			new EncyclopediaSortController(new TextObject("{=FQ2m5e5E}Slots"), new EncyclopediaListShipSlotCountComparer())
		};
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaShipPage";
	}

	public override string GetStringID()
	{
		return "EncyclopediaShip";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_encyclopedia_ships");
	}

	public override MBObjectBase GetObject(string typeName, string stringID)
	{
		return MBObjectManager.Instance.GetObject<ShipHull>(stringID);
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		if (o is ShipHull shipHull)
		{
			if (shipHull.IsReady)
			{
				return shipHull.IsInitialized;
			}
			return false;
		}
		return false;
	}

	private static bool CanPlayerSeeValuesOf(ShipHull shipHull)
	{
		return true;
	}
}
