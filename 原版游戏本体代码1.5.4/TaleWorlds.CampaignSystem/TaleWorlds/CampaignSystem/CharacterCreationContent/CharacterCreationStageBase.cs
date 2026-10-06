namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public abstract class CharacterCreationStageBase
{
	public ICharacterCreationStageListener Listener { get; set; }

	protected internal virtual void OnFinalize()
	{
		Listener?.OnStageFinalize();
	}
}
