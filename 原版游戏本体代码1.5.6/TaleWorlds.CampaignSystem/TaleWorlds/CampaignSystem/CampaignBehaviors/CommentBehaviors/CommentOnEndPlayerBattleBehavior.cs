using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnEndPlayerBattleBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnPlayerBattleEnded(MapEvent mapEvent)
	{
		if (!mapEvent.IsHideoutBattle || mapEvent.BattleState != BattleState.None)
		{
			LogEntry.AddLogEntry(new PlayerBattleEndedLogEntry(mapEvent));
		}
	}
}
