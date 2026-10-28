using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetStonePileAmmo : GameNetworkMessage
{
	public MissionObjectId StonePileId { get; private set; }

	public int AmmoCount { get; private set; }

	public SetStonePileAmmo(MissionObjectId stonePileId, int ammoCount)
	{
		StonePileId = stonePileId;
		AmmoCount = ammoCount;
	}

	public SetStonePileAmmo()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		StonePileId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		AmmoCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponAmmoCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(StonePileId);
		GameNetworkMessage.WriteIntToPacket(AmmoCount, CompressionMission.RangedSiegeWeaponAmmoCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set ammo left to: " + AmmoCount + " on StonePile with ID: " + StonePileId;
	}
}
