using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingItemFlagVM : ItemFlagVM
{
	private bool _isDisplayed;

	private string _iconPath;

	[DataSourceProperty]
	public bool IsDisplayed
	{
		get
		{
			return _isDisplayed;
		}
		set
		{
			if (value != _isDisplayed)
			{
				_isDisplayed = value;
				OnPropertyChangedWithValue(value, "IsDisplayed");
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

	public CraftingItemFlagVM(string iconPath, TextObject hint, bool isDisplayed)
		: base(iconPath, hint)
	{
		IsDisplayed = isDisplayed;
		IconPath = "SPGeneral\\" + iconPath;
	}
}
