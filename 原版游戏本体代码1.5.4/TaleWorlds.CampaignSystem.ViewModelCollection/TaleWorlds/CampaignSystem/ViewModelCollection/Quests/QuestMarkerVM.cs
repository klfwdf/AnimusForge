using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

public class QuestMarkerVM : ViewModel
{
	private bool _isTrackMarker;

	private int _questMarkerType;

	private HintViewModel _questHint;

	public TextObject QuestTitle { get; private set; }

	public TextObject QuestHintText { get; private set; }

	public CampaignUIHelper.IssueQuestFlags IssueQuestFlag { get; private set; }

	[DataSourceProperty]
	public bool IsTrackMarker
	{
		get
		{
			return _isTrackMarker;
		}
		set
		{
			if (value != _isTrackMarker)
			{
				_isTrackMarker = value;
				OnPropertyChangedWithValue(value, "IsTrackMarker");
			}
		}
	}

	[DataSourceProperty]
	public int QuestMarkerType
	{
		get
		{
			return _questMarkerType;
		}
		set
		{
			if (value != _questMarkerType)
			{
				_questMarkerType = value;
				OnPropertyChangedWithValue(value, "QuestMarkerType");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel QuestHint
	{
		get
		{
			return _questHint;
		}
		set
		{
			if (value != _questHint)
			{
				_questHint = value;
				OnPropertyChangedWithValue(value, "QuestHint");
			}
		}
	}

	public QuestMarkerVM(CampaignUIHelper.IssueQuestFlags issueQuestFlag, TextObject questTitle = null, TextObject questHintText = null)
	{
		RefreshWith(issueQuestFlag, questTitle, questHintText);
	}

	public void RefreshWith(CampaignUIHelper.IssueQuestFlags issueQuestFlag, TextObject questTitle = null, TextObject questHintText = null)
	{
		IssueQuestFlag = issueQuestFlag;
		QuestMarkerType = (int)issueQuestFlag;
		QuestTitle = questTitle ?? TextObject.GetEmpty();
		QuestHintText = questHintText;
		if (QuestHintText != null)
		{
			QuestHint = new HintViewModel(QuestHintText);
		}
		IsTrackMarker = issueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedIssue || issueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (!TextObject.IsNullOrEmpty(QuestHintText))
		{
			QuestHint = new HintViewModel(QuestHintText);
		}
		else
		{
			QuestHint = new HintViewModel();
		}
	}
}
