using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponClassSelectionPopupVM : ViewModel
{
	private readonly Action<int> _onSelect;

	private readonly List<CraftingTemplate> _templatesList;

	private readonly Func<CraftingTemplate, int> _getUnlockedPiecesCount;

	private readonly Func<CraftingTemplate, int> _getUninspectedPiecesCount;

	private string _popupHeader;

	private bool _isVisible;

	private MBBindingList<WeaponClassVM> _weaponClasses;

	[DataSourceProperty]
	public string PopupHeader
	{
		get
		{
			return _popupHeader;
		}
		set
		{
			if (value != _popupHeader)
			{
				_popupHeader = value;
				OnPropertyChangedWithValue(value, "PopupHeader");
			}
		}
	}

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
				Game.Current?.EventManager.TriggerEvent(new CraftingWeaponClassSelectionOpenedEvent(_isVisible));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<WeaponClassVM> WeaponClasses
	{
		get
		{
			return _weaponClasses;
		}
		set
		{
			if (value != _weaponClasses)
			{
				_weaponClasses = value;
				OnPropertyChangedWithValue(value, "WeaponClasses");
			}
		}
	}

	public WeaponClassSelectionPopupVM(List<CraftingTemplate> templatesList, Action<int> onSelect, Func<CraftingTemplate, int> getUnlockedPiecesCount, Func<CraftingTemplate, int> getUninspectedPiecesCount)
	{
		WeaponClasses = new MBBindingList<WeaponClassVM>();
		_onSelect = onSelect;
		_templatesList = templatesList;
		_getUnlockedPiecesCount = getUnlockedPiecesCount;
		_getUninspectedPiecesCount = getUninspectedPiecesCount;
		foreach (CraftingTemplate templates in _templatesList)
		{
			WeaponClasses.Add(new WeaponClassVM(_templatesList.IndexOf(templates), templates, ExecuteSelectWeaponClass));
		}
		RefreshList();
		RefreshValues();
	}

	private void RefreshList()
	{
		foreach (WeaponClassVM weaponClass in WeaponClasses)
		{
			weaponClass.UnlockedPiecesCount = _getUnlockedPiecesCount?.Invoke(weaponClass.Template) ?? 0;
			Func<CraftingTemplate, int> getUninspectedPiecesCount = _getUninspectedPiecesCount;
			weaponClass.HasNewlyUnlockedPieces = getUninspectedPiecesCount != null && getUninspectedPiecesCount(weaponClass.Template) > 0;
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PopupHeader = new TextObject("{=wZGj3qO1}Choose What to Craft").ToString();
	}

	public void ExecuteSelectWeaponClass(int index)
	{
		if (WeaponClasses[index].IsSelected)
		{
			ExecuteClosePopup();
			return;
		}
		_onSelect?.Invoke(index);
		ExecuteClosePopup();
	}

	public void ExecuteClosePopup()
	{
		IsVisible = false;
	}

	public void ExecuteOpenPopup()
	{
		IsVisible = true;
		RefreshList();
	}
}
