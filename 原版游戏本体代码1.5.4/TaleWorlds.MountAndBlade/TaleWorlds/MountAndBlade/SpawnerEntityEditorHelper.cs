using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SpawnerEntityEditorHelper
{
	public enum Axis
	{
		x,
		y,
		z
	}

	public enum PermissionType
	{
		scale,
		rotation
	}

	public struct Permission
	{
		public PermissionType TypeOfPermission;

		public Axis PermittedAxis;

		public Permission(PermissionType permission, Axis axis)
		{
			TypeOfPermission = permission;
			PermittedAxis = axis;
		}
	}

	private List<Tuple<string, Permission, Action<float>>> _stableChildrenPermissions = new List<Tuple<string, Permission, Action<float>>>();

	private ScriptComponentBehavior spawner_;

	private List<KeyValuePair<string, MatrixFrame>> stableChildrenFrames = new List<KeyValuePair<string, MatrixFrame>>();

	public bool LockGhostParent = true;

	private bool _ghostMovementMode;

	private PathTracker _tracker;

	private float _ghostObjectPosition;

	private string _pathName;

	private bool _enableAutoGhostMovement;

	private readonly List<GameEntity> _wheels = new List<GameEntity>();

	public bool IsValid { get; private set; }

	public GameEntity SpawnedGhostEntity { get; private set; }

	public SpawnerEntityEditorHelper(ScriptComponentBehavior spawner)
	{
		spawner_ = spawner;
		if (AddGhostEntity(spawner_.GameEntity, GetGhostName()) != null)
		{
			SyncMatrixFrames(first: true);
			IsValid = true;
		}
		else
		{
			Debug.FailedAssert("No prefab found. Spawner script will remove itself.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\SpawnerEntityEditorHelper.cs", ".ctor", 75);
			spawner.GameEntity.RemoveScriptComponent(spawner_.ScriptComponent.Pointer, 11);
		}
	}

	public GameEntity GetGhostEntityOrChild(string name)
	{
		if (SpawnedGhostEntity.Name == name)
		{
			return SpawnedGhostEntity;
		}
		List<GameEntity> children = new List<GameEntity>();
		SpawnedGhostEntity.GetChildrenRecursive(ref children);
		GameEntity gameEntity = children.FirstOrDefault((GameEntity x) => x.Name == name);
		if (gameEntity != null)
		{
			return gameEntity;
		}
		return null;
	}

	public void Tick(float dt)
	{
		if (SpawnedGhostEntity.Parent != spawner_.GameEntity)
		{
			IsValid = false;
			spawner_.GameEntity.RemoveScriptComponent(spawner_.ScriptComponent.Pointer, 12);
		}
		if (!IsValid)
		{
			return;
		}
		if (LockGhostParent)
		{
			bool num = SpawnedGhostEntity.GetFrame() != MatrixFrame.Identity;
			MatrixFrame frame = MatrixFrame.Identity;
			SpawnedGhostEntity.SetFrame(ref frame);
			if (num)
			{
				SpawnedGhostEntity.UpdateTriadFrameForEditor();
			}
		}
		SyncMatrixFrames(first: false);
		if (_ghostMovementMode)
		{
			UpdateGhostMovement(dt);
		}
	}

	public void GivePermission(string childName, Permission permission, Action<float> onChangeFunction)
	{
		_stableChildrenPermissions.Add(Tuple.Create(childName, permission, onChangeFunction));
	}

	private void ApplyPermissions()
	{
		foreach (Tuple<string, Permission, Action<float>> item in _stableChildrenPermissions)
		{
			KeyValuePair<string, MatrixFrame> keyValuePair = stableChildrenFrames.Find((KeyValuePair<string, MatrixFrame> x) => x.Key == item.Item1);
			MatrixFrame frame = GetGhostEntityOrChild(item.Item1).GetFrame();
			if (frame.NearlyEquals(keyValuePair.Value))
			{
				continue;
			}
			switch (item.Item2.TypeOfPermission)
			{
			case PermissionType.scale:
			{
				ref Vec3 origin = ref frame.origin;
				MatrixFrame value = keyValuePair.Value;
				if (!origin.NearlyEquals(in value.origin, 0.0001f) || !frame.rotation.f.NormalizedCopy().NearlyEquals(keyValuePair.Value.rotation.f.NormalizedCopy(), 0.0001f) || !frame.rotation.u.NormalizedCopy().NearlyEquals(keyValuePair.Value.rotation.u.NormalizedCopy(), 0.0001f) || !frame.rotation.s.NormalizedCopy().NearlyEquals(keyValuePair.Value.rotation.s.NormalizedCopy(), 0.0001f))
				{
					break;
				}
				switch (item.Item2.PermittedAxis)
				{
				case Axis.x:
				{
					ref Vec3 f4 = ref frame.rotation.f;
					value = keyValuePair.Value;
					if (!f4.NearlyEquals(in value.rotation.f))
					{
						ChangeStableChildMatrixFrame(item.Item1, frame);
						item.Item3(frame.rotation.f.Length);
					}
					break;
				}
				case Axis.y:
				{
					ref Vec3 s4 = ref frame.rotation.s;
					value = keyValuePair.Value;
					if (!s4.NearlyEquals(in value.rotation.s))
					{
						ChangeStableChildMatrixFrame(item.Item1, frame);
						item.Item3(frame.rotation.s.Length);
					}
					break;
				}
				case Axis.z:
				{
					ref Vec3 u4 = ref frame.rotation.u;
					value = keyValuePair.Value;
					if (!u4.NearlyEquals(in value.rotation.u))
					{
						ChangeStableChildMatrixFrame(item.Item1, frame);
						item.Item3(frame.rotation.u.Length);
					}
					break;
				}
				}
				break;
			}
			case PermissionType.rotation:
				switch (item.Item2.PermittedAxis)
				{
				case Axis.x:
				{
					ref Vec3 f2 = ref frame.rotation.f;
					MatrixFrame value = keyValuePair.Value;
					if (f2.NearlyEquals(in value.rotation.f))
					{
						break;
					}
					ref Vec3 u2 = ref frame.rotation.u;
					MatrixFrame value2 = keyValuePair.Value;
					if (!u2.NearlyEquals(in value2.rotation.u))
					{
						ref Vec3 s2 = ref frame.rotation.s;
						MatrixFrame value3 = keyValuePair.Value;
						if (s2.NearlyEquals(in value3.rotation.s))
						{
							ChangeStableChildMatrixFrame(item.Item1, frame);
							item.Item3(frame.rotation.GetEulerAngles().x);
						}
					}
					break;
				}
				case Axis.y:
				{
					ref Vec3 s3 = ref frame.rotation.s;
					MatrixFrame value = keyValuePair.Value;
					if (s3.NearlyEquals(in value.rotation.s))
					{
						break;
					}
					ref Vec3 u3 = ref frame.rotation.u;
					MatrixFrame value2 = keyValuePair.Value;
					if (!u3.NearlyEquals(in value2.rotation.u))
					{
						ref Vec3 f3 = ref frame.rotation.f;
						MatrixFrame value3 = keyValuePair.Value;
						if (f3.NearlyEquals(in value3.rotation.f))
						{
							ChangeStableChildMatrixFrame(item.Item1, frame);
							item.Item3(frame.rotation.GetEulerAngles().y);
						}
					}
					break;
				}
				case Axis.z:
				{
					ref Vec3 f = ref frame.rotation.f;
					MatrixFrame value = keyValuePair.Value;
					if (f.NearlyEquals(in value.rotation.f))
					{
						break;
					}
					ref Vec3 s = ref frame.rotation.s;
					MatrixFrame value2 = keyValuePair.Value;
					if (!s.NearlyEquals(in value2.rotation.s))
					{
						ref Vec3 u = ref frame.rotation.u;
						MatrixFrame value3 = keyValuePair.Value;
						if (u.NearlyEquals(in value3.rotation.u))
						{
							ChangeStableChildMatrixFrame(item.Item1, frame);
							item.Item3(frame.rotation.GetEulerAngles().z);
						}
					}
					break;
				}
				}
				break;
			}
		}
	}

	private void ChangeStableChildMatrixFrame(string childName, MatrixFrame matrixFrame)
	{
		stableChildrenFrames.RemoveAll((KeyValuePair<string, MatrixFrame> x) => x.Key == childName);
		KeyValuePair<string, MatrixFrame> item = new KeyValuePair<string, MatrixFrame>(childName, matrixFrame);
		stableChildrenFrames.Add(item);
		if (HasField(spawner_, childName, findRestricted: true))
		{
			SetSpawnerMatrixFrame(spawner_, childName, matrixFrame);
		}
	}

	public void ChangeStableChildMatrixFrameAndApply(string childName, MatrixFrame matrixFrame, bool updateTriad = true)
	{
		ChangeStableChildMatrixFrame(childName, matrixFrame);
		GetGhostEntityOrChild(childName).SetFrame(ref matrixFrame);
		if (updateTriad)
		{
			SpawnedGhostEntity.UpdateTriadFrameForEditorForAllChildren();
		}
	}

	private GameEntity AddGhostEntity(WeakGameEntity parent, List<string> possibleEntityNames)
	{
		spawner_.GameEntity.RemoveAllChildren();
		foreach (string possibleEntityName in possibleEntityNames)
		{
			if (GameEntity.PrefabExists(possibleEntityName))
			{
				SpawnedGhostEntity = GameEntity.Instantiate(parent.Scene, possibleEntityName, callScriptCallbacks: true);
				break;
			}
		}
		if (SpawnedGhostEntity == null)
		{
			return null;
		}
		SpawnedGhostEntity.SetMobility(GameEntity.Mobility.Dynamic);
		SpawnedGhostEntity.EntityFlags |= EntityFlags.DontSaveToScene;
		parent.AddChild(SpawnedGhostEntity.WeakEntity);
		MatrixFrame frame = MatrixFrame.Identity;
		SpawnedGhostEntity.SetFrame(ref frame);
		GetChildrenInitialFrames();
		SpawnedGhostEntity.UpdateTriadFrameForEditorForAllChildren();
		return SpawnedGhostEntity;
	}

	private void SyncMatrixFrames(bool first)
	{
		ApplyPermissions();
		List<GameEntity> children = new List<GameEntity>();
		SpawnedGhostEntity.GetChildrenRecursive(ref children);
		foreach (GameEntity item in children)
		{
			if (HasField(spawner_, item.Name, findRestricted: false))
			{
				if (first)
				{
					MatrixFrame frame = (MatrixFrame)GetFieldValue(spawner_, item.Name);
					if (!frame.IsZero)
					{
						item.SetFrame(ref frame);
					}
				}
				else
				{
					SetSpawnerMatrixFrame(spawner_, item.Name, item.GetFrame());
				}
			}
			else
			{
				MatrixFrame frame2 = stableChildrenFrames.Find((KeyValuePair<string, MatrixFrame> x) => x.Key == item.Name).Value;
				if (!frame2.NearlyEquals(item.GetFrame()))
				{
					item.SetFrame(ref frame2);
					SpawnedGhostEntity.UpdateTriadFrameForEditorForAllChildren();
				}
			}
		}
	}

	private void GetChildrenInitialFrames()
	{
		List<GameEntity> children = new List<GameEntity>();
		SpawnedGhostEntity.GetChildrenRecursive(ref children);
		foreach (GameEntity item in children)
		{
			if (!HasField(spawner_, item.Name, findRestricted: false))
			{
				stableChildrenFrames.Add(new KeyValuePair<string, MatrixFrame>(item.Name, item.GetFrame()));
			}
		}
	}

	private List<string> GetGhostName()
	{
		string prefabName = GetPrefabName();
		List<string> list = new List<string>();
		list.Add(prefabName + "_ghost");
		prefabName = prefabName.Remove(prefabName.Length - prefabName.Split(new char[1] { '_' }).Last().Length - 1);
		list.Add(prefabName + "_ghost");
		return list;
	}

	public string GetPrefabName()
	{
		return spawner_.GameEntity.Name.Remove(spawner_.GameEntity.Name.Length - spawner_.GameEntity.Name.Split(new char[1] { '_' }).Last().Length - 1);
	}

	public void SetupGhostMovement(string pathName)
	{
		_ghostMovementMode = true;
		_pathName = pathName;
		Path pathWithName = SpawnedGhostEntity.Scene.GetPathWithName(pathName);
		Vec3 scaleVector = SpawnedGhostEntity.GetFrame().rotation.GetScaleVector();
		_tracker = new PathTracker(pathWithName, scaleVector);
		_ghostObjectPosition = ((pathWithName != null) ? pathWithName.GetTotalLength() : 0f);
		SpawnedGhostEntity.UpdateTriadFrameForEditor();
		List<GameEntity> children = new List<GameEntity>();
		SpawnedGhostEntity.GetChildrenRecursive(ref children);
		_wheels.Clear();
		_wheels.AddRange(children.Where((GameEntity x) => x.HasTag("wheel")));
	}

	public void SetEnableAutoGhostMovement(bool enableAutoGhostMovement)
	{
		_enableAutoGhostMovement = enableAutoGhostMovement;
		if (!_enableAutoGhostMovement && _tracker.IsValid)
		{
			_ghostObjectPosition = _tracker.GetPathLength();
		}
	}

	private void UpdateGhostMovement(float dt)
	{
		if (_tracker.HasChanged)
		{
			SetupGhostMovement(_pathName);
			_tracker.Advance(_tracker.GetPathLength());
		}
		if (spawner_.GameEntity.IsSelectedOnEditor() || SpawnedGhostEntity.IsSelectedOnEditor())
		{
			if (_tracker.IsValid)
			{
				float num = 10f;
				if (Input.DebugInput.IsShiftDown())
				{
					num = 1f;
				}
				if (Input.DebugInput.IsKeyDown(InputKey.MouseScrollUp))
				{
					_ghostObjectPosition += dt * num;
				}
				else if (Input.DebugInput.IsKeyDown(InputKey.MouseScrollDown))
				{
					_ghostObjectPosition -= dt * num;
				}
				if (_enableAutoGhostMovement)
				{
					_ghostObjectPosition += dt * num;
					if (_ghostObjectPosition >= _tracker.GetPathLength())
					{
						_ghostObjectPosition = 0f;
					}
				}
				_ghostObjectPosition = MBMath.ClampFloat(_ghostObjectPosition, 0f, _tracker.GetPathLength());
			}
			else
			{
				_ghostObjectPosition = 0f;
			}
		}
		if (_tracker.IsValid)
		{
			MatrixFrame m = spawner_.GameEntity.GetGlobalFrame();
			_tracker.Advance(0f);
			_tracker.CurrentFrameAndColor(out var frame, out var color);
			if (m != frame)
			{
				spawner_.GameEntity.SetGlobalFrame(in frame);
				spawner_.GameEntity.UpdateTriadFrameForEditor();
			}
			_tracker.Advance(_ghostObjectPosition);
			_tracker.CurrentFrameAndColor(out frame, out color);
			if (_wheels.Count == 2)
			{
				frame = LinearInterpolatedIK(ref _tracker);
			}
			if (m != frame)
			{
				SpawnedGhostEntity.SetGlobalFrame(in frame);
				SpawnedGhostEntity.UpdateTriadFrameForEditor();
			}
			_tracker.Reset();
		}
		else if (SpawnedGhostEntity.GetGlobalFrame() != spawner_.GameEntity.GetGlobalFrame())
		{
			SpawnedGhostEntity.SetGlobalFrame(spawner_.GameEntity.GetGlobalFrame());
			SpawnedGhostEntity.UpdateTriadFrameForEditor();
		}
	}

	private MatrixFrame LinearInterpolatedIK(ref PathTracker pathTracker)
	{
		pathTracker.CurrentFrameAndColor(out var frame, out var color);
		return MatrixFrame.Lerp(in frame, SiegeWeaponMovementComponent.FindGroundFrameForWheelsStatic(ref frame, 2.45f, 1.3f, SpawnedGhostEntity.WeakEntity, _wheels, SpawnedGhostEntity.Scene), color.x);
	}

	private static object GetFieldValue(object src, string propName)
	{
		return src.GetType().GetField(propName).GetValue(src);
	}

	private static bool HasField(object obj, string propertyName, bool findRestricted)
	{
		if (obj.GetType().GetField(propertyName) != null)
		{
			if (!findRestricted)
			{
				return obj.GetType().GetField(propertyName).GetCustomAttribute<RestrictedAccess>() == null;
			}
			return true;
		}
		return false;
	}

	private static bool SetSpawnerMatrixFrame(object target, string propertyName, MatrixFrame value)
	{
		value.Fill();
		FieldInfo field = target.GetType().GetField(propertyName);
		if (field != null)
		{
			field.SetValue(target, value);
			return true;
		}
		return false;
	}
}
