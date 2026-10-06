using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardGainedSkillsListPanel : ListPanel
{
	private float _scrollGradientPadding = 55f;

	private ScoreboardSkillItemHoverToggleWidget _currentUnit;

	public ScoreboardGainedSkillsListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		if (base.IsVisible)
		{
			UpdateVerticalPosRelatedToCurrentUnit();
		}
	}

	private void UpdateVerticalPosRelatedToCurrentUnit()
	{
		if (_currentUnit != null)
		{
			float num = base.EventManager.PageSize.Y - 107f * base._scaleToUse;
			if (num - (_currentUnit.GlobalPosition.Y + base.Size.Y) < base.Size.Y + _scrollGradientPadding)
			{
				float num2 = num - (_scrollGradientPadding * base._scaleToUse + base.Size.Y);
				base.ScaledPositionYOffset = num2 - _currentUnit.GlobalPosition.Y + _currentUnit.ParentWidget.ParentWidget.ParentWidget.LocalPosition.Y;
			}
			else
			{
				base.ScaledPositionYOffset = _currentUnit.ParentWidget.ParentWidget.ParentWidget.LocalPosition.Y;
			}
		}
		else
		{
			base.ScaledPositionYOffset = -1000f;
		}
	}

	public void SetCurrentUnit(ScoreboardSkillItemHoverToggleWidget unit)
	{
		_currentUnit = unit;
		if (unit != null)
		{
			if (base.ChildCount > 0)
			{
				RemoveAllChildren();
			}
			List<Widget> allSkillWidgets = _currentUnit.GetAllSkillWidgets();
			for (int i = 0; i < allSkillWidgets.Count; i++)
			{
				Widget widget = allSkillWidgets[i];
				Widget targetWidget = new Widget(base.Context);
				CopyWidgetProperties(ref targetWidget, widget);
				Widget targetWidget2 = new Widget(base.Context);
				CopyWidgetProperties(ref targetWidget2, widget.Children[0]);
				TextWidget targetTextWidget = new TextWidget(base.Context);
				CopyWidgetProperties(ref targetTextWidget, (TextWidget)widget.Children[1]);
				AddChild(targetWidget);
				targetWidget.AddChild(targetWidget2);
				targetWidget.AddChild(targetTextWidget);
			}
			UpdateVerticalPosRelatedToCurrentUnit();
			base.IsVisible = true;
		}
		else
		{
			RemoveAllChildren();
			base.IsVisible = false;
		}
	}

	private void CopyWidgetProperties(ref TextWidget targetTextWidget, TextWidget sourceTextWidget)
	{
		targetTextWidget.Text = sourceTextWidget.Text;
		Widget targetWidget = targetTextWidget;
		CopyWidgetProperties(ref targetWidget, sourceTextWidget);
	}

	private void CopyWidgetProperties(ref Widget targetWidget, Widget sourceWidget)
	{
		targetWidget.WidthSizePolicy = sourceWidget.WidthSizePolicy;
		targetWidget.HeightSizePolicy = sourceWidget.HeightSizePolicy;
		targetWidget.SuggestedWidth = sourceWidget.SuggestedWidth;
		targetWidget.SuggestedHeight = sourceWidget.SuggestedHeight;
		targetWidget.MarginTop = sourceWidget.MarginTop;
		targetWidget.MarginBottom = sourceWidget.MarginBottom;
		targetWidget.MarginLeft = sourceWidget.MarginLeft;
		targetWidget.MarginRight = sourceWidget.MarginRight;
		targetWidget.Sprite = sourceWidget.Sprite;
		if (targetWidget is BrushWidget brushWidget && sourceWidget is BrushWidget brushWidget2)
		{
			brushWidget.Brush = brushWidget2.ReadOnlyBrush;
		}
		targetWidget.VerticalAlignment = sourceWidget.VerticalAlignment;
	}
}
