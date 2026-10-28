using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Kingdom;

public class DecisionSupporterGridWidget : GridWidget
{
	public int VisibleCount { get; set; } = 4;

	public TextWidget MoreTextWidget { get; set; }

	public DecisionSupporterGridWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.IsVisible = child.GetSiblingIndex() < VisibleCount;
		UpdateMoreText();
	}

	private void UpdateMoreText()
	{
		if (MoreTextWidget != null)
		{
			MoreTextWidget.IsVisible = base.ChildCount > VisibleCount;
			if (MoreTextWidget.IsVisible)
			{
				MoreTextWidget.Text = "+" + (base.ChildCount - VisibleCount);
			}
		}
	}
}
