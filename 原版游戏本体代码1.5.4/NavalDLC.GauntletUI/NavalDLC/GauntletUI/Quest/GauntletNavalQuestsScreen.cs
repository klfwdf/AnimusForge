using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.MountAndBlade.View.Screens;

namespace NavalDLC.GauntletUI.Quest;

[GameStateScreen(typeof(QuestsState))]
public class GauntletNavalQuestsScreen : GauntletQuestsScreen
{
	public GauntletNavalQuestsScreen(QuestsState questsState)
		: base(questsState)
	{
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_dataSource == null)
		{
			return;
		}
		for (int i = 0; i < _dataSource.ActiveQuestsList.Count; i++)
		{
			QuestItemVM questItemVM = _dataSource.ActiveQuestsList[i];
			if (questItemVM.Quest != null)
			{
				questItemVM.IsNavalQuest = questItemVM.Quest.SpecialQuestType == "NavalStoryline";
			}
		}
	}
}
