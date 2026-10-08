using SandBox.View.Map;using TaleWorlds.CampaignSystem.GameState;using TaleWorlds.MountAndBlade;
using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.Core;using TaleWorlds.Library;using EventRecordEntry=AnimusForge.MyBehavior.EventRecordEntry;
namespace AnimusForge;
// Main-thread reading rewards consume the sole notice state and current weekly record query.
internal sealed class WeeklyNoticeGameAdapter {
internal const int WeeklyReportReadingXpBatchSize=20;
private readonly WeeklyNoticeStateOwner _state;
private readonly Func<string,EventRecordEntry> _findRecord;
private readonly Func<EventRecordEntry,string,bool> _showBulletin;
private readonly Func<string,string,int,string> _defaultTitle;
private readonly Func<EventRecordEntry,string> _subtitle;
internal WeeklyNoticeGameAdapter(WeeklyNoticeStateOwner state,Func<string,EventRecordEntry> findRecord,Func<WeeklyNoticePort> noticePort=null,Func<bool> isCurrent=null,Func<EventRecordEntry,string,bool> showBulletin=null,Func<string,string,int,string> defaultTitle=null,Func<EventRecordEntry,string> subtitle=null) {_state=state;_findRecord=findRecord;_noticePort=noticePort;_isCurrent=isCurrent;_showBulletin=showBulletin;_defaultTitle=defaultTitle;_subtitle=subtitle;}
internal bool OpenWeeklyReportNoticeFromMap(string eventId)
	{
		string text = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		EventRecordEntry eventRecordEntry = _findRecord(text);
		if (eventRecordEntry == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("这条周报记录已经不存在。"));
			_state.MarkRead(text);
			return true;
		}
		bool hasFullReport = !string.IsNullOrWhiteSpace(eventRecordEntry.Summary);
		// Bulletins have no three-section chronicle body; they render as a single column.
		bool isBulletin = WorldBulletinStateOwner.IsWorldBulletinEventId(text);
		if (isBulletin && hasFullReport && _showBulletin(eventRecordEntry, text))
		{
			_state.MarkRead(text);
			return true;
		}
		string bodyText = WeeklyEventRecordStateOwner.BuildWeeklyReportPopupBodyText(eventRecordEntry);
		bool flag = DevWeeklyReportPopup.Show(BuildWeeklyReportNoticeTitle(eventRecordEntry, _defaultTitle), _subtitle(eventRecordEntry), bodyText, null, "", useChronicleColumns: hasFullReport && !isBulletin, useShortReportLayout: !hasFullReport, showCloseButton: false, minimumDwellSeconds: 10.0, onMinimumDwellMet: delegate
		{
			TryAwardWeeklyReportReadingXp(text);
		});
		if (flag)
		{
			_state.MarkRead(text);
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("打开周报失败。"));
		}
		return flag;
	}
internal static string ResolveNearestWeeklyReportKingdomId(IEnumerable<string> kingdomIds)
	{
		List<string> list = (kingdomIds ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		List<string> kingdomIdsByPlayerProximity = MemoryEntityIdentityBannerlordAdapter.GetKingdomIdsByPlayerProximity(list);
		string text = kingdomIdsByPlayerProximity.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return (list[0] ?? "").Trim();
	}
internal void TryAwardWeeklyReportReadingXp(string eventId)
	{
		string text = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("weekly_report:", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (_state.ReadingXpClaimedEventIds == null)
		{
			_state.ReadingXpClaimedEventIds = new List<string>();
		}
		if (_state.ReadingXpClaimedEventIds.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableWeeklyReportReadingXpReward)
		{
			return;
		}
		int xpPerHundred = Math.Max(0, Math.Min(100, settings.WeeklyReportReadingXpPerHundredChars));
		int skillCap = Math.Max(0, Math.Min(500, settings.WeeklyReportReadingXpSkillCap));
		if (xpPerHundred <= 0 || skillCap <= 0)
		{
			return;
		}
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return;
		}
		if (!CanAwardWeeklyReportReadingXp(mainHero, out string ineligibleReason))
		{
			Logger.Log("EventWeeklyReport", "[ReadingXp] skipped eventId=" + text + " reason=" + ineligibleReason);
			return;
		}
		EventRecordEntry eventRecordEntry = _findRecord(text);
		if (eventRecordEntry == null)
		{
			return;
		}
		string bodyText = WeeklyEventRecordStateOwner.BuildWeeklyReportPopupBodyText(eventRecordEntry);
		WeeklyReportSectionSplit split = WeeklyReportTextHelper.SplitChronicleBody(bodyText);
		int leadershipXp;
		int charmXp;
		int stewardXp;
		if (split.HasExplicitSections)
		{
			leadershipXp = CalculateWeeklyReportReadingXp(WeeklyReportTextHelper.CountMeaningfulUnits(split.MilitaryEventsText), xpPerHundred, skillCap);
			charmXp = CalculateWeeklyReportReadingXp(WeeklyReportTextHelper.CountMeaningfulUnits(split.DiplomaticAffairsText), xpPerHundred, skillCap);
			stewardXp = CalculateWeeklyReportReadingXp(WeeklyReportTextHelper.CountMeaningfulUnits(split.DomesticRealmText), xpPerHundred, skillCap);
		}
		else
		{
			int sharedXp = CalculateWeeklyReportReadingXp(WeeklyReportTextHelper.CountMeaningfulUnits(split.NormalizedBodyText), xpPerHundred, skillCap);
			// Bulletins have no chronicle sections and publish every few days: one body's worth of XP is split
			// across the three skills instead of being granted to each, so their total matches a sectioned report.
			if (WorldBulletinStateOwner.IsWorldBulletinEventId(text))
			{
				sharedXp = (sharedXp + 2) / 3;
			}
			leadershipXp = sharedXp;
			charmXp = sharedXp;
			stewardXp = sharedXp;
		}
		if (leadershipXp <= 0 && charmXp <= 0 && stewardXp <= 0)
		{
			return;
		}
		_state.ReadingXpClaimedEventIds.Add(text);
		_state.NormalizeReadingXpPendingBatch();
		_state.ReadingXpPendingCount++;
		_state.ReadingXpPendingCharm += charmXp;
		_state.ReadingXpPendingLeadership += leadershipXp;
		_state.ReadingXpPendingSteward += stewardXp;
		Logger.Log("EventWeeklyReport", "[ReadingXp] accumulated eventId=" + text + " pending_count=" + _state.ReadingXpPendingCount + "/" + WeeklyReportReadingXpBatchSize + " charm+=" + charmXp + " leadership+=" + leadershipXp + " steward+=" + stewardXp + " pending_charm=" + _state.ReadingXpPendingCharm + " pending_leadership=" + _state.ReadingXpPendingLeadership + " pending_steward=" + _state.ReadingXpPendingSteward + " per100=" + xpPerHundred + " cap=" + skillCap);
		if (_state.ReadingXpPendingCount < WeeklyReportReadingXpBatchSize)
		{
			return;
		}
		int batchCount = _state.ReadingXpPendingCount;
		int batchCharm = _state.ReadingXpPendingCharm;
		int batchLeadership = _state.ReadingXpPendingLeadership;
		int batchSteward = _state.ReadingXpPendingSteward;
		if (!TryAwardWeeklyReportReadingXpBatch(mainHero, batchLeadership, batchCharm, batchSteward, out string awardFailureReason))
		{
			Logger.Log("EventWeeklyReport", "[ReadingXp][WARN] batch_award_failed count=" + batchCount + " charm=" + batchCharm + " leadership=" + batchLeadership + " steward=" + batchSteward + " reason=" + awardFailureReason);
			return;
		}
		_state.ReadingXpPendingCount = 0;
		_state.ReadingXpPendingCharm = 0;
		_state.ReadingXpPendingLeadership = 0;
		_state.ReadingXpPendingSteward = 0;
		InformationManager.DisplayMessage(new InformationMessage("周报研读突破：累计研读 " + batchCount + " 篇，魅力 +" + batchCharm + "，统御 +" + batchLeadership + "，管理 +" + batchSteward + "。"));
		Logger.Log("EventWeeklyReport", "[ReadingXp] batch_awarded count=" + batchCount + " charm=" + batchCharm + " leadership=" + batchLeadership + " steward=" + batchSteward);
	}

internal static bool CanAwardWeeklyReportReadingXp(Hero hero, out string reason)
	{
		reason = "";
		if (hero == null)
		{
			reason = "hero_null";
			return false;
		}
		if (hero.HeroDeveloper == null)
		{
			reason = "hero_developer_null";
			return false;
		}
		try
		{
			if (hero.IsChild)
			{
				reason = "main_hero_under_age age=" + Math.Round(hero.Age, 2);
				return false;
			}
		}
		catch (Exception ex)
		{
			reason = "age_check_failed " + ex.Message;
			return false;
		}
		return true;
	}

internal static bool TryAwardWeeklyReportReadingXpBatch(Hero hero, int leadershipXp, int charmXp, int stewardXp, out string failureReason)
	{
		failureReason = "";
		if (!CanAwardWeeklyReportReadingXp(hero, out failureReason))
		{
			return false;
		}
		try
		{
			if (leadershipXp > 0)
			{
				hero.AddSkillXp(DefaultSkills.Leadership, leadershipXp);
			}
			if (charmXp > 0)
			{
				hero.AddSkillXp(DefaultSkills.Charm, charmXp);
			}
			if (stewardXp > 0)
			{
				hero.AddSkillXp(DefaultSkills.Steward, stewardXp);
			}
			return true;
		}
		catch (Exception ex)
		{
			failureReason = ex.GetType().Name + ": " + ex.Message;
			return false;
		}
	}

internal static int CalculateWeeklyReportReadingXp(int meaningfulUnitCount, int xpPerHundred, int skillCap)
	{
		if (meaningfulUnitCount <= 0 || xpPerHundred <= 0 || skillCap <= 0)
		{
			return 0;
		}
		int xp = (int)Math.Round((double)meaningfulUnitCount * xpPerHundred / 100.0, MidpointRounding.AwayFromZero);
		return Math.Max(0, Math.Min(skillCap, xp));
	}

internal static List<string> SanitizeWeeklyReportReadingXpClaimedEventIds(IEnumerable<string> source)
	{
		return WeeklyNoticeStateOwner.SanitizeWeeklyReportEventIds(source);
	}

internal MapNotificationView RegisteredMapNotificationView;
private readonly Func<WeeklyNoticePort> _noticePort;
private readonly Func<bool> _isCurrent;
internal static bool CanPublishWeeklyReportMapNotification()
	{
		try
		{
			return Mission.Current == null && Game.Current?.GameStateManager?.ActiveState is MapState && MapScreen.Instance?.MapNotificationView != null;
		}
		catch
		{
			return false;
		}
	}

internal bool TryEnsureWeeklyReportMapNotificationRegistered()
	{
		try
		{
			MapNotificationView mapNotificationView = MapScreen.Instance?.MapNotificationView;
			if (mapNotificationView == null)
			{
				return false;
			}
			if (!ReferenceEquals(RegisteredMapNotificationView, mapNotificationView))
			{
				_state.ResetShown();
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeWeeklyReportMapNotification), typeof(AnimusForgeWeeklyReportMapNotificationItemVM));
				RegisteredMapNotificationView = mapNotificationView;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[WARN] register weekly report map notification failed: " + ex.Message);
			return false;
		}
	}

internal void TryPublishUnreadWeeklyReportMapNotifications() { if (_state.Unread == null || _state.Unread.Count == 0 || !IsWeeklyReportMapNotificationEnabled()) return; _state.NormalizeUnreadWeeklyReportNoticesForCurrentPolicy(_noticePort()); if (_state.Unread.Count == 0 || !CanPublishWeeklyReportMapNotification() || !TryEnsureWeeklyReportMapNotificationRegistered()) return; _state.PublishPending(_noticePort()); }

internal void QueueWeeklyReportMapNotice(string eventId) { if (IsWeeklyReportMapNotificationEnabled()) _state.Queue(eventId); }

internal static bool IsWeeklyReportMapNotificationEnabled()
	{
		return DuelSettings.IsWeeklyReportMapNotificationEnabled();
	}

internal void OnMapNoticeRemoved(InformationData data)
	{
		if (!_isCurrent())
		{
			return;
		}
		if (data is AnimusForgeWeeklyReportMapNotification weeklyReportMapNotification)
		{
			_state.MarkRead(weeklyReportMapNotification.EventId);
		}
	}

internal static string BuildWeeklyReportNoticeTitle(EventRecordEntry entry, Func<string,string,int,string> defaultTitle)
	{
		string text = (entry?.Title ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return defaultTitle(entry?.EventKind, entry?.ScopeKingdomId, entry?.WeekIndex ?? 0);
	}

internal static string BuildWeeklyReportNoticeDescription(EventRecordEntry entry, Func<EventRecordEntry,string> subtitle)
	{
		string text = subtitle(entry);
		return string.IsNullOrWhiteSpace(text) ? "点击查看当前周报。" : ("点击查看：" + text);
	}
}
