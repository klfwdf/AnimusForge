using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public abstract class ScriptComponentBehavior : DotNetObject
{
	[Flags]
	public enum TickRequirement : uint
	{
		None = 0u,
		TickOccasionally = 1u,
		Tick = 2u,
		TickParallel = 4u,
		TickParallel2 = 8u,
		FixedTick = 0x10u,
		FixedParallelTick = 0x20u,
		TickParallel3 = 0x40u
	}

	private static List<ScriptComponentBehavior> _prefabScriptComponents;

	private static List<ScriptComponentBehavior> _undoStackScriptComponents;

	private WeakGameEntity _gameEntity;

	private WeakNativeObjectReference _scriptComponent;

	private TickRequirement _lastTickRequirement;

	private static readonly Dictionary<string, string[]> CachedFields;

	private WeakNativeObjectReference _scene;

	public WeakGameEntity GameEntity => _gameEntity;

	public ManagedScriptComponent ScriptComponent
	{
		get
		{
			return _scriptComponent?.GetNativeObject() as ManagedScriptComponent;
		}
		private set
		{
			_scriptComponent = new WeakNativeObjectReference(value);
		}
	}

	protected ManagedScriptHolder ManagedScriptHolder { get; private set; }

	public Scene Scene
	{
		get
		{
			return _scene?.GetNativeObject() as Scene;
		}
		private set
		{
			_scene = new WeakNativeObjectReference(value);
		}
	}

	protected void InvalidateWeakPointersIfValid()
	{
		_scriptComponent.ManualInvalidate();
	}

	static ScriptComponentBehavior()
	{
		_prefabScriptComponents = new List<ScriptComponentBehavior>();
		_undoStackScriptComponents = new List<ScriptComponentBehavior>();
		if (CachedFields == null)
		{
			CachedFields = new Dictionary<string, string[]>();
			CacheEditableFieldsForAllScriptComponents();
		}
	}

	internal void Construct(UIntPtr myEntityPtr, ManagedScriptComponent scriptComponent)
	{
		_gameEntity = new WeakGameEntity(myEntityPtr);
		Scene = _gameEntity.Scene;
		ScriptComponent = scriptComponent;
	}

	internal void SetOwnerManagedScriptHolder(ManagedScriptHolder managedScriptHolder)
	{
		ManagedScriptHolder = managedScriptHolder;
	}

	private void SetScriptComponentToTickAux(TickRequirement value)
	{
		if (_lastTickRequirement != value)
		{
			ManagedScriptHolder.UpdateTickRequirement(this, _lastTickRequirement, value);
			_lastTickRequirement = value;
		}
	}

	public void SetScriptComponentToTick(TickRequirement tickReq)
	{
		if (ManagedScriptHolder != null)
		{
			SetScriptComponentToTickAux(tickReq);
		}
	}

	public void SetScriptComponentToTickMT(TickRequirement value)
	{
		if (ManagedScriptHolder != null)
		{
			lock (ManagedScriptHolder.AddRemoveLockObject)
			{
				SetScriptComponentToTickAux(value);
			}
		}
	}

	[EngineCallback(null, false)]
	internal void AddScriptComponentToTick()
	{
		lock (_prefabScriptComponents)
		{
			if (!_prefabScriptComponents.Contains(this))
			{
				_prefabScriptComponents.Add(this);
			}
		}
	}

	[EngineCallback(null, false)]
	internal void RegisterAsPrefabScriptComponent()
	{
		lock (_prefabScriptComponents)
		{
			if (!_prefabScriptComponents.Contains(this))
			{
				_prefabScriptComponents.Add(this);
			}
		}
	}

	[EngineCallback(null, false)]
	internal void DeregisterAsPrefabScriptComponent()
	{
		lock (_prefabScriptComponents)
		{
			_prefabScriptComponents.Remove(this);
		}
	}

	[EngineCallback(null, false)]
	internal void RegisterAsUndoStackScriptComponent()
	{
		if (!_undoStackScriptComponents.Contains(this))
		{
			_undoStackScriptComponents.Add(this);
		}
	}

	[EngineCallback(null, false)]
	internal void DeregisterAsUndoStackScriptComponent()
	{
		if (_undoStackScriptComponents.Contains(this))
		{
			_undoStackScriptComponents.Remove(this);
		}
	}

	[EngineCallback(null, false)]
	protected internal virtual void SetScene(Scene scene)
	{
		Scene = scene;
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnInit()
	{
	}

	[EngineCallback(null, false)]
	protected internal void HandleOnRemoved(int removeReason)
	{
		OnRemoved(removeReason);
		_scene = null;
	}

	protected virtual void OnRemoved(int removeReason)
	{
	}

	public virtual TickRequirement GetTickRequirement()
	{
		return TickRequirement.None;
	}

	protected internal virtual bool CanPhysicsCollideBetweenTwoEntities(WeakGameEntity myEntity, BodyFlags myEntityBodyFlags, WeakGameEntity otherEntity, BodyFlags otherEntityBodyFlags)
	{
		return true;
	}

	protected internal virtual void OnFixedTick(float fixedDt)
	{
		Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnFixedTick", 253);
	}

	protected internal virtual void OnParallelFixedTick(float fixedDt)
	{
		Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnParallelFixedTick", 259);
	}

	protected internal virtual void OnTick(float dt)
	{
		Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnTick", 265);
	}

	protected internal virtual void OnTickParallel(float dt)
	{
		Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnTickParallel", 271);
	}

	protected internal virtual void OnTickParallel2(float dt)
	{
	}

	protected internal virtual void OnTickParallel3(float dt)
	{
	}

	protected internal virtual void OnTickOccasionally(float currentFrameDeltaTime)
	{
		Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnTickOccasionally", 289);
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnPreInit()
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnEditorInit()
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnEditorTick(float dt)
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnEditorValidate()
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual bool IsOnlyVisual()
	{
		return false;
	}

	[EngineCallback(null, false)]
	protected internal virtual bool MovesEntity()
	{
		return true;
	}

	[EngineCallback(null, false)]
	protected internal virtual bool DisablesOroCreation()
	{
		return true;
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnEditorVariableChanged(string variableName)
	{
	}

	protected internal virtual bool SkeletonPostIntegrateCallback(AnimResult animResult)
	{
		return false;
	}

	[EngineCallback(null, false)]
	internal static bool SkeletonPostIntegrateCallbackAux(ScriptComponentBehavior script, UIntPtr animResultPointer)
	{
		AnimResult animResult = AnimResult.CreateWithPointer(animResultPointer);
		return script.SkeletonPostIntegrateCallback(animResult);
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnSceneSave(string saveFolder)
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual bool OnCheckForProblems()
	{
		return false;
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnSaveAsPrefab()
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnTerrainReload(int step)
	{
	}

	[EngineCallback(null, false)]
	protected internal void OnPhysicsCollisionAux(ref PhysicsContact contact, UIntPtr entity0, UIntPtr entity1)
	{
		OnPhysicsCollision(ref contact, new WeakGameEntity(entity0), new WeakGameEntity(entity1));
	}

	protected internal virtual void OnPhysicsCollision(ref PhysicsContact contact, WeakGameEntity entity0, WeakGameEntity entity1)
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnEditModeVisibilityChanged(bool currentVisibility)
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnBoundingBoxValidate()
	{
	}

	[EngineCallback(null, false)]
	protected internal virtual void OnDynamicNavmeshVertexUpdate()
	{
	}

	private static void CacheEditableFieldsForAllScriptComponents()
	{
		foreach (KeyValuePair<string, Type> moduleType in Managed.ModuleTypes)
		{
			Type value = moduleType.Value;
			string key = moduleType.Key;
			object[] customAttributesSafe = value.GetCustomAttributesSafe(typeof(ScriptComponentParams), inherit: true);
			if (customAttributesSafe.Length != 0)
			{
				ScriptComponentParams scriptComponentParams = (ScriptComponentParams)customAttributesSafe[0];
				if (scriptComponentParams.NameOverride.Length > 0)
				{
					key = scriptComponentParams.NameOverride;
				}
			}
			CachedFields.Add(key, CollectEditableFields(value));
		}
	}

	private static string[] CollectEditableFields(Type type)
	{
		List<string> list = new List<string>();
		List<FieldInfo> list2 = new List<FieldInfo>();
		while (type != null)
		{
			list2.AddRange(type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
			type = type.BaseType;
		}
		for (int i = 0; i < list2.Count; i++)
		{
			FieldInfo fieldInfo = list2[i];
			string item = list2[i].Name;
			object[] customAttributesSafe = fieldInfo.GetCustomAttributesSafe(typeof(EditableScriptComponentVariable), inherit: true);
			bool flag = false;
			if (customAttributesSafe.Length != 0)
			{
				EditableScriptComponentVariable editableScriptComponentVariable = (EditableScriptComponentVariable)customAttributesSafe[0];
				_ = fieldInfo.IsStatic;
				_ = fieldInfo.IsInitOnly;
				if (editableScriptComponentVariable.OverrideFieldName.Length > 0)
				{
					item = editableScriptComponentVariable.OverrideFieldName;
				}
				flag = editableScriptComponentVariable.Visible;
			}
			else if (!fieldInfo.IsPrivate && !fieldInfo.IsFamily)
			{
				flag = true;
			}
			if (fieldInfo.IsStatic)
			{
				flag = false;
			}
			if (flag)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	[EngineCallback(null, false)]
	internal static string[] GetEditableFields(string className)
	{
		CachedFields.TryGetValue(className, out var value);
		return value;
	}
}
