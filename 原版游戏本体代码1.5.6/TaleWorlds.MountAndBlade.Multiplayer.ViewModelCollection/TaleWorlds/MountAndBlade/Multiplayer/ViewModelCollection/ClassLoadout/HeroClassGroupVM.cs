using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class HeroClassGroupVM : ViewModel
{
	public readonly MultiplayerClassDivisions.MPHeroClassGroup HeroClassGroup;

	private readonly Action<HeroPerkVM, MPPerkVM> _onPerkSelect;

	private string _name;

	private string _iconType;

	private string _iconPath;

	private MBBindingList<HeroClassVM> _subClasses;

	public bool IsValid => SubClasses.Count > 0;

	[DataSourceProperty]
	public MBBindingList<HeroClassVM> SubClasses
	{
		get
		{
			return _subClasses;
		}
		set
		{
			if (value != _subClasses)
			{
				_subClasses = value;
				OnPropertyChangedWithValue(value, "SubClasses");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (value != _iconType)
			{
				_iconType = value;
				OnPropertyChangedWithValue(value, "IconType");
				IconPath = "TroopBanners\\ClassType_" + value;
			}
		}
	}

	[DataSourceProperty]
	public string IconPath
	{
		get
		{
			return _iconPath;
		}
		set
		{
			if (value != _iconPath)
			{
				_iconPath = value;
				OnPropertyChangedWithValue(value, "IconPath");
			}
		}
	}

	public HeroClassGroupVM(Action<HeroClassVM> onSelect, Action<HeroPerkVM, MPPerkVM> onPerkSelect, MultiplayerClassDivisions.MPHeroClassGroup heroClassGroup, MultiplayerBattleColors.MultiplayerCultureColorInfo colorInfo)
	{
		HeroClassGroup = heroClassGroup;
		_onPerkSelect = onPerkSelect;
		IconType = heroClassGroup.StringId;
		SubClasses = new MBBindingList<HeroClassVM>();
		_ = GameNetwork.MyPeer.GetComponent<MissionPeer>().Team;
		foreach (MultiplayerClassDivisions.MPHeroClass item in from h in MultiplayerClassDivisions.GetMPHeroClasses(GameNetwork.MyPeer.GetComponent<MissionPeer>().Culture)
			where h.ClassGroup.Equals(heroClassGroup)
			select h)
		{
			SubClasses.Add(new HeroClassVM(onSelect, _onPerkSelect, item, colorInfo));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = HeroClassGroup.Name.ToString();
		SubClasses.ApplyActionOnAllItems(delegate(HeroClassVM x)
		{
			x.RefreshValues();
		});
	}
}
