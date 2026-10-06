using TaleWorlds.Engine.Options;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class SyncRelevantGameOptionsToServer : GameNetworkMessage
{
	public bool SendMeBloodEvents { get; private set; }

	public bool SendMeSoundEvents { get; private set; }

	public SyncRelevantGameOptionsToServer()
	{
		SendMeBloodEvents = true;
		SendMeSoundEvents = true;
	}

	public void InitializeOptions()
	{
		SendMeBloodEvents = BannerlordConfig.ShowBlood;
		SendMeSoundEvents = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume) > 0.01f && NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) > 0.01f;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SendMeBloodEvents = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		SendMeSoundEvents = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(SendMeBloodEvents);
		GameNetworkMessage.WriteBoolToPacket(SendMeSoundEvents);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.General;
	}

	protected override string OnGetLogFormat()
	{
		return "SyncRelevantGameOptionsToServer";
	}
}
