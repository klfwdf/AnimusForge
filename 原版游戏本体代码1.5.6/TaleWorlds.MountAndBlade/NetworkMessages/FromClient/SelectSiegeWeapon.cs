using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class SelectSiegeWeapon : GameNetworkMessage
{
	public MissionObjectId SiegeWeaponId { get; private set; }

	public SelectSiegeWeapon(MissionObjectId siegeWeaponId)
	{
		SiegeWeaponId = siegeWeaponId;
	}

	public SelectSiegeWeapon()
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
		return "Select SiegeWeapon with ID: " + SiegeWeaponId;
	}
}
