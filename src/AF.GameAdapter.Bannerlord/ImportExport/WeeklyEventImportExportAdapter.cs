using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Weekly file protocol and imports use the original record/opening authority and codecs.
internal sealed class WeeklyEventImportExportAdapter
{
 private readonly WeeklyEventRecordStateOwner _weekly;
 private readonly Func<List<EventRecordEntry>,List<EventRecordEntry>> _sanitize;
 private readonly Action _markOpening, _notifyTimeline, _openEventMenu;
 private readonly Func<string> _fingerprint;
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 internal WeeklyEventImportExportAdapter(WeeklyEventRecordStateOwner weekly,
  Func<List<EventRecordEntry>,List<EventRecordEntry>> sanitize, Action markOpening,
  Func<string> fingerprint, Action notifyTimeline, Action openEventMenu,
  Action<string,string,Action,Action,Action> showDuplicate)
 { _weekly=weekly; _sanitize=sanitize; _markOpening=markOpening; _fingerprint=fingerprint;
  _notifyTimeline=notifyTimeline; _openEventMenu=openEventMenu; _showDuplicate=showDuplicate; }
 internal bool TryLoadEventDataFromImportDir(string importDir,out EventImportPayload payload,out string error)
  => WeeklyEventDataImportOwner.TryLoadEventDataFromImportDir(importDir,out payload,out error,
   path=>PlayerExportsStore.ReadJson<EventWorldOpeningSummaryJson>(path)?.Summary,
   PlayerExportsStore.ReadJson<Dictionary<string,string>>,PlayerExportsStore.ReadJson<List<EventRecordEntry>>,_sanitize);
	internal void ApplyImportedEventData(EventImportPayload payload, bool overwriteExisting)
    {
        if (payload == null) return;
        WeeklyEventDataImportOwner.ApplyOpening(payload, overwriteExisting,
            ref _weekly.WorldOpening, ref _weekly.KingdomOpenings, _markOpening);
        if (!payload.HasEventRecordsFile) return;
        string previous = _fingerprint();
        WeeklyEventDataImportOwner.ApplyRecords(payload, overwriteExisting, ref _weekly.Records, _sanitize);
        if (!string.Equals(previous, _fingerprint(), StringComparison.Ordinal))
            Interlocked.Increment(ref _weekly.PublishedHistoryRevision);
        _notifyTimeline();
    }

	internal void ExportEventData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ExportEventDataToDir(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ExportEventDataToDir(string exportDir)
	{
		string text = Path.Combine(exportDir, "event_data");
		Directory.CreateDirectory(text);
		PlayerExportsStore.ClearCandidateJsonFiles(text);
		PlayerExportsStore.WriteJson(Path.Combine(text, "WorldOpeningSummary.json"), new EventWorldOpeningSummaryJson
		{
			Summary = (_weekly.WorldOpening ?? "").Trim()
		});
		PlayerExportsStore.WriteJson(Path.Combine(text, "KingdomOpeningSummaries.json"), BuildEventKingdomSummaryExportMap());
		PlayerExportsStore.WriteJson(Path.Combine(text, "EventRecords.json"), _sanitize(_weekly.Records));
	}

	internal Dictionary<string, string> BuildEventKingdomSummaryExportMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (_weekly.KingdomOpenings == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, string> item in _weekly.KingdomOpenings)
		{
			string text = (item.Key ?? "").Trim();
			string text2 = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
			{
				dictionary[text] = text2;
			}
		}
		return dictionary;
	}

	internal void ImportEventData(string folderName) => ImportEventDataScoped(folderName, null);

	internal void ImportEventDataScoped(string folderName, OnboardingImportStep completion)
	{
        if (completion != null && !completion.CanApply()) return;
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			if (!TryLoadEventDataFromImportDir(importDir, out var payload, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：" + error));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			int num = 0;
			int num2 = 0;
			if (payload.HasWorldSummaryFile)
			{
				num2 = 1;
				if (!string.IsNullOrWhiteSpace(_weekly.WorldOpening))
				{
					num = 1;
				}
			}
			int num3 = 0;
			int num4 = payload.HasKingdomSummariesFile ? payload.KingdomSummaries.Count : 0;
			if (payload.HasKingdomSummariesFile && _weekly.KingdomOpenings != null)
			{
				foreach (string key in payload.KingdomSummaries.Keys)
				{
					if (!string.IsNullOrWhiteSpace(key) && _weekly.KingdomOpenings.ContainsKey(key))
					{
						num3++;
					}
				}
			}
			int num5 = 0;
			int num6 = payload.HasEventRecordsFile ? payload.EventRecords.Count : 0;
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_weekly.Records != null)
			{
				foreach (EventRecordEntry eventRecordEntry in _weekly.Records)
				{
					string text = (eventRecordEntry?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						hashSet.Add(text);
					}
				}
			}
			if (payload.HasEventRecordsFile)
			{
				foreach (EventRecordEntry eventRecordEntry2 in payload.EventRecords)
				{
					string text2 = (eventRecordEntry2?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && hashSet.Contains(text2))
					{
						num5++;
					}
				}
			}
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				ApplyImportedEventData(payload, overwriteExisting: true);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));

                completion?.Finish(OnboardingImportResult.Completed);
};
			Action onSkipDuplicates = delegate
			{
				ApplyImportedEventData(payload, overwriteExisting: false);
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));

                completion?.Finish(OnboardingImportResult.Completed);
};
            if (completion != null) { action = completion.Guard(action); onSkipDuplicates = completion.Guard(onSkipDuplicates); }
			if (flag)
			{
				string text3 = "导入数据与当前游戏存在重复。\n世界开局概要：" + num + "/" + num2 + "\n王国开局概要：" + num3 + "/" + num4 + "\n事件记录：" + num5 + "/" + num6 + "\n请选择处理方式：";
				completion?.Pending();
				_showDuplicate("检测到重复 - 事件编辑", text3, action, onSkipDuplicates, delegate
				{
                    if (completion != null && !completion.CanApply()) return;
                    completion?.Finish(OnboardingImportResult.Cancelled);
					if (completion == null) _openEventMenu();
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
            completion?.Finish(OnboardingImportResult.Failed);
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

 internal void PrepareEventPayload(string importDir, out EventImportPayload eventPayload, out int eventWorldDupCount, out int eventWorldTotalCount, out int eventKingdomDupCount, out int eventKingdomTotalCount, out int eventRecordDupCount, out int eventRecordTotalCount)
 {
 eventPayload=null; eventWorldDupCount=0; eventWorldTotalCount=0; eventKingdomDupCount=0; eventKingdomTotalCount=0; eventRecordDupCount=0; eventRecordTotalCount=0;
			try
			{
				if (TryLoadEventDataFromImportDir(importDir, out eventPayload, out var _))
				{
					if (eventPayload.HasWorldSummaryFile)
					{
						eventWorldTotalCount = 1;
						if (!string.IsNullOrWhiteSpace(_weekly.WorldOpening))
						{
							eventWorldDupCount = 1;
						}
					}
					if (eventPayload.HasKingdomSummariesFile)
					{
						eventKingdomTotalCount = eventPayload.KingdomSummaries.Count;
						if ((_weekly.KingdomOpenings != null))
						{
							foreach (string key4 in eventPayload.KingdomSummaries.Keys)
							{
								if (!string.IsNullOrWhiteSpace(key4) && _weekly.KingdomOpenings.ContainsKey(key4))
								{
									eventKingdomDupCount++;
								}
							}
						}
					}
					if (eventPayload.HasEventRecordsFile)
					{
						eventRecordTotalCount = eventPayload.EventRecords.Count;
						HashSet<string> hashSet4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						if (_weekly.Records != null)
						{
							foreach (EventRecordEntry eventRecordEntry in _weekly.Records)
							{
								string text12 = (eventRecordEntry?.EventId ?? "").Trim();
								if (!string.IsNullOrWhiteSpace(text12))
								{
									hashSet4.Add(text12);
								}
							}
						}
						foreach (EventRecordEntry eventRecordEntry2 in eventPayload.EventRecords)
						{
							string text13 = (eventRecordEntry2?.EventId ?? "").Trim();
							if (!string.IsNullOrWhiteSpace(text13) && hashSet4.Contains(text13))
							{
								eventRecordDupCount++;
							}
						}
					}
				}
			}
			catch
			{
				eventWorldDupCount = 0;
				eventWorldTotalCount = 0;
				eventKingdomDupCount = 0;
				eventKingdomTotalCount = 0;
				eventRecordDupCount = 0;
				eventRecordTotalCount = 0;
				eventPayload = null;
			}
 }
}
