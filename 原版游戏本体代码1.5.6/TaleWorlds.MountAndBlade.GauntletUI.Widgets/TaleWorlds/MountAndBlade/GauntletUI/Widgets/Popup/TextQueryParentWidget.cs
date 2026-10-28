using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Popup;

public class TextQueryParentWidget : Widget
{
	private EditableTextWidget _editableTextWidget;

	[Editor(false)]
	public EditableTextWidget TextInputWidget
	{
		get
		{
			return _editableTextWidget;
		}
		set
		{
			if (value != _editableTextWidget)
			{
				_editableTextWidget = value;
				FocusOnTextQuery();
			}
		}
	}

	public TextQueryParentWidget(UIContext context)
		: base(context)
	{
	}

	private void FocusOnTextQuery()
	{
		base.EventManager.FocusedWidget = TextInputWidget;
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		if (TextInputWidget != null)
		{
			FocusOnTextQuery();
		}
	}
}
