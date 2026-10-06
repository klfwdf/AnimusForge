using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class SpawnerEntityMissionHelper
{
	private const string EnabledSuffix = "_enabled";

	public GameEntity SpawnedEntity;

	private GameEntity _ownerEntity;

	private SpawnerBase _spawner;

	private string _gameEntityName;

	private bool _fireVersion;

	public SpawnerEntityMissionHelper(SpawnerBase spawner, bool fireVersion = false)
	{
		_spawner = spawner;
		_fireVersion = fireVersion;
		_ownerEntity = GameEntity.CreateFromWeakEntity(_spawner.GameEntity);
		_gameEntityName = _ownerEntity.Name;
		if (SpawnPrefab(_ownerEntity, GetPrefabName()) != null)
		{
			SyncMatrixFrames();
		}
		else
		{
			Debug.FailedAssert("Spawner couldn't spawn a proper entity.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\SpawnerEntityMissionHelper.cs", ".ctor", 34);
		}
		_spawner.AssignParameters(this);
		CallSetSpawnedFromSpawnerOfScripts();
	}

	private GameEntity SpawnPrefab(GameEntity parent, string entityName)
	{
		InstantiateEntity(parent, entityName);
		SpawnedEntity.SetMobility(GameEntity.Mobility.Dynamic);
		SpawnedEntity.EntityFlags |= EntityFlags.DontSaveToScene;
		parent.AddChild(SpawnedEntity);
		MatrixFrame frame = MatrixFrame.Identity;
		SpawnedEntity.SetFrame(ref frame);
		string[] tags = _ownerEntity.Tags;
		foreach (string tag in tags)
		{
			SpawnedEntity.AddTag(tag);
		}
		return SpawnedEntity;
	}

	protected virtual void InstantiateEntity(GameEntity parent, string entityName)
	{
		SpawnedEntity = GameEntity.Instantiate(parent.Scene, entityName, callScriptCallbacks: false);
	}

	private void RemoveChildEntity(GameEntity child)
	{
		child.CallScriptCallbacks(registerScriptComponents: false);
		child.Remove(85);
	}

	private void SyncMatrixFrames()
	{
		List<GameEntity> children = new List<GameEntity>();
		SpawnedEntity.GetChildrenRecursive(ref children);
		foreach (GameEntity item in children)
		{
			if (HasField(_spawner, item.Name))
			{
				MatrixFrame frame = (MatrixFrame)GetFieldValue(_spawner, item.Name);
				item.SetFrame(ref frame);
			}
			if (HasField(_spawner, item.Name + "_enabled") && !(bool)GetFieldValue(_spawner, item.Name + "_enabled"))
			{
				RemoveChildEntity(item);
			}
		}
	}

	private void CallSetSpawnedFromSpawnerOfScripts()
	{
		foreach (GameEntity entityAndChild in SpawnedEntity.GetEntityAndChildren())
		{
			foreach (ScriptComponentBehavior item in from x in entityAndChild.GetScriptComponents()
				where x is ISpawnable
				select x)
			{
				(item as ISpawnable).SetSpawnedFromSpawner();
			}
		}
	}

	private string GetPrefabName()
	{
		string text;
		if (_spawner.ToBeSpawnedOverrideName != "")
		{
			text = _spawner.ToBeSpawnedOverrideName;
		}
		else
		{
			text = _gameEntityName;
			text = text.Remove(_gameEntityName.Length - _gameEntityName.Split(new char[1] { '_' }).Last().Length - 1);
		}
		if (_fireVersion)
		{
			text = ((!(_spawner.ToBeSpawnedOverrideNameForFireVersion != "")) ? (text + "_fire") : _spawner.ToBeSpawnedOverrideNameForFireVersion);
		}
		return text;
	}

	private static object GetFieldValue(object src, string propName)
	{
		return src.GetType().GetField(propName).GetValue(src);
	}

	private static bool HasField(object obj, string propertyName)
	{
		return obj.GetType().GetField(propertyName) != null;
	}
}
