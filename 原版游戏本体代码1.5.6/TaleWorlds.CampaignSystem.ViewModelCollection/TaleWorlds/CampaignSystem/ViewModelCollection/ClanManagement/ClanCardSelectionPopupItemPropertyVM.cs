using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanCardSelectionPopupItemPropertyVM : ViewModel
{
	private readonly TextObject _titleText;

	private readonly TextObject _valueText;

	private string _title;

	private string _value;

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	public ClanCardSelectionPopupItemPropertyVM(in ClanCardSelectionItemPropertyInfo info)
	{
		_titleText = info.Title;
		_valueText = info.Value;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _titleText?.ToString() ?? string.Empty;
		Value = _valueText?.ToString() ?? string.Empty;
	}
}
