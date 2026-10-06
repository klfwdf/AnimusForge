using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class UnselectSiegeWeapon : GameNetworkMessage
{
	public MissionObjectId SiegeWeaponId { get; private set; }

	public UnselectSiegeWeapon(MissionObjectId siegeWeaponId)
	{
		SiegeWeaponId = siegeWeaponId;
	}

	public UnselectSiegeWeapon()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SiegeWeaponId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(SiegeWeaponId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Deselect SiegeWeapon with ID: " + SiegeWeaponId;
	}
}
