using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipStatsVM : ViewModel
{
	private readonly Ship _ship;

	private MBBindingList<ShipStatVM> _statList;

	[DataSourceProperty]
	public MBBindingList<ShipStatVM> StatList
	{
		get
		{
			return _statList;
		}
		set
		{
			if (value != _statList)
			{
				_statList = value;
				OnPropertyChangedWithValue(value, "StatList");
			}
		}
	}

	public ShipStatsVM(Ship ship)
	{
		_ship = ship;
		StatList = new MBBindingList<ShipStatVM>();
		RefreshStats(_ship.HitPoints, null);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		StatList.ApplyActionOnAllItems(delegate(ShipStatVM s)
		{
			s.RefreshValues();
		});
	}

	public void RefreshStats(float currentHp, MBReadOnlyList<(string, ShipUpgradePiece)> newlySelectedPieces)
	{
		StatList.Clear();
		MissionShipObject missionShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(_ship.ShipHull.MissionShipObjectId);
		if (missionShipObject == null)
		{
			Debug.FailedAssert("Failed to find mission ship object with id: " + _ship.ShipHull.MissionShipObjectId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\ShipStatsVM.cs", "RefreshStats", 40);
			return;
		}
		MBList<ShipUpgradePiece> mBList = new MBList<ShipUpgradePiece>();
		foreach (KeyValuePair<string, ShipSlot> availableSlot in _ship.ShipHull.AvailableSlots)
		{
			mBList.Add(_ship.GetPieceAtSlot(availableSlot.Key));
		}
		float num = 1f;
		float num2 = 1f;
		float num3 = 1f;
		float num4 = 1f;
		float num5 = 1f;
		for (int i = 0; i < mBList.Count; i++)
		{
			ShipUpgradePiece shipUpgradePiece = mBList[i];
			if (shipUpgradePiece != null)
			{
				num += shipUpgradePiece.CampaignSpeedBonusMultiplier;
				num2 += shipUpgradePiece.MaxHitPointsBonusMultiplier;
				num3 += shipUpgradePiece.InventoryCapacityBonusMultiplier;
				num4 += shipUpgradePiece.ShipWeightBonusMultiplier;
				num5 += shipUpgradePiece.CrewCapacityBonusMultiplier;
			}
		}
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = 0f;
		int num11 = 0;
		if (newlySelectedPieces != null && newlySelectedPieces.Count > 0)
		{
			for (int j = 0; j < newlySelectedPieces.Count; j++)
			{
				string item = newlySelectedPieces[j].Item1;
				ShipUpgradePiece item2 = newlySelectedPieces[j].Item2;
				if (item2 != null)
				{
					num6 += item2.CampaignSpeedBonusMultiplier;
					num7 += item2.MaxHitPointsBonusMultiplier;
					num8 += item2.InventoryCapacityBonusMultiplier;
					num9 += item2.ShipWeightBonusMultiplier;
					num10 += item2.CrewCapacityBonusMultiplier;
					num11 += item2.SeaWorthinessBonus;
				}
				ShipUpgradePiece pieceAtSlot = _ship.GetPieceAtSlot(item);
				if (pieceAtSlot != null)
				{
					num6 -= pieceAtSlot.CampaignSpeedBonusMultiplier;
					num7 -= pieceAtSlot.MaxHitPointsBonusMultiplier;
					num8 -= pieceAtSlot.InventoryCapacityBonusMultiplier;
					num9 -= pieceAtSlot.ShipWeightBonusMultiplier;
					num10 -= pieceAtSlot.CrewCapacityBonusMultiplier;
					num11 -= pieceAtSlot.SeaWorthinessBonus;
				}
			}
		}
		num6 /= num;
		num7 /= num2;
		num8 /= num3;
		num9 /= num4;
		num10 /= num5;
		StatList.Add(new ShipStatVM("hull", new TextObject("{=wEmx6fZi}Hull"), _ship.ShipHull.Name.ToString()));
		StatList.Add(new ShipStatVM("class", new TextObject("{=sqdzHOPe}Class"), GetClassStr(_ship)));
		StatList.Add(new ShipStatVM("crew", new TextObject("{=wXCM8BnW}Crew"), GetCrewCapacityStr(_ship), GetBonusStr(num10, isPercentage: true), num10 > 0f, () => GetCrewCapacityTooltip(_ship)));
		StatList.Add(new ShipStatVM("cargo_capacity", new TextObject("{=IE1KbkaH}Cargo Capacity"), _ship.InventoryCapacity.ToString(), GetBonusStr(num8, isPercentage: true), num8 > 0f));
		StatList.Add(new ShipStatVM("weight", new TextObject("{=4Dd2xgPm}Weight"), (missionShipObject.Mass * (1f + _ship.ShipWeightFactor)).ToString("0"), GetBonusStr(num9, isPercentage: true), num9 < 0f));
		StatList.Add(new ShipStatVM("travel_speed", new TextObject("{=DbERaPfF}Travel Speed"), _ship.GetCampaignSpeed().ToString("0.##"), GetBonusStr(num6, isPercentage: true), num6 > 0f));
		StatList.Add(new ShipStatVM("sail_type", new TextObject("{=PJyFY05L}Sail"), GetSailTypeStr(missionShipObject)));
		StatList.Add(new ShipStatVM("draft_type", new TextObject("{=I4bu7cLr}Draft"), GetDraftTypeStr(_ship)));
		StatList.Add(new ShipStatVM("sea_worthiness", new TextObject("{=yCzuXN3O}Seaworthiness"), _ship.SeaWorthiness.ToString(), GetBonusStr(num11, isPercentage: false), num11 > 0));
		StatList.Add(new ShipStatVM("hit_points", new TextObject("{=oBbiVeKE}Hit Points"), GetHitPointsStr(_ship, currentHp), GetBonusStr(num7, isPercentage: true), num7 > 0f));
	}

	private string GetBonusStr(float bonus, bool isPercentage)
	{
		if (MathF.Abs(bonus) < 0.001f)
		{
			return string.Empty;
		}
		if (isPercentage)
		{
			string variable = GameTexts.FindText("str_NUMBER_percent").SetTextVariable("NUMBER", (bonus * 100f).ToString("+#;-#")).ToString();
			return GameTexts.FindText("str_STR_in_parentheses").SetTextVariable("STR", variable).ToString();
		}
		return GameTexts.FindText("str_STR_in_parentheses").SetTextVariable("STR", bonus.ToString("+#;-#")).ToString();
	}

	private string GetClassStr(Ship ship)
	{
		return GameTexts.FindText("str_ship_type", ship.ShipHull.Type.ToString().ToLowerInvariant()).ToString();
	}

	private string GetCrewCapacityStr(Ship ship)
	{
		int skeletalCrewCapacity = ship.SkeletalCrewCapacity;
		int mainDeckCrewCapacity = ship.MainDeckCrewCapacity;
		int num = ship.TotalCrewCapacity - ship.MainDeckCrewCapacity;
		TextObject textObject = ((num <= 0) ? new TextObject("{=!}{SKELETAL} • {DECK}") : new TextObject("{=!}{SKELETAL} • {DECK} + {RESERVE}"));
		return textObject.SetTextVariable("SKELETAL", skeletalCrewCapacity).SetTextVariable("DECK", mainDeckCrewCapacity).SetTextVariable("RESERVE", num)
			.ToString();
	}

	private List<TooltipProperty> GetCrewCapacityTooltip(Ship ship)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		int skeletalCrewCapacity = ship.SkeletalCrewCapacity;
		int mainDeckCrewCapacity = ship.MainDeckCrewCapacity;
		int totalCrewCapacity = ship.TotalCrewCapacity;
		int num = totalCrewCapacity - mainDeckCrewCapacity;
		list.Add(new TooltipProperty(new TextObject("{=kalMphFt}Skeletal Capacity").ToString(), skeletalCrewCapacity.ToString(), 0));
		list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewskeletal").ToString(), -1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
		list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		list.Add(new TooltipProperty(new TextObject("{=Bt82dbKu}Deck Capacity").ToString(), mainDeckCrewCapacity.ToString(), 0));
		list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewdeck").ToString(), -1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
		list.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		list.Add(new TooltipProperty(new TextObject("{=HThruy9f}Reserve Capacity").ToString(), num.ToString(), 0));
		list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewreserve").ToString(), -1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
		list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
		list.Add(new TooltipProperty(new TextObject("{=kLvWPxIK}Total Capacity").ToString(), totalCrewCapacity.ToString(), 0));
		list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewtotal").ToString(), -1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.MultiLine));
		return list;
	}

	private string GetSailTypeStr(MissionShipObject missionShipObject)
	{
		if (missionShipObject.HasSails)
		{
			bool flag = missionShipObject.Sails.Any((ShipSail x) => x.Type == SailType.Lateen);
			bool flag2 = missionShipObject.Sails.Any((ShipSail x) => x.Type == SailType.Square);
			if (flag && flag2)
			{
				return new TextObject("{=bXJLb0BE}Hybrid").ToString();
			}
			if (flag)
			{
				return new TextObject("{=kNxD2oer}Lateen").ToString();
			}
			if (flag2)
			{
				return new TextObject("{=squareSail}Square").ToString();
			}
		}
		return new TextObject("{=koX9okuG}None").ToString();
	}

	private string GetDraftTypeStr(Ship ship)
	{
		if (ship.ShipHull.CanNavigateShallowWater)
		{
			return new TextObject("{=ShipDraftTypeShallow}Shallow").ToString();
		}
		return new TextObject("{=ShipDraftTypeDeep}Deep").ToString();
	}

	private string GetHitPointsStr(Ship ship, float currentHp)
	{
		return GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", currentHp.ToString("0")).SetTextVariable("RIGHT", ship.MaxHitPoints.ToString("0"))
			.ToString();
	}
}
