namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public interface ICharacterCreationStateHandler
{
	void OnCharacterCreationFinalized();

	void OnRefresh();

	void OnStageCreated(CharacterCreationStageBase stage);
}
