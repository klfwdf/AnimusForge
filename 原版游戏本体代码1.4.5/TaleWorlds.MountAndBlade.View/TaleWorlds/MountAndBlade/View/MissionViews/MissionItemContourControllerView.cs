using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionItemContourControllerView : MissionView
{
	private const float SceneItemQueryFreq = 1f;

	private readonly WeakGameEntity[] _tempPickableEntities = new WeakGameEntity[128];

	private readonly UIntPtr[] _pickableItemsId = new UIntPtr[128];

	private readonly List<GameEntity> _contourItems = new List<GameEntity>();

	private GameEntity _focusedGameEntity;

	private IFocusable _currentFocusedObject;

	private bool _isContourAppliedToAllItems;

	private bool _isContourAppliedToFocusedItem;

	private readonly uint _nonFocusedDefaultContourColor = new Color(0.85f, 0.85f, 0.85f).ToUnsignedInteger();

	private readonly uint _nonFocusedAmmoContourColor = new Color(0f, 0.73f, 1f).ToUnsignedInteger();

	private readonly uint _nonFocusedThrowableContourColor = new Color(0.051f, 0.988f, 0.18f).ToUnsignedInteger();

	private readonly uint _nonFocusedBannerContourColor = new Color(0.521f, 0.988f, 0.521f).ToUnsignedInteger();

	private readonly uint _focusedContourColor = new Color(1f, 0.84f, 0.35f).ToUnsignedInteger();

	private float _lastItemQueryTime;

	private static bool IsAllowedByOption
	{
		get
		{
			if (BannerlordConfig.HideBattleUI)
			{
				return GameNetwork.IsMultiplayer;
			}
			return true;
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!IsAllowedByOption)
		{
			return;
		}
		if (Agent.Main != null && base.MissionScreen.InputManager.IsGameKeyDown(5))
		{
			RemoveContourFromAllItems();
			PopulateContourListWithNearbyItems();
			ApplyContourToAllItems();
			_lastItemQueryTime = base.Mission.CurrentTime;
		}
		else
		{
			RemoveContourFromAllItems();
			_contourItems.Clear();
		}
		if (_isContourAppliedToAllItems)
		{
			float currentTime = base.Mission.CurrentTime;
			if (currentTime - _lastItemQueryTime > 1f)
			{
				RemoveContourFromAllItems();
				PopulateContourListWithNearbyItems();
				_lastItemQueryTime = currentTime;
			}
		}
	}

	public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
	{
		base.OnFocusGained(agent, focusableObject, isInteractable);
		if (!(IsAllowedByOption && focusableObject != _currentFocusedObject && isInteractable))
		{
			return;
		}
		_currentFocusedObject = focusableObject;
		if (focusableObject is UsableMissionObject usableMissionObject)
		{
			if (usableMissionObject is SpawnedItemEntity spawnedItemEntity)
			{
				_focusedGameEntity = GameEntity.CreateFromWeakEntity(spawnedItemEntity.GameEntity);
			}
			else if (!string.IsNullOrEmpty(usableMissionObject.ActionMessage.ToString()) && !string.IsNullOrEmpty(usableMissionObject.DescriptionMessage.ToString()))
			{
				_focusedGameEntity = GameEntity.CreateFromWeakEntity(usableMissionObject.GameEntity);
			}
			else
			{
				UsableMachine usableMachineFromPoint = GetUsableMachineFromPoint(usableMissionObject);
				if (usableMachineFromPoint != null)
				{
					_focusedGameEntity = GameEntity.CreateFromWeakEntity(usableMachineFromPoint.GameEntity);
				}
			}
		}
		AddContourToFocusedItem();
	}

	public override void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		base.OnFocusLost(agent, focusableObject);
		if (IsAllowedByOption)
		{
			RemoveContourFromFocusedItem();
			_currentFocusedObject = null;
			_focusedGameEntity = null;
		}
	}

	private void PopulateContourListWithNearbyItems()
	{
		_contourItems.Clear();
		float num = (GameNetwork.IsSessionActive ? 1f : 3f);
		Agent main = Agent.Main;
		float num2 = main.GetMaximumForwardUnlimitedSpeed() * num;
		Vec3 boundingBoxMin = main.Position - new Vec3(num2, num2, 1f);
		Vec3 boundingBoxMax = main.Position + new Vec3(num2, num2, 2.5f);
		Vec3 position = base.MissionScreen.CombatCamera.Position;
		Vec3 position2 = main.Position;
		float num3 = new Vec3(position.x, position.y).Distance(new Vec3(position2.x, position2.y));
		Vec3 vec = position * (1f - num3) + (position + base.MissionScreen.CombatCamera.Direction) * num3;
		int num4 = base.Mission.Scene.SelectEntitiesInBoxWithScriptComponent<SpawnedItemEntity>(ref boundingBoxMin, ref boundingBoxMax, _tempPickableEntities, _pickableItemsId, isFixedTick: false);
		float collisionDistance;
		for (int i = 0; i < num4; i++)
		{
			WeakGameEntity weakGameEntity = _tempPickableEntities[i];
			SpawnedItemEntity firstScriptOfType = weakGameEntity.GetFirstScriptOfType<SpawnedItemEntity>();
			if (firstScriptOfType == null)
			{
				continue;
			}
			Vec3 vec2 = weakGameEntity.ComputeGlobalPhysicsBoundingBoxCenter();
			Vec3 vec3 = (vec2 - vec).NormalizedCopy();
			Vec3 globalPosition = weakGameEntity.GlobalPosition;
			Vec3 vec4 = (globalPosition - vec).NormalizedCopy();
			if ((!base.Mission.Scene.RayCastForClosestEntityOrTerrain(vec + vec3 * 0.2f, vec2, out collisionDistance, out WeakGameEntity collidedEntity, 0.2f, BodyFlags.CommonFocusRayCastExcludeFlags) || !collidedEntity.IsValid || !(collidedEntity == weakGameEntity)) && (!base.Mission.Scene.RayCastForClosestEntityOrTerrain(vec + vec4 * 0.2f, globalPosition, out collisionDistance, out WeakGameEntity collidedEntity2, 0.2f, BodyFlags.CommonFocusRayCastExcludeFlags) || !collidedEntity2.IsValid || !(collidedEntity2 == weakGameEntity)))
			{
				continue;
			}
			if (firstScriptOfType.IsBanner())
			{
				if (MissionGameModels.Current.BattleBannerBearersModel.IsInteractableFormationBanner(firstScriptOfType, main))
				{
					_contourItems.Add(GameEntity.CreateFromWeakEntity(weakGameEntity));
				}
			}
			else
			{
				_contourItems.Add(GameEntity.CreateFromWeakEntity(weakGameEntity));
			}
		}
		int num5 = base.Mission.Scene.SelectEntitiesInBoxWithScriptComponent<SpawnedItemEntity>(ref boundingBoxMin, ref boundingBoxMax, _tempPickableEntities, _pickableItemsId, isFixedTick: true);
		for (int j = 0; j < num5; j++)
		{
			WeakGameEntity weakGameEntity2 = _tempPickableEntities[j];
			SpawnedItemEntity firstScriptOfType2 = weakGameEntity2.GetFirstScriptOfType<SpawnedItemEntity>();
			if (firstScriptOfType2 == null)
			{
				continue;
			}
			Vec3 vec5 = weakGameEntity2.ComputeGlobalPhysicsBoundingBoxCenter();
			Vec3 vec6 = (vec5 - vec).NormalizedCopy();
			Vec3 globalPosition2 = weakGameEntity2.GlobalPosition;
			Vec3 vec7 = (globalPosition2 - vec).NormalizedCopy();
			if ((!base.Mission.Scene.RayCastForClosestEntityOrTerrainFixedPhysics(vec + vec6 * 0.2f, vec5, out collisionDistance, out WeakGameEntity collidedEntity3, 0.2f, BodyFlags.CommonFocusRayCastExcludeFlags) || !collidedEntity3.IsValid || !(collidedEntity3 == weakGameEntity2)) && (!base.Mission.Scene.RayCastForClosestEntityOrTerrainFixedPhysics(vec + vec7 * 0.2f, globalPosition2, out collisionDistance, out WeakGameEntity collidedEntity4, 0.2f, BodyFlags.CommonFocusRayCastExcludeFlags) || !collidedEntity4.IsValid || !(collidedEntity4 == weakGameEntity2)))
			{
				continue;
			}
			if (firstScriptOfType2.IsBanner())
			{
				if (MissionGameModels.Current.BattleBannerBearersModel.IsInteractableFormationBanner(firstScriptOfType2, main))
				{
					_contourItems.Add(GameEntity.CreateFromWeakEntity(weakGameEntity2));
				}
			}
			else
			{
				_contourItems.Add(GameEntity.CreateFromWeakEntity(weakGameEntity2));
			}
		}
		int num6 = base.Mission.Scene.SelectEntitiesInBoxWithScriptComponent<UsableMachine>(ref boundingBoxMin, ref boundingBoxMax, _tempPickableEntities, _pickableItemsId, isFixedTick: false);
		for (int k = 0; k < num6; k++)
		{
			WeakGameEntity weakEntity = _tempPickableEntities[k];
			UsableMachine firstScriptOfType3 = weakEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType3 != null && !firstScriptOfType3.IsDisabled)
			{
				WeakGameEntity validStandingPointForAgentWithoutDistanceCheck = firstScriptOfType3.GetValidStandingPointForAgentWithoutDistanceCheck(main);
				if (validStandingPointForAgentWithoutDistanceCheck.IsValid && !(validStandingPointForAgentWithoutDistanceCheck.GetFirstScriptOfType<UsableMissionObject>() is SpawnedItemEntity) && validStandingPointForAgentWithoutDistanceCheck.GetScriptComponents().FirstOrDefault((ScriptComponentBehavior sc) => sc is IFocusable) is IFocusable focusable && focusable is UsableMissionObject)
				{
					_contourItems.Add(GameEntity.CreateFromWeakEntity(weakEntity));
				}
			}
		}
	}

	private void ApplyContourToAllItems()
	{
		if (_isContourAppliedToAllItems)
		{
			return;
		}
		foreach (GameEntity contourItem in _contourItems)
		{
			uint nonFocusedColor = GetNonFocusedColor(contourItem);
			uint value = ((contourItem == _focusedGameEntity) ? _focusedContourColor : nonFocusedColor);
			contourItem.SetContourColor(value);
		}
		_isContourAppliedToAllItems = true;
	}

	private uint GetNonFocusedColor(GameEntity entity)
	{
		ItemObject obj = entity.GetFirstScriptOfType<SpawnedItemEntity>()?.WeaponCopy.Item;
		WeaponComponentData weaponComponentData = obj?.PrimaryWeapon;
		ItemObject.ItemTypeEnum? itemTypeEnum = obj?.ItemType;
		if (obj != null && obj.HasBannerComponent)
		{
			return _nonFocusedBannerContourColor;
		}
		if ((weaponComponentData != null && weaponComponentData.IsAmmo) || itemTypeEnum == ItemObject.ItemTypeEnum.Arrows || itemTypeEnum == ItemObject.ItemTypeEnum.Bolts || itemTypeEnum == ItemObject.ItemTypeEnum.SlingStones || itemTypeEnum == ItemObject.ItemTypeEnum.Bullets)
		{
			return _nonFocusedAmmoContourColor;
		}
		if (itemTypeEnum == ItemObject.ItemTypeEnum.Thrown)
		{
			return _nonFocusedThrowableContourColor;
		}
		return _nonFocusedDefaultContourColor;
	}

	private void RemoveContourFromAllItems()
	{
		if (!_isContourAppliedToAllItems)
		{
			return;
		}
		foreach (GameEntity contourItem in _contourItems)
		{
			if (_focusedGameEntity == null || contourItem != _focusedGameEntity)
			{
				contourItem.SetContourColor(null);
			}
		}
		_isContourAppliedToAllItems = false;
	}

	private void AddContourToFocusedItem()
	{
		if (_focusedGameEntity != null && !_isContourAppliedToFocusedItem)
		{
			_focusedGameEntity.SetContourColor(_focusedContourColor);
			_isContourAppliedToFocusedItem = true;
		}
	}

	private void RemoveContourFromFocusedItem()
	{
		if (_focusedGameEntity != null && _isContourAppliedToFocusedItem)
		{
			if (_contourItems.Contains(_focusedGameEntity))
			{
				_focusedGameEntity.SetContourColor(_nonFocusedDefaultContourColor);
			}
			else
			{
				_focusedGameEntity.SetContourColor(null);
			}
			_isContourAppliedToFocusedItem = false;
		}
	}

	private UsableMachine GetUsableMachineFromPoint(UsableMissionObject standingPoint)
	{
		WeakGameEntity weakGameEntity = standingPoint.GameEntity;
		while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
		{
			weakGameEntity = weakGameEntity.Parent;
		}
		if (weakGameEntity.IsValid)
		{
			UsableMachine firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType != null)
			{
				return firstScriptOfType;
			}
		}
		return null;
	}
}
