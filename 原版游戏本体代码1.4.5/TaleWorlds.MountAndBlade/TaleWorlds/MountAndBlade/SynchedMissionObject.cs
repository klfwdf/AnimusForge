using System;
using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class SynchedMissionObject : MissionObject
{
	private enum SynchState
	{
		SynchronizeCompleted,
		SynchronizePosition,
		SynchronizeFrame,
		SynchronizeFrameOverTime
	}

	[Flags]
	public enum SynchFlags : uint
	{
		SynchNone = 0u,
		SynchTransform = 1u,
		SynchAnimation = 2u,
		SynchBodyFlags = 4u,
		SyncColors = 8u,
		SynchAll = uint.MaxValue
	}

	private SynchFlags _initialSynchFlags;

	private SynchState _synchState;

	private MatrixFrame _lastSynchedFrame;

	private MatrixFrame _firstFrame;

	private float _timer;

	private float _duration;

	public uint Color { get; private set; }

	public uint Color2 { get; private set; }

	public bool SynchronizeCompleted => _synchState == SynchState.SynchronizeCompleted;

	protected internal override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		if (!SynchronizeCompleted)
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		if (SynchronizeCompleted)
		{
			return;
		}
		MatrixFrame frame = base.GameEntity.GetFrame();
		if ((_synchState == SynchState.SynchronizePosition && _lastSynchedFrame.origin.NearlyEquals(in frame.origin)) || _lastSynchedFrame.NearlyEquals(frame))
		{
			SetSynchState(SynchState.SynchronizeCompleted);
			return;
		}
		MatrixFrame frame2 = default(MatrixFrame);
		frame2.origin = ((_synchState == SynchState.SynchronizeFrameOverTime) ? MBMath.Lerp(_firstFrame.origin, _lastSynchedFrame.origin, _timer / _duration, 0.2f * dt) : MBMath.Lerp(frame.origin, _lastSynchedFrame.origin, 8f * dt, 0.2f * dt));
		if (_synchState == SynchState.SynchronizeFrame || _synchState == SynchState.SynchronizeFrameOverTime)
		{
			frame2.rotation.s = MBMath.Lerp(frame.rotation.s, _lastSynchedFrame.rotation.s, 8f * dt, 0.2f * dt);
			frame2.rotation.f = MBMath.Lerp(frame.rotation.f, _lastSynchedFrame.rotation.f, 8f * dt, 0.2f * dt);
			frame2.rotation.u = MBMath.Lerp(frame.rotation.u, _lastSynchedFrame.rotation.u, 8f * dt, 0.2f * dt);
			if (frame2.origin != _lastSynchedFrame.origin || frame2.rotation.s != _lastSynchedFrame.rotation.s || frame2.rotation.f != _lastSynchedFrame.rotation.f || frame2.rotation.u != _lastSynchedFrame.rotation.u)
			{
				frame2.rotation.Orthonormalize();
				if (_lastSynchedFrame.rotation.HasScale())
				{
					frame2.rotation.ApplyScaleLocal(_lastSynchedFrame.rotation.GetScaleVector());
				}
			}
			base.GameEntity.SetFrame(ref frame2);
		}
		else
		{
			base.GameEntity.SetLocalPosition(frame2.origin);
		}
		_timer = TaleWorlds.Library.MathF.Min(_timer + dt, _duration);
	}

	private void SetSynchState(SynchState newState)
	{
		if (newState != _synchState)
		{
			_synchState = newState;
			SetScriptComponentToTick(GetTickRequirement());
		}
	}

	public void SetLocalPositionSmoothStep(ref Vec3 targetPosition)
	{
		_lastSynchedFrame.origin = targetPosition;
		SetSynchState(SynchState.SynchronizePosition);
	}

	public virtual void SetVisibleSynched(bool value, bool forceChildrenVisible = false)
	{
		bool flag = base.GameEntity.IsVisibleIncludeParents() != value;
		List<WeakGameEntity> children = null;
		if (!flag && forceChildrenVisible)
		{
			children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
			foreach (WeakGameEntity item in children)
			{
				if (item.GetPhysicsState() != value)
				{
					flag = true;
					break;
				}
			}
		}
		if (!(base.GameEntity.IsValid && flag))
		{
			return;
		}
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetMissionObjectVisibility(base.Id, value));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		base.GameEntity.SetVisibilityExcludeParents(value);
		if (!forceChildrenVisible)
		{
			return;
		}
		if (children == null)
		{
			children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
		}
		foreach (WeakGameEntity item2 in children)
		{
			item2.SetVisibilityExcludeParents(value);
		}
	}

	public virtual void SetPhysicsStateSynched(bool value, bool setChildren = true)
	{
	}

	public virtual void SetDisabledSynched()
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetMissionObjectDisabled(base.Id));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		SetDisabledAndMakeInvisible();
	}

	public void SetFrameSynched(ref MatrixFrame frame, bool isClient = false)
	{
		if (!(base.GameEntity.GetFrame() != frame) && _synchState == SynchState.SynchronizeCompleted)
		{
			return;
		}
		_duration = 0f;
		_timer = 0f;
		if (GameNetwork.IsClientOrReplay)
		{
			_lastSynchedFrame = frame;
			SetSynchState(SynchState.SynchronizeFrame);
			return;
		}
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetMissionObjectFrame(base.Id, ref frame));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		SetSynchState(SynchState.SynchronizeCompleted);
		base.GameEntity.SetFrame(ref frame);
		_initialSynchFlags |= SynchFlags.SynchTransform;
	}

	public void SetGlobalFrameSynched(ref MatrixFrame frame, bool isClient = false)
	{
		_duration = 0f;
		_timer = 0f;
		if (!(base.GameEntity.GetGlobalFrame() != frame))
		{
			return;
		}
		if (GameNetwork.IsClientOrReplay)
		{
			_lastSynchedFrame = (base.GameEntity.Parent.IsValid ? base.GameEntity.Parent.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame) : frame);
			SetSynchState(SynchState.SynchronizeFrame);
			return;
		}
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetMissionObjectGlobalFrame(base.Id, ref frame));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		SetSynchState(SynchState.SynchronizeCompleted);
		base.GameEntity.SetGlobalFrame(in frame);
		_initialSynchFlags |= SynchFlags.SynchTransform;
	}

	public void SetFrameSynchedOverTime(ref MatrixFrame frame, float duration, bool isClient = false)
	{
		if (base.GameEntity.GetFrame() != frame || duration.ApproximatelyEqualsTo(0f))
		{
			_firstFrame = base.GameEntity.GetFrame();
			_lastSynchedFrame = frame;
			SetSynchState(SynchState.SynchronizeFrameOverTime);
			_duration = (duration.ApproximatelyEqualsTo(0f) ? 0.1f : duration);
			_timer = 0f;
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectFrameOverTime(base.Id, ref frame, duration));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			_initialSynchFlags |= SynchFlags.SynchTransform;
		}
	}

	public void SetGlobalFrameSynchedOverTime(ref MatrixFrame frame, float duration, bool isClient = false)
	{
		if (base.GameEntity.GetGlobalFrame() != frame || duration.ApproximatelyEqualsTo(0f))
		{
			_firstFrame = base.GameEntity.GetFrame();
			_lastSynchedFrame = (base.GameEntity.Parent.IsValid ? base.GameEntity.Parent.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame) : frame);
			SetSynchState(SynchState.SynchronizeFrameOverTime);
			_duration = (duration.ApproximatelyEqualsTo(0f) ? 0.1f : duration);
			_timer = 0f;
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectGlobalFrameOverTime(base.Id, ref frame, duration));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			_initialSynchFlags |= SynchFlags.SynchTransform;
		}
	}

	public void SetAnimationAtChannelSynched(string animationName, int channelNo, float animationSpeed = 1f)
	{
		SetAnimationAtChannelSynched(MBAnimation.GetAnimationIndexWithName(animationName), channelNo, animationSpeed);
	}

	public void SetAnimationAtChannelSynched(int animationIndex, int channelNo, float animationSpeed = 1f)
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			int animationIndexAtChannel = base.GameEntity.Skeleton.GetAnimationIndexAtChannel(channelNo);
			bool flag = true;
			if (animationIndexAtChannel == animationIndex && base.GameEntity.Skeleton.GetAnimationSpeedAtChannel(channelNo).ApproximatelyEqualsTo(animationSpeed) && base.GameEntity.Skeleton.GetAnimationParameterAtChannel(channelNo) < 0.02f)
			{
				flag = false;
			}
			if (flag)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectAnimationAtChannel(base.Id, channelNo, animationIndex, animationSpeed));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
				_initialSynchFlags |= SynchFlags.SynchAnimation;
			}
		}
		base.GameEntity.Skeleton.SetAnimationAtChannel(animationIndex, channelNo, animationSpeed);
	}

	public void SetAnimationChannelParameterSynched(int channelNo, float parameter)
	{
		if (!base.GameEntity.Skeleton.GetAnimationParameterAtChannel(channelNo).ApproximatelyEqualsTo(parameter))
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectAnimationChannelParameter(base.Id, channelNo, parameter));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			base.GameEntity.Skeleton.SetAnimationParameterAtChannel(channelNo, parameter);
			_initialSynchFlags |= SynchFlags.SynchAnimation;
		}
	}

	public void PauseSkeletonAnimationSynched()
	{
		if (!base.GameEntity.IsSkeletonAnimationPaused())
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectAnimationPaused(base.Id, isPaused: true));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			base.GameEntity.PauseSkeletonAnimation();
			_initialSynchFlags |= SynchFlags.SynchAnimation;
		}
	}

	public void ResumeSkeletonAnimationSynched()
	{
		if (base.GameEntity.IsSkeletonAnimationPaused())
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectAnimationPaused(base.Id, isPaused: false));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			base.GameEntity.ResumeSkeletonAnimation();
			_initialSynchFlags |= SynchFlags.SynchAnimation;
		}
	}

	public void BurstParticlesSynched(bool doChildren = true)
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new BurstMissionObjectParticles(base.Id, doChildren: false));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		base.GameEntity.BurstEntityParticle(doChildren);
	}

	public void ApplyImpulseSynched(Vec3 localPosition, Vec3 impulse)
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetMissionObjectImpulse(base.Id, localPosition, impulse));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		base.GameEntity.ApplyLocalImpulseToDynamicBody(localPosition, impulse);
		_initialSynchFlags |= SynchFlags.SynchTransform;
	}

	public void AddBodyFlagsSynched(BodyFlags flags, bool applyToChildren = true)
	{
		if ((base.GameEntity.BodyFlag & flags) != flags)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new AddMissionObjectBodyFlags(base.Id, flags, applyToChildren));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			base.GameEntity.AddBodyFlags(flags, applyToChildren);
			_initialSynchFlags |= SynchFlags.SynchBodyFlags;
		}
	}

	public void RemoveBodyFlagsSynched(BodyFlags flags, bool applyToChildren = true)
	{
		if ((base.GameEntity.BodyFlag & flags) != BodyFlags.None)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new RemoveMissionObjectBodyFlags(base.Id, flags, applyToChildren));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			base.GameEntity.RemoveBodyFlags(flags, applyToChildren);
			_initialSynchFlags |= SynchFlags.SynchBodyFlags;
		}
	}

	public void SetTeamColors(uint color, uint color2)
	{
		Color = color;
		Color2 = color2;
		base.GameEntity.SetColor(color, color2, "use_team_color");
	}

	public virtual void SetTeamColorsSynched(uint color, uint color2)
	{
		if (base.GameEntity.IsValid)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectColors(base.Id, color, color2));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			SetTeamColors(color, color2);
			_initialSynchFlags |= SynchFlags.SyncColors;
		}
	}

	public virtual void WriteToNetwork()
	{
		GameNetworkMessage.WriteBoolToPacket(base.GameEntity.GetVisibilityExcludeParents());
		GameNetworkMessage.WriteBoolToPacket(_initialSynchFlags.HasAnyFlag(SynchFlags.SynchTransform));
		if (_initialSynchFlags.HasAnyFlag(SynchFlags.SynchTransform))
		{
			GameNetworkMessage.WriteMatrixFrameToPacket(base.GameEntity.GetFrame());
			GameNetworkMessage.WriteBoolToPacket(_synchState == SynchState.SynchronizeFrameOverTime);
			if (_synchState == SynchState.SynchronizeFrameOverTime)
			{
				GameNetworkMessage.WriteMatrixFrameToPacket(_lastSynchedFrame);
				GameNetworkMessage.WriteFloatToPacket(_duration - _timer, CompressionMission.FlagCapturePointDurationCompressionInfo);
			}
		}
		Skeleton skeleton = base.GameEntity.Skeleton;
		GameNetworkMessage.WriteBoolToPacket(skeleton != null);
		if (skeleton != null)
		{
			int animationIndexAtChannel = skeleton.GetAnimationIndexAtChannel(0);
			bool num = animationIndexAtChannel >= 0;
			GameNetworkMessage.WriteBoolToPacket(num && _initialSynchFlags.HasAnyFlag(SynchFlags.SynchAnimation));
			if (num && _initialSynchFlags.HasAnyFlag(SynchFlags.SynchAnimation))
			{
				float animationSpeedAtChannel = skeleton.GetAnimationSpeedAtChannel(0);
				float animationParameterAtChannel = skeleton.GetAnimationParameterAtChannel(0);
				GameNetworkMessage.WriteIntToPacket(animationIndexAtChannel, CompressionBasic.AnimationIndexCompressionInfo);
				GameNetworkMessage.WriteFloatToPacket(animationSpeedAtChannel, CompressionBasic.AnimationSpeedCompressionInfo);
				GameNetworkMessage.WriteFloatToPacket(animationParameterAtChannel, CompressionBasic.AnimationProgressCompressionInfo);
				GameNetworkMessage.WriteBoolToPacket(base.GameEntity.IsSkeletonAnimationPaused());
			}
		}
		GameNetworkMessage.WriteBoolToPacket(_initialSynchFlags.HasAnyFlag(SynchFlags.SyncColors));
		if (_initialSynchFlags.HasAnyFlag(SynchFlags.SyncColors))
		{
			GameNetworkMessage.WriteUintToPacket(Color, CompressionBasic.ColorCompressionInfo);
			GameNetworkMessage.WriteUintToPacket(Color2, CompressionBasic.ColorCompressionInfo);
		}
		GameNetworkMessage.WriteBoolToPacket(base.IsDisabled);
	}

	public virtual void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
	{
		var (baseSynchedMissionObjectReadableRecord, _) = synchedMissionObjectReadableRecord;
		if (allowVisibilityUpdate)
		{
			base.GameEntity.SetVisibilityExcludeParents(baseSynchedMissionObjectReadableRecord.SetVisibilityExcludeParents);
		}
		if (baseSynchedMissionObjectReadableRecord.SynchTransform)
		{
			MatrixFrame frame = baseSynchedMissionObjectReadableRecord.GameObjectFrame;
			base.GameEntity.SetFrame(ref frame);
			if (baseSynchedMissionObjectReadableRecord.SynchronizeFrameOverTime)
			{
				_firstFrame = baseSynchedMissionObjectReadableRecord.GameObjectFrame;
				_lastSynchedFrame = baseSynchedMissionObjectReadableRecord.LastSynchedFrame;
				SetSynchState(SynchState.SynchronizeFrameOverTime);
				_duration = baseSynchedMissionObjectReadableRecord.Duration;
				_timer = 0f;
				if (_duration.ApproximatelyEqualsTo(0f))
				{
					_duration = 0.1f;
				}
			}
		}
		if (baseSynchedMissionObjectReadableRecord.HasSkeleton && baseSynchedMissionObjectReadableRecord.SynchAnimation)
		{
			base.GameEntity.Skeleton.SetAnimationAtChannel(baseSynchedMissionObjectReadableRecord.AnimationIndex, 0, baseSynchedMissionObjectReadableRecord.AnimationSpeed, 0f);
			base.GameEntity.Skeleton.SetAnimationParameterAtChannel(0, baseSynchedMissionObjectReadableRecord.AnimationParameter);
			if (baseSynchedMissionObjectReadableRecord.IsSkeletonAnimationPaused)
			{
				base.GameEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, base.GameEntity.GetGlobalFrame(), tickAnimsForChildren: true);
				base.GameEntity.PauseSkeletonAnimation();
			}
			else
			{
				base.GameEntity.ResumeSkeletonAnimation();
			}
		}
		if (baseSynchedMissionObjectReadableRecord.SynchColors)
		{
			SetTeamColors(baseSynchedMissionObjectReadableRecord.Color, baseSynchedMissionObjectReadableRecord.Color2);
		}
		if (baseSynchedMissionObjectReadableRecord.IsDisabled)
		{
			SetDisabledAndMakeInvisible();
		}
	}
}
