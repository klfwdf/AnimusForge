using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class FocusAddedByPlayerEvent : EventBase
{
	public Hero AddedPlayer { get; private set; }

	public SkillObject AddedSkill { get; private set; }

	public FocusAddedByPlayerEvent(Hero addedPlayer, SkillObject addedSkill)
	{
		AddedPlayer = addedPlayer;
		AddedSkill = addedSkill;
	}
}
