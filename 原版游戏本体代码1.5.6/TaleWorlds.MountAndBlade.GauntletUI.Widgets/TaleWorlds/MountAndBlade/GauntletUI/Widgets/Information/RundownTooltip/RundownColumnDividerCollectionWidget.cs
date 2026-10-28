using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information.RundownTooltip;

public class RundownColumnDividerCollectionWidget : ListPanel
{
	private Sprite _dividerSprite;

	private Color _dividerColor = new Color(1f, 1f, 1f);

	public float DividerWidth { get; set; }

	[Editor(false)]
	public Sprite DividerSprite
	{
		get
		{
			return _dividerSprite;
		}
		set
		{
			if (value != _dividerSprite)
			{
				_dividerSprite = value;
				OnPropertyChanged(value, "DividerSprite");
			}
		}
	}

	[Editor(false)]
	public Color DividerColor
	{
		get
		{
			return _dividerColor;
		}
		set
		{
			if (value != _dividerColor)
			{
				_dividerColor = value;
				OnPropertyChanged(value, "DividerColor");
			}
		}
	}

	public RundownColumnDividerCollectionWidget(UIContext context)
		: base(context)
	{
	}

	public void Refresh(IReadOnlyList<float> columnWidths)
	{
		RemoveAllChildren();
		for (int i = 0; i < columnWidths.Count - 1; i++)
		{
			Widget widget = CreateFixedSpaceWidget(columnWidths[i] * base._inverseScaleToUse - DividerWidth);
			AddChild(widget);
			AddChild(CreateDividerWidget());
		}
		AddChild(CreateStretchedSpaceWidget());
	}

	private Widget CreateFixedSpaceWidget(float width)
	{
		return new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.StretchToParent,
			SuggestedWidth = width
		};
	}

	private Widget CreateStretchedSpaceWidget()
	{
		return new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.StretchToParent
		};
	}

	private Widget CreateDividerWidget()
	{
		return new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.StretchToParent,
			SuggestedWidth = DividerWidth,
			Sprite = DividerSprite,
			Color = DividerColor
		};
	}
}
