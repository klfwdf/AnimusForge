using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions;

public static class DestroyShipAction
{
	public enum ShipDestroyDetail
	{
		ApplyDefault,
		ApplyByDiscard
	}

	private static void ApplyInternal(Ship ship, ShipDestroyDetail detail)
	{
		PartyBase owner = ship.Owner;
		owner?.MobileParty?.SetNavalVisualAsDirty();
		ship.Owner = null;
		CampaignEventDispatcher.Instance.OnShipDestroyed(owner, ship, detail);
	}

	public static void Apply(Ship ship)
	{
		ApplyInternal(ship, ShipDestroyDetail.ApplyDefault);
	}

	public static void ApplyByDiscard(Ship ship)
	{
		ApplyInternal(ship, ShipDestroyDetail.ApplyByDiscard);
	}
}
