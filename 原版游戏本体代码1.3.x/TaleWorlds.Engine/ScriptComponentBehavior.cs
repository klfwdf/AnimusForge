using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000088 RID: 136
	public abstract class ScriptComponentBehavior : DotNetObject
	{
		// Token: 0x06000C17 RID: 3095 RVA: 0x0000D413 File Offset: 0x0000B613
		protected void InvalidateWeakPointersIfValid()
		{
			this._scriptComponent.ManualInvalidate();
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000C18 RID: 3096 RVA: 0x0000D420 File Offset: 0x0000B620
		public WeakGameEntity GameEntity
		{
			get
			{
				return this._gameEntity;
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000C19 RID: 3097 RVA: 0x0000D428 File Offset: 0x0000B628
		// (set) Token: 0x06000C1A RID: 3098 RVA: 0x0000D441 File Offset: 0x0000B641
		public ManagedScriptComponent ScriptComponent
		{
			get
			{
				WeakNativeObjectReference scriptComponent = this._scriptComponent;
				return ((scriptComponent != null) ? scriptComponent.GetNativeObject() : null) as ManagedScriptComponent;
			}
			private set
			{
				this._scriptComponent = new WeakNativeObjectReference(value);
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000C1B RID: 3099 RVA: 0x0000D44F File Offset: 0x0000B64F
		// (set) Token: 0x06000C1C RID: 3100 RVA: 0x0000D457 File Offset: 0x0000B657
		private protected ManagedScriptHolder ManagedScriptHolder { protected get; private set; }

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000C1D RID: 3101 RVA: 0x0000D460 File Offset: 0x0000B660
		// (set) Token: 0x06000C1E RID: 3102 RVA: 0x0000D479 File Offset: 0x0000B679
		public Scene Scene
		{
			get
			{
				WeakNativeObjectReference scene = this._scene;
				return ((scene != null) ? scene.GetNativeObject() : null) as Scene;
			}
			private set
			{
				this._scene = new WeakNativeObjectReference(value);
			}
		}

		// Token: 0x06000C1F RID: 3103 RVA: 0x0000D487 File Offset: 0x0000B687
		static ScriptComponentBehavior()
		{
			if (ScriptComponentBehavior.CachedFields == null)
			{
				ScriptComponentBehavior.CachedFields = new Dictionary<string, string[]>();
				ScriptComponentBehavior.CacheEditableFieldsForAllScriptComponents();
			}
		}

		// Token: 0x06000C21 RID: 3105 RVA: 0x0000D4BB File Offset: 0x0000B6BB
		internal void Construct(UIntPtr myEntityPtr, ManagedScriptComponent scriptComponent)
		{
			this._gameEntity = new WeakGameEntity(myEntityPtr);
			this.Scene = this._gameEntity.Scene;
			this.ScriptComponent = scriptComponent;
		}

		// Token: 0x06000C22 RID: 3106 RVA: 0x0000D4E1 File Offset: 0x0000B6E1
		internal void SetOwnerManagedScriptHolder(ManagedScriptHolder managedScriptHolder)
		{
			this.ManagedScriptHolder = managedScriptHolder;
		}

		// Token: 0x06000C23 RID: 3107 RVA: 0x0000D4EA File Offset: 0x0000B6EA
		private void SetScriptComponentToTickAux(ScriptComponentBehavior.TickRequirement value)
		{
			if (this._lastTickRequirement != value)
			{
				this.ManagedScriptHolder.UpdateTickRequirement(this, this._lastTickRequirement, value);
				this._lastTickRequirement = value;
			}
		}

		// Token: 0x06000C24 RID: 3108 RVA: 0x0000D50F File Offset: 0x0000B70F
		public void SetScriptComponentToTick(ScriptComponentBehavior.TickRequirement tickReq)
		{
			if (this.ManagedScriptHolder != null)
			{
				this.SetScriptComponentToTickAux(tickReq);
			}
		}

		// Token: 0x06000C25 RID: 3109 RVA: 0x0000D520 File Offset: 0x0000B720
		public void SetScriptComponentToTickMT(ScriptComponentBehavior.TickRequirement value)
		{
			if (this.ManagedScriptHolder != null)
			{
				object addRemoveLockObject = this.ManagedScriptHolder.AddRemoveLockObject;
				lock (addRemoveLockObject)
				{
					this.SetScriptComponentToTickAux(value);
				}
			}
		}

		// Token: 0x06000C26 RID: 3110 RVA: 0x0000D570 File Offset: 0x0000B770
		[EngineCallback(null, false)]
		internal void AddScriptComponentToTick()
		{
			List<ScriptComponentBehavior> prefabScriptComponents = ScriptComponentBehavior._prefabScriptComponents;
			lock (prefabScriptComponents)
			{
				if (!ScriptComponentBehavior._prefabScriptComponents.Contains(this))
				{
					ScriptComponentBehavior._prefabScriptComponents.Add(this);
				}
			}
		}

		// Token: 0x06000C27 RID: 3111 RVA: 0x0000D5C4 File Offset: 0x0000B7C4
		[EngineCallback(null, false)]
		internal void RegisterAsPrefabScriptComponent()
		{
			List<ScriptComponentBehavior> prefabScriptComponents = ScriptComponentBehavior._prefabScriptComponents;
			lock (prefabScriptComponents)
			{
				if (!ScriptComponentBehavior._prefabScriptComponents.Contains(this))
				{
					ScriptComponentBehavior._prefabScriptComponents.Add(this);
				}
			}
		}

		// Token: 0x06000C28 RID: 3112 RVA: 0x0000D618 File Offset: 0x0000B818
		[EngineCallback(null, false)]
		internal void DeregisterAsPrefabScriptComponent()
		{
			List<ScriptComponentBehavior> prefabScriptComponents = ScriptComponentBehavior._prefabScriptComponents;
			lock (prefabScriptComponents)
			{
				ScriptComponentBehavior._prefabScriptComponents.Remove(this);
			}
		}

		// Token: 0x06000C29 RID: 3113 RVA: 0x0000D660 File Offset: 0x0000B860
		[EngineCallback(null, false)]
		internal void RegisterAsUndoStackScriptComponent()
		{
			if (!ScriptComponentBehavior._undoStackScriptComponents.Contains(this))
			{
				ScriptComponentBehavior._undoStackScriptComponents.Add(this);
			}
		}

		// Token: 0x06000C2A RID: 3114 RVA: 0x0000D67A File Offset: 0x0000B87A
		[EngineCallback(null, false)]
		internal void DeregisterAsUndoStackScriptComponent()
		{
			if (ScriptComponentBehavior._undoStackScriptComponents.Contains(this))
			{
				ScriptComponentBehavior._undoStackScriptComponents.Remove(this);
			}
		}

		// Token: 0x06000C2B RID: 3115 RVA: 0x0000D695 File Offset: 0x0000B895
		[EngineCallback(null, false)]
		protected internal virtual void SetScene(Scene scene)
		{
			this.Scene = scene;
		}

		// Token: 0x06000C2C RID: 3116 RVA: 0x0000D69E File Offset: 0x0000B89E
		[EngineCallback(null, false)]
		protected internal virtual void OnInit()
		{
		}

		// Token: 0x06000C2D RID: 3117 RVA: 0x0000D6A0 File Offset: 0x0000B8A0
		[EngineCallback(null, false)]
		protected internal void HandleOnRemoved(int removeReason)
		{
			this.OnRemoved(removeReason);
			this._scene = null;
		}

		// Token: 0x06000C2E RID: 3118 RVA: 0x0000D6B0 File Offset: 0x0000B8B0
		protected virtual void OnRemoved(int removeReason)
		{
		}

		// Token: 0x06000C2F RID: 3119 RVA: 0x0000D6B2 File Offset: 0x0000B8B2
		public virtual ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			return ScriptComponentBehavior.TickRequirement.None;
		}

		// Token: 0x06000C30 RID: 3120 RVA: 0x0000D6B5 File Offset: 0x0000B8B5
		protected internal virtual bool CanPhysicsCollideBetweenTwoEntities(WeakGameEntity myEntity, WeakGameEntity otherEntity)
		{
			return true;
		}

		// Token: 0x06000C31 RID: 3121 RVA: 0x0000D6B8 File Offset: 0x0000B8B8
		protected internal virtual void OnFixedTick(float fixedDt)
		{
			Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnFixedTick", 253);
		}

		// Token: 0x06000C32 RID: 3122 RVA: 0x0000D6D3 File Offset: 0x0000B8D3
		protected internal virtual void OnParallelFixedTick(float fixedDt)
		{
			Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnParallelFixedTick", 259);
		}

		// Token: 0x06000C33 RID: 3123 RVA: 0x0000D6EE File Offset: 0x0000B8EE
		protected internal virtual void OnTick(float dt)
		{
			Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnTick", 265);
		}

		// Token: 0x06000C34 RID: 3124 RVA: 0x0000D709 File Offset: 0x0000B909
		protected internal virtual void OnTickParallel(float dt)
		{
			Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnTickParallel", 271);
		}

		// Token: 0x06000C35 RID: 3125 RVA: 0x0000D724 File Offset: 0x0000B924
		protected internal virtual void OnTickParallel2(float dt)
		{
		}

		// Token: 0x06000C36 RID: 3126 RVA: 0x0000D726 File Offset: 0x0000B926
		protected internal virtual void OnTickParallel3(float dt)
		{
		}

		// Token: 0x06000C37 RID: 3127 RVA: 0x0000D728 File Offset: 0x0000B928
		protected internal virtual void OnTickOccasionally(float currentFrameDeltaTime)
		{
			Debug.FailedAssert("This base function should never be called.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\ScriptComponentBehavior.cs", "OnTickOccasionally", 289);
		}

		// Token: 0x06000C38 RID: 3128 RVA: 0x0000D743 File Offset: 0x0000B943
		[EngineCallback(null, false)]
		protected internal virtual void OnPreInit()
		{
		}

		// Token: 0x06000C39 RID: 3129 RVA: 0x0000D745 File Offset: 0x0000B945
		[EngineCallback(null, false)]
		protected internal virtual void OnEditorInit()
		{
		}

		// Token: 0x06000C3A RID: 3130 RVA: 0x0000D747 File Offset: 0x0000B947
		[EngineCallback(null, false)]
		protected internal virtual void OnEditorTick(float dt)
		{
		}

		// Token: 0x06000C3B RID: 3131 RVA: 0x0000D749 File Offset: 0x0000B949
		[EngineCallback(null, false)]
		protected internal virtual void OnEditorValidate()
		{
		}

		// Token: 0x06000C3C RID: 3132 RVA: 0x0000D74B File Offset: 0x0000B94B
		[EngineCallback(null, false)]
		protected internal virtual bool IsOnlyVisual()
		{
			return false;
		}

		// Token: 0x06000C3D RID: 3133 RVA: 0x0000D74E File Offset: 0x0000B94E
		[EngineCallback(null, false)]
		protected internal virtual bool MovesEntity()
		{
			return true;
		}

		// Token: 0x06000C3E RID: 3134 RVA: 0x0000D751 File Offset: 0x0000B951
		[EngineCallback(null, false)]
		protected internal virtual bool DisablesOroCreation()
		{
			return true;
		}

		// Token: 0x06000C3F RID: 3135 RVA: 0x0000D754 File Offset: 0x0000B954
		[EngineCallback(null, false)]
		protected internal virtual void OnEditorVariableChanged(string variableName)
		{
		}

		// Token: 0x06000C40 RID: 3136 RVA: 0x0000D756 File Offset: 0x0000B956
		protected internal virtual bool SkeletonPostIntegrateCallback(AnimResult animResult)
		{
			return false;
		}

		// Token: 0x06000C41 RID: 3137 RVA: 0x0000D75C File Offset: 0x0000B95C
		[EngineCallback(null, false)]
		internal static bool SkeletonPostIntegrateCallbackAux(ScriptComponentBehavior script, UIntPtr animResultPointer)
		{
			AnimResult animResult = AnimResult.CreateWithPointer(animResultPointer);
			return script.SkeletonPostIntegrateCallback(animResult);
		}

		// Token: 0x06000C42 RID: 3138 RVA: 0x0000D777 File Offset: 0x0000B977
		[EngineCallback(null, false)]
		protected internal virtual void OnSceneSave(string saveFolder)
		{
		}

		// Token: 0x06000C43 RID: 3139 RVA: 0x0000D779 File Offset: 0x0000B979
		[EngineCallback(null, false)]
		protected internal virtual bool OnCheckForProblems()
		{
			return false;
		}

		// Token: 0x06000C44 RID: 3140 RVA: 0x0000D77C File Offset: 0x0000B97C
		[EngineCallback(null, false)]
		protected internal virtual void OnSaveAsPrefab()
		{
		}

		// Token: 0x06000C45 RID: 3141 RVA: 0x0000D77E File Offset: 0x0000B97E
		[EngineCallback(null, false)]
		protected internal virtual void OnTerrainReload(int step)
		{
		}

		// Token: 0x06000C46 RID: 3142 RVA: 0x0000D780 File Offset: 0x0000B980
		[EngineCallback(null, false)]
		protected internal void OnPhysicsCollisionAux(ref PhysicsContact contact, UIntPtr entity0, UIntPtr entity1, bool isFirstShape)
		{
			this.OnPhysicsCollision(ref contact, new WeakGameEntity(entity0), new WeakGameEntity(entity1), isFirstShape);
		}

		// Token: 0x06000C47 RID: 3143 RVA: 0x0000D797 File Offset: 0x0000B997
		protected internal virtual void OnPhysicsCollision(ref PhysicsContact contact, WeakGameEntity entity0, WeakGameEntity entity1, bool isFirstShape)
		{
		}

		// Token: 0x06000C48 RID: 3144 RVA: 0x0000D799 File Offset: 0x0000B999
		[EngineCallback(null, false)]
		protected internal virtual void OnEditModeVisibilityChanged(bool currentVisibility)
		{
		}

		// Token: 0x06000C49 RID: 3145 RVA: 0x0000D79B File Offset: 0x0000B99B
		[EngineCallback(null, false)]
		protected internal virtual void OnBoundingBoxValidate()
		{
		}

		// Token: 0x06000C4A RID: 3146 RVA: 0x0000D79D File Offset: 0x0000B99D
		[EngineCallback(null, false)]
		protected internal virtual void OnDynamicNavmeshVertexUpdate()
		{
		}

		// Token: 0x06000C4B RID: 3147 RVA: 0x0000D7A0 File Offset: 0x0000B9A0
		private static void CacheEditableFieldsForAllScriptComponents()
		{
			foreach (KeyValuePair<string, Type> keyValuePair in Managed.ModuleTypes)
			{
				Type value = keyValuePair.Value;
				string key = keyValuePair.Key;
				object[] customAttributesSafe = value.GetCustomAttributesSafe(typeof(ScriptComponentParams), true);
				if (customAttributesSafe.Length != 0)
				{
					ScriptComponentParams scriptComponentParams = (ScriptComponentParams)customAttributesSafe[0];
					if (scriptComponentParams.NameOverride.Length > 0)
					{
						key = scriptComponentParams.NameOverride;
					}
				}
				ScriptComponentBehavior.CachedFields.Add(key, ScriptComponentBehavior.CollectEditableFields(value));
			}
		}

		// Token: 0x06000C4C RID: 3148 RVA: 0x0000D848 File Offset: 0x0000BA48
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
				object[] customAttributesSafe = fieldInfo.GetCustomAttributesSafe(typeof(EditableScriptComponentVariable), true);
				bool flag = false;
				if (customAttributesSafe.Length != 0)
				{
					EditableScriptComponentVariable editableScriptComponentVariable = (EditableScriptComponentVariable)customAttributesSafe[0];
					bool isStatic = fieldInfo.IsStatic;
					bool isInitOnly = fieldInfo.IsInitOnly;
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

		// Token: 0x06000C4D RID: 3149 RVA: 0x0000D938 File Offset: 0x0000BB38
		[EngineCallback(null, false)]
		internal static string[] GetEditableFields(string className)
		{
			string[] result;
			ScriptComponentBehavior.CachedFields.TryGetValue(className, out result);
			return result;
		}

		// Token: 0x040001B4 RID: 436
		private static List<ScriptComponentBehavior> _prefabScriptComponents = new List<ScriptComponentBehavior>();

		// Token: 0x040001B5 RID: 437
		private static List<ScriptComponentBehavior> _undoStackScriptComponents = new List<ScriptComponentBehavior>();

		// Token: 0x040001B6 RID: 438
		private WeakGameEntity _gameEntity;

		// Token: 0x040001B7 RID: 439
		private WeakNativeObjectReference _scriptComponent;

		// Token: 0x040001B8 RID: 440
		private ScriptComponentBehavior.TickRequirement _lastTickRequirement;

		// Token: 0x040001B9 RID: 441
		private static readonly Dictionary<string, string[]> CachedFields;

		// Token: 0x040001BB RID: 443
		private WeakNativeObjectReference _scene;

		// Token: 0x020000D0 RID: 208
		[Flags]
		public enum TickRequirement : uint
		{
			// Token: 0x0400043B RID: 1083
			None = 0U,
			// Token: 0x0400043C RID: 1084
			TickOccasionally = 1U,
			// Token: 0x0400043D RID: 1085
			Tick = 2U,
			// Token: 0x0400043E RID: 1086
			TickParallel = 4U,
			// Token: 0x0400043F RID: 1087
			TickParallel2 = 8U,
			// Token: 0x04000440 RID: 1088
			FixedTick = 16U,
			// Token: 0x04000441 RID: 1089
			FixedParallelTick = 32U,
			// Token: 0x04000442 RID: 1090
			TickParallel3 = 64U
		}
	}
}
