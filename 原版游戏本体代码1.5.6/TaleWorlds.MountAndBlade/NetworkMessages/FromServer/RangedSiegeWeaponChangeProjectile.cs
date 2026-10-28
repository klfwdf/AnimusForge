using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RangedSiegeWeaponChangeProjectile : GameNetworkMessage
{
	public MissionObjectId RangedSiegeWeaponId { get; private set; }

	public int Index { get; private set; }

	public RangedSiegeWeaponChangeProjectile(MissionObjectId rangedSiegeWeaponId, int index)
	{
		RangedSiegeWeaponId = rangedSiegeWeaponId;
		Index = index;
	}

	public RangedSiegeWeaponChangeProjectile()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RangedSiegeWeaponId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Index = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponAmmoIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(RangedSiegeWeaponId);
		GameNetworkMessage.WriteIntToPacket(Index, CompressionMission.RangedSiegeWeaponAmmoIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Changed Projectile Type Index to: " + Index + " on RangedSiegeWeapon with ID: " + RangedSiegeWeaponId;
	}
}
