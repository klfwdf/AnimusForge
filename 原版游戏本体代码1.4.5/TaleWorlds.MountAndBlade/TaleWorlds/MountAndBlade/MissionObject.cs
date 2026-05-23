using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionObject : ScriptComponentBehavior
{
	protected enum DynamicNavmeshLocalIds
	{
		Inside = 1,
		Enter,
		Exit,
		Blocker,
		Extra1,
		Extra2,
		Extra3,
		ConditionalBlocker,
		Reserved1,
		Count
	}

	public const int MaxNavMeshPerDynamicObject = 50;

	[EditableScriptComponentVariable(true, "")]
	protected string NavMeshPrefabName = "";

	protected int DynamicNavmeshIdStart;

	private Mission Mission => Mission.Current;

	public MissionObjectId Id { get; set; }

	public bool IsDisabled { get; private set; }

	public virtual TextObject HitObjectName { get; }

	public bool CreatedAtRuntime => Id.CreatedAtRuntime;

	public MissionObject()
	{
		MissionObjectId id = new MissionObjectId(-1);
		Id = id;
	}

	public virtual void SetAbilityOfFaces(bool enabled)
	{
		if (DynamicNavmeshIdStart > 0)
		{
			for (int i = DynamicNavmeshIdStart; i < DynamicNavmeshIdStart + 10; i++)
			{
				base.GameEntity.Scene.SetAbilityOfFacesWithId(i, enabled);
			}
		}
	}

	protected void SetAbilityOfConditionalFaces(bool enabled)
	{
		if (DynamicNavmeshIdStart > 0)
		{
			base.GameEntity.Scene.SetAbilityOfFacesWithId(DynamicNavmeshIdStart + 8, enabled);
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		if (!GameNetwork.IsClientOrReplay)
		{
			AttachDynamicNavmeshToEntity();
			SetAbilityOfFaces(base.GameEntity.IsValid && base.GameEntity.IsVisibleIncludeParents());
			SetAbilityOfConditionalFaces(base.GameEntity.IsValid && base.GameEntity.IsVisibleIncludeParents());
		}
	}

	protected virtual void AttachDynamicNavmeshToEntity()
	{
		if (NavMeshPrefabName.Length > 0)
		{
			DynamicNavmeshIdStart = Mission.Current.GetNextDynamicNavMeshIdStart();
			base.GameEntity.Scene.ImportNavigationMeshPrefab(NavMeshPrefabName, DynamicNavmeshIdStart);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 1, isConnected: false);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 2, isConnected: true);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 3, isConnected: true);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 4, isConnected: false, isBlocker: true, autoLocalize: false, finalizeBlockerConvexHullComputation: false, updateEntityFrame: false);
			GetEntityToAttachNavMeshFaces().AttachNavigationMeshFaces(DynamicNavmeshIdStart + 8, isConnected: false, isBlocker: true, autoLocalize: false, finalizeBlockerConvexHullComputation: true);
			SetAbilityOfFaces(base.GameEntity.IsValid && base.GameEntity.GetPhysicsState());
		}
	}

	protected virtual WeakGameEntity GetEntityToAttachNavMeshFaces()
	{
		return base.GameEntity;
	}

	protected internal override bool OnCheckForProblems()
	{
		base.OnCheckForProblems();
		bool result = false;
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		children.Add(base.GameEntity);
		base.GameEntity.GetChildrenRecursive(ref children);
		bool flag = false;
		foreach (WeakGameEntity item in children)
		{
			flag = flag || (item.HasPhysicsDefinitionWithoutFlags(1) && !item.PhysicsDescBodyFlag.HasAnyFlag(BodyFlags.CommonCollisionExcludeFlagsForMissile));
		}
		Vec3 scaleVector = base.GameEntity.GetGlobalFrame().rotation.GetScaleVector();
		bool flag2 = !(MathF.Abs(scaleVector.x - scaleVector.y) < 0.01f) || !(MathF.Abs(scaleVector.x - scaleVector.z) < 0.01f);
		if (flag && flag2)
		{
			MBEditor.AddEntityWarning(base.GameEntity, "Mission object has non-uniform scale and physics object. This is not supported because any attached focusable item to this mesh will not work within this configuration.");
			result = true;
		}
		return result;
	}

	protected internal override void OnPreInit()
	{
		base.OnPreInit();
		if (Mission != null)
		{
			int id = -1;
			bool createdAtRuntime;
			if (Mission.IsLoadingFinished)
			{
				createdAtRuntime = true;
				if (!GameNetwork.IsClientOrReplay)
				{
					id = Mission.GetFreeRuntimeMissionObjectId();
				}
			}
			else
			{
				createdAtRuntime = false;
				id = Mission.GetFreeSceneMissionObjectId();
			}
			Id = new MissionObjectId(id, createdAtRuntime);
			Mission.AddActiveMissionObject(this);
		}
		base.GameEntity.SetAsReplayEntity();
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("batched_physics_entity");
		if (firstChildEntityWithTag != WeakGameEntity.Invalid)
		{
			firstChildEntityWithTag.CreateVariableRatePhysics(forChildren: true);
		}
		_ = base.GameEntity.Name;
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			_ = child.BodyFlag;
			if (child != firstChildEntityWithTag)
			{
				child.CreateVariableRatePhysics(forChildren: true);
			}
		}
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}

	protected internal virtual void OnMissionReset()
	{
	}

	public virtual void AfterMissionStart()
	{
	}

	public virtual void OnMissionEnded()
	{
	}

	public virtual void OnDeploymentFinished()
	{
	}

	protected internal virtual bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage, out float finalDamage, out float fireDamage, out float modifiedFireDamage)
	{
		reportDamage = false;
		finalDamage = damage;
		fireDamage = -1f;
		modifiedFireDamage = -1f;
		return false;
	}

	public void SetEnabled(bool isParentObject = false)
	{
		if (!IsDisabled)
		{
			return;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			SetAbilityOfFaces(enabled: true);
		}
		if (isParentObject && base.GameEntity != null)
		{
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
			foreach (MissionObject item in from sc in children.SelectMany((WeakGameEntity ac) => ac.GetScriptComponents())
				where sc is MissionObject
				select sc as MissionObject)
			{
				item.SetEnabled();
			}
		}
		Mission.Current.ActivateMissionObject(this);
		IsDisabled = false;
	}

	public void SetEnabledAndMakeVisible(bool isParentObject = false, bool enableFaces = false)
	{
		SetEnabledAndMakeVisibleAux(isParentObject, enableFaces);
		SetScriptComponentToTick(GetTickRequirement());
		if (!(base.GameEntity != null))
		{
			return;
		}
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			int scriptCount = item.GetScriptCount();
			for (int i = 0; i < scriptCount; i++)
			{
				ScriptComponentBehavior scriptAtIndex = item.GetScriptAtIndex(i);
				scriptAtIndex?.SetScriptComponentToTick(scriptAtIndex.GetTickRequirement());
			}
		}
	}

	private void SetEnabledAndMakeVisibleAux(bool isParentObject, bool enableFaces)
	{
		if (enableFaces && !GameNetwork.IsClientOrReplay)
		{
			SetAbilityOfFaces(enabled: true);
		}
		if (isParentObject && base.GameEntity != null)
		{
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
			foreach (MissionObject item in children.SelectMany((WeakGameEntity ac) => ac.GetScriptComponents<MissionObject>()))
			{
				item.SetEnabledAndMakeVisibleAux(isParentObject: false, enableFaces);
			}
		}
		Mission.Current.ActivateMissionObject(this);
		IsDisabled = false;
		if (base.GameEntity != null)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: true);
			base.GameEntity.SetPhysicsState(isEnabled: true, setChildren: false);
		}
	}

	public void SetDisabled(bool isParentObject = false)
	{
		if (IsDisabled)
		{
			return;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			SetAbilityOfFaces(enabled: false);
		}
		if (isParentObject && base.GameEntity.IsValid)
		{
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
			foreach (MissionObject item in children.SelectMany((WeakGameEntity ac) => ac.GetScriptComponents<MissionObject>()))
			{
				item.SetDisabled();
			}
		}
		Mission.Current.DeactivateMissionObject(this);
		IsDisabled = true;
	}

	public void SetDisabledAndMakeInvisible(bool isParentObject = false, bool disableFaces = false)
	{
		if (disableFaces && !GameNetwork.IsClientOrReplay)
		{
			SetAbilityOfFaces(enabled: false);
		}
		if (isParentObject && base.GameEntity.IsValid)
		{
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
			foreach (MissionObject item in children.SelectMany((WeakGameEntity ac) => ac.GetScriptComponents<MissionObject>()))
			{
				item.SetDisabledAndMakeInvisible(isParentObject: false, disableFaces);
			}
		}
		Mission.Current.DeactivateMissionObject(this);
		IsDisabled = true;
		if (base.GameEntity.IsValid)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: false);
			base.GameEntity.SetPhysicsState(isEnabled: false, setChildren: false);
			SetScriptComponentToTick(GetTickRequirement());
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		if (!GameNetwork.IsClientOrReplay)
		{
			SetAbilityOfFaces(enabled: false);
		}
		if (Mission != null)
		{
			Mission.OnMissionObjectRemoved(this, removeReason);
		}
	}

	public virtual void OnEndMission()
	{
	}

	protected internal override bool MovesEntity()
	{
		return true;
	}

	public virtual void AddStuckMissile(GameEntity missileEntity)
	{
		base.GameEntity.AddChild(missileEntity.WeakEntity);
	}
}
