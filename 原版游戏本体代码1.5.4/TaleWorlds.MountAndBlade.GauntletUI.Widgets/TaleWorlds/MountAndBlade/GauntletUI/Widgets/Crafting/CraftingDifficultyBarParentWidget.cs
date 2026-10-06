using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingDifficultyBarParentWidget : Widget
{
	private float _offsetIntolerance = 3f;

	private bool _areOffsetsDirty;

	private bool _updatePositions;

	private TextWidget _smithingLevelTextWidget;

	private TextWidget _orderDifficultyTextWidget;

	public int OrderDifficulty { get; set; }

	public int SmithingLevel { get; set; }

	public TextWidget SmithingLevelTextWidget
	{
		get
		{
			return _smithingLevelTextWidget;
		}
		set
		{
			if (value != _smithingLevelTextWidget)
			{
				_smithingLevelTextWidget = value;
				_smithingLevelTextWidget.PropertyChanged += OnWidgetPositionUpdated;
			}
		}
	}

	public TextWidget OrderDifficultyTextWidget
	{
		get
		{
			return _orderDifficultyTextWidget;
		}
		set
		{
			if (value != _orderDifficultyTextWidget)
			{
				_orderDifficultyTextWidget = value;
				_orderDifficultyTextWidget.PropertyChanged += OnWidgetPositionUpdated;
			}
		}
	}

	public CraftingDifficultyBarParentWidget(UIContext context)
		: base(context)
	{
	}

	private void OnWidgetPositionUpdated(PropertyOwnerObject ownerObject, string propertyName, object value)
	{
		if (propertyName == "Text")
		{
			_areOffsetsDirty = true;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (SmithingLevelTextWidget == null || OrderDifficultyTextWidget == null)
		{
			return;
		}
		if (_updatePositions)
		{
			TextWidget textWidget = ((OrderDifficulty < SmithingLevel) ? SmithingLevelTextWidget : OrderDifficultyTextWidget);
			TextWidget textWidget2 = ((textWidget == SmithingLevelTextWidget) ? OrderDifficultyTextWidget : SmithingLevelTextWidget);
			if (textWidget.GlobalPosition.Y + (textWidget.Size.Y + _offsetIntolerance) >= textWidget2.GlobalPosition.Y)
			{
				textWidget.PositionYOffset = 0f - textWidget.Size.Y;
				textWidget2.PositionYOffset = 0f;
			}
			else
			{
				textWidget.PositionYOffset = 0f;
				textWidget2.PositionYOffset = 0f;
			}
			_updatePositions = false;
		}
		if (_areOffsetsDirty)
		{
			SmithingLevelTextWidget.PositionYOffset = 0f;
			OrderDifficultyTextWidget.PositionYOffset = 0f;
			_updatePositions = true;
			_areOffsetsDirty = false;
		}
	}
}
