using System;using System.Collections.Generic;using System.Linq;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class WeeklyNoticeStateOwner {
 private List<string> _unread=new();private readonly HashSet<string> _unreadIds=new(StringComparer.OrdinalIgnoreCase);
 private readonly Queue<string> _pending=new();
 internal readonly HashSet<string> Shown=new(StringComparer.OrdinalIgnoreCase);
 internal bool NormalizedForPolicy;
 internal List<string> Unread { get=>_unread;set { _unread=SanitizeUnreadWeeklyReportNoticeEventIds(value);RebuildPending(); } }
 private void RebuildPending(){_pending.Clear();_unreadIds.Clear();foreach(var id in _unread){_unreadIds.Add(id);if(!Shown.Contains(id))_pending.Enqueue(id);}}
 internal void ResetShown(){Shown.Clear();RebuildPending();}
 internal void Queue(string eventId){string id=(eventId??"").Trim();if(id.Length==0||!_unreadIds.Add(id))return;_unread.Add(id);_pending.Enqueue(id);}
 internal void MarkRead(string eventId){string id=(eventId??"").Trim();if(id.Length==0)return;Shown.Remove(id);if(!_unreadIds.Remove(id))return;_unread.RemoveAll(x=>string.Equals((x??"").Trim(),id,StringComparison.OrdinalIgnoreCase));}
 // No sanitation/copy/full unread scan on idle ticks. One bounded pending cursor owns publication.
 internal int PublishPending(WeeklyNoticePort port,int budget=8){int visited=0;while(visited<budget&&_pending.Count>0){string id=_pending.Peek();visited++;if(!_unreadIds.Contains(id)||Shown.Contains(id)){_pending.Dequeue();continue;}var record=port.FindRecord(id);if(record==null){MarkRead(id);_pending.Dequeue();continue;}port.Publish(record);Shown.Add(id);_pending.Dequeue();}return visited;}
internal static List<string> SanitizeUnreadWeeklyReportNoticeEventIds(IEnumerable<string> source)
	{
		return SanitizeWeeklyReportEventIds(source);
	}
internal void NormalizeUnreadWeeklyReportNoticesForCurrentPolicy(WeeklyNoticePort port)
	{
		if (NormalizedForPolicy)
		{
			return;
		}
		try
		{
			List<string> unreadEventIds = SanitizeUnreadWeeklyReportNoticeEventIds(Unread);
			Dictionary<int, List<EventRecordEntry>> kingdomReportsByWeek = new Dictionary<int, List<EventRecordEntry>>();
			HashSet<string> retainedBulletinIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string eventId in unreadEventIds)
			{
				EventRecordEntry entry = port.FindRecord(eventId);
				if (entry != null && port.IsBulletin(entry.EventId))
				{
					// One bulletin now; kingdom-scope bulletins from earlier builds no longer pop.
					if (string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
					{
						retainedBulletinIds.Add(entry.EventId ?? "");
					}
					continue;
				}
				if (entry == null || !string.Equals((entry.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (!kingdomReportsByWeek.TryGetValue(entry.WeekIndex, out List<EventRecordEntry> entries))
				{
					entries = new List<EventRecordEntry>();
					kingdomReportsByWeek[entry.WeekIndex] = entries;
				}
				entries.Add(entry);
			}
			HashSet<string> retainedEventIds = new HashSet<string>(retainedBulletinIds, StringComparer.OrdinalIgnoreCase);
			foreach (List<EventRecordEntry> entries in kingdomReportsByWeek.Values)
			{
				string nearestKingdomId = port.NearestKingdom(entries.Select((EventRecordEntry x) => x?.ScopeKingdomId));
				EventRecordEntry nearestEntry = entries.FirstOrDefault((EventRecordEntry x) => string.Equals((x?.ScopeKingdomId ?? "").Trim(), nearestKingdomId, StringComparison.OrdinalIgnoreCase));
				if (nearestEntry != null)
				{
					retainedEventIds.Add(nearestEntry.EventId ?? "");
				}
			}
			Unread = unreadEventIds.Where((string x) => retainedEventIds.Contains(x)).ToList();
		}
		catch (Exception ex)
		{
			port.Log("EventWeeklyReport", "[WARN] normalize legacy weekly report notices failed: " + ex.Message);
		}
		finally
		{
			NormalizedForPolicy = true;
		}
	}
internal static List<string> SanitizeWeeklyReportEventIds(IEnumerable<string> source)
	{
		return (source ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Where((string x) => x.StartsWith("weekly_report:", StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}
}
internal sealed class WeeklyNoticePort {internal Func<string,EventRecordEntry> FindRecord;internal Func<string,bool> IsBulletin;internal Func<IEnumerable<string>,string> NearestKingdom;internal Action<string,string> Log;internal Action<EventRecordEntry> Publish;}
