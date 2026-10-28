using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class ItemFlagVM : ViewModel
{
	private string _icon;

	private HintViewModel _hint;

	[DataSourceProperty]
	public string Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			if (value != _icon)
			{
				_icon = value;
				OnPropertyChangedWithValue(value, "Icon");
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

	public ItemFlagVM(string iconName, TextObject hint)
	{
		Icon = GetIconPath(iconName);
		Hint = new HintViewModel(hint);
	}

	private string GetIconPath(string iconName)
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "GetIconPath");
		mBStringBuilder.Append("<img src=\"SPGeneral\\");
		mBStringBuilder.Append(iconName);
		mBStringBuilder.Append("\"/>");
		return mBStringBuilder.ToStringAndRelease();
	}
}
