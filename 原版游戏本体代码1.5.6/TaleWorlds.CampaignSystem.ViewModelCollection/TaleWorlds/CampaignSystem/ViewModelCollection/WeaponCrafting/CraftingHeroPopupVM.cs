using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingHeroPopupVM : ViewModel
{
	private readonly Func<MBBindingList<CraftingAvailableHeroItemVM>> GetCraftingHeroes;

	private bool _isVisible;

	private string _selectHeroText;

	private InputKeyItemVM _exitInputKey;

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	[DataSourceProperty]
	public string SelectHeroText
	{
		get
		{
			return _selectHeroText;
		}
		set
		{
			if (value != _selectHeroText)
			{
				_selectHeroText = value;
				OnPropertyChangedWithValue(value, "SelectHeroText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingAvailableHeroItemVM> CraftingHeroes => GetCraftingHeroes();

	[DataSourceProperty]
	public InputKeyItemVM ExitInputKey
	{
		get
		{
			return _exitInputKey;
		}
		set
		{
			if (value != _exitInputKey)
			{
				_exitInputKey = value;
				OnPropertyChangedWithValue(value, "ExitInputKey");
			}
		}
	}

	public CraftingHeroPopupVM(Func<MBBindingList<CraftingAvailableHeroItemVM>> getCraftingHeroes)
	{
		GetCraftingHeroes = getCraftingHeroes;
		SelectHeroText = new TextObject("{=xaeXEj8J}Select character for smithing").ToString();
	}

	public void ExecuteOpenPopup()
	{
		IsVisible = true;
	}

	public void ExecuteClosePopup()
	{
		IsVisible = false;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ExitInputKey?.OnFinalize();
	}

	public void SetExitInputKey(HotKey hotKey)
	{
		ExitInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
