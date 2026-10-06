using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;

public class MPLobbyClassFilterClassItemVM : ViewModel
{
	private Action<MPLobbyClassFilterClassItemVM> _onSelect;

	private bool _isEnabled;

	private bool _isSelected;

	private Color _cultureColor;

	private string _name;

	private string _iconType;

	public MultiplayerClassDivisions.MPHeroClass HeroClass { get; private set; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor
	{
		get
		{
			return _cultureColor;
		}
		set
		{
			if (value != _cultureColor)
			{
				_cultureColor = value;
				OnPropertyChangedWithValue(value, "CultureColor");
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
			}
		}
	}

	public MPLobbyClassFilterClassItemVM(BasicCultureObject culture, MultiplayerClassDivisions.MPHeroClass heroClass, Action<MPLobbyClassFilterClassItemVM> onSelect)
	{
		HeroClass = heroClass;
		_onSelect = onSelect;
		CultureColor = Color.FromUint(culture.Color);
		IconType = HeroClass.IconType.ToString();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = HeroClass.HeroName.ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		HeroClass = null;
	}

	private void ExecuteSelect()
	{
		if (_onSelect != null)
		{
			_onSelect(this);
		}
	}
}
