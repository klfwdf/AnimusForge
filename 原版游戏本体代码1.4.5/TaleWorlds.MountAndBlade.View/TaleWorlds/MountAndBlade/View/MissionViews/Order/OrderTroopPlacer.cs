using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Order;

public class OrderTroopPlacer : MissionView
{
	public enum CursorState
	{
		Invisible,
		Normal,
		Ground,
		Rotation,
		Count,
		OrderableEntity
	}

	private bool _suspendTroopPlacer;

	public bool IsDrawingForced;

	public bool IsDrawingFacing;

	public bool IsDrawingForming;

	public Action OnUnitDeployed;

	private bool _isMouseDown;

	private List<GameEntity> _orderPositionEntities;

	private List<GameEntity> _orderRotationEntities;

	private bool _formationDrawingMode;

	private Formation _mouseOverFormation;

	private Vec2 _lastMousePosition;

	private Vec2 _deltaMousePosition;

	private int _mouseOverDirection;

	private WorldPosition? _formationDrawingStartingPosition;

	private Vec2? _formationDrawingStartingPointOfMouse;

	private float? _formationDrawingStartingTime;

	private bool _restrictOrdersToDeploymentBoundaries;

	private bool _initialized;

	private Timer formationDrawTimer;

	private bool _wasDrawingForced;

	private bool _wasDrawingFacing;

	private bool _wasDrawingForming;

	private GameEntity _widthEntityLeft;

	private GameEntity _widthEntityRight;

	private bool _isDrawnThisFrame;

	private bool _wasDrawnPreviousFrame;

	private OrderController _orderController;

	public bool SuspendTroopPlacer
	{
		get
		{
			return _suspendTroopPlacer;
		}
		set
		{
			_suspendTroopPlacer = value;
			if (value)
			{
				HideOrderPositionEntities();
			}
			else
			{
				_formationDrawingStartingPosition = null;
			}
			Reset();
		}
	}

	public OrderFlag OrderFlag { get; private set; }

	private Team _playerTeam => base.Mission.PlayerTeam;

	protected CursorState ActiveCursorState { get; private set; }

	protected OrderController OrderController
	{
		get
		{
			if (_orderController != null)
			{
				return _orderController;
			}
			return Mission.Current.PlayerTeam.PlayerOrderController;
		}
	}

	private bool IsDeployment
	{
		get
		{
			Mission mission = base.Mission;
			if (mission == null)
			{
				return false;
			}
			return mission.Mode == MissionMode.Deployment;
		}
	}

	public OrderTroopPlacer(OrderController orderController)
	{
		_orderController = orderController;
	}

	protected virtual OrderFlag CreateOrderFlag()
	{
		return new OrderFlag(base.Mission, base.MissionScreen);
	}

	protected virtual bool CanUpdate()
	{
		return OrderController.SelectedFormations.Count > 0;
	}

	protected virtual bool HasSelectedFormations()
	{
		return OrderController.SelectedFormations.Count > 0;
	}

	protected virtual CursorState GetCursorState()
	{
		CursorState cursorState = CursorState.Invisible;
		if (HasSelectedFormations())
		{
			if (!TryGetScreenMiddleToWorldPosition(out var _, out var collisionDistance, out var collidedEntity))
			{
				collisionDistance = 1000f;
			}
			if (cursorState == CursorState.Invisible && collisionDistance < 1000f)
			{
				if (!_formationDrawingMode && !collidedEntity.IsValid)
				{
					for (int i = 0; i < _orderRotationEntities.Count; i++)
					{
						GameEntity gameEntity = _orderRotationEntities[i];
						if (gameEntity.IsVisibleIncludeParents() && collidedEntity == gameEntity)
						{
							_mouseOverFormation = OrderController.SelectedFormations.ElementAt(i / 2);
							_mouseOverDirection = 1 - (i & 1);
							cursorState = CursorState.Rotation;
							break;
						}
					}
				}
				if (cursorState == CursorState.Invisible && base.MissionScreen.OrderFlag?.FocusedOrderableObject != null)
				{
					cursorState = CursorState.OrderableEntity;
				}
				if (cursorState == CursorState.Invisible)
				{
					cursorState = GetGroundOrNormalCursor();
				}
			}
		}
		if (cursorState != CursorState.Ground && cursorState != CursorState.Rotation)
		{
			_mouseOverDirection = 0;
		}
		return cursorState;
	}

	protected virtual Vec3 GetGroundedVec3(WorldPosition worldPosition)
	{
		return worldPosition.GetGroundVec3();
	}

	protected virtual bool TryGetScreenMiddleToWorldPosition(out WorldPosition worldPosition, out float collisionDistance, out WeakGameEntity collidedEntity)
	{
		base.MissionScreen.ScreenPointToWorldRay(GetScreenPoint(), out var rayBegin, out var rayEnd);
		if (base.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float collisionDistance2, out WeakGameEntity collidedEntity2, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora))
		{
			Vec3 vec = rayEnd - rayBegin;
			vec.Normalize();
			collisionDistance = collisionDistance2;
			collidedEntity = collidedEntity2;
			worldPosition = new WorldPosition(base.Mission.Scene, UIntPtr.Zero, rayBegin + vec * collisionDistance, hasValidZ: false);
			return true;
		}
		worldPosition = WorldPosition.Invalid;
		collisionDistance = 0f;
		collidedEntity = WeakGameEntity.Invalid;
		return false;
	}

	protected bool TryGetScreenMiddleToWorldPosition(out WorldPosition worldPosition, out float collisionDistance)
	{
		WeakGameEntity collidedEntity;
		return TryGetScreenMiddleToWorldPosition(out worldPosition, out collisionDistance, out collidedEntity);
	}

	protected bool TryGetScreenMiddleToWorldPosition(out WorldPosition worldPosition, out WeakGameEntity collidedEntity)
	{
		float collisionDistance;
		return TryGetScreenMiddleToWorldPosition(out worldPosition, out collisionDistance, out collidedEntity);
	}

	protected bool TryGetScreenMiddleToWorldPosition(out WorldPosition worldPosition)
	{
		float collisionDistance;
		WeakGameEntity collidedEntity;
		return TryGetScreenMiddleToWorldPosition(out worldPosition, out collisionDistance, out collidedEntity);
	}

	protected Vec2 GetScreenPoint()
	{
		if (!base.MissionScreen.MouseVisible)
		{
			return new Vec2(0.5f, 0.5f) + _deltaMousePosition;
		}
		return base.Input.GetMousePositionRanged() + _deltaMousePosition;
	}

	public CursorState GetGroundOrNormalCursor()
	{
		if (!_formationDrawingMode)
		{
			return CursorState.Normal;
		}
		return CursorState.Ground;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		OrderFlag = CreateOrderFlag();
		_formationDrawingStartingPosition = null;
		_formationDrawingStartingPointOfMouse = null;
		_formationDrawingStartingTime = null;
		_orderRotationEntities = new List<GameEntity>();
		_orderPositionEntities = new List<GameEntity>();
		formationDrawTimer = new Timer(MBCommon.GetApplicationTime(), 1f / 30f);
		_widthEntityLeft = GameEntity.CreateEmpty(base.Mission.Scene);
		_widthEntityLeft.AddComponent(MetaMesh.GetCopy("order_arrow_a"));
		_widthEntityLeft.SetVisibilityExcludeParents(visible: false);
		_widthEntityRight = GameEntity.CreateEmpty(base.Mission.Scene);
		_widthEntityRight.AddComponent(MetaMesh.GetCopy("order_arrow_a"));
		_widthEntityRight.SetVisibilityExcludeParents(visible: false);
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!_initialized)
		{
			MissionPeer missionPeer = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>() : null);
			if (base.Mission.PlayerTeam != null || (missionPeer != null && (missionPeer.Team == base.Mission.AttackerTeam || missionPeer.Team == base.Mission.DefenderTeam)))
			{
				_initialized = true;
			}
		}
	}

	public void RestrictOrdersToDeploymentBoundaries(bool enabled)
	{
		_restrictOrdersToDeploymentBoundaries = enabled;
	}

	private void UpdateFormationDrawingForFacingOrder(bool giveOrder)
	{
		_isDrawnThisFrame = true;
		Vec2 asVec = base.MissionScreen.GetOrderFlagPosition().AsVec2;
		Vec2 orderLookAtDirection = OrderController.GetOrderLookAtDirection(OrderController.SelectedFormations, asVec);
		OrderController.SimulateNewFacingOrder(orderLookAtDirection, out var simulationAgentFrames);
		int num = 0;
		HideOrderPositionEntities();
		foreach (WorldPosition item in simulationAgentFrames)
		{
			AddOrderPositionEntity(num, GetGroundedVec3(item), giveOrder);
			num++;
		}
	}

	private void UpdateFormationDrawingForDestination(bool giveOrder)
	{
		_isDrawnThisFrame = true;
		OrderController.SimulateDestinationFrames(out var simulationAgentFrames);
		int num = 0;
		HideOrderPositionEntities();
		foreach (WorldPosition item in simulationAgentFrames)
		{
			AddOrderPositionEntity(num, GetGroundedVec3(item), giveOrder, 0.7f);
			num++;
		}
	}

	private void UpdateFormationDrawingForFormingOrder(bool giveOrder)
	{
		_isDrawnThisFrame = true;
		MatrixFrame orderFlagFrame = base.MissionScreen.GetOrderFlagFrame();
		Vec3 origin = orderFlagFrame.origin;
		Vec2 asVec = orderFlagFrame.rotation.f.AsVec2;
		float orderFormCustomWidth = OrderController.GetOrderFormCustomWidth(OrderController.SelectedFormations, origin);
		OrderController.SimulateNewCustomWidthOrder(orderFormCustomWidth, out var simulationAgentFrames);
		Formation formation = TaleWorlds.Core.Extensions.MaxBy(OrderController.SelectedFormations, (Formation f) => f.CountOfUnits);
		int num = 0;
		HideOrderPositionEntities();
		foreach (WorldPosition item in simulationAgentFrames)
		{
			item.GetNavMesh();
			AddOrderPositionEntity(num, GetGroundedVec3(item), giveOrder);
			num++;
		}
		float unitDiameter = formation.UnitDiameter;
		float interval = formation.Interval;
		int num2 = TaleWorlds.Library.MathF.Max(0, (int)((orderFormCustomWidth - unitDiameter) / (interval + unitDiameter) + 1E-05f)) + 1;
		float num3 = (float)(num2 - 1) * (interval + unitDiameter);
		for (int num4 = 0; num4 < num2; num4++)
		{
			Vec2 a = new Vec2((float)num4 * (interval + unitDiameter) - num3 / 2f, 0f);
			Vec2 vec = asVec.TransformToParentUnitF(a);
			WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, origin, hasValidZ: false);
			worldPosition.SetVec2(worldPosition.AsVec2 + vec);
			AddOrderPositionEntity(num++, GetGroundedVec3(worldPosition), fadeOut: false);
		}
	}

	public void UpdateFormationDrawing(bool giveOrder)
	{
		_isDrawnThisFrame = true;
		HideOrderPositionEntities();
		if (!_formationDrawingStartingPosition.HasValue)
		{
			return;
		}
		WorldPosition worldPosition = WorldPosition.Invalid;
		bool flag = false;
		if (base.MissionScreen.MouseVisible && _formationDrawingStartingPointOfMouse.HasValue)
		{
			Vec2 vec = _formationDrawingStartingPointOfMouse.Value - base.Input.GetMousePositionPixel();
			if (TaleWorlds.Library.MathF.Abs(vec.x) < 10f && TaleWorlds.Library.MathF.Abs(vec.y) < 10f)
			{
				flag = true;
				worldPosition = _formationDrawingStartingPosition.Value;
			}
		}
		if (base.MissionScreen.MouseVisible && _formationDrawingStartingTime.HasValue && base.Mission.CurrentTime - _formationDrawingStartingTime.Value < 0.3f)
		{
			flag = true;
			worldPosition = _formationDrawingStartingPosition.Value;
		}
		if (!flag)
		{
			if (!TryGetScreenMiddleToWorldPosition(out var worldPosition2))
			{
				return;
			}
			worldPosition = worldPosition2;
		}
		WorldPosition worldPosition3;
		if (_mouseOverDirection == 1)
		{
			worldPosition3 = worldPosition;
			worldPosition = _formationDrawingStartingPosition.Value;
		}
		else
		{
			worldPosition3 = _formationDrawingStartingPosition.Value;
		}
		if (OrderFlag.IsPositionOnValidGround(worldPosition3) && (!_restrictOrdersToDeploymentBoundaries || !base.Mission.DeploymentPlan.HasDeploymentBoundaries(base.Mission.PlayerTeam) || base.Mission.DeploymentPlan.IsPositionInsideDeploymentBoundaries(base.Mission.PlayerTeam, worldPosition3.AsVec2)))
		{
			bool isFormationLayoutVertical = !base.DebugInput.IsControlDown();
			UpdateFormationDrawingForMovementOrder(giveOrder, worldPosition3, worldPosition, isFormationLayoutVertical);
			_deltaMousePosition *= TaleWorlds.Library.MathF.Max(1f - (base.Input.GetMousePositionRanged() - _lastMousePosition).Length * 10f, 0f);
			_lastMousePosition = base.Input.GetMousePositionRanged();
		}
	}

	private void UpdateFormationDrawingForMovementOrder(bool giveOrder, WorldPosition formationRealStartingPosition, WorldPosition formationRealEndingPosition, bool isFormationLayoutVertical)
	{
		_isDrawnThisFrame = true;
		OrderController.SimulateNewOrderWithPositionAndDirection(formationRealStartingPosition, formationRealEndingPosition, out var simulationAgentFrames, isFormationLayoutVertical);
		if (giveOrder)
		{
			if (!isFormationLayoutVertical)
			{
				OrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegmentWithHorizontalLayout, formationRealStartingPosition, formationRealEndingPosition);
			}
			else
			{
				OrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment, formationRealStartingPosition, formationRealEndingPosition);
			}
		}
		int num = 0;
		foreach (WorldPosition item in simulationAgentFrames)
		{
			AddOrderPositionEntity(num, GetGroundedVec3(item), giveOrder);
			num++;
		}
	}

	private void HandleMouseDown()
	{
		if (!HasSelectedFormations())
		{
			return;
		}
		switch (ActiveCursorState)
		{
		case CursorState.Normal:
		{
			_formationDrawingMode = true;
			if (TryGetScreenMiddleToWorldPosition(out var worldPosition3))
			{
				_formationDrawingStartingPosition = worldPosition3;
				_formationDrawingStartingPointOfMouse = base.Input.GetMousePositionPixel();
				_formationDrawingStartingTime = base.Mission.CurrentTime;
			}
			else
			{
				_formationDrawingStartingPosition = null;
				_formationDrawingStartingPointOfMouse = null;
				_formationDrawingStartingTime = null;
			}
			break;
		}
		case CursorState.Rotation:
			if (_mouseOverFormation.CountOfUnits > 0)
			{
				HideNonSelectedOrderRotationEntities(_mouseOverFormation);
				OrderController.ClearSelectedFormations();
				OrderController.SelectFormation(_mouseOverFormation);
				_formationDrawingMode = true;
				WorldPosition worldPosition = _mouseOverFormation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.GroundVec3);
				Vec2 direction = _mouseOverFormation.Direction;
				direction.RotateCCW(-System.MathF.PI / 2f);
				_formationDrawingStartingPosition = worldPosition;
				_formationDrawingStartingPosition.Value.SetVec2(_formationDrawingStartingPosition.Value.AsVec2 + direction * ((_mouseOverDirection == 1) ? 0.5f : (-0.5f)) * _mouseOverFormation.Width);
				WorldPosition worldPosition2 = worldPosition;
				worldPosition2.SetVec2(worldPosition2.AsVec2 + direction * ((_mouseOverDirection == 1) ? (-0.5f) : 0.5f) * _mouseOverFormation.Width);
				Vec2 vec = base.MissionScreen.SceneView.WorldPointToScreenPoint(GetGroundedVec3(worldPosition2));
				Vec2 screenPoint = GetScreenPoint();
				_deltaMousePosition = vec - screenPoint;
				_lastMousePosition = base.Input.GetMousePositionRanged();
			}
			break;
		case CursorState.Invisible:
		case CursorState.Ground:
			break;
		}
	}

	private void HandleMouseUp()
	{
		if (ActiveCursorState == CursorState.Ground)
		{
			if (IsDrawingFacing || _wasDrawingFacing)
			{
				UpdateFormationDrawingForFacingOrder(giveOrder: true);
			}
			else if (IsDrawingForming || _wasDrawingForming)
			{
				UpdateFormationDrawingForFormingOrder(giveOrder: true);
			}
			else
			{
				UpdateFormationDrawing(giveOrder: true);
			}
			if (IsDeployment)
			{
				OnUnitDeployed?.Invoke();
				UISoundsHelper.PlayUISound("event:/ui/mission/deploy");
			}
		}
		_formationDrawingMode = false;
		_deltaMousePosition = Vec2.Zero;
	}

	private void AddOrderPositionEntity(int entityIndex, in Vec3 groundPosition, bool fadeOut, float alpha = -1f)
	{
		while (_orderPositionEntities.Count <= entityIndex)
		{
			GameEntity gameEntity = GameEntity.CreateEmpty(base.Mission.Scene);
			gameEntity.EntityFlags |= EntityFlags.NotAffectedBySeason;
			MetaMesh copy = MetaMesh.GetCopy("order_flag_small");
			gameEntity.AddComponent(copy);
			gameEntity.SetVisibilityExcludeParents(visible: false);
			_orderPositionEntities.Add(gameEntity);
		}
		GameEntity gameEntity2 = _orderPositionEntities[entityIndex];
		MatrixFrame frame = new MatrixFrame(Mat3.Identity, in groundPosition);
		gameEntity2.SetFrame(ref frame);
		if (alpha != -1f)
		{
			gameEntity2.SetVisibilityExcludeParents(visible: true);
			gameEntity2.SetAlpha(alpha);
		}
		else if (fadeOut)
		{
			gameEntity2.FadeOut(0.3f, isRemovingFromScene: false);
		}
		else
		{
			gameEntity2.FadeIn();
		}
	}

	private void HideNonSelectedOrderRotationEntities(Formation formation)
	{
		for (int i = 0; i < _orderRotationEntities.Count; i++)
		{
			GameEntity gameEntity = _orderRotationEntities[i];
			if (gameEntity == null && gameEntity.IsVisibleIncludeParents() && OrderController.SelectedFormations.ElementAt(i / 2) != formation)
			{
				gameEntity.SetVisibilityExcludeParents(visible: false);
				gameEntity.BodyFlag |= BodyFlags.Disabled;
			}
		}
	}

	private void HideOrderPositionEntities()
	{
		foreach (GameEntity orderPositionEntity in _orderPositionEntities)
		{
			orderPositionEntity.HideIfNotFadingOut();
		}
		for (int i = 0; i < _orderRotationEntities.Count; i++)
		{
			GameEntity gameEntity = _orderRotationEntities[i];
			gameEntity.SetVisibilityExcludeParents(visible: false);
			gameEntity.BodyFlag |= BodyFlags.Disabled;
		}
	}

	[Conditional("DEBUG")]
	private void DebugTick(float dt)
	{
		_ = _initialized;
	}

	private void Reset()
	{
		_isMouseDown = false;
		_formationDrawingMode = false;
		_formationDrawingStartingPosition = null;
		_formationDrawingStartingPointOfMouse = null;
		_formationDrawingStartingTime = null;
		_mouseOverFormation = null;
		ActiveCursorState = GetCursorState();
	}

	public override void OnMissionScreenTick(float dt)
	{
		if (!_initialized)
		{
			return;
		}
		ActiveCursorState = GetCursorState();
		base.OnMissionScreenTick(dt);
		if (!CanUpdate())
		{
			return;
		}
		_isDrawnThisFrame = false;
		if (SuspendTroopPlacer)
		{
			return;
		}
		if (base.Input.IsKeyPressed(InputKey.LeftMouseButton) || base.Input.IsKeyPressed(InputKey.ControllerRTrigger))
		{
			_isMouseDown = true;
			HandleMouseDown();
		}
		if ((base.Input.IsKeyReleased(InputKey.LeftMouseButton) || base.Input.IsKeyReleased(InputKey.ControllerRTrigger)) && _isMouseDown)
		{
			_isMouseDown = false;
			HandleMouseUp();
		}
		else if ((base.Input.IsKeyDown(InputKey.LeftMouseButton) || base.Input.IsKeyDown(InputKey.ControllerRTrigger)) && _isMouseDown)
		{
			if (formationDrawTimer.Check(MBCommon.GetApplicationTime()) && !IsDrawingFacing && !IsDrawingForming && ActiveCursorState == CursorState.Ground && GetGroundOrNormalCursor() == CursorState.Ground)
			{
				UpdateFormationDrawing(giveOrder: false);
			}
		}
		else if (IsDrawingForced)
		{
			if (formationDrawTimer.Check(MBCommon.GetApplicationTime()))
			{
				Reset();
				HandleMouseDown();
				UpdateFormationDrawing(giveOrder: false);
			}
		}
		else if (IsDrawingFacing || _wasDrawingFacing)
		{
			if (IsDrawingFacing)
			{
				Reset();
				UpdateFormationDrawingForFacingOrder(giveOrder: false);
			}
		}
		else if (IsDrawingForming || _wasDrawingForming)
		{
			if (IsDrawingForming)
			{
				Reset();
				UpdateFormationDrawingForFormingOrder(giveOrder: false);
			}
		}
		else if (_wasDrawingForced)
		{
			Reset();
		}
		else
		{
			UpdateFormationDrawingForDestination(giveOrder: false);
		}
		if (!base.Input.IsKeyDown(InputKey.LeftMouseButton) && !base.Input.IsKeyDown(InputKey.ControllerRTrigger) && _isMouseDown)
		{
			Reset();
		}
		foreach (GameEntity orderPositionEntity in _orderPositionEntities)
		{
			orderPositionEntity.SetPreviousFrameInvalid();
		}
		foreach (GameEntity orderRotationEntity in _orderRotationEntities)
		{
			orderRotationEntity.SetPreviousFrameInvalid();
		}
		_wasDrawingForced = IsDrawingForced;
		_wasDrawingFacing = IsDrawingFacing;
		_wasDrawingForming = IsDrawingForming;
		_wasDrawnPreviousFrame = _isDrawnThisFrame;
	}
}
