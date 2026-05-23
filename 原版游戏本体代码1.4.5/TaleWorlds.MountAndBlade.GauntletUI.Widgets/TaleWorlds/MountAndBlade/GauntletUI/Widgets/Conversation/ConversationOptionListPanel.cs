using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;

public class ConversationOptionListPanel : ListPanel
{
	public ButtonWidget OptionButtonWidget { get; set; }

	public ConversationOptionListPanel(UIContext context)
		: base(context)
	{
	}
}
