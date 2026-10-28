using System.Linq;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class JournalLogsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
		CampaignEvents.IssueLogAddedEvent.AddNonSerializedListener(this, OnIssueLogAdded);
		CampaignEvents.QuestLogAddedEvent.AddNonSerializedListener(this, OnQuestLogAdded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnIssueLogAdded(IssueBase issue, bool hideInformation)
	{
		JournalLogEntry journalLogEntry = GetRelatedLog(issue);
		if (journalLogEntry == null)
		{
			journalLogEntry = CreateRelatedLog(issue);
			LogEntry.AddLogEntry(journalLogEntry);
		}
		journalLogEntry.Update(GetEntries(issue));
	}

	private void OnQuestLogAdded(QuestBase quest, bool hideInformation)
	{
		JournalLogEntry journalLogEntry = GetRelatedLog(quest);
		if (journalLogEntry == null)
		{
			journalLogEntry = CreateRelatedLog(quest);
			LogEntry.AddLogEntry(journalLogEntry);
		}
		journalLogEntry.Update(GetEntries(quest));
	}

	private void OnQuestStarted(QuestBase quest)
	{
		JournalLogEntry journalLogEntry = GetRelatedLog(quest);
		if (journalLogEntry == null)
		{
			journalLogEntry = CreateRelatedLog(quest);
			LogEntry.AddLogEntry(journalLogEntry);
		}
		journalLogEntry.Update(GetEntries(quest));
		LogEntry.AddLogEntry(new IssueQuestStartLogEntry(journalLogEntry.RelatedHero));
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		JournalLogEntry journalLogEntry = GetRelatedLog(quest);
		if (journalLogEntry == null)
		{
			journalLogEntry = CreateRelatedLog(quest);
			LogEntry.AddLogEntry(journalLogEntry);
		}
		journalLogEntry.Update(GetEntries(quest), detail);
		LogEntry.AddLogEntry(new IssueQuestLogEntry(journalLogEntry.RelatedHero, journalLogEntry.Antagonist, detail));
	}

	private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver)
	{
		if (issueSolver == Hero.MainHero)
		{
			JournalLogEntry journalLogEntry = GetRelatedLog(issue);
			if (journalLogEntry == null)
			{
				journalLogEntry = CreateRelatedLog(issue);
				LogEntry.AddLogEntry(journalLogEntry);
			}
			journalLogEntry.Update(GetEntries(issue), details);
		}
	}

	private JournalLogEntry CreateRelatedLog(IssueBase issue)
	{
		if (issue.IssueQuest != null)
		{
			return new JournalLogEntry(issue.IssueQuest.Title, issue.IssueQuest.QuestGiver, null, issue.IssueQuest.IsSpecialQuest, issue, issue.IssueQuest);
		}
		return new JournalLogEntry(issue.Title, issue.IssueOwner, issue.CounterOfferHero, false, issue);
	}

	private JournalLogEntry CreateRelatedLog(QuestBase quest)
	{
		IssueBase issueOfQuest = IssueManager.GetIssueOfQuest(quest);
		if (issueOfQuest != null)
		{
			return CreateRelatedLog(issueOfQuest);
		}
		return new JournalLogEntry(quest.Title, quest.QuestGiver, null, quest.IsSpecialQuest, quest);
	}

	private JournalLogEntry GetRelatedLog(IssueBase issue)
	{
		return Campaign.Current.LogEntryHistory.FindLastGameActionLog((JournalLogEntry x) => x.IsRelatedTo(issue));
	}

	private JournalLogEntry GetRelatedLog(QuestBase quest)
	{
		IssueBase issueOfQuest = IssueManager.GetIssueOfQuest(quest);
		if (issueOfQuest != null)
		{
			return GetRelatedLog(issueOfQuest);
		}
		return Campaign.Current.LogEntryHistory.FindLastGameActionLog((JournalLogEntry x) => x.IsRelatedTo(quest));
	}

	private MBReadOnlyList<JournalLog> GetEntries(IssueBase issue)
	{
		if (issue.IssueQuest == null)
		{
			return issue.JournalEntries;
		}
		MBList<JournalLog> mBList = issue.JournalEntries.ToMBList();
		JournalLog journalLog = issue.IssueQuest.JournalEntries.FirstOrDefault();
		if (journalLog != null)
		{
			int i;
			for (i = 0; i < mBList.Count; i++)
			{
				if (mBList[i].LogTime > journalLog.LogTime)
				{
					i--;
					break;
				}
			}
			mBList.InsertRange(i, issue.IssueQuest.JournalEntries);
		}
		return mBList;
	}

	private MBReadOnlyList<JournalLog> GetEntries(QuestBase quest)
	{
		IssueBase issueOfQuest = IssueManager.GetIssueOfQuest(quest);
		if (issueOfQuest != null)
		{
			return GetEntries(issueOfQuest);
		}
		return quest.JournalEntries;
	}
}
