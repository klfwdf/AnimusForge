namespace TaleWorlds.CampaignSystem.GameState;

public interface IInventoryStateHandler
{
	void ExecuteLootingScript();

	void ExecuteSellAllLoot();

	void ExecuteBuyConsumableItem();
}
