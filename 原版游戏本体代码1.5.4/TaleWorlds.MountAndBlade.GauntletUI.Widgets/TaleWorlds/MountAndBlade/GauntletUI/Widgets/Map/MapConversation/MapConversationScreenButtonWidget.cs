using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapConversation;

public class MapConversationScreenButtonWidget : ButtonWidget
{
	private bool _isBarterActive;

	public Widget ConversationParent { get; set; }

	public bool IsBarterActive
	{
		get
		{
			return _isBarterActive;
		}
		set
		{
			if (_isBarterActive != value)
			{
				_isBarterActive = value;
				ConversationParent.IsVisible = !IsBarterActive;
			}
		}
	}

	public MapConversationScreenButtonWidget(UIContext context)
		: base(context)
	{
	}
}
