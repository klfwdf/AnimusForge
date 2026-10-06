using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;

public abstract class MPArmoryCosmeticItemBaseVM : ViewModel
{
	public readonly CosmeticElement Cosmetic;

	public readonly string CosmeticID;

	private bool _isUnlocked;

	private bool _isUsed;

	private bool _areActionsEnabled;

	private bool _isSelectable;

	private bool _isUnequippable;

	private int _cost;

	private int _rarity;

	private int _itemType;

	private string _name;

	private string _ownedText;

	private string _actionText;

	private string _previewText;

	private ItemImageIdentifierVM _icon;

	private InputKeyItemVM _actionKey;

	private InputKeyItemVM _previewKey;

	public string UnequipText { get; private set; }

	public CosmeticsManager.CosmeticType CosmeticType { get; }

	[DataSourceProperty]
	public bool IsUnlocked
	{
		get
		{
			return _isUnlocked;
		}
		set
		{
			if (value != _isUnlocked)
			{
				_isUnlocked = value;
				OnPropertyChangedWithValue(value, "IsUnlocked");
				UpdatePreviewAndActionTexts();
			}
		}
	}

	[DataSourceProperty]
	public bool IsUsed
	{
		get
		{
			return _isUsed;
		}
		set
		{
			if (value != _isUsed)
			{
				_isUsed = value;
				OnPropertyChangedWithValue(value, "IsUsed");
				UpdatePreviewAndActionTexts();
			}
		}
	}

	[DataSourceProperty]
	public bool AreActionsEnabled
	{
		get
		{
			return _areActionsEnabled;
		}
		set
		{
			if (value != _areActionsEnabled)
			{
				_areActionsEnabled = value;
				OnPropertyChangedWithValue(value, "AreActionsEnabled");
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

	[DataSourceProperty]
	public bool IsUnequippable
	{
		get
		{
			return _isUnequippable;
		}
		set
		{
			if (value != _isUnequippable)
			{
				_isUnequippable = value;
				OnPropertyChangedWithValue(value, "IsUnequippable");
			}
		}
	}

	[DataSourceProperty]
	public int Cost
	{
		get
		{
			return _cost;
		}
		set
		{
			if (value != _cost)
			{
				_cost = value;
				OnPropertyChangedWithValue(value, "Cost");
			}
		}
	}

	[DataSourceProperty]
	public int Rarity
	{
		get
		{
			return _rarity;
		}
		set
		{
			if (value != _rarity)
			{
				_rarity = value;
				OnPropertyChangedWithValue(value, "Rarity");
			}
		}
	}

	[DataSourceProperty]
	public int ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (value != _itemType)
			{
				_itemType = value;
				OnPropertyChangedWithValue(value, "ItemType");
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
	public string OwnedText
	{
		get
		{
			return _ownedText;
		}
		set
		{
			if (value != _ownedText)
			{
				_ownedText = value;
				OnPropertyChangedWithValue(value, "OwnedText");
			}
		}
	}

	[DataSourceProperty]
	public string ActionText
	{
		get
		{
			return _actionText;
		}
		set
		{
			if (value != _actionText)
			{
				_actionText = value;
				OnPropertyChangedWithValue(value, "ActionText");
			}
		}
	}

	[DataSourceProperty]
	public string PreviewText
	{
		get
		{
			return _previewText;
		}
		set
		{
			if (value != _previewText)
			{
				_previewText = value;
				OnPropertyChangedWithValue(value, "PreviewText");
			}
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM Icon
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
	public InputKeyItemVM ActionKey
	{
		get
		{
			return _actionKey;
		}
		set
		{
			if (value != _actionKey)
			{
				_actionKey = value;
				OnPropertyChangedWithValue(value, "ActionKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PreviewKey
	{
		get
		{
			return _previewKey;
		}
		set
		{
			if (value != _previewKey)
			{
				_previewKey = value;
				OnPropertyChangedWithValue(value, "PreviewKey");
			}
		}
	}

	public static event Action<MPArmoryCosmeticItemBaseVM> OnEquipped;

	public static event Action<MPArmoryCosmeticItemBaseVM> OnPurchaseRequested;

	public static event Action<MPArmoryCosmeticItemBaseVM> OnPreviewed;

	public MPArmoryCosmeticItemBaseVM(CosmeticElement cosmetic, string cosmeticID, CosmeticsManager.CosmeticType cosmeticType)
	{
		Cosmetic = cosmetic;
		CosmeticID = cosmeticID;
		Cost = cosmetic.Cost;
		Rarity = (int)cosmetic.Rarity;
		CosmeticType = cosmeticType;
		IsUnequippable = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OwnedText = new TextObject("{=B5bcj3pC}Owned").ToString();
		UnequipText = new TextObject("{=QndVFTbx}Unequip").ToString();
		UpdatePreviewAndActionTexts();
	}

	public override void OnFinalize()
	{
		ActionKey?.OnFinalize();
		PreviewKey?.OnFinalize();
	}

	public void ExecuteAction()
	{
		if (IsUnlocked)
		{
			MPArmoryCosmeticItemBaseVM.OnEquipped?.Invoke(this);
		}
		else
		{
			MPArmoryCosmeticItemBaseVM.OnPurchaseRequested(this);
		}
	}

	public void ExecutePreview()
	{
		MPArmoryCosmeticItemBaseVM.OnPreviewed(this);
	}

	public void ExecuteEnableActions()
	{
		AreActionsEnabled = true;
	}

	public void ExecuteDisableActions()
	{
		AreActionsEnabled = false;
	}

	protected void UpdatePreviewAndActionTexts()
	{
		if (IsUnlocked)
		{
			if (IsUsed)
			{
				ActionText = (IsUnequippable ? UnequipText : string.Empty);
			}
			else
			{
				ActionText = new TextObject("{=DKqLY1aJ}Equip").ToString();
			}
		}
		else
		{
			ActionText = new TextObject("{=i2mNBaxE}Obtain").ToString();
		}
		PreviewText = new TextObject("{=un7poy9x}Preview").ToString();
	}

	public void RefreshKeyBindings(HotKey actionKey, HotKey previewKey)
	{
		if (IsUnlocked && IsUsed && !IsUnequippable)
		{
			ActionKey = InputKeyItemVM.CreateFromHotKey(null, isConsoleOnly: false);
		}
		else if (actionKey.GroupId != ActionKey?.HotKey?.GroupId || actionKey.Id != ActionKey?.HotKey?.Id)
		{
			ActionKey = InputKeyItemVM.CreateFromHotKey(actionKey, isConsoleOnly: false);
		}
		if (previewKey.GroupId != PreviewKey?.HotKey?.GroupId || previewKey.Id != PreviewKey?.HotKey?.Id)
		{
			PreviewKey = InputKeyItemVM.CreateFromHotKey(previewKey, isConsoleOnly: false);
		}
		UpdatePreviewAndActionTexts();
	}
}
