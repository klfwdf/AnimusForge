using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleHeroDragWidget : Widget
{
	private bool _isDirty;

	private int _stackCount;

	private BrushWidget _stackDragWidget;

	private ImageIdentifierWidget _stackThumbnailWidget;

	private string _innerBrushName;

	[Editor(false)]
	public int StackCount
	{
		get
		{
			return _stackCount;
		}
		set
		{
			if (value != _stackCount)
			{
				_stackCount = value;
				OnPropertyChanged(value, "StackCount");
				OnStackCountChanged();
			}
		}
	}

	[Editor(false)]
	public BrushWidget StackDragWidget
	{
		get
		{
			return _stackDragWidget;
		}
		set
		{
			if (value != _stackDragWidget)
			{
				_stackDragWidget = value;
				OnPropertyChanged(value, "StackDragWidget");
			}
		}
	}

	[Editor(false)]
	public ImageIdentifierWidget StackThumbnailWidget
	{
		get
		{
			return _stackThumbnailWidget;
		}
		set
		{
			if (value != _stackThumbnailWidget)
			{
				_stackThumbnailWidget = value;
				OnPropertyChanged(value, "StackThumbnailWidget");
			}
		}
	}

	[Editor(false)]
	public string InnerBrushName
	{
		get
		{
			return _innerBrushName;
		}
		set
		{
			if (value != _innerBrushName)
			{
				_innerBrushName = value;
				OnPropertyChanged(value, "InnerBrushName");
			}
		}
	}

	public OrderOfBattleHeroDragWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!base.IsVisible || !_isDirty || StackDragWidget == null)
		{
			return;
		}
		RemoveAllChildren();
		for (int i = 0; i < StackCount; i++)
		{
			BrushWidget brushWidget = new BrushWidget(base.Context)
			{
				Brush = StackDragWidget.ReadOnlyBrush,
				DoNotAcceptEvents = false,
				SuggestedHeight = StackDragWidget.SuggestedHeight,
				SuggestedWidth = StackDragWidget.SuggestedWidth,
				ScaledPositionXOffset = i * 5,
				ScaledPositionYOffset = i * 5
			};
			if (i == StackCount - 1)
			{
				BrushWidget widget = new BrushWidget(brushWidget.Context)
				{
					Brush = base.Context.GetBrush(InnerBrushName),
					WidthSizePolicy = SizePolicy.StretchToParent,
					HeightSizePolicy = SizePolicy.StretchToParent,
					MarginBottom = 5f,
					MarginTop = 5f,
					MarginLeft = 5f,
					MarginRight = 5f,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				ImageIdentifierWidget widget2 = new ImageIdentifierWidget(brushWidget.Context)
				{
					WidthSizePolicy = SizePolicy.Fixed,
					HeightSizePolicy = SizePolicy.Fixed,
					SuggestedWidth = StackThumbnailWidget.SuggestedWidth,
					SuggestedHeight = StackThumbnailWidget.SuggestedHeight,
					MarginTop = StackThumbnailWidget.MarginTop,
					MarginLeft = StackThumbnailWidget.MarginLeft,
					AdditionalArgs = StackThumbnailWidget.AdditionalArgs,
					ImageId = StackThumbnailWidget.ImageId,
					TextureProviderName = StackThumbnailWidget.TextureProviderName
				};
				brushWidget.AddChild(widget);
				brushWidget.AddChild(widget2);
			}
			AddChild(brushWidget);
		}
		_isDirty = false;
	}

	private void OnStackCountChanged()
	{
		_isDirty = true;
	}
}
