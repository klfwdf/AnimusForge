using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class QuestsState : TaleWorlds.Core.GameState
{
	private IQuestsStateHandler _handler;

	public IssueBase InitialSelectedIssue { get; private set; }

	public QuestBase InitialSelectedQuest { get; private set; }

	public JournalLogEntry InitialSelectedLog { get; private set; }

	public override bool IsMenuState => true;

	public IQuestsStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public QuestsState()
	{
	}

	public QuestsState(IssueBase initialSelectedIssue)
	{
		InitialSelectedIssue = initialSelectedIssue;
	}

	public QuestsState(QuestBase initialSelectedQuest)
	{
		InitialSelectedQuest = initialSelectedQuest;
	}

	public QuestsState(JournalLogEntry initialSelectedLog)
	{
		InitialSelectedLog = initialSelectedLog;
	}
}
