using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class SceneNotificationDescriptionTextWidget : TextWidget
{
	private int _cachedLineCount;

	private TextHorizontalAlignment _defaultAlignment;

	private TextHorizontalAlignment _multiLineAlignment;

	[Editor(false)]
	public TextHorizontalAlignment MultiLineAlignment
	{
		get
		{
			return _multiLineAlignment;
		}
		set
		{
			_multiLineAlignment = value;
		}
	}

	public SceneNotificationDescriptionTextWidget(UIContext context)
		: base(context)
	{
		_defaultAlignment = base.ReadOnlyBrush.TextHorizontalAlignment;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_text.LineCount != _cachedLineCount)
		{
			_cachedLineCount = _text.LineCount;
			if (_cachedLineCount == 1)
			{
				base.ReadOnlyBrush.TextHorizontalAlignment = _defaultAlignment;
			}
			else
			{
				base.ReadOnlyBrush.TextHorizontalAlignment = MultiLineAlignment;
			}
		}
	}
}
