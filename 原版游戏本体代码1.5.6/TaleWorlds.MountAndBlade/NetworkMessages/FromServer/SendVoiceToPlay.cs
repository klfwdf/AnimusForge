using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SendVoiceToPlay : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public byte[] Buffer { get; private set; }

	public int BufferLength { get; private set; }

	public SendVoiceToPlay()
	{
	}

	public SendVoiceToPlay(NetworkCommunicator peer, byte[] buffer, int bufferLength)
	{
		Peer = peer;
		Buffer = buffer;
		BufferLength = bufferLength;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteByteArrayToPacket(Buffer, 0, BufferLength);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		Buffer = new byte[1440];
		BufferLength = GameNetworkMessage.ReadByteArrayFromPacket(Buffer, 0, 1440, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.None;
	}

	protected override string OnGetLogFormat()
	{
		return string.Empty;
	}
}
