using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class DropWeapon : GameNetworkMessage
{
	public bool IsDefendPressed { get; private set; }

	public EquipmentIndex ForcedSlotIndexToDropWeaponFrom { get; private set; }

	public DropWeapon(bool isDefendPressed, EquipmentIndex forcedSlotIndexToDropWeaponFrom)
	{
		IsDefendPressed = isDefendPressed;
		ForcedSlotIndexToDropWeaponFrom = forcedSlotIndexToDropWeaponFrom;
	}

	public DropWeapon()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		IsDefendPressed = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		ForcedSlotIndexToDropWeaponFrom = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WieldSlotCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(IsDefendPressed);
		GameNetworkMessage.WriteIntToPacket((int)ForcedSlotIndexToDropWeaponFrom, CompressionMission.WieldSlotCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items;
	}

	protected override string OnGetLogFormat()
	{
		bool flag = ForcedSlotIndexToDropWeaponFrom != EquipmentIndex.None;
		return "Dropping " + ((!flag) ? "equipped" : "") + " weapon" + (flag ? (" " + (int)ForcedSlotIndexToDropWeaponFrom) : "");
	}
}
