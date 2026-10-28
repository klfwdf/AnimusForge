namespace TaleWorlds.CampaignSystem.Siege;

public interface ISiegeEventVisual
{
	void Initialize();

	void OnSiegeEventEnd();

	void Tick();
}
