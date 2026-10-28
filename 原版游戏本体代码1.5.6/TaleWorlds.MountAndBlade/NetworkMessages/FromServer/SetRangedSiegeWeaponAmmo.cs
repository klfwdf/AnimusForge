using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetRangedSiegeWeaponAmmo : GameNetworkMessage
{
	public MissionObjectId RangedSiegeWeaponId { get; private set; }

	public int AmmoCount { get; private set; }

	public SetRangedSiegeWeaponAmmo(MissionObjectId rangedSiegeWeaponId, int ammoCount)
	{
		RangedSiegeWeaponId = rangedSiegeWeaponId;
		AmmoCount = ammoCount;
	}

	public SetRangedSiegeWeaponAmmo()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RangedSiegeWeaponId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		AmmoCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponAmmoCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(RangedSiegeWeaponId);
		GameNetworkMessage.WriteIntToPacket(AmmoCount, CompressionMission.RangedSiegeWeaponAmmoCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set ammo left to: " + AmmoCount + " on RangedSiegeWeapon with ID: " + RangedSiegeWeaponId;
	}
}
