namespace TaleWorlds.CampaignSystem.GameState;

public interface IPartyScreenPrisonHandler
{
	void ExecuteTakeAllPrisonersScript();

	void ExecuteDoneScript();

	void ExecuteResetScript();

	void ExecuteSellAllPrisoners();
}
