using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class AdminRequestClassRestrictionChange : GameNetworkMessage
{
	public FormationClass ClassToChangeRestriction { get; private set; }

	public bool NewValue { get; private set; }

	public AdminRequestClassRestrictionChange()
	{
	}

	public AdminRequestClassRestrictionChange(FormationClass classToChangeRestriction, bool newValue)
	{
		ClassToChangeRestriction = classToChangeRestriction;
		NewValue = newValue;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ClassToChangeRestriction = (FormationClass)GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		NewValue = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)ClassToChangeRestriction, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(NewValue);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"AdminRequestClassRestrictionChange for {ClassToChangeRestriction} to be {NewValue}";
	}
}
