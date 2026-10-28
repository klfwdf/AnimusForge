using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace MCM.UI.Dropdown;

internal abstract class MCMSelectorItemVMBase : ViewModel
{
	protected string? _stringItem;

	protected bool _canBeSelected = true;

	protected HintViewModel? _hint;

	protected bool _isSelected;

	[DataSourceProperty]
	public bool CanBeSelected
	{
		get
		{
			return _canBeSelected;
		}
		set
		{
			SetField(ref _canBeSelected, value, "CanBeSelected");
		}
	}

	[DataSourceProperty]
	public string? StringItem
	{
		get
		{
			return _stringItem;
		}
		set
		{
			SetField(ref _stringItem, value, "StringItem");
		}
	}

	[DataSourceProperty]
	public HintViewModel? Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			SetField(ref _hint, value, "Hint");
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
			SetField(ref _isSelected, value, "IsSelected");
		}
	}
}
