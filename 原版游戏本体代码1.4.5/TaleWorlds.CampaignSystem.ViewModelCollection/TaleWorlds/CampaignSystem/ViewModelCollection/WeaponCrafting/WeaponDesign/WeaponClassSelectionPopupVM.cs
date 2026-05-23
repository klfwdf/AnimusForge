using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponClassSelectionPopupVM : ViewModel
{
	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private readonly Action<int> _onSelect;

	private readonly List<CraftingTemplate> _templatesList;

	private readonly Func<CraftingTemplate, int> _getUnlockedPiecesCount;

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

	public WeaponClassSelectionPopupVM(ICraftingCampaignBehavior craftingBehavior, List<CraftingTemplate> templatesList, Action<int> onSelect, Func<CraftingTemplate, int> getUnlockedPiecesCount)
	{
		WeaponClasses = new MBBindingList<WeaponClassVM>();
		_craftingBehavior = craftingBehavior;
		_onSelect = onSelect;
		_templatesList = templatesList;
		_getUnlockedPiecesCount = getUnlockedPiecesCount;
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
			weaponClass.HasNewlyUnlockedPieces = weaponClass.NewlyUnlockedPieceCount > 0;
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PopupHeader = new TextObject("{=wZGj3qO1}Choose What to Craft").ToString();
	}

	public void UpdateNewlyUnlockedPiecesCount(List<CraftingPiece> newlyUnlockedPieces)
	{
		for (int i = 0; i < WeaponClasses.Count; i++)
		{
			WeaponClassVM weaponClassVM = WeaponClasses[i];
			int num = 0;
			for (int j = 0; j < newlyUnlockedPieces.Count; j++)
			{
				CraftingPiece craftingPiece = newlyUnlockedPieces[j];
				if (weaponClassVM.Template.IsPieceTypeUsable(craftingPiece.PieceType))
				{
					CraftingPiece craftingPiece2 = FindPieceInTemplate(weaponClassVM.Template, craftingPiece);
					if (craftingPiece2 != null && !craftingPiece2.IsHiddenOnDesigner && _craftingBehavior.IsOpened(craftingPiece2, weaponClassVM.Template))
					{
						num++;
					}
				}
			}
			weaponClassVM.NewlyUnlockedPieceCount = num;
		}
	}

	private CraftingPiece FindPieceInTemplate(CraftingTemplate template, CraftingPiece piece)
	{
		foreach (CraftingPiece piece2 in template.Pieces)
		{
			if (piece.StringId == piece2.StringId)
			{
				return piece2;
			}
		}
		return null;
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
