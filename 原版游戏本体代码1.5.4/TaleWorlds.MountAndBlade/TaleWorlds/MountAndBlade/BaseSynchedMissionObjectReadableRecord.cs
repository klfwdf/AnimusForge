using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

[DefineSynchedMissionObjectType(typeof(SynchedMissionObject))]
public struct BaseSynchedMissionObjectReadableRecord
{
	public bool SetVisibilityExcludeParents { get; private set; }

	public bool SynchTransform { get; private set; }

	public MatrixFrame GameObjectFrame { get; private set; }

	public bool SynchronizeFrameOverTime { get; private set; }

	public MatrixFrame LastSynchedFrame { get; private set; }

	public float Duration { get; private set; }

	public bool HasSkeleton { get; private set; }

	public bool SynchAnimation { get; private set; }

	public int AnimationIndex { get; private set; }

	public float AnimationSpeed { get; private set; }

	public float AnimationParameter { get; private set; }

	public bool IsSkeletonAnimationPaused { get; private set; }

	public bool SynchColors { get; private set; }

	public uint Color { get; private set; }

	public uint Color2 { get; private set; }

	public bool IsDisabled { get; private set; }

	public bool ReadFromNetwork(ref bool bufferReadValid)
	{
		SetVisibilityExcludeParents = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		SynchTransform = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (SynchTransform)
		{
			GameObjectFrame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref bufferReadValid);
			SynchronizeFrameOverTime = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			if (SynchronizeFrameOverTime)
			{
				LastSynchedFrame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref bufferReadValid);
				Duration = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.FlagCapturePointDurationCompressionInfo, ref bufferReadValid);
			}
		}
		HasSkeleton = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (HasSkeleton)
		{
			SynchAnimation = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			if (SynchAnimation)
			{
				AnimationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AnimationIndexCompressionInfo, ref bufferReadValid);
				AnimationSpeed = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationSpeedCompressionInfo, ref bufferReadValid);
				AnimationParameter = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationProgressCompressionInfo, ref bufferReadValid);
				IsSkeletonAnimationPaused = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			}
		}
		SynchColors = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (SynchColors)
		{
			Color = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
			Color2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		}
		IsDisabled = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	public void SetSetVisibilityExcludeParents(bool visible)
	{
		SetVisibilityExcludeParents = visible;
	}

	public static (BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) CreateFromNetworkWithTypeIndex(int typeIndex)
	{
		bool bufferReadValid = true;
		BaseSynchedMissionObjectReadableRecord item = default(BaseSynchedMissionObjectReadableRecord);
		item.ReadFromNetwork(ref bufferReadValid);
		ISynchedMissionObjectReadableRecord synchedMissionObjectReadableRecord = null;
		if (typeIndex >= 0)
		{
			synchedMissionObjectReadableRecord = Activator.CreateInstance(GameNetwork.GetSynchedMissionObjectReadableRecordTypeFromIndex(typeIndex)) as ISynchedMissionObjectReadableRecord;
			synchedMissionObjectReadableRecord.ReadFromNetwork(ref bufferReadValid);
		}
		return (item, synchedMissionObjectReadableRecord);
	}
}
