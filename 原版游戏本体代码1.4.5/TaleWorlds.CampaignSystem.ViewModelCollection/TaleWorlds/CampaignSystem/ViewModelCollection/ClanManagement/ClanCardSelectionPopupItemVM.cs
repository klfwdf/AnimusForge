using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanCardSelectionPopupItemVM : ViewModel
{
	private readonly TextObject _titleText;

	private readonly TextObject _disabledReasonText;

	private readonly TextObject _specialActionText;

	private readonly Action<ClanCardSelectionPopupItemVM> _onSelected;

	private ImageIdentifierVM _image;

	private MBBindingList<ClanCardSelectionPopupItemPropertyVM> _properties;

	private HintViewModel _disabledHint;

	private string _title;

	private string _spriteType;

	private string _spriteName;

	private string _spriteLabel;

	private string _specialAction;

	private bool _hasImage;

	private bool _hasSprite;

	private bool _isSpecialActionItem;

	private bool _isDisabled;

	private bool _isSelected;

	public object Identifier { get; }

	public TextObject ActionResultText { get; }

	[DataSourceProperty]
	public ImageIdentifierVM Image
	{
		get
		{
			return _image;
		}
		set
		{
			if (value != _image)
			{
				_image = value;
				OnPropertyChangedWithValue(value, "Image");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanCardSelectionPopupItemPropertyVM> Properties
	{
		get
		{
			return _properties;
		}
		set
		{
			if (value != _properties)
			{
				_properties = value;
				OnPropertyChangedWithValue(value, "Properties");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

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
	public string SpriteType
	{
		get
		{
			return _spriteType;
		}
		set
		{
			if (value != _spriteType)
			{
				_spriteType = value;
				OnPropertyChangedWithValue(value, "SpriteType");
			}
		}
	}

	[DataSourceProperty]
	public string SpriteName
	{
		get
		{
			return _spriteName;
		}
		set
		{
			if (value != _spriteName)
			{
				_spriteName = value;
				OnPropertyChangedWithValue(value, "SpriteName");
			}
		}
	}

	[DataSourceProperty]
	public string SpriteLabel
	{
		get
		{
			return _spriteLabel;
		}
		set
		{
			if (value != _spriteLabel)
			{
				_spriteLabel = value;
				OnPropertyChangedWithValue(value, "SpriteLabel");
			}
		}
	}

	[DataSourceProperty]
	public string SpecialAction
	{
		get
		{
			return _specialAction;
		}
		set
		{
			if (value != _specialAction)
			{
				_specialAction = value;
				OnPropertyChangedWithValue(value, "SpecialAction");
			}
		}
	}

	[DataSourceProperty]
	public bool HasImage
	{
		get
		{
			return _hasImage;
		}
		set
		{
			if (value != _hasImage)
			{
				_hasImage = value;
				OnPropertyChangedWithValue(value, "HasImage");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSprite
	{
		get
		{
			return _hasSprite;
		}
		set
		{
			if (value != _hasSprite)
			{
				_hasSprite = value;
				OnPropertyChangedWithValue(value, "HasSprite");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSpecialActionItem
	{
		get
		{
			return _isSpecialActionItem;
		}
		set
		{
			if (value != _isSpecialActionItem)
			{
				_isSpecialActionItem = value;
				OnPropertyChangedWithValue(value, "IsSpecialActionItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
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

	public ClanCardSelectionPopupItemVM(in ClanCardSelectionItemInfo info, Action<ClanCardSelectionPopupItemVM> onSelected)
	{
		Identifier = info.Identifier;
		_onSelected = onSelected;
		ActionResultText = info.ActionResult;
		_titleText = info.Title;
		_disabledReasonText = info.DisabledReason;
		_specialActionText = info.SpecialActionText;
		DisabledHint = new HintViewModel();
		Properties = new MBBindingList<ClanCardSelectionPopupItemPropertyVM>();
		if (info.Properties != null)
		{
			foreach (ClanCardSelectionItemPropertyInfo property in info.Properties)
			{
				ClanCardSelectionItemPropertyInfo info2 = property;
				Properties.Add(new ClanCardSelectionPopupItemPropertyVM(in info2));
			}
		}
		IsDisabled = info.IsDisabled;
		IsSpecialActionItem = info.IsSpecialActionItem;
		HasSprite = !string.IsNullOrEmpty(info.SpriteName);
		HasImage = info.Image != null;
		SpriteType = info.SpriteType.ToString();
		SpriteName = info.SpriteName ?? string.Empty;
		SpriteLabel = info.SpriteLabel ?? string.Empty;
		Image = new GenericImageIdentifierVM(info.Image);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _titleText?.ToString() ?? string.Empty;
		SpecialAction = _specialActionText?.ToString() ?? string.Empty;
		DisabledHint.HintText = (IsDisabled ? _disabledReasonText : TextObject.GetEmpty());
		Properties.ApplyActionOnAllItems(delegate(ClanCardSelectionPopupItemPropertyVM x)
		{
			x.RefreshValues();
		});
	}

	public void ExecuteSelect()
	{
		_onSelected?.Invoke(this);
	}
}
