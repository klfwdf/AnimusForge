using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets.Graph;

public class GraphWidget : Widget
{
	private Widget _dynamicWidgetsContainer;

	private bool _willRefreshThisFrame;

	private Vec2 _planeExtendedSize;

	private Vec2 _planeSize;

	private Vec2 _totalSizeCached;

	private Widget _lineContainerWidget;

	private int _rowCount;

	private int _columnCount;

	private int _horizontalLabelCount;

	private float _horizontalMinValue;

	private float _horizontalMaxValue;

	private int _verticalLabelCount;

	private float _verticalMinValue;

	private float _verticalMaxValue;

	private Sprite _planeLineSprite;

	private Color _planeLineColor;

	private float _leftSpace;

	private float _topSpace;

	private float _rightSpace;

	private float _bottomSpace;

	private float _planeMarginTop;

	private float _planeMarginRight;

	private int _numberOfValueLabelDecimalPlaces;

	private Brush _horizontalValueLabelsBrush;

	private Brush _verticalValueLabelsBrush;

	private Brush _lineBrush;

	public int RowCount
	{
		get
		{
			return _rowCount;
		}
		set
		{
			if (value != _rowCount)
			{
				_rowCount = value;
				OnPropertyChanged(value, "RowCount");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public int ColumnCount
	{
		get
		{
			return _columnCount;
		}
		set
		{
			if (value != _columnCount)
			{
				_columnCount = value;
				OnPropertyChanged(value, "ColumnCount");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public int HorizontalLabelCount
	{
		get
		{
			return _horizontalLabelCount;
		}
		set
		{
			if (value != _horizontalLabelCount)
			{
				_horizontalLabelCount = value;
				OnPropertyChanged(value, "HorizontalLabelCount");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float HorizontalMinValue
	{
		get
		{
			return _horizontalMinValue;
		}
		set
		{
			if (value != _horizontalMinValue)
			{
				_horizontalMinValue = value;
				OnPropertyChanged(value, "HorizontalMinValue");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float HorizontalMaxValue
	{
		get
		{
			return _horizontalMaxValue;
		}
		set
		{
			if (value != _horizontalMaxValue)
			{
				_horizontalMaxValue = value;
				OnPropertyChanged(value, "HorizontalMaxValue");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public int VerticalLabelCount
	{
		get
		{
			return _verticalLabelCount;
		}
		set
		{
			if (value != _verticalLabelCount)
			{
				_verticalLabelCount = value;
				OnPropertyChanged(value, "VerticalLabelCount");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float VerticalMinValue
	{
		get
		{
			return _verticalMinValue;
		}
		set
		{
			if (value != _verticalMinValue)
			{
				_verticalMinValue = value;
				OnPropertyChanged(value, "VerticalMinValue");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float VerticalMaxValue
	{
		get
		{
			return _verticalMaxValue;
		}
		set
		{
			if (value != _verticalMaxValue)
			{
				_verticalMaxValue = value;
				OnPropertyChanged(value, "VerticalMaxValue");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public Sprite PlaneLineSprite
	{
		get
		{
			return _planeLineSprite;
		}
		set
		{
			if (value != _planeLineSprite)
			{
				_planeLineSprite = value;
				OnPropertyChanged(value, "PlaneLineSprite");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public Color PlaneLineColor
	{
		get
		{
			return _planeLineColor;
		}
		set
		{
			if (value != _planeLineColor)
			{
				_planeLineColor = value;
				OnPropertyChanged(value, "PlaneLineColor");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float LeftSpace
	{
		get
		{
			return _leftSpace;
		}
		set
		{
			if (value != _leftSpace)
			{
				_leftSpace = value;
				OnPropertyChanged(value, "LeftSpace");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float TopSpace
	{
		get
		{
			return _topSpace;
		}
		set
		{
			if (value != _topSpace)
			{
				_topSpace = value;
				OnPropertyChanged(value, "TopSpace");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float RightSpace
	{
		get
		{
			return _rightSpace;
		}
		set
		{
			if (value != _rightSpace)
			{
				_rightSpace = value;
				OnPropertyChanged(value, "RightSpace");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float BottomSpace
	{
		get
		{
			return _bottomSpace;
		}
		set
		{
			if (value != _bottomSpace)
			{
				_bottomSpace = value;
				OnPropertyChanged(value, "BottomSpace");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float PlaneMarginTop
	{
		get
		{
			return _planeMarginTop;
		}
		set
		{
			if (value != _planeMarginTop)
			{
				_planeMarginTop = value;
				OnPropertyChanged(value, "PlaneMarginTop");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public float PlaneMarginRight
	{
		get
		{
			return _planeMarginRight;
		}
		set
		{
			if (value != _planeMarginRight)
			{
				_planeMarginRight = value;
				OnPropertyChanged(value, "PlaneMarginRight");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public int NumberOfValueLabelDecimalPlaces
	{
		get
		{
			return _numberOfValueLabelDecimalPlaces;
		}
		set
		{
			if (value != _numberOfValueLabelDecimalPlaces)
			{
				_numberOfValueLabelDecimalPlaces = value;
				OnPropertyChanged(value, "NumberOfValueLabelDecimalPlaces");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public Brush HorizontalValueLabelsBrush
	{
		get
		{
			return _horizontalValueLabelsBrush;
		}
		set
		{
			if (value != _horizontalValueLabelsBrush)
			{
				_horizontalValueLabelsBrush = value;
				OnPropertyChanged(value, "HorizontalValueLabelsBrush");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public Brush VerticalValueLabelsBrush
	{
		get
		{
			return _verticalValueLabelsBrush;
		}
		set
		{
			if (value != _verticalValueLabelsBrush)
			{
				_verticalValueLabelsBrush = value;
				OnPropertyChanged(value, "VerticalValueLabelsBrush");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public Brush LineBrush
	{
		get
		{
			return _lineBrush;
		}
		set
		{
			if (value != _lineBrush)
			{
				_lineBrush = value;
				OnPropertyChanged(value, "LineBrush");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public Widget LineContainerWidget
	{
		get
		{
			return _lineContainerWidget;
		}
		set
		{
			if (value == _lineContainerWidget)
			{
				return;
			}
			if (_lineContainerWidget != null)
			{
				_lineContainerWidget.EventFire -= OnLineContainerEventFire;
				foreach (Widget child in LineContainerWidget.Children)
				{
					if (child is GraphLineWidget graphLineWidget)
					{
						graphLineWidget.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Remove(graphLineWidget.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(OnPointAdded));
					}
				}
			}
			_lineContainerWidget = value;
			if (_lineContainerWidget != null)
			{
				_lineContainerWidget.EventFire += OnLineContainerEventFire;
				foreach (Widget child2 in LineContainerWidget.Children)
				{
					if (child2 is GraphLineWidget graphLineWidget2)
					{
						graphLineWidget2.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Combine(graphLineWidget2.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(OnPointAdded));
					}
				}
			}
			OnPropertyChanged(value, "LineContainerWidget");
			RefreshOnNextLateUpdate();
		}
	}

	public GraphWidget(UIContext context)
		: base(context)
	{
		RefreshOnNextLateUpdate();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		bool num = Mathf.Abs(_totalSizeCached.X - base.Size.X) > 1E-05f || Mathf.Abs(_totalSizeCached.Y - base.Size.Y) > 1E-05f;
		_totalSizeCached = base.Size;
		if (num)
		{
			RefreshOnNextLateUpdate();
		}
	}

	private void Refresh()
	{
		if (_dynamicWidgetsContainer != null)
		{
			RemoveChild(_dynamicWidgetsContainer);
		}
		_dynamicWidgetsContainer = new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.StretchToParent
		};
		AddChildAtIndex(_dynamicWidgetsContainer, 0);
		_planeExtendedSize = base.Size * base._inverseScaleToUse - new Vec2(LeftSpace + RightSpace, TopSpace + BottomSpace);
		_planeSize = _planeExtendedSize - new Vec2(PlaneMarginRight, PlaneMarginTop);
		Widget widget = new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.StretchToParent,
			MarginLeft = LeftSpace,
			MarginRight = RightSpace,
			MarginBottom = BottomSpace,
			MarginTop = TopSpace,
			DoNotAcceptEvents = true,
			DoNotPassEventsToChildren = true
		};
		_dynamicWidgetsContainer.AddChild(widget);
		RefreshPlaneLines(widget);
		RefreshLabels(_dynamicWidgetsContainer, isHorizontal: true);
		RefreshLabels(_dynamicWidgetsContainer, isHorizontal: false);
		RefreshGraphLines();
		_willRefreshThisFrame = false;
	}

	private void RefreshPlaneLines(Widget planeWidget)
	{
		int num = 1;
		ListPanel listPanel = CreatePlaneLinesListPanel(LayoutMethod.VerticalTopToBottom);
		float marginBottom = _planeSize.Y / (float)RowCount - (float)num;
		for (int i = 0; i < RowCount; i++)
		{
			Widget widget = new Widget(base.Context)
			{
				WidthSizePolicy = SizePolicy.StretchToParent,
				HeightSizePolicy = SizePolicy.Fixed,
				SuggestedHeight = num,
				MarginBottom = marginBottom,
				Sprite = PlaneLineSprite,
				Color = PlaneLineColor
			};
			listPanel.AddChild(widget);
		}
		ListPanel listPanel2 = CreatePlaneLinesListPanel(LayoutMethod.HorizontalLeftToRight);
		float marginLeft = _planeSize.X / (float)ColumnCount - (float)num;
		for (int j = 0; j < ColumnCount; j++)
		{
			Widget widget2 = new Widget(base.Context)
			{
				WidthSizePolicy = SizePolicy.Fixed,
				HeightSizePolicy = SizePolicy.StretchToParent,
				SuggestedWidth = num,
				MarginLeft = marginLeft,
				Sprite = PlaneLineSprite,
				Color = PlaneLineColor
			};
			listPanel2.AddChild(widget2);
		}
		planeWidget.AddChild(listPanel);
		planeWidget.AddChild(listPanel2);
	}

	private void RefreshLabels(Widget container, bool isHorizontal)
	{
		int num = (isHorizontal ? HorizontalLabelCount : VerticalLabelCount);
		float num2 = (isHorizontal ? HorizontalMaxValue : VerticalMaxValue);
		float num3 = (isHorizontal ? HorizontalMinValue : VerticalMinValue);
		if (num > 1)
		{
			int num4 = (isHorizontal ? 2 : 4);
			ListPanel listPanel = new ListPanel(base.Context)
			{
				WidthSizePolicy = (isHorizontal ? SizePolicy.StretchToParent : SizePolicy.Fixed),
				HeightSizePolicy = ((!isHorizontal) ? SizePolicy.StretchToParent : SizePolicy.Fixed),
				SuggestedWidth = (isHorizontal ? 0f : (LeftSpace - (float)num4)),
				SuggestedHeight = (isHorizontal ? (BottomSpace - (float)num4) : 0f),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Bottom,
				MarginLeft = (isHorizontal ? LeftSpace : 0f),
				MarginBottom = (isHorizontal ? 0f : BottomSpace),
				DoNotAcceptEvents = true,
				DoNotPassEventsToChildren = true
			};
			listPanel.StackLayout.LayoutMethod = ((!isHorizontal) ? LayoutMethod.VerticalTopToBottom : LayoutMethod.HorizontalLeftToRight);
			float num5 = (num2 - num3) / (float)(num - 1);
			for (int i = 0; i < num - 1; i++)
			{
				float labelValue = num3 + num5 * (float)i;
				TextWidget widget = CreateLabelText(labelValue, isHorizontal);
				listPanel.AddChild(widget);
			}
			Widget widget2 = new Widget(base.Context)
			{
				WidthSizePolicy = ((!isHorizontal) ? SizePolicy.StretchToParent : SizePolicy.Fixed),
				HeightSizePolicy = (isHorizontal ? SizePolicy.StretchToParent : SizePolicy.Fixed),
				SuggestedWidth = (isHorizontal ? (RightSpace + PlaneMarginRight) : 0f),
				SuggestedHeight = (isHorizontal ? 0f : (TopSpace + PlaneMarginTop))
			};
			TextWidget widget3 = CreateLabelText(num2, isHorizontal);
			widget2.AddChild(widget3);
			listPanel.AddChild(widget2);
			container.AddChild(listPanel);
		}
	}

	private void RefreshGraphLines()
	{
		if (LineContainerWidget == null)
		{
			return;
		}
		foreach (Widget child in LineContainerWidget.Children)
		{
			if (child is GraphLineWidget graphLineWidget)
			{
				RefreshLine(graphLineWidget);
			}
		}
	}

	private void RefreshLine(GraphLineWidget graphLineWidget)
	{
		graphLineWidget.MarginLeft = LeftSpace;
		graphLineWidget.MarginRight = RightSpace + PlaneMarginRight;
		graphLineWidget.MarginBottom = BottomSpace;
		graphLineWidget.MarginTop = TopSpace + PlaneMarginTop;
		foreach (Widget item in graphLineWidget.PointContainerWidget?.Children ?? new List<Widget>())
		{
			if (item is GraphLinePointWidget graphLinePointWidget)
			{
				RefreshPoint(graphLinePointWidget, graphLineWidget);
			}
		}
	}

	private void RefreshPoint(GraphLinePointWidget graphLinePointWidget, GraphLineWidget graphLineWidget)
	{
		bool num = HorizontalMaxValue - HorizontalMinValue > 1E-05f;
		bool flag = VerticalMaxValue - VerticalMinValue > 1E-05f;
		if (num && flag)
		{
			float value = (graphLinePointWidget.HorizontalValue - HorizontalMinValue) / (HorizontalMaxValue - HorizontalMinValue);
			value = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
			float marginLeft = _planeSize.X * value - graphLinePointWidget.SuggestedWidth * 0.5f;
			float value2 = (graphLinePointWidget.VerticalValue - VerticalMinValue) / (VerticalMaxValue - VerticalMinValue);
			value2 = TaleWorlds.Library.MathF.Clamp(value2, 0f, 1f);
			float marginBottom = _planeSize.Y * value2 - graphLinePointWidget.SuggestedHeight * 0.5f;
			string state = (string.IsNullOrEmpty(graphLineWidget.LineBrushStateName) ? "Default" : graphLineWidget.LineBrushStateName);
			graphLinePointWidget.MarginLeft = marginLeft;
			graphLinePointWidget.MarginBottom = marginBottom;
			graphLinePointWidget.SetState(state);
		}
	}

	private ListPanel CreatePlaneLinesListPanel(LayoutMethod layoutMethod)
	{
		ListPanel listPanel = new ListPanel(base.Context);
		listPanel.WidthSizePolicy = SizePolicy.StretchToParent;
		listPanel.HeightSizePolicy = SizePolicy.StretchToParent;
		listPanel.MarginTop = PlaneMarginTop;
		listPanel.MarginRight = PlaneMarginRight;
		listPanel.StackLayout.LayoutMethod = layoutMethod;
		return listPanel;
	}

	private TextWidget CreateLabelText(float labelValue, bool isHorizontal)
	{
		TextWidget textWidget = new TextWidget(base.Context)
		{
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.StretchToParent,
			Text = labelValue.ToString("G" + NumberOfValueLabelDecimalPlaces)
		};
		Brush brush = (isHorizontal ? HorizontalValueLabelsBrush : VerticalValueLabelsBrush);
		if (brush != null)
		{
			textWidget.Brush = brush.Clone();
		}
		textWidget.Brush.TextHorizontalAlignment = ((!isHorizontal) ? TextHorizontalAlignment.Right : TextHorizontalAlignment.Left);
		textWidget.Brush.TextVerticalAlignment = ((!isHorizontal) ? TextVerticalAlignment.Bottom : TextVerticalAlignment.Top);
		return textWidget;
	}

	private void OnLineContainerEventFire(Widget widget, string eventName, object[] eventArgs)
	{
		GraphLineWidget graphLineWidget;
		if (eventArgs.Length == 0 || (graphLineWidget = eventArgs[0] as GraphLineWidget) == null)
		{
			return;
		}
		if (eventName == "ItemAdd")
		{
			GraphLineWidget graphLineWidget2 = graphLineWidget;
			graphLineWidget2.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Combine(graphLineWidget2.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(OnPointAdded));
			AddLateUpdateAction(delegate
			{
				RefreshLine(graphLineWidget);
			});
		}
		else if (eventName == "ItemRemove")
		{
			GraphLineWidget graphLineWidget3 = graphLineWidget;
			graphLineWidget3.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Remove(graphLineWidget3.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(OnPointAdded));
		}
	}

	private void OnPointAdded(GraphLineWidget graphLineWidget, GraphLinePointWidget graphLinePointWidget)
	{
		AddLateUpdateAction(delegate
		{
			RefreshPoint(graphLinePointWidget, graphLineWidget);
		});
	}

	private void AddLateUpdateAction(Action action)
	{
		base.EventManager.AddLateUpdateAction(this, delegate
		{
			action?.Invoke();
		}, 1);
	}

	private void RefreshOnNextLateUpdate()
	{
		if (!_willRefreshThisFrame)
		{
			_willRefreshThisFrame = true;
			AddLateUpdateAction(Refresh);
		}
	}
}
