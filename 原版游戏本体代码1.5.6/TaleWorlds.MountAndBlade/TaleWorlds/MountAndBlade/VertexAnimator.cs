using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class VertexAnimator : SynchedMissionObject
{
	[DefineSynchedMissionObjectType(typeof(VertexAnimator))]
	public struct VertexAnimatorRecord : ISynchedMissionObjectReadableRecord
	{
		public int BeginKey { get; private set; }

		public int EndKey { get; private set; }

		public float Speed { get; private set; }

		public float Progress { get; private set; }

		public bool ReadFromNetwork(ref bool bufferReadValid)
		{
			BeginKey = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AnimationKeyCompressionInfo, ref bufferReadValid);
			EndKey = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AnimationKeyCompressionInfo, ref bufferReadValid);
			Speed = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.VertexAnimationSpeedCompressionInfo, ref bufferReadValid);
			Progress = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationProgressCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}
	}

	public float Speed;

	public int BeginKey;

	public int EndKey;

	private bool _playOnce;

	private float _curAnimTime;

	private bool _isPlaying;

	private readonly List<Mesh> _animatedMeshes = new List<Mesh>();

	private float Progress
	{
		get
		{
			return (_curAnimTime - (float)BeginKey) / (float)(EndKey - BeginKey);
		}
		set
		{
			_curAnimTime = (float)BeginKey + value * (float)(EndKey - BeginKey);
			Mesh firstMesh = base.GameEntity.GetFirstMesh();
			if (firstMesh != null)
			{
				firstMesh.MorphTime = _curAnimTime;
			}
		}
	}

	public VertexAnimator()
	{
		Speed = 20f;
	}

	private void SetIsPlaying(bool value)
	{
		if (_isPlaying != value)
		{
			_isPlaying = value;
			SetScriptComponentToTick(GetTickRequirement());
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		RefreshEditDataUsers();
		SetIsPlaying(value: true);
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected internal override void OnEditorInit()
	{
		OnInit();
	}

	public override TickRequirement GetTickRequirement()
	{
		if (_isPlaying)
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!_isPlaying)
		{
			return;
		}
		if (_curAnimTime < (float)BeginKey)
		{
			_curAnimTime = BeginKey;
		}
		base.GameEntity.SetMorphFrameOfComponents(_curAnimTime);
		_curAnimTime += dt * Speed;
		if (!(_curAnimTime > (float)EndKey))
		{
			return;
		}
		if (_curAnimTime > (float)EndKey && _playOnce)
		{
			SetIsPlaying(value: false);
			_curAnimTime = EndKey;
			base.GameEntity.SetMorphFrameOfComponents(_curAnimTime);
		}
		else
		{
			int num = 0;
			while (_curAnimTime > (float)EndKey && ++num < 100)
			{
				_curAnimTime = (float)BeginKey + (_curAnimTime - (float)EndKey);
			}
		}
	}

	public void PlayOnce()
	{
		Play();
		_playOnce = true;
	}

	public void Pause()
	{
		SetIsPlaying(value: false);
	}

	public void Play()
	{
		Stop();
		Resume();
	}

	public void Resume()
	{
		SetIsPlaying(value: true);
	}

	public void Stop()
	{
		SetIsPlaying(value: false);
		_curAnimTime = BeginKey;
		Mesh firstMesh = base.GameEntity.GetFirstMesh();
		if (firstMesh != null)
		{
			firstMesh.MorphTime = _curAnimTime;
		}
	}

	public void StopAndGoToEnd()
	{
		SetIsPlaying(value: false);
		_curAnimTime = EndKey;
		Mesh firstMesh = base.GameEntity.GetFirstMesh();
		if (firstMesh != null)
		{
			firstMesh.MorphTime = _curAnimTime;
		}
	}

	public void SetAnimation(int beginKey, int endKey, float speed)
	{
		BeginKey = beginKey;
		EndKey = endKey;
		Speed = speed;
	}

	public void SetAnimationSynched(int beginKey, int endKey, float speed)
	{
		if (beginKey != BeginKey || endKey != EndKey || speed != Speed)
		{
			BeginKey = beginKey;
			EndKey = endKey;
			Speed = speed;
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectVertexAnimation(base.Id, beginKey, endKey, speed));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}
	}

	public void SetProgressSynched(float value)
	{
		if (MathF.Abs(Progress - value) > 0.0001f)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetMissionObjectVertexAnimationProgress(base.Id, value));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			Progress = value;
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		int count = _animatedMeshes.Count;
		for (int i = 0; i < count; i++)
		{
			_animatedMeshes[i].ReleaseEditDataUser();
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		int componentCount = base.GameEntity.GetComponentCount(TaleWorlds.Engine.GameEntity.ComponentType.MetaMesh);
		bool flag = false;
		for (int i = 0; i < componentCount; i++)
		{
			MetaMesh metaMesh = base.GameEntity.GetComponentAtIndex(i, TaleWorlds.Engine.GameEntity.ComponentType.MetaMesh) as MetaMesh;
			for (int j = 0; j < metaMesh.MeshCount; j++)
			{
				int count = _animatedMeshes.Count;
				bool flag2 = false;
				for (int k = 0; k < count; k++)
				{
					if (metaMesh.GetMeshAtIndex(j) == _animatedMeshes[k])
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			RefreshEditDataUsers();
		}
		OnTick(dt);
	}

	private void RefreshEditDataUsers()
	{
		foreach (Mesh animatedMesh in _animatedMeshes)
		{
			animatedMesh.ReleaseEditDataUser();
		}
		_animatedMeshes.Clear();
		int componentCount = base.GameEntity.GetComponentCount(TaleWorlds.Engine.GameEntity.ComponentType.MetaMesh);
		for (int i = 0; i < componentCount; i++)
		{
			MetaMesh metaMesh = base.GameEntity.GetComponentAtIndex(i, TaleWorlds.Engine.GameEntity.ComponentType.MetaMesh) as MetaMesh;
			for (int j = 0; j < metaMesh.MeshCount; j++)
			{
				Mesh meshAtIndex = metaMesh.GetMeshAtIndex(j);
				meshAtIndex.AddEditDataUser();
				meshAtIndex.HintVerticesDynamic();
				meshAtIndex.HintIndicesDynamic();
				_animatedMeshes.Add(meshAtIndex);
				Mesh baseMesh = meshAtIndex.GetBaseMesh();
				if (baseMesh != null)
				{
					baseMesh.AddEditDataUser();
					_animatedMeshes.Add(baseMesh);
				}
			}
		}
	}

	public override void WriteToNetwork()
	{
		base.WriteToNetwork();
		GameNetworkMessage.WriteIntToPacket(BeginKey, CompressionBasic.AnimationKeyCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(EndKey, CompressionBasic.AnimationKeyCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(Speed, CompressionBasic.VertexAnimationSpeedCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(Progress, CompressionBasic.AnimationProgressCompressionInfo);
	}

	public override void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
	{
		base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord, allowVisibilityUpdate);
		VertexAnimatorRecord vertexAnimatorRecord = (VertexAnimatorRecord)(object)synchedMissionObjectReadableRecord.Item2;
		BeginKey = vertexAnimatorRecord.BeginKey;
		EndKey = vertexAnimatorRecord.EndKey;
		Speed = vertexAnimatorRecord.Speed;
		Progress = vertexAnimatorRecord.Progress;
	}
}
