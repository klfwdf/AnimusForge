using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

[EncyclopediaViewModel(typeof(ShipHull))]
public class EncyclopediaShipPageVM : EncyclopediaContentPageVM
{
	private readonly ShipHull _shipHull;

	private readonly MissionShipObject _missionShip;

	private string _descriptionText;

	private string _prefabId;

	private string _nameText;

	private string _availableUpgradesText;

	private string _statsText;

	private string _sailType;

	private EncyclopediaShipStatVM _sailTypeStat;

	private MBBindingList<EncyclopediaShipStatVM> _statList;

	private MBBindingList<EncyclopediaShipSlotVM> _allShipSlots;

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string AvailableUpgradesText
	{
		get
		{
			return _availableUpgradesText;
		}
		set
		{
			if (value != _availableUpgradesText)
			{
				_availableUpgradesText = value;
				OnPropertyChangedWithValue(value, "AvailableUpgradesText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string PrefabId
	{
		get
		{
			return _prefabId;
		}
		set
		{
			if (value != _prefabId)
			{
				_prefabId = value;
				OnPropertyChangedWithValue(value, "PrefabId");
			}
		}
	}

	[DataSourceProperty]
	public string StatsText
	{
		get
		{
			return _statsText;
		}
		set
		{
			if (value != _statsText)
			{
				_statsText = value;
				OnPropertyChangedWithValue(value, "StatsText");
			}
		}
	}

	[DataSourceProperty]
	public string SailType
	{
		get
		{
			return _sailType;
		}
		set
		{
			if (value != _sailType)
			{
				_sailType = value;
				OnPropertyChangedWithValue(value, "SailType");
			}
		}
	}

	[DataSourceProperty]
	public EncyclopediaShipStatVM SailTypeStat
	{
		get
		{
			return _sailTypeStat;
		}
		set
		{
			if (value != _sailTypeStat)
			{
				_sailTypeStat = value;
				OnPropertyChangedWithValue(value, "SailTypeStat");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaShipStatVM> StatList
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

	[DataSourceProperty]
	public MBBindingList<EncyclopediaShipSlotVM> AllShipSlots
	{
		get
		{
			return _allShipSlots;
		}
		set
		{
			if (value != _allShipSlots)
			{
				_allShipSlots = value;
				OnPropertyChangedWithValue(value, "AllShipSlots");
			}
		}
	}

	public EncyclopediaShipPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
		_shipHull = base.Obj as ShipHull;
		_missionShip = MBObjectManager.Instance.GetObject<MissionShipObject>(_shipHull.MissionShipObjectId);
		StatList = new MBBindingList<EncyclopediaShipStatVM>();
		AllShipSlots = new MBBindingList<EncyclopediaShipSlotVM>();
		base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(_shipHull);
		SailType = GetSailType();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = GetName();
		PrefabId = GetPrefabIdOfShipHull(_shipHull);
		DescriptionText = _shipHull.Description?.ToString() ?? "";
		AvailableUpgradesText = new TextObject("{=0xN2FaYa}Available Upgrades").ToString();
		RefreshShipSlots();
		RefreshStats();
		UpdateBookmarkHintText();
	}

	private void RefreshShipSlots()
	{
		AllShipSlots.Clear();
		foreach (ShipSlot shipSlot in MBObjectManager.Instance.GetObjectTypeList<ShipSlot>())
		{
			AllShipSlots.Add(new EncyclopediaShipSlotVM(shipSlot.TypeId, _shipHull.AvailableSlots.Values.Any((ShipSlot x) => x.TypeId == shipSlot.TypeId)));
		}
		AllShipSlots.Add(new EncyclopediaShipSlotVM("figurehead", _shipHull.CanEquipFigurehead));
	}

	private void RefreshStats()
	{
		StatList.Clear();
		StatsText = new TextObject("{=ffjTMejn}Stats").ToString();
		StatList.Add(new EncyclopediaShipStatVM("hull", new TextObject("{=wEmx6fZi}Hull"), _shipHull.Name.ToString()));
		StatList.Add(new EncyclopediaShipStatVM("class", new TextObject("{=sqdzHOPe}Class"), GameTexts.FindText("str_ship_type", _shipHull.Type.ToString().ToLowerInvariant()).ToString()));
		StatList.Add(new EncyclopediaShipStatVM("crew", new TextObject("{=wXCM8BnW}Crew"), GetCrewCapacityStr(), GetCrewCapacityTooltip));
		StatList.Add(new EncyclopediaShipStatVM("cargo_capacity", new TextObject("{=IE1KbkaH}Cargo Capacity"), _shipHull.InventoryCapacity.ToString()));
		StatList.Add(new EncyclopediaShipStatVM("weight", new TextObject("{=4Dd2xgPm}Weight"), _missionShip.Mass.ToString("0")));
		StatList.Add(new EncyclopediaShipStatVM("travel_speed", new TextObject("{=DbERaPfF}Travel Speed"), _shipHull.BaseSpeed.ToString("0.##")));
		SailTypeStat = new EncyclopediaShipStatVM("sail_type", new TextObject("{=PJyFY05L}Sail"), GetSailTypeDescription());
		StatList.Add(SailTypeStat);
		StatList.Add(new EncyclopediaShipStatVM("draft_type", new TextObject("{=I4bu7cLr}Draft"), GetDraftTypeStr()));
		StatList.Add(new EncyclopediaShipStatVM("sea_worthiness", new TextObject("{=yCzuXN3O}Seaworthiness"), _shipHull.SeaWorthiness.ToString()));
		StatList.Add(new EncyclopediaShipStatVM("hit_points", new TextObject("{=oBbiVeKE}Hit Points"), _shipHull.MaxHitPoints.ToString()));
	}

	private string GetSailType()
	{
		if (_missionShip.HasSails)
		{
			bool flag = _missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Lateen);
			bool flag2 = _missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Square);
			if (flag && flag2)
			{
				return "Hybrid";
			}
			if (flag)
			{
				return "Lateen";
			}
			if (flag2)
			{
				return "Square";
			}
		}
		return "None";
	}

	private string GetSailTypeDescription()
	{
		if (_missionShip.HasSails)
		{
			bool flag = _missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Lateen);
			bool flag2 = _missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Square);
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

	private string GetDraftTypeStr()
	{
		if (_shipHull.CanNavigateShallowWater)
		{
			return new TextObject("{=ShipDraftTypeShallow}Shallow").ToString();
		}
		return new TextObject("{=ShipDraftTypeDeep}Deep").ToString();
	}

	private string GetCrewCapacityStr()
	{
		int skeletalCrewCapacity = _shipHull.SkeletalCrewCapacity;
		int mainDeckCrewCapacity = _shipHull.MainDeckCrewCapacity;
		int num = _shipHull.TotalCrewCapacity - _shipHull.MainDeckCrewCapacity;
		TextObject textObject = ((num <= 0) ? new TextObject("{=!}{SKELETAL} • {DECK}") : new TextObject("{=!}{SKELETAL} • {DECK} + {RESERVE}"));
		return textObject.SetTextVariable("SKELETAL", skeletalCrewCapacity).SetTextVariable("DECK", mainDeckCrewCapacity).SetTextVariable("RESERVE", num)
			.ToString();
	}

	private List<TooltipProperty> GetCrewCapacityTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		int skeletalCrewCapacity = _shipHull.SkeletalCrewCapacity;
		int mainDeckCrewCapacity = _shipHull.MainDeckCrewCapacity;
		int totalCrewCapacity = _shipHull.TotalCrewCapacity;
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

	public override string GetName()
	{
		return _shipHull.Name.ToString();
	}

	private static string GetPrefabIdOfShipHull(ShipHull shipHull)
	{
		return MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId)?.Prefab ?? string.Empty;
	}

	public override string GetNavigationBarURL()
	{
		return string.Concat(string.Concat(string.Concat(HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ", HyperlinkTexts.GetGenericHyperlinkText("ListPage-Ships", GameTexts.FindText("str_encyclopedia_ships").ToString())), " \\ "), GetName());
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public override void ExecuteSwitchBookmarkedState()
	{
		base.ExecuteSwitchBookmarkedState();
		if (base.IsBookmarked)
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(_shipHull);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(_shipHull);
		}
	}
}
