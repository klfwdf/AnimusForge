using System;

namespace TaleWorlds.CampaignSystem.Issues.IssueQuestTasks;

public class TalkToNpcQuestTask : QuestTaskBase
{
	private CharacterObject _character;

	public TalkToNpcQuestTask(Hero hero, Action onSucceededAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction)
	{
		_character = hero.CharacterObject;
	}

	public TalkToNpcQuestTask(CharacterObject character, Action onSucceededAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction)
	{
		_character = character;
	}

	public bool IsTaskCharacter()
	{
		return _character == CharacterObject.OneToOneConversationCharacter;
	}

	protected override void OnFinished()
	{
		_character = null;
	}

	public override void SetReferences()
	{
	}
}
