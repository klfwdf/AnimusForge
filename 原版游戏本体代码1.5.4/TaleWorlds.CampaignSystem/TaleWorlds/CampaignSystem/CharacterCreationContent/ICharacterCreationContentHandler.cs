namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public interface ICharacterCreationContentHandler
{
	void InitializeContent(CharacterCreationManager characterCreationManager);

	void AfterInitializeContent(CharacterCreationManager characterCreationManager);

	void OnStageCompleted(CharacterCreationStageBase stage);

	void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager);
}
