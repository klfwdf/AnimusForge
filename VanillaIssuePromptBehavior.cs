using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public class VanillaIssuePromptBehavior : CampaignBehaviorBase
{
	private sealed class RecentQuestRecord
	{
		public string QuestId;

		public string QuestTitle;

		public string GiverId;

		public string GiverName;

		public string CompletionDetail;

		public int RewardGold;

		public float CompletionDay;

		public List<string> RecentJournalEntries = new List<string>();
	}

	private Dictionary<string, string> _recentQuestCompletionStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static VanillaIssuePromptBehavior Instance { get; private set; }

	public VanillaIssuePromptBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_recentQuestCompletionStorage == null)
		{
			_recentQuestCompletionStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData("_vanillaIssueRecentQuestCompletion_v1", ref _recentQuestCompletionStorage);
		if (_recentQuestCompletionStorage == null)
		{
			_recentQuestCompletionStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	public bool TryGetRecentCompletionRecord(Hero giver, out string questTitle, out string completionDetail, out int rewardGold, out List<string> recentJournalEntries, bool consumeOnRead = false)
	{
		questTitle = "";
		completionDetail = "";
		rewardGold = 0;
		recentJournalEntries = null;
		if (giver == null || string.IsNullOrWhiteSpace(giver.StringId) || _recentQuestCompletionStorage == null)
		{
			return false;
		}
		if (!_recentQuestCompletionStorage.TryGetValue(giver.StringId, out var value) || string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		try
		{
			RecentQuestRecord recentQuestRecord = JsonConvert.DeserializeObject<RecentQuestRecord>(value);
			if (recentQuestRecord == null)
			{
				return false;
			}
			float num = (float)CampaignTime.Now.ToDays;
			if (num - recentQuestRecord.CompletionDay > 3f)
			{
				_recentQuestCompletionStorage.Remove(giver.StringId);
				return false;
			}
			questTitle = recentQuestRecord.QuestTitle ?? "";
			completionDetail = recentQuestRecord.CompletionDetail ?? "";
			rewardGold = recentQuestRecord.RewardGold;
			recentJournalEntries = recentQuestRecord.RecentJournalEntries ?? new List<string>();
			if (consumeOnRead)
			{
				_recentQuestCompletionStorage.Remove(giver.StringId);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		try
		{
			Hero questGiver = quest?.QuestGiver;
			if (questGiver == null || string.IsNullOrWhiteSpace(questGiver.StringId))
			{
				return;
			}
			RecentQuestRecord recentQuestRecord = new RecentQuestRecord
			{
				QuestId = quest.StringId ?? "",
				QuestTitle = (quest.Title?.ToString() ?? "").Trim(),
				GiverId = questGiver.StringId ?? "",
				GiverName = (questGiver.Name?.ToString() ?? "").Trim(),
				CompletionDetail = detail.ToString(),
				RewardGold = quest.RewardGold,
				CompletionDay = (float)CampaignTime.Now.ToDays
			};
			try
			{
				foreach (JournalLog item in quest.JournalEntries)
				{
					string text = (item?.LogText?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						recentQuestRecord.RecentJournalEntries.Add(text);
					}
				}
				if (recentQuestRecord.RecentJournalEntries.Count > 3)
				{
					recentQuestRecord.RecentJournalEntries = recentQuestRecord.RecentJournalEntries.GetRange(Math.Max(0, recentQuestRecord.RecentJournalEntries.Count - 3), Math.Min(3, recentQuestRecord.RecentJournalEntries.Count));
				}
			}
			catch
			{
			}
			_recentQuestCompletionStorage[questGiver.StringId] = JsonConvert.SerializeObject(recentQuestRecord);
		}
		catch
		{
		}
	}
}
