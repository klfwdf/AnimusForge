using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;

namespace AnimusForge;

public sealed class AnimusForgeTopDownListPanel : ListPanel
{
	public AnimusForgeTopDownListPanel(UIContext context)
		: base(context)
	{
#if BANNERLORD_1_4_OR_GREATER
		StackLayout.LayoutMethod = LayoutMethod.VerticalTopToBottom;
#else
		StackLayout.LayoutMethod = LayoutMethod.VerticalBottomToTop;
#endif
	}
}
