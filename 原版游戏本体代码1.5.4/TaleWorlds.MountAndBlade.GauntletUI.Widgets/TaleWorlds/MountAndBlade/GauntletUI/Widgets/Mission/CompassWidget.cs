using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class CompassWidget : Widget
{
	private Widget _itemContainerPanel;

	private Widget _markerContainerPanel;

	[DataSourceProperty]
	public Widget ItemContainerPanel
	{
		get
		{
			return _itemContainerPanel;
		}
		set
		{
			if (_itemContainerPanel != value)
			{
				_itemContainerPanel = value;
				OnPropertyChanged(value, "ItemContainerPanel");
			}
		}
	}

	[DataSourceProperty]
	public Widget MarkerContainerPanel
	{
		get
		{
			return _markerContainerPanel;
		}
		set
		{
			if (_markerContainerPanel != value)
			{
				_markerContainerPanel = value;
				OnPropertyChanged(value, "MarkerContainerPanel");
			}
		}
	}

	public CompassWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		HandleHorizontalPositioning();
		HandleMarkerPositioning();
	}

	private void HandleHorizontalPositioning()
	{
		if (ItemContainerPanel.ChildCount <= 0)
		{
			return;
		}
		List<List<Widget>> list = new List<List<Widget>>();
		float valueFrom = 0f;
		float num = ItemContainerPanel.ParentWidget.MeasuredSize.X * base._inverseScaleToUse - 50f;
		for (int i = 0; i < ItemContainerPanel.ChildCount; i++)
		{
			CompassElementWidget compassElementWidget = ItemContainerPanel.GetChild(i) as CompassElementWidget;
			if (compassElementWidget.IsHidden)
			{
				continue;
			}
			float amount = (compassElementWidget.Position + 1f) * 0.5f;
			compassElementWidget.MarginLeft = MBMath.Lerp(valueFrom, num, amount);
			bool flag = false;
			if (list.Count > 0)
			{
				List<Widget> list2 = list[list.Count - 1];
				for (int num2 = list2.Count - 1; num2 >= 0; num2--)
				{
					if (Math.Abs(list2[num2].MarginLeft - compassElementWidget.MarginLeft) < 10f)
					{
						flag = true;
						compassElementWidget.MarginLeft = list[list.Count - 1][list[list.Count - 1].Count - 1].MarginLeft + 10f;
						list[list.Count - 1].Add(compassElementWidget);
						if (compassElementWidget.MarginLeft > num)
						{
							float marginLeft = compassElementWidget.MarginLeft;
							compassElementWidget.MarginLeft = num;
							float num3 = marginLeft - compassElementWidget.MarginLeft;
							for (int j = 1; j < list2.Count; j++)
							{
								int index = list2.Count - 1 - j;
								list2[index].MarginLeft -= num3;
							}
						}
						break;
					}
				}
			}
			if (!flag)
			{
				list.Add(new List<Widget>());
				list[list.Count - 1].Add(compassElementWidget);
			}
		}
	}

	private void HandleMarkerPositioning()
	{
		if (MarkerContainerPanel.ChildCount > 0)
		{
			float valueFrom = 0f;
			float valueTo = MarkerContainerPanel.ParentWidget.MeasuredSize.X * base._inverseScaleToUse;
			for (int i = 0; i < MarkerContainerPanel.ChildCount; i++)
			{
				CompassMarkerTextWidget compassMarkerTextWidget = MarkerContainerPanel.GetChild(i) as CompassMarkerTextWidget;
				float num = (compassMarkerTextWidget.Position + 1f) * 0.5f;
				compassMarkerTextWidget.MarginLeft = MBMath.Lerp(valueFrom, valueTo, num) - compassMarkerTextWidget.Size.X * 0.5f;
				compassMarkerTextWidget.IsHidden = MBMath.ApproximatelyEquals(num, 0f) || MBMath.ApproximatelyEquals(num, 1f);
			}
		}
	}
}
