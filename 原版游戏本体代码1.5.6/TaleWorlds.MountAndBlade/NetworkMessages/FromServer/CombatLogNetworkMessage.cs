using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CombatLogNetworkMessage : GameNetworkMessage
{
	public int AttackerAgentIndex { get; private set; }

	public int VictimAgentIndex { get; private set; }

	public MissionObjectId MissionObjectHitId { get; private set; }

	public DamageTypes DamageType { get; private set; }

	public bool CrushedThrough { get; private set; }

	public bool Chamber { get; private set; }

	public bool IsRangedAttack { get; private set; }

	public bool IsFriendlyFire { get; private set; }

	public bool IsFatalDamage { get; private set; }

	public bool IsSpecialDamage { get; private set; }

	public BoneBodyPartType BodyPartHit { get; private set; }

	public float HitSpeed { get; private set; }

	public float Distance { get; private set; }

	public int InflictedDamage { get; private set; }

	public int AbsorbedDamage { get; private set; }

	public int ModifiedDamage { get; private set; }

	public int ReflectedDamage { get; private set; }

	public CombatLogNetworkMessage()
	{
	}

	public CombatLogNetworkMessage(int attackerAgentIndex, int victimAgentIndex, MissionObjectId missionObjectHitId, CombatLogData combatLogData)
	{
		AttackerAgentIndex = attackerAgentIndex;
		VictimAgentIndex = victimAgentIndex;
		MissionObjectHitId = missionObjectHitId;
		DamageType = combatLogData.DamageType;
		CrushedThrough = combatLogData.CrushedThrough;
		Chamber = combatLogData.Chamber;
		IsRangedAttack = combatLogData.IsRangedAttack;
		IsFriendlyFire = combatLogData.IsFriendlyFire;
		IsFatalDamage = combatLogData.IsFatalDamage;
		IsSpecialDamage = combatLogData.IsSpecialDamage;
		BodyPartHit = combatLogData.BodyPartHit;
		HitSpeed = combatLogData.HitSpeed;
		Distance = combatLogData.Distance;
		InflictedDamage = combatLogData.InflictedDamage;
		AbsorbedDamage = combatLogData.AbsorbedDamage;
		ModifiedDamage = combatLogData.ModifiedDamage;
		ReflectedDamage = combatLogData.ReflectedDamage;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AttackerAgentIndex);
		GameNetworkMessage.WriteAgentIndexToPacket(VictimAgentIndex);
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectHitId);
		GameNetworkMessage.WriteIntToPacket((int)DamageType, CompressionBasic.AgentHitDamageTypeCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(CrushedThrough);
		GameNetworkMessage.WriteBoolToPacket(Chamber);
		GameNetworkMessage.WriteBoolToPacket(IsRangedAttack);
		GameNetworkMessage.WriteBoolToPacket(IsFriendlyFire);
		GameNetworkMessage.WriteBoolToPacket(IsFatalDamage);
		GameNetworkMessage.WriteBoolToPacket(IsSpecialDamage);
		GameNetworkMessage.WriteIntToPacket((int)BodyPartHit, CompressionBasic.AgentHitBodyPartCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(HitSpeed, CompressionBasic.AgentHitRelativeSpeedCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(Distance, CompressionBasic.AgentHitRelativeSpeedCompressionInfo);
		AbsorbedDamage = MBMath.ClampInt(AbsorbedDamage, 0, 2000);
		InflictedDamage = MBMath.ClampInt(InflictedDamage, 0, 2000);
		ModifiedDamage = MBMath.ClampInt(ModifiedDamage, -2000, 2000);
		ReflectedDamage = MBMath.ClampInt(ReflectedDamage, 0, 2000);
		GameNetworkMessage.WriteIntToPacket(AbsorbedDamage, CompressionBasic.AgentHitDamageCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(InflictedDamage, CompressionBasic.AgentHitDamageCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ModifiedDamage, CompressionBasic.AgentHitModifiedDamageCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ReflectedDamage, CompressionBasic.AgentHitDamageCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AttackerAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		VictimAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		MissionObjectHitId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		DamageType = (DamageTypes)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitDamageTypeCompressionInfo, ref bufferReadValid);
		CrushedThrough = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		Chamber = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsRangedAttack = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsFriendlyFire = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsFatalDamage = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsSpecialDamage = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		BodyPartHit = (BoneBodyPartType)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitBodyPartCompressionInfo, ref bufferReadValid);
		HitSpeed = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AgentHitRelativeSpeedCompressionInfo, ref bufferReadValid);
		Distance = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AgentHitRelativeSpeedCompressionInfo, ref bufferReadValid);
		AbsorbedDamage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitDamageCompressionInfo, ref bufferReadValid);
		InflictedDamage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitDamageCompressionInfo, ref bufferReadValid);
		ModifiedDamage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitModifiedDamageCompressionInfo, ref bufferReadValid);
		ReflectedDamage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitDamageCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Agent got hit.";
	}
}
