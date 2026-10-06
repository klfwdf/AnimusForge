using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information.RundownTooltip;

public class RundownTooltipWidget : TooltipWidget
{
	private enum ValueCategorization
	{
		None,
		LargeIsBetter,
		SmallIsBetter
	}

	private readonly Color _defaultValueColor = new Color(1f, 1f, 1f);

	private readonly Color _negativeValueColor = new Color(71f / 85f, 11f / 85f, 11f / 85f);

	private readonly Color _positiveValueColor = new Color(0.38039216f, 0.7490196f, 1f / 3f);

	private bool _willRefreshThisFrame;

	private IReadOnlyList<float> _lastCheckedColumnWidths = new List<float>();

	private GridWidget _lineContainerWidget;

	private RundownColumnDividerCollectionWidget _dividerCollectionWidget;

	private int _valueCategorizationAsInt;

	[Editor(false)]
	public GridWidget LineContainerWidget
	{
		get
		{
			return _lineContainerWidget;
		}
		set
		{
			if (value != _lineContainerWidget)
			{
				if (_lineContainerWidget != null)
				{
					_lineContainerWidget.EventFire -= OnLineContainerEventFire;
				}
				_lineContainerWidget = value;
				OnPropertyChanged(value, "LineContainerWidget");
				RefreshOnNextLateUpdate();
				if (_lineContainerWidget != null)
				{
					_lineContainerWidget.EventFire += OnLineContainerEventFire;
				}
			}
		}
	}

	[Editor(false)]
	public RundownColumnDividerCollectionWidget DividerCollectionWidget
	{
		get
		{
			return _dividerCollectionWidget;
		}
		set
		{
			if (value != _dividerCollectionWidget)
			{
				_dividerCollectionWidget = value;
				OnPropertyChanged(value, "DividerCollectionWidget");
				RefreshOnNextLateUpdate();
			}
		}
	}

	[Editor(false)]
	public int ValueCategorizationAsInt
	{
		get
		{
			return _valueCategorizationAsInt;
		}
		set
		{
			if (value != _valueCategorizationAsInt)
			{
				_valueCategorizationAsInt = value;
				OnPropertyChanged(value, "ValueCategorizationAsInt");
				RefreshOnNextLateUpdate();
			}
		}
	}

	public RundownTooltipWidget(UIContext context)
		: base(context)
	{
		RefreshOnNextLateUpdate();
		_animationDelayInFrames = 2;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (LineContainerWidget == null)
		{
			return;
		}
		GridLayout gridLayout = LineContainerWidget.GridLayout;
		bool flag = _lastCheckedColumnWidths.Count != gridLayout.ColumnWidths.Count;
		bool flag2 = false;
		for (int i = 0; i < _lastCheckedColumnWidths.Count; i++)
		{
			float num = _lastCheckedColumnWidths[i];
			float num2 = ((i < gridLayout.ColumnWidths.Count) ? gridLayout.ColumnWidths[i] : (-1f));
			if (MathF.Abs(num - num2) > 1E-05f)
			{
				flag2 = true;
				break;
			}
		}
		if (flag || flag2)
		{
			_lastCheckedColumnWidths = gridLayout.ColumnWidths;
			DividerCollectionWidget?.Refresh(gridLayout.ColumnWidths);
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		GridLayout gridLayout = LineContainerWidget.GridLayout;
		for (int i = 0; i < LineContainerWidget.ChildCount; i++)
		{
			RundownLineWidget obj = LineContainerWidget.GetChild(i) as RundownLineWidget;
			int num = i / LineContainerWidget.RowCount;
			obj.RefreshValueOffset((num < gridLayout.ColumnWidths.Count) ? gridLayout.ColumnWidths[num] : (-1f));
		}
	}

	private void Refresh()
	{
		ValueCategorization valueCategorizationAsInt = (ValueCategorization)ValueCategorizationAsInt;
		if (LineContainerWidget != null)
		{
			List<RundownLineWidget> list = new List<RundownLineWidget>();
			float num = 0f;
			float num2 = 0f;
			foreach (Widget child in LineContainerWidget.Children)
			{
				if (child is RundownLineWidget rundownLineWidget)
				{
					list.Add(rundownLineWidget);
					float value = rundownLineWidget.Value;
					if (value < num)
					{
						num = value;
					}
					if (value > num2)
					{
						num2 = value;
					}
				}
			}
			foreach (RundownLineWidget item in list)
			{
				float value2 = item.Value;
				Brush brush = item.ValueTextWidget.Brush;
				Color fontColor = _defaultValueColor;
				if (valueCategorizationAsInt != ValueCategorization.None)
				{
					float num3 = ((value2 < 0f) ? num : num2);
					float ratio = MathF.Abs(value2 / num3);
					float num4 = (float)((valueCategorizationAsInt == ValueCategorization.LargeIsBetter) ? 1 : (-1)) * value2;
					fontColor = Color.Lerp(_defaultValueColor, (num4 < 0f) ? _negativeValueColor : _positiveValueColor, ratio);
				}
				brush.FontColor = fontColor;
			}
		}
		_willRefreshThisFrame = false;
	}

	private void RefreshOnNextLateUpdate()
	{
		if (!_willRefreshThisFrame)
		{
			_willRefreshThisFrame = true;
			base.EventManager.AddLateUpdateAction(this, delegate
			{
				Refresh();
			}, 1);
		}
	}

	private void OnLineContainerEventFire(Widget widget, string eventName, object[] args)
	{
		if (eventName == "ItemAdd" || eventName == "ItemRemove")
		{
			RefreshOnNextLateUpdate();
		}
	}
}
