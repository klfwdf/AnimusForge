using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public class GameEntityWithWorldPosition
{
	private MatrixFrame _customLocalFrame;

	private readonly WeakGameEntity _gameEntity;

	private WorldPosition _worldPosition;

	private Mat3 _orthonormalRotation;

	public WeakGameEntity GameEntity => _gameEntity;

	public WorldPosition WorldPosition
	{
		get
		{
			ValidateWorldPosition();
			return _worldPosition;
		}
	}

	public WorldFrame WorldFrame
	{
		get
		{
			Mat3 orthonormalRotation = (_customLocalFrame.rotation.IsIdentity() ? GameEntity.GetGlobalFrame().rotation : GameEntity.GetGlobalFrame().rotation.TransformToParent(in _customLocalFrame.rotation));
			if (!orthonormalRotation.NearlyEquals(in _orthonormalRotation))
			{
				_orthonormalRotation = orthonormalRotation;
				_orthonormalRotation.Orthonormalize();
			}
			return new WorldFrame(_orthonormalRotation, WorldPosition);
		}
	}

	public Vec2 AsVec2
	{
		get
		{
			ValidateWorldPosition();
			return _worldPosition.AsVec2;
		}
	}

	public GameEntityWithWorldPosition(WeakGameEntity gameEntity)
	{
		_customLocalFrame = MatrixFrame.Identity;
		_gameEntity = gameEntity;
		Scene scene = gameEntity.Scene;
		float groundHeightAtPosition = scene.GetGroundHeightAtPosition(gameEntity.GlobalPosition);
		_worldPosition = new WorldPosition(scene, UIntPtr.Zero, new Vec3(gameEntity.GlobalPosition.AsVec2, groundHeightAtPosition), hasValidZ: false);
		_worldPosition.GetGroundVec3();
		_orthonormalRotation = gameEntity.GetGlobalFrame().rotation;
		_orthonormalRotation.Orthonormalize();
	}

	private void ValidateWorldPosition()
	{
		Vec3 position = (_customLocalFrame.IsIdentity ? GameEntity.GetGlobalFrame().origin : GameEntity.GetGlobalFrame().TransformToParent(in _customLocalFrame).origin);
		if (!_worldPosition.AsVec2.NearlyEquals(position.AsVec2))
		{
			_worldPosition.SetVec3(UIntPtr.Zero, position, hasValidZ: false);
		}
	}

	public void InvalidateWorldPosition()
	{
		_worldPosition.State = ZValidityState.Invalid;
	}

	public void SetCustomLocalFrame(in MatrixFrame customLocalFrame)
	{
		_customLocalFrame = customLocalFrame;
	}

	public UIntPtr GetNavMesh()
	{
		ValidateWorldPosition();
		return _worldPosition.GetNavMesh();
	}

	public Vec3 GetNavMeshVec3()
	{
		ValidateWorldPosition();
		return _worldPosition.GetNavMeshVec3();
	}
}
