using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus;

public class MenuCallbackArgs
{
	public float DeltaTime;

	public bool IsEnabled = true;

	public TextObject Text;

	public TextObject Tooltip;

	public GameMenuOption.IssueQuestFlags OptionQuestData;

	public GameMenuOption.LeaveType optionLeaveType;

	public TextObject MenuTitle;

	public MenuContext MenuContext { get; private set; }

	public MapState MapState { get; private set; }

	public MenuCallbackArgs(MenuContext menuContext, TextObject text)
	{
		MenuContext = menuContext;
		Text = text;
	}

	public MenuCallbackArgs(MapState mapState, TextObject text)
	{
		MapState = mapState;
		Text = text;
	}

	public MenuCallbackArgs(MapState mapState, TextObject text, float dt)
	{
		MapState = mapState;
		Text = text;
		DeltaTime = dt;
	}
}
