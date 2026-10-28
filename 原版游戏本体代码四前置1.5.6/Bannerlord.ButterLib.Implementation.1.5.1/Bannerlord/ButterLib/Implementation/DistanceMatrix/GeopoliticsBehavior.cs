using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.ButterLib.Implementation.DistanceMatrix;

internal sealed class GeopoliticsBehavior : CampaignBehaviorBase
{
	public DistanceMatrixImplementation<Settlement>? SettlementDistanceMatrix { get; private set; }

	public DistanceMatrixImplementation<Clan>? ClanDistanceMatrix { get; private set; }

	public DistanceMatrixImplementation<Kingdom>? KingdomDistanceMatrix { get; private set; }

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, InitializeOnLoad);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, InitializeOnLoad);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, UpdateOnSettlementOwnerChanged);
	}

	private void InitializeOnLoad(CampaignGameStarter gameStarterObject)
	{
		SettlementDistanceMatrix = new DistanceMatrixImplementation<Settlement>();
		ClanDistanceMatrix = new DistanceMatrixImplementation<Clan>();
		KingdomDistanceMatrix = new DistanceMatrixImplementation<Kingdom>();
	}

	private void UpdateOnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		Clan clan = oldOwner.Clan;
		Clan clan2 = newOwner.Clan;
		if ((clan2 != null || clan != null) && clan2 != clan)
		{
			ClanDistanceMatrix = new DistanceMatrixImplementation<Clan>();
			if (clan2?.Kingdom != clan?.Kingdom)
			{
				KingdomDistanceMatrix = new DistanceMatrixImplementation<Kingdom>();
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
