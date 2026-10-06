using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class MPPerkVM : ViewModel
{
	public readonly IReadOnlyPerkObject Perk;

	private readonly Action<MPPerkVM> _onSelectPerk;

	private string _iconType;

	private string _name;

	private string _description;

	private bool _isSelectable;

	private HintViewModel _hint;

	public int PerkIndex { get; private set; }

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

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
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
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChangedWithValue(value, "IsSelectable");
			}
		}
	}

	public MPPerkVM(Action<MPPerkVM> onSelectPerk, IReadOnlyPerkObject perk, bool isSelectable, int perkIndex)
	{
		Perk = perk;
		PerkIndex = perkIndex;
		_onSelectPerk = onSelectPerk;
		IconType = perk.IconId;
		IsSelectable = isSelectable;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Perk.Name.ToString();
		Description = Perk.Description.ToString();
		GameTexts.SetVariable("newline", "\n");
		Hint = new HintViewModel(Perk.Description);
	}

	public void ExecuteSelectPerk()
	{
		_onSelectPerk(this);
	}
}
