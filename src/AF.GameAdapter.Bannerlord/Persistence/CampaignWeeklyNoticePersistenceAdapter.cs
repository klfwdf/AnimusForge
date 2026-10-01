using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

internal static class CampaignWeeklyNoticePersistenceAdapter
{
    internal static void Save(IDataStore dataStore, WeeklyNoticeStateOwner owner, Func<string, bool> recordExists)
    {
				owner.Unread = WeeklyNoticeStateOwner.SanitizeUnreadWeeklyReportNoticeEventIds(owner.Unread).Where((string x) => recordExists(x)).ToList();
				List<string> unreadWeeklyReportNoticeEventIds = new List<string>(owner.Unread);
				dataStore.SyncData("_af_unreadWeeklyReportNotices_v1", ref unreadWeeklyReportNoticeEventIds);
				owner.Unread = WeeklyNoticeStateOwner.SanitizeUnreadWeeklyReportNoticeEventIds(unreadWeeklyReportNoticeEventIds);
				owner.ReadingXpClaimedEventIds = WeeklyNoticeStateOwner.SanitizeWeeklyReportEventIds(owner.ReadingXpClaimedEventIds).Where((string x) => recordExists(x)).ToList();
				List<string> weeklyReportReadingXpClaimedEventIds = new List<string>(owner.ReadingXpClaimedEventIds);
				dataStore.SyncData("_af_weeklyReportReadingXpClaimed_v1", ref weeklyReportReadingXpClaimedEventIds);
				owner.ReadingXpClaimedEventIds = WeeklyNoticeStateOwner.SanitizeWeeklyReportEventIds(weeklyReportReadingXpClaimedEventIds).Where((string x) => recordExists(x)).ToList();
				owner.NormalizeReadingXpPendingBatch();
				int weeklyReportReadingXpPendingCount = owner.ReadingXpPendingCount;
				int weeklyReportReadingXpPendingCharm = owner.ReadingXpPendingCharm;
				int weeklyReportReadingXpPendingLeadership = owner.ReadingXpPendingLeadership;
				int weeklyReportReadingXpPendingSteward = owner.ReadingXpPendingSteward;
				dataStore.SyncData("_afowner.ReadingXpPendingCount_v1", ref weeklyReportReadingXpPendingCount);
				dataStore.SyncData("_afowner.ReadingXpPendingCharm_v1", ref weeklyReportReadingXpPendingCharm);
				dataStore.SyncData("_afowner.ReadingXpPendingLeadership_v1", ref weeklyReportReadingXpPendingLeadership);
				dataStore.SyncData("_afowner.ReadingXpPendingSteward_v1", ref weeklyReportReadingXpPendingSteward);
				owner.ReadingXpPendingCount = weeklyReportReadingXpPendingCount;
				owner.ReadingXpPendingCharm = weeklyReportReadingXpPendingCharm;
				owner.ReadingXpPendingLeadership = weeklyReportReadingXpPendingLeadership;
				owner.ReadingXpPendingSteward = weeklyReportReadingXpPendingSteward;
				owner.NormalizeReadingXpPendingBatch();
    }

    internal static void Load(IDataStore dataStore, WeeklyNoticeStateOwner owner, Func<string, bool> recordExists)
    {
			List<string> unreadWeeklyReportNoticeEventIdsLoad = new List<string>();
			dataStore.SyncData("_af_unreadWeeklyReportNotices_v1", ref unreadWeeklyReportNoticeEventIdsLoad);
			owner.Unread = WeeklyNoticeStateOwner.SanitizeUnreadWeeklyReportNoticeEventIds(unreadWeeklyReportNoticeEventIdsLoad).Where((string x) => recordExists(x)).ToList();
			List<string> weeklyReportReadingXpClaimedEventIdsLoad = new List<string>();
			dataStore.SyncData("_af_weeklyReportReadingXpClaimed_v1", ref weeklyReportReadingXpClaimedEventIdsLoad);
			owner.ReadingXpClaimedEventIds = WeeklyNoticeStateOwner.SanitizeWeeklyReportEventIds(weeklyReportReadingXpClaimedEventIdsLoad).Where((string x) => recordExists(x)).ToList();
			dataStore.SyncData("_afowner.ReadingXpPendingCount_v1", ref owner.ReadingXpPendingCount);
			dataStore.SyncData("_afowner.ReadingXpPendingCharm_v1", ref owner.ReadingXpPendingCharm);
			dataStore.SyncData("_afowner.ReadingXpPendingLeadership_v1", ref owner.ReadingXpPendingLeadership);
			dataStore.SyncData("_afowner.ReadingXpPendingSteward_v1", ref owner.ReadingXpPendingSteward);
			owner.NormalizeReadingXpPendingBatch();
    }

}
