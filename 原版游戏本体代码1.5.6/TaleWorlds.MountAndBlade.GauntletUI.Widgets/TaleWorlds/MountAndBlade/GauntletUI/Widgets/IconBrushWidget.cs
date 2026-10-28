using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class IconBrushWidget : ButtonWidget
{
	private Brush _iconBrush;

	private string _iconId;

	private bool _useStylesFromSourceIcon;

	private bool _useIconSize;

	public Brush IconBrush
	{
		get
		{
			return _iconBrush;
		}
		set
		{
			if (value != _iconBrush)
			{
				_iconBrush = value;
				OnPropertyChanged(value, "IconBrush");
				UpdateIcon();
			}
		}
	}

	public string IconID
	{
		get
		{
			return _iconId;
		}
		set
		{
			if (value != _iconId)
			{
				_iconId = value;
				OnPropertyChanged(value, "IconID");
				UpdateIcon();
			}
		}
	}

	public bool UseStylesFromSourceIcon
	{
		get
		{
			return _useStylesFromSourceIcon;
		}
		set
		{
			if (value != _useStylesFromSourceIcon)
			{
				_useStylesFromSourceIcon = value;
				OnPropertyChanged(value, "UseStylesFromSourceIcon");
				UpdateIcon();
			}
		}
	}

	public bool UseIconSize
	{
		get
		{
			return _useIconSize;
		}
		set
		{
			if (value != _useIconSize)
			{
				_useIconSize = value;
				OnPropertyChanged(value, "UseIconSize");
				UpdateIcon();
			}
		}
	}

	public IconBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIcon()
	{
		if (IconBrush == null || string.IsNullOrEmpty(IconID))
		{
			return;
		}
		BrushLayer layer = IconBrush.GetLayer(IconID);
		if (base.Brush == null)
		{
			return;
		}
		Sprite sprite = layer?.Sprite;
		base.Brush.Sprite = sprite;
		if (sprite != null && UseIconSize)
		{
			base.SuggestedWidth = sprite.Width;
			base.SuggestedHeight = sprite.Height;
		}
		foreach (BrushLayer layer2 in base.Brush.Layers)
		{
			if (UseStylesFromSourceIcon && layer != null)
			{
				layer2.FillFrom(layer);
			}
			else
			{
				layer2.Sprite = sprite;
			}
		}
	}
}
