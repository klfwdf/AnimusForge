using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace StoryMode;

public abstract class StoryModeQuestBase : QuestBase
{
	public override string SpecialQuestType => "MainStoryline";

	public override bool IsRemainingTimeHidden => true;

	protected StoryModeQuestBase(string questId, Hero questGiver, CampaignTime duration)
		: base(questId, questGiver, duration, 0)
	{
	}

	protected override void OnTimedOut()
	{
		base.OnTimedOut();
		TextObject text = new TextObject("{=JTPmw3cb}You couldn't complete the quest in time.");
		AddLog(text);
	}
}
