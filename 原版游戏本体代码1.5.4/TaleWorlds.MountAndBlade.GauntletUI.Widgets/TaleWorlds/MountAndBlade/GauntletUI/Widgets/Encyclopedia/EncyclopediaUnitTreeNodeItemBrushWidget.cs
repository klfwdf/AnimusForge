using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaUnitTreeNodeItemBrushWidget : BrushWidget
{
	private Action<Widget, Widget> _listItemAddedHandler;

	private bool _isLinesDirty;

	private bool _isAlternativeUpgrade;

	private ListPanel _childContainer;

	private Widget _lineContainer;

	private Brush _lineBrush;

	private Brush _alternateLineBrush;

	[Editor(false)]
	public bool IsAlternativeUpgrade
	{
		get
		{
			return _isAlternativeUpgrade;
		}
		set
		{
			if (value != _isAlternativeUpgrade)
			{
				_isAlternativeUpgrade = value;
				OnPropertyChanged(value, "IsAlternativeUpgrade");
			}
		}
	}

	[Editor(false)]
	public ListPanel ChildContainer
	{
		get
		{
			return _childContainer;
		}
		set
		{
			if (_childContainer != value)
			{
				_childContainer?.ItemAddEventHandlers.Remove(_listItemAddedHandler);
				_childContainer = value;
				OnPropertyChanged(value, "ChildContainer");
				_childContainer?.ItemAddEventHandlers.Add(_listItemAddedHandler);
			}
		}
	}

	[Editor(false)]
	public Widget LineContainer
	{
		get
		{
			return _lineContainer;
		}
		set
		{
			if (_lineContainer != value)
			{
				_lineContainer = value;
				OnPropertyChanged(value, "LineContainer");
			}
		}
	}

	[Editor(false)]
	public Brush LineBrush
	{
		get
		{
			return _lineBrush;
		}
		set
		{
			if (_lineBrush != value)
			{
				_lineBrush = value;
				OnPropertyChanged(value, "LineBrush");
			}
		}
	}

	[Editor(false)]
	public Brush AlternateLineBrush
	{
		get
		{
			return _alternateLineBrush;
		}
		set
		{
			if (_alternateLineBrush != value)
			{
				_alternateLineBrush = value;
				OnPropertyChanged(value, "AlternateLineBrush");
			}
		}
	}

	public EncyclopediaUnitTreeNodeItemBrushWidget(UIContext context)
		: base(context)
	{
		_listItemAddedHandler = OnListItemAdded;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isLinesDirty)
		{
			return;
		}
		if (ChildContainer.ChildCount == LineContainer.ChildCount)
		{
			float num = base.GlobalPosition.X + base.Size.X * 0.5f;
			for (int i = 0; i < ChildContainer.ChildCount; i++)
			{
				Widget child = ChildContainer.GetChild(i);
				Widget child2 = LineContainer.GetChild(i);
				float num2 = child.GlobalPosition.X + child.Size.X * 0.5f;
				bool flag = num > num2;
				child2.SetState(flag ? "Left" : "Right");
				float num3 = (child2.ScaledSuggestedWidth = TaleWorlds.Library.MathF.Abs(num - num2));
				child2.ScaledPositionXOffset = (num3 * 0.5f + 5f * base._scaleToUse) * (float)((!flag) ? 1 : (-1));
			}
		}
		_isLinesDirty = false;
	}

	public void OnListItemAdded(Widget parentWidget, Widget addedWidget)
	{
		Widget widget = CreateLineWidget();
		if (ChildContainer.ChildCount == 1)
		{
			widget.SetState("Straight");
		}
		else
		{
			_isLinesDirty = true;
		}
	}

	private Widget CreateLineWidget()
	{
		BrushWidget brushWidget = new BrushWidget(base.Context)
		{
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.StretchToParent,
			Brush = LineBrush
		};
		brushWidget.SuggestedWidth = brushWidget.ReadOnlyBrush.Sprite.Width;
		brushWidget.SuggestedHeight = brushWidget.ReadOnlyBrush.Sprite.Height;
		brushWidget.HorizontalAlignment = HorizontalAlignment.Center;
		brushWidget.AddState("Left");
		brushWidget.AddState("Right");
		brushWidget.AddState("Straight");
		LineContainer.AddChild(brushWidget);
		return brushWidget;
	}
}
