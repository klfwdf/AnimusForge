using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestCultureChange : GameNetworkMessage
{
	public BasicCultureObject Culture { get; private set; }

	public RequestCultureChange()
	{
	}

	public RequestCultureChange(BasicCultureObject culture)
	{
		Culture = culture;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteObjectReferenceToPacket(Culture, CompressionBasic.GUIDCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Culture = (BasicCultureObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Requested culture: " + Culture.Name;
	}
}
