using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateAgentVisuals : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public int VisualsIndex { get; private set; }

	public BasicCharacterObject Character { get; private set; }

	public Equipment Equipment { get; private set; }

	public int BodyPropertiesSeed { get; private set; }

	public bool IsFemale { get; private set; }

	public int SelectedEquipmentSetIndex { get; private set; }

	public int TroopCountInFormation { get; private set; }

	public CreateAgentVisuals(NetworkCommunicator peer, AgentBuildData agentBuildData, int selectedEquipmentSetIndex, int troopCountInFormation = 0)
	{
		Peer = peer;
		VisualsIndex = agentBuildData.AgentVisualsIndex;
		Character = agentBuildData.AgentCharacter;
		BodyPropertiesSeed = agentBuildData.AgentEquipmentSeed;
		IsFemale = agentBuildData.AgentIsFemale;
		Equipment = new Equipment();
		Equipment.FillFrom(agentBuildData.AgentOverridenSpawnEquipment);
		SelectedEquipmentSetIndex = selectedEquipmentSetIndex;
		TroopCountInFormation = troopCountInFormation;
	}

	public CreateAgentVisuals()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		VisualsIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentOffsetCompressionInfo, ref bufferReadValid);
		Character = (BasicCharacterObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		Equipment = new Equipment();
		bool flag = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; (int)equipmentIndex < (flag ? 12 : 10); equipmentIndex++)
		{
			EquipmentElement itemRosterElement = ModuleNetworkData.ReadItemReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
			if (!bufferReadValid)
			{
				break;
			}
			Equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, itemRosterElement);
		}
		BodyPropertiesSeed = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.RandomSeedCompressionInfo, ref bufferReadValid);
		IsFemale = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		SelectedEquipmentSetIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		TroopCountInFormation = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket(VisualsIndex, CompressionMission.AgentOffsetCompressionInfo);
		GameNetworkMessage.WriteObjectReferenceToPacket(Character, CompressionBasic.GUIDCompressionInfo);
		bool flag = Equipment[EquipmentIndex.ArmorItemEndSlot].Item != null;
		GameNetworkMessage.WriteBoolToPacket(flag);
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; (int)equipmentIndex < (flag ? 12 : 10); equipmentIndex++)
		{
			ModuleNetworkData.WriteItemReferenceToPacket(Equipment.GetEquipmentFromSlot(equipmentIndex));
		}
		GameNetworkMessage.WriteIntToPacket(BodyPropertiesSeed, CompressionBasic.RandomSeedCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(IsFemale);
		GameNetworkMessage.WriteIntToPacket(SelectedEquipmentSetIndex, CompressionBasic.MissionObjectIDCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(TroopCountInFormation, CompressionBasic.PlayerCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Create AgentVisuals for peer: " + Peer.UserName + ", and with Index: " + VisualsIndex;
	}
}
