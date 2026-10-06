using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetRangedSiegeWeaponState : GameNetworkMessage
{
	public MissionObjectId RangedSiegeWeaponId { get; private set; }

	public RangedSiegeWeapon.WeaponState State { get; private set; }

	public SetRangedSiegeWeaponState(MissionObjectId rangedSiegeWeaponId, RangedSiegeWeapon.WeaponState state)
	{
		RangedSiegeWeaponId = rangedSiegeWeaponId;
		State = state;
	}

	public SetRangedSiegeWeaponState()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RangedSiegeWeaponId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		State = (RangedSiegeWeapon.WeaponState)GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponStateCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(RangedSiegeWeaponId);
		GameNetworkMessage.WriteIntToPacket((int)State, CompressionMission.RangedSiegeWeaponStateCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set RangedSiegeWeapon State to: ", State, " on RangedSiegeWeapon with ID: ", RangedSiegeWeaponId);
	}
}
