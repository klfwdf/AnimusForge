using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Adds the read-only world-message timeline to every normal settlement menu.
/// </summary>
public sealed class WorldMessageTimelineMenuBehavior : CampaignBehaviorBase
{
	private const string OptionText = "传闻";

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		if (starter == null)
		{
			return;
		}
		starter.AddGameMenuOption("town", "animusforge_world_messages_town", OptionText, WorldMessagesMenuOptionCondition, WorldMessagesMenuOptionConsequence, isLeave: false, -1);
		starter.AddGameMenuOption("castle", "animusforge_world_messages_castle", OptionText, WorldMessagesMenuOptionCondition, WorldMessagesMenuOptionConsequence, isLeave: false, -1);
		starter.AddGameMenuOption("village", "animusforge_world_messages_village", OptionText, WorldMessagesMenuOptionCondition, WorldMessagesMenuOptionConsequence, isLeave: false, -1);
	}

	private static bool WorldMessagesMenuOptionCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		return settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
	}

	private static void WorldMessagesMenuOptionConsequence(MenuCallbackArgs args)
	{
		if (!WorldMessageTimelineUi.Show())
		{
			InformationManager.DisplayMessage(new InformationMessage("当前无法打开传闻界面。"));
		}
	}
}
