using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class AutoHideRichTextWidget : RichTextWidget
{
	private Widget _widgetToHideIfEmpty;

	[Editor(false)]
	public Widget WidgetToHideIfEmpty
	{
		get
		{
			return _widgetToHideIfEmpty;
		}
		set
		{
			if (_widgetToHideIfEmpty != value)
			{
				_widgetToHideIfEmpty = value;
				OnPropertyChanged(value, "WidgetToHideIfEmpty");
			}
		}
	}

	public AutoHideRichTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (WidgetToHideIfEmpty != null)
		{
			WidgetToHideIfEmpty.IsVisible = base.Text != string.Empty;
		}
		base.IsVisible = base.Text != string.Empty;
	}
}
