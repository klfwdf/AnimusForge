using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AttachWeaponToAgent : GameNetworkMessage
{
	public MissionWeapon Weapon { get; private set; }

	public int AgentIndex { get; private set; }

	public sbyte BoneIndex { get; private set; }

	public MatrixFrame AttachLocalFrame { get; private set; }

	public AttachWeaponToAgent(MissionWeapon weapon, int agentIndex, sbyte boneIndex, MatrixFrame attachLocalFrame)
	{
		Weapon = weapon;
		AgentIndex = agentIndex;
		BoneIndex = boneIndex;
		AttachLocalFrame = attachLocalFrame;
	}

	public AttachWeaponToAgent()
	{
	}

	protected override void OnWrite()
	{
		ModuleNetworkData.WriteWeaponReferenceToPacket(Weapon);
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket(BoneIndex, CompressionMission.BoneIndexCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(AttachLocalFrame.origin, CompressionBasic.LocalPositionCompressionInfo);
		GameNetworkMessage.WriteRotationMatrixToPacket(AttachLocalFrame.rotation);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Weapon = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		BoneIndex = (sbyte)GameNetworkMessage.ReadIntFromPacket(CompressionMission.BoneIndexCompressionInfo, ref bufferReadValid);
		Vec3 o = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.LocalPositionCompressionInfo, ref bufferReadValid);
		Mat3 rot = GameNetworkMessage.ReadRotationMatrixFromPacket(ref bufferReadValid);
		if (bufferReadValid)
		{
			AttachLocalFrame = new MatrixFrame(in rot, in o);
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items | MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("AttachWeaponToAgent with name: ", (!Weapon.IsEmpty) ? Weapon.Item.Name : TextObject.GetEmpty(), " to bone index: ", BoneIndex, " on agent agent-index: ", AgentIndex);
	}
}
