using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade;

public class WallSegment : SynchedMissionObject, IPointDefendable, ICastleKeyPosition
{
	private const string WaitPositionTag = "wait_pos";

	private const string MiddlePositionTag = "middle_pos";

	private const string AttackerWaitPositionTag = "attacker_wait_pos";

	private const string SolidChildTag = "solid_child";

	private const string BrokenChildTag = "broken_child";

	[EditableScriptComponentVariable(true, "")]
	private int _properGroundOutsideNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _properGroundInsideNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _underDebrisOutsideNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _underDebrisInsideNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _overDebrisOutsideNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _overDebrisInsideNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _underDebrisGenericNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _overDebrisGenericNavmeshID = -1;

	[EditableScriptComponentVariable(true, "")]
	private int _onSolidWallGenericNavmeshID = -1;

	public string SideTag;

	public TacticalPosition MiddlePosition { get; private set; }

	public TacticalPosition WaitPosition { get; private set; }

	public TacticalPosition AttackerWaitPosition { get; private set; }

	public IPrimarySiegeWeapon AttackerSiegeWeapon { get; set; }

	public IEnumerable<DefencePoint> DefencePoints { get; protected set; }

	public bool IsBreachedWall { get; private set; }

	public WorldFrame MiddleFrame { get; private set; }

	public WorldFrame DefenseWaitFrame { get; private set; }

	public WorldFrame AttackerWaitFrame { get; private set; } = WorldFrame.Invalid;

	public FormationAI.BehaviorSide DefenseSide { get; private set; }

	public Vec3 GetPosition()
	{
		return base.GameEntity.GlobalPosition;
	}

	public WallSegment()
	{
		AttackerSiegeWeapon = null;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		switch (SideTag)
		{
		case "left":
			DefenseSide = FormationAI.BehaviorSide.Left;
			break;
		case "middle":
			DefenseSide = FormationAI.BehaviorSide.Middle;
			break;
		case "right":
			DefenseSide = FormationAI.BehaviorSide.Right;
			break;
		default:
			DefenseSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
			break;
		}
		WeakGameEntity weakGameEntity = base.GameEntity.GetChildren().FirstOrDefault((WeakGameEntity ce) => ce.HasTag("solid_child"));
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		List<WeakGameEntity> list2 = new List<WeakGameEntity>();
		if (weakGameEntity.IsValid)
		{
			list = weakGameEntity.CollectChildrenEntitiesWithTag("middle_pos");
			list2 = weakGameEntity.CollectChildrenEntitiesWithTag("wait_pos");
		}
		else
		{
			list = base.GameEntity.CollectChildrenEntitiesWithTag("middle_pos");
			list2 = base.GameEntity.CollectChildrenEntitiesWithTag("wait_pos");
		}
		MatrixFrame globalFrame;
		if (list.Count > 0)
		{
			WeakGameEntity weakGameEntity2 = list[0];
			MiddlePosition = weakGameEntity2.GetFirstScriptOfType<TacticalPosition>();
			globalFrame = weakGameEntity2.GetGlobalFrame();
		}
		else
		{
			globalFrame = base.GameEntity.GetGlobalFrame();
		}
		MiddleFrame = new WorldFrame(globalFrame.rotation, globalFrame.origin.ToWorldPosition());
		if (list2.Count > 0)
		{
			WeakGameEntity weakGameEntity3 = list2[0];
			WaitPosition = weakGameEntity3.GetFirstScriptOfType<TacticalPosition>();
			globalFrame = weakGameEntity3.GetGlobalFrame();
			DefenseWaitFrame = new WorldFrame(globalFrame.rotation, globalFrame.origin.ToWorldPosition());
		}
		else
		{
			DefenseWaitFrame = MiddleFrame;
		}
	}

	protected internal override bool MovesEntity()
	{
		return false;
	}

	public void OnChooseUsedWallSegment(bool isBroken)
	{
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("solid_child");
		WeakGameEntity firstChildEntityWithTag2 = base.GameEntity.GetFirstChildEntityWithTag("broken_child");
		Scene scene = base.GameEntity.Scene;
		if (isBroken)
		{
			firstChildEntityWithTag.GetFirstScriptOfType<WallSegment>().SetDisabledSynched();
			firstChildEntityWithTag2.GetFirstScriptOfType<WallSegment>().SetVisibleSynched(value: true);
			if (!GameNetwork.IsClientOrReplay)
			{
				if (_properGroundOutsideNavmeshID > 0 && _underDebrisOutsideNavmeshID > 0)
				{
					scene.SeparateFacesWithId(_properGroundOutsideNavmeshID, _underDebrisOutsideNavmeshID);
				}
				if (_properGroundInsideNavmeshID > 0 && _underDebrisInsideNavmeshID > 0)
				{
					scene.SeparateFacesWithId(_properGroundInsideNavmeshID, _underDebrisInsideNavmeshID);
				}
				if (_underDebrisOutsideNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_underDebrisOutsideNavmeshID, isEnabled: false);
				}
				if (_underDebrisInsideNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_underDebrisInsideNavmeshID, isEnabled: false);
				}
				if (_underDebrisGenericNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_underDebrisGenericNavmeshID, isEnabled: false);
				}
				if (_overDebrisOutsideNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_overDebrisOutsideNavmeshID, isEnabled: true);
					if (_properGroundOutsideNavmeshID > 0)
					{
						scene.MergeFacesWithId(_overDebrisOutsideNavmeshID, _properGroundOutsideNavmeshID, 0);
					}
				}
				if (_overDebrisInsideNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_overDebrisInsideNavmeshID, isEnabled: true);
					if (_properGroundInsideNavmeshID > 0)
					{
						scene.MergeFacesWithId(_overDebrisInsideNavmeshID, _properGroundInsideNavmeshID, 1);
					}
				}
				if (_overDebrisGenericNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_overDebrisGenericNavmeshID, isEnabled: true);
				}
				if (_onSolidWallGenericNavmeshID > 0)
				{
					scene.SetAbilityOfFacesWithId(_onSolidWallGenericNavmeshID, isEnabled: false);
				}
				foreach (StrategicArea item in from c in firstChildEntityWithTag.GetChildren()
					where c.HasScriptOfType<StrategicArea>()
					select c.GetFirstScriptOfType<StrategicArea>())
				{
					item.OnParentGameEntityVisibilityChanged(isVisible: false);
				}
				foreach (StrategicArea item2 in from c in firstChildEntityWithTag2.GetChildren()
					where c.HasScriptOfType<StrategicArea>()
					select c.GetFirstScriptOfType<StrategicArea>())
				{
					item2.OnParentGameEntityVisibilityChanged(isVisible: true);
				}
			}
			IsBreachedWall = true;
			List<WeakGameEntity> list = firstChildEntityWithTag2.CollectChildrenEntitiesWithTag("middle_pos");
			if (list.Count > 0)
			{
				WeakGameEntity weakGameEntity = list.FirstOrDefault();
				MiddlePosition = weakGameEntity.GetFirstScriptOfType<TacticalPosition>();
				MatrixFrame globalFrame = weakGameEntity.GetGlobalFrame();
				MiddleFrame = new WorldFrame(globalFrame.rotation, globalFrame.origin.ToWorldPosition());
			}
			else
			{
				MBDebug.ShowWarning("Broken child of wall does not have middle position");
				MatrixFrame globalFrame2 = firstChildEntityWithTag2.GetGlobalFrame();
				MiddleFrame = new WorldFrame(globalFrame2.rotation, new WorldPosition(scene, UIntPtr.Zero, globalFrame2.origin, hasValidZ: false));
			}
			List<WeakGameEntity> list2 = firstChildEntityWithTag2.CollectChildrenEntitiesWithTag("wait_pos");
			if (list2.Count > 0)
			{
				WeakGameEntity weakGameEntity2 = list2.FirstOrDefault();
				WaitPosition = weakGameEntity2.GetFirstScriptOfType<TacticalPosition>();
				MatrixFrame globalFrame3 = weakGameEntity2.GetGlobalFrame();
				DefenseWaitFrame = new WorldFrame(globalFrame3.rotation, globalFrame3.origin.ToWorldPosition());
			}
			else
			{
				DefenseWaitFrame = MiddleFrame;
			}
			firstChildEntityWithTag.GetFirstScriptOfType<WallSegment>()?.SetDisabledAndMakeInvisible(isParentObject: true);
			WeakGameEntity weakGameEntity3 = firstChildEntityWithTag2.CollectChildrenEntitiesWithTag("attacker_wait_pos").FirstOrDefault();
			if (weakGameEntity3.IsValid)
			{
				MatrixFrame globalFrame4 = weakGameEntity3.GetGlobalFrame();
				AttackerWaitFrame = new WorldFrame(globalFrame4.rotation, globalFrame4.origin.ToWorldPosition());
				AttackerWaitPosition = weakGameEntity3.GetFirstScriptOfType<TacticalPosition>();
			}
		}
		else
		{
			if (GameNetwork.IsClientOrReplay)
			{
				return;
			}
			firstChildEntityWithTag.GetFirstScriptOfType<WallSegment>().SetVisibleSynched(value: true);
			firstChildEntityWithTag2.GetFirstScriptOfType<WallSegment>().SetDisabledSynched();
			if (_overDebrisOutsideNavmeshID > 0)
			{
				scene.SetAbilityOfFacesWithId(_overDebrisOutsideNavmeshID, isEnabled: false);
			}
			if (_overDebrisInsideNavmeshID > 0)
			{
				scene.SetAbilityOfFacesWithId(_overDebrisInsideNavmeshID, isEnabled: false);
			}
			if (_overDebrisGenericNavmeshID > 0)
			{
				scene.SetAbilityOfFacesWithId(_overDebrisGenericNavmeshID, isEnabled: false);
			}
			foreach (StrategicArea item3 in from c in firstChildEntityWithTag.GetChildren()
				where c.HasScriptOfType<StrategicArea>()
				select c.GetFirstScriptOfType<StrategicArea>())
			{
				item3.OnParentGameEntityVisibilityChanged(isVisible: true);
			}
			foreach (StrategicArea item4 in from c in firstChildEntityWithTag2.GetChildren()
				where c.HasScriptOfType<StrategicArea>()
				select c.GetFirstScriptOfType<StrategicArea>())
			{
				item4.OnParentGameEntityVisibilityChanged(isVisible: false);
			}
		}
	}

	protected internal override void OnEditorValidate()
	{
		base.OnEditorValidate();
	}

	protected internal override bool OnCheckForProblems()
	{
		bool result = base.OnCheckForProblems();
		if (!base.Scene.IsMultiplayerScene() && SideTag == "left")
		{
			List<GameEntity> entities = new List<GameEntity>();
			base.Scene.GetEntities(ref entities);
			int num = 0;
			foreach (GameEntity item in entities)
			{
				if (base.GameEntity.GetUpgradeLevelOfEntity() == item.GetUpgradeLevelOfEntity() && item.GetFirstScriptOfType<SiegeLadderSpawner>() != null)
				{
					num++;
				}
			}
			if (num != 4)
			{
				MBEditor.AddEntityWarning(base.GameEntity, "The siege ladder count in the scene is not 4, for upgrade level " + base.GameEntity.GetUpgradeLevelOfEntity() + ". Current siege ladder count: " + num);
				result = true;
			}
		}
		return result;
	}
}
