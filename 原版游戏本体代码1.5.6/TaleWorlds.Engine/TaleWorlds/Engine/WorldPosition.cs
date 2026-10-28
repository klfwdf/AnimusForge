using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglWorld_position::Plain_world_position", false, null)]
public struct WorldPosition
{
	public enum WorldPositionEnforcedCache
	{
		None,
		NavMeshVec3,
		GroundVec3
	}

	private readonly UIntPtr _scene;

	private UIntPtr _navMesh;

	private UIntPtr _nearestNavMesh;

	private Vec3 _position;

	[CustomEngineStructMemberData("normal_")]
	public Vec3 Normal;

	private Vec2 _lastValidZPosition;

	[CustomEngineStructMemberData("z_validity_state_")]
	public ZValidityState State;

	public static readonly WorldPosition Invalid = new WorldPosition(UIntPtr.Zero, UIntPtr.Zero, Vec3.Invalid, hasValidZ: false);

	public Vec2 AsVec2 => _position.AsVec2;

	public float X => _position.x;

	public float Y => _position.y;

	public bool IsValid
	{
		get
		{
			if (AsVec2.IsValid)
			{
				return _scene != UIntPtr.Zero;
			}
			return false;
		}
	}

	internal WorldPosition(UIntPtr scenePointer, Vec3 position)
		: this(scenePointer, UIntPtr.Zero, position, hasValidZ: false)
	{
	}

	internal WorldPosition(UIntPtr scenePointer, UIntPtr navMesh, Vec3 position, bool hasValidZ)
	{
		_scene = scenePointer;
		_navMesh = navMesh;
		_nearestNavMesh = _navMesh;
		_position = position;
		Normal = Vec3.Zero;
		if (hasValidZ)
		{
			_lastValidZPosition = _position.AsVec2;
			State = ZValidityState.Valid;
		}
		else
		{
			_lastValidZPosition = Vec2.Invalid;
			State = ZValidityState.Invalid;
		}
	}

	public WorldPosition(Scene scene, Vec3 position)
		: this((scene != null) ? scene.Pointer : UIntPtr.Zero, UIntPtr.Zero, position, hasValidZ: false)
	{
	}

	public WorldPosition(Scene scene, UIntPtr navMesh, Vec3 position, bool hasValidZ)
		: this((scene != null) ? scene.Pointer : UIntPtr.Zero, navMesh, position, hasValidZ)
	{
	}

	public void SetVec3(UIntPtr navMesh, Vec3 position, bool hasValidZ)
	{
		_navMesh = navMesh;
		_nearestNavMesh = _navMesh;
		_position = position;
		Normal = Vec3.Zero;
		if (hasValidZ)
		{
			_lastValidZPosition = _position.AsVec2;
			State = ZValidityState.Valid;
		}
		else
		{
			_lastValidZPosition = Vec2.Invalid;
			State = ZValidityState.Invalid;
		}
	}

	private void ValidateZ(ZValidityState minimumValidityState)
	{
		if (State < minimumValidityState)
		{
			EngineApplicationInterface.IScene.WorldPositionValidateZ(ref this, (int)minimumValidityState);
		}
	}

	private void ValidateZMT(ZValidityState minimumValidityState)
	{
		if (State < minimumValidityState)
		{
			using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
			{
				EngineApplicationInterface.IScene.WorldPositionValidateZ(ref this, (int)minimumValidityState);
			}
		}
	}

	public UIntPtr GetNavMesh()
	{
		ValidateZ(ZValidityState.ValidAccordingToNavMesh);
		return _navMesh;
	}

	public UIntPtr GetNavMeshMT()
	{
		ValidateZMT(ZValidityState.ValidAccordingToNavMesh);
		return _navMesh;
	}

	public UIntPtr GetNearestNavMesh()
	{
		EngineApplicationInterface.IScene.WorldPositionComputeNearestNavMesh(ref this);
		return _nearestNavMesh;
	}

	public float GetNavMeshZ()
	{
		ValidateZ(ZValidityState.ValidAccordingToNavMesh);
		if (State >= ZValidityState.ValidAccordingToNavMesh)
		{
			return _position.z;
		}
		return float.NaN;
	}

	public float GetNavMeshZMT()
	{
		ValidateZMT(ZValidityState.ValidAccordingToNavMesh);
		if (State >= ZValidityState.ValidAccordingToNavMesh)
		{
			return _position.z;
		}
		return float.NaN;
	}

	public float GetGroundZ()
	{
		ValidateZ(ZValidityState.Valid);
		if (State >= ZValidityState.Valid)
		{
			return _position.z;
		}
		return float.NaN;
	}

	public float GetGroundZMT()
	{
		ValidateZMT(ZValidityState.Valid);
		if (State >= ZValidityState.Valid)
		{
			return _position.z;
		}
		return float.NaN;
	}

	public Vec3 GetNavMeshVec3()
	{
		return new Vec3(_position.AsVec2, GetNavMeshZ());
	}

	public Vec3 GetNavMeshVec3MT()
	{
		return new Vec3(_position.AsVec2, GetNavMeshZMT());
	}

	public Vec3 GetGroundVec3()
	{
		return new Vec3(_position.AsVec2, GetGroundZ());
	}

	public Vec3 GetGroundVec3MT()
	{
		return new Vec3(_position.AsVec2, GetGroundZMT());
	}

	public Vec3 GetVec3WithoutValidity()
	{
		return _position;
	}

	public void SetVec2MT(Vec2 value)
	{
		if (_position.AsVec2 != value)
		{
			if (State != ZValidityState.Invalid)
			{
				State = ZValidityState.Invalid;
			}
			else if (!_lastValidZPosition.IsValid)
			{
				ValidateZMT(ZValidityState.ValidAccordingToNavMesh);
				State = ZValidityState.Invalid;
			}
			_position.x = value.x;
			_position.y = value.y;
		}
	}

	public void SetVec2(Vec2 value)
	{
		if (_position.AsVec2 != value)
		{
			if (State != ZValidityState.Invalid)
			{
				State = ZValidityState.Invalid;
			}
			else if (!_lastValidZPosition.IsValid)
			{
				ValidateZ(ZValidityState.ValidAccordingToNavMesh);
				State = ZValidityState.Invalid;
			}
			_position.x = value.x;
			_position.y = value.y;
		}
	}

	public float DistanceSquaredWithLimit(in Vec3 targetPoint, float limitSquared)
	{
		float num = _position.AsVec2.DistanceSquared(targetPoint.AsVec2);
		if (num <= limitSquared)
		{
			return GetGroundVec3().DistanceSquared(targetPoint);
		}
		return num;
	}
}
