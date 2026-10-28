using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;

public class MPLobbyClassFilterClassGroupItemVM : ViewModel
{
	private string _name;

	private MBBindingList<MPLobbyClassFilterClassItemVM> _classes;

	public MultiplayerClassDivisions.MPHeroClassGroup ClassGroup { get; set; }

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
	public MBBindingList<MPLobbyClassFilterClassItemVM> Classes
	{
		get
		{
			return _classes;
		}
		set
		{
			if (value != _classes)
			{
				_classes = value;
				OnPropertyChangedWithValue(value, "Classes");
			}
		}
	}

	public MPLobbyClassFilterClassGroupItemVM(MultiplayerClassDivisions.MPHeroClassGroup classGroup)
	{
		ClassGroup = classGroup;
		Classes = new MBBindingList<MPLobbyClassFilterClassItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = ClassGroup.Name.ToString();
		Classes.ApplyActionOnAllItems(delegate(MPLobbyClassFilterClassItemVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ClassGroup = null;
	}

	public void AddClass(BasicCultureObject culture, MultiplayerClassDivisions.MPHeroClass heroClass, Action<MPLobbyClassFilterClassItemVM> onSelect)
	{
		MPLobbyClassFilterClassItemVM item = new MPLobbyClassFilterClassItemVM(culture, heroClass, onSelect);
		Classes.Add(item);
	}
}
