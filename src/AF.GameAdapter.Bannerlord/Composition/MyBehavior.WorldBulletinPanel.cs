using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Per-issue facts the panel needs that the published text no longer carries (minor kinds for the ¶ tags).
// Written once per publish, capped, so the save grows by a few dozen short strings at most.


// Instant-bulletin panel: built on open (user click, not a tick path); one linear pass over the body text
// and one lookup into a <=48 entry layout list.
public partial class MyBehavior
{
	private const int WorldBulletinMaxLayouts = 48;

	private const int WorldBulletinMaxMetaKingdoms = 3;

	private const string WorldBulletinMajorHeader = "【大事件】";

	private const string WorldBulletinMinorHeader = "【其他消息】";

	private void RecordWorldBulletinLayout(string eventId, WorldBulletinSelection selection) => WorldBulletinState.RecordWorldBulletinLayout(eventId, selection);

	private WorldBulletinLayout FindWorldBulletinLayout(string eventId) => WorldBulletinState.FindWorldBulletinLayout(eventId);

	private bool TryShowWorldBulletinPanel(EventRecordEntry entry, string eventId)
	{
		WorldBulletinPanelData data;
		try
		{
			data = BuildWorldBulletinPanelData(entry, eventId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletinPanel", "[WARN] panel data build failed, using legacy popup: " + ex.Message);
			return false;
		}
		return DevWeeklyReportPopup.ShowWorldBulletin(data, 10.0, delegate
		{
			TryAwardWeeklyReportReadingXp(eventId);
		});
	}

	private WorldBulletinPanelData BuildWorldBulletinPanelData(EventRecordEntry entry, string eventId) => WorldBulletinState.BuildWorldBulletinPanelData(entry, eventId);

	private static void SplitWorldBulletinBody(string summary, out string major, out List<string> minors) => WorldBulletinStateOwner.SplitWorldBulletinBody(summary, out major, out minors);

	// kingdom ids: weekly_report:kingdom:bulletin:{kingdom}:{seq}:{day}; world: weekly_report:world:bulletin:{seq}:{day}.
	private static string ParseWorldBulletinIssueNumber(string eventId) => WorldBulletinStateOwner.ParseWorldBulletinIssueNumber(eventId);

	private static string BuildWorldBulletinMetaText(EventRecordEntry entry, WorldBulletinLayout layout, bool worldScope) => WorldBulletinStateOwner.BuildWorldBulletinMetaText(entry, layout, worldScope, ResolveKingdomDisplay);

	private static string WorldBulletinCategoryForKind(string kind) => WorldBulletinStateOwner.WorldBulletinCategoryForKind(kind);
}
