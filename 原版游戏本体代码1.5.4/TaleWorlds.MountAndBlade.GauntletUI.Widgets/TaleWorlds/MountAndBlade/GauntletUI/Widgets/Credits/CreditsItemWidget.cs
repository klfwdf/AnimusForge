using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Credits;

public class CreditsItemWidget : Widget
{
	private bool _initialized;

	private string _itemType;

	private Widget _categoryWidget;

	private Widget _sectionWidget;

	private Widget _entryWidget;

	private Widget _emptyLineWidget;

	private Widget _imageWidget;

	[Editor(false)]
	public string ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (_itemType != value)
			{
				_itemType = value;
				OnPropertyChanged(value, "ItemType");
			}
		}
	}

	[Editor(false)]
	public Widget CategoryWidget
	{
		get
		{
			return _categoryWidget;
		}
		set
		{
			if (_categoryWidget != value)
			{
				_categoryWidget = value;
				OnPropertyChanged(value, "CategoryWidget");
			}
		}
	}

	[Editor(false)]
	public Widget ImageWidget
	{
		get
		{
			return _imageWidget;
		}
		set
		{
			if (_imageWidget != value)
			{
				_imageWidget = value;
				OnPropertyChanged(value, "ImageWidget");
			}
		}
	}

	[Editor(false)]
	public Widget SectionWidget
	{
		get
		{
			return _sectionWidget;
		}
		set
		{
			if (_sectionWidget != value)
			{
				_sectionWidget = value;
				OnPropertyChanged(value, "SectionWidget");
			}
		}
	}

	[Editor(false)]
	public Widget EntryWidget
	{
		get
		{
			return _entryWidget;
		}
		set
		{
			if (_entryWidget != value)
			{
				_entryWidget = value;
				OnPropertyChanged(value, "EntryWidget");
			}
		}
	}

	[Editor(false)]
	public Widget EmptyLineWidget
	{
		get
		{
			return _emptyLineWidget;
		}
		set
		{
			if (_emptyLineWidget != value)
			{
				_emptyLineWidget = value;
				OnPropertyChanged(value, "EmptyLineWidget");
			}
		}
	}

	public CreditsItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			RefreshItemWidget();
			_initialized = true;
		}
	}

	private void RefreshItemWidget()
	{
		if (string.IsNullOrEmpty(ItemType))
		{
			return;
		}
		if (CategoryWidget != null)
		{
			CategoryWidget.IsVisible = ItemType == "Category";
		}
		if (SectionWidget != null)
		{
			SectionWidget.IsVisible = ItemType == "Section";
		}
		if (EntryWidget != null)
		{
			EntryWidget.IsVisible = ItemType == "Entry";
		}
		if (EmptyLineWidget != null)
		{
			EmptyLineWidget.IsVisible = ItemType == "EmptyLine";
		}
		if (ImageWidget != null)
		{
			ImageWidget.IsVisible = ItemType == "Image";
			if (ImageWidget.Sprite != null)
			{
				ImageWidget.SuggestedWidth = ImageWidget.Sprite.Width;
				ImageWidget.SuggestedHeight = ImageWidget.Sprite.Height;
			}
		}
	}
}
