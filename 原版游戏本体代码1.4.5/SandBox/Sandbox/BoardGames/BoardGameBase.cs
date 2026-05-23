using System;
using System.Collections.Generic;
using SandBox.BoardGames.AI;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.BoardGames;

public abstract class BoardGameBase
{
	public const string StringBoardGame = "str_boardgame";

	public const string StringForfeitQuestion = "str_boardgame_forfeit_question";

	public const string StringMovePiecePlayer = "str_boardgame_move_piece_player";

	public const string StringMovePieceOpponent = "str_boardgame_move_piece_opponent";

	public const string StringCapturePiecePlayer = "str_boardgame_capture_piece_player";

	public const string StringCapturePieceOpponent = "str_boardgame_capture_piece_opponent";

	public const string StringVictoryMessage = "str_boardgame_victory_message";

	public const string StringDefeatMessage = "str_boardgame_defeat_message";

	public const string StringDrawMessage = "str_boardgame_draw_message";

	public const string StringNoAvailableMovesPlayer = "str_boardgame_no_available_moves_player";

	public const string StringNoAvailableMovesOpponent = "str_boardgame_no_available_moves_opponent";

	public const string StringSeegaBarrierByP1DrawMessage = "str_boardgame_seega_barrier_by_player_one_draw_message";

	public const string StringSeegaBarrierByP2DrawMessage = "str_boardgame_seega_barrier_by_player_two_draw_message";

	public const string StringSeegaBarrierByP1VictoryMessage = "str_boardgame_seega_barrier_by_player_one_victory_message";

	public const string StringSeegaBarrierByP2VictoryMessage = "str_boardgame_seega_barrier_by_player_two_victory_message";

	public const string StringSeegaBarrierByP1DefeatMessage = "str_boardgame_seega_barrier_by_player_one_defeat_message";

	public const string StringSeegaBarrierByP2DefeatMessage = "str_boardgame_seega_barrier_by_player_two_defeat_message";

	public const string StringRollDicePlayer = "str_boardgame_roll_dice_player";

	public const string StringRollDiceOpponent = "str_boardgame_roll_dice_opponent";

	protected const int InvalidDice = -1;

	protected const float DelayBeforeMovingAnyPawn = 0.25f;

	protected const float DelayBetweenPawnMovementsBegin = 0.15f;

	private const float DiceRollAnimationDuration = 1f;

	private const float DraggingDuration = 0.2f;

	private const int UnitsToPlacePerTurnInMovementStage = 1;

	protected uint PawnSelectedFactor = uint.MaxValue;

	protected uint PawnUnselectedFactor = 4282203453u;

	protected MissionBoardGameLogic MissionHandler;

	protected GameEntity BoardEntity;

	protected GameEntity DiceBoard;

	protected bool JustStoppedDraggingUnit;

	protected CapturedPawnsPool PlayerOnePool;

	protected bool ReadyToPlay;

	protected CapturedPawnsPool PlayerTwoPool;

	protected bool SettingUpBoard = true;

	protected bool HasToMovePawnsAcross;

	protected float DiceRollAnimationTimer;

	protected int MovesLeftToEndTurn;

	protected bool DiceRollAnimationRunning;

	protected int DiceRollSoundCodeID;

	private List<Move> _validMoves;

	private PawnBase _selectedUnit;

	private Vec3 _userRayBegin;

	private Vec3 _userRayEnd;

	private float _draggingTimer;

	private bool _draggingSelectedUnit;

	private float _rotationApplied;

	private float _rotationTarget;

	private bool _rotationCompleted;

	private bool _deselectUnit;

	private bool _firstTickAfterReady = true;

	private bool _waitingAIForfeitResponse;

	public abstract int TileCount { get; }

	protected abstract bool RotateBoard { get; }

	protected abstract bool PreMovementStagePresent { get; }

	protected abstract bool DiceRollRequired { get; }

	protected virtual int UnitsToPlacePerTurnInPreMovementStage => 1;

	protected virtual PawnBase SelectedUnit
	{
		get
		{
			return _selectedUnit;
		}
		set
		{
			OnBeforeSelectedUnitChanged(_selectedUnit, value);
			_selectedUnit = value;
			OnAfterSelectedUnitChanged();
		}
	}

	public TextObject Name { get; }

	public bool InPreMovementStage { get; protected set; }

	public TileBase[] Tiles { get; protected set; }

	public List<PawnBase> PlayerOneUnits { get; protected set; }

	public List<PawnBase> PlayerTwoUnits { get; protected set; }

	public int LastDice { get; protected set; }

	public bool IsReady
	{
		get
		{
			if (ReadyToPlay)
			{
				return !SettingUpBoard;
			}
			return false;
		}
	}

	public PlayerTurn PlayerWhoStarted { get; private set; }

	public GameOverEnum GameOverInfo { get; private set; }

	public PlayerTurn PlayerTurn { get; protected set; }

	protected IInputContext InputManager => MissionHandler.Mission.InputManager;

	protected List<PawnBase> PawnSelectFilter { get; }

	protected BoardGameAIBase AIOpponent => MissionHandler.AIOpponent;

	private bool DiceRolled => LastDice != -1;

	protected BoardGameBase(MissionBoardGameLogic mission, TextObject name, PlayerTurn startingPlayer)
	{
		Name = name;
		MissionHandler = mission;
		SetStartingPlayer(startingPlayer);
		PlayerOnePool = new CapturedPawnsPool();
		PlayerTwoPool = new CapturedPawnsPool();
		PlayerOneUnits = new List<PawnBase>();
		PlayerTwoUnits = new List<PawnBase>();
		PawnSelectFilter = new List<PawnBase>();
	}

	public abstract void InitializeUnits();

	public abstract void InitializeTiles();

	public abstract void InitializeSound();

	public abstract List<Move> CalculateValidMoves(PawnBase pawn);

	protected abstract PawnBase SelectPawn(PawnBase pawn);

	protected abstract bool CheckGameEnded();

	protected abstract void OnAfterBoardSetUp();

	protected virtual void OnAfterBoardRotated()
	{
	}

	protected virtual void OnBeforeEndTurn()
	{
	}

	public virtual void RollDice()
	{
	}

	protected virtual void UpdateAllTilesPositions()
	{
	}

	public virtual void InitializeDiceBoard()
	{
	}

	public virtual void Reset()
	{
		PlayerOnePool.PawnCount = 0;
		PlayerTwoPool.PawnCount = 0;
		ClearValidMoves();
		SelectedUnit = null;
		PawnSelectFilter.Clear();
		GameOverInfo = GameOverEnum.GameStillInProgress;
		_draggingSelectedUnit = false;
		JustStoppedDraggingUnit = false;
		_draggingTimer = 0f;
		MissionHandler.AIOpponent?.ResetThinking();
		ReadyToPlay = false;
		_firstTickAfterReady = true;
		_rotationCompleted = !RotateBoard;
		SettingUpBoard = true;
		UnfocusAllPawns();
		for (int i = 0; i < TileCount; i++)
		{
			Tiles[i].Reset();
		}
		MovesLeftToEndTurn = ((!PreMovementStagePresent) ? 1 : UnitsToPlacePerTurnInPreMovementStage);
		LastDice = -1;
		_waitingAIForfeitResponse = false;
	}

	protected virtual void OnPawnArrivesGoalPosition(PawnBase pawn, Vec3 prevPos, Vec3 currentPos)
	{
		if (IsReady && pawn.IsPlaced && !pawn.Captured && pawn.MovingToDifferentTile)
		{
			MovesLeftToEndTurn--;
		}
		pawn.MovingToDifferentTile = false;
	}

	protected virtual void HandlePreMovementStage(float dt)
	{
		Debug.FailedAssert("HandlePreMovementStage is not implemented for " + MissionHandler.CurrentBoardGame, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBase.cs", "HandlePreMovementStage", 293);
	}

	public virtual void InitializeCapturedUnitsZones()
	{
		PlayerOnePool.Entity = Mission.Current.Scene.FindEntityWithTag((PlayerWhoStarted == PlayerTurn.PlayerOne) ? "captured_pawns_pool_1" : "captured_pawns_pool_2");
		PlayerOnePool.PawnCount = 0;
		PlayerTwoPool.Entity = Mission.Current.Scene.FindEntityWithTag((PlayerWhoStarted == PlayerTurn.PlayerOne) ? "captured_pawns_pool_2" : "captured_pawns_pool_1");
		PlayerTwoPool.PawnCount = 0;
	}

	protected virtual void HandlePreMovementStageAI(Move move)
	{
		Debug.FailedAssert("HandlePreMovementStageAI is not implemented for " + MissionHandler.CurrentBoardGame, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBase.cs", "HandlePreMovementStageAI", 311);
	}

	public virtual void SetPawnCaptured(PawnBase pawn, bool fake = false)
	{
		pawn.Captured = true;
	}

	public virtual List<List<Move>> CalculateAllValidMoves(BoardGameSide side)
	{
		List<List<Move>> list = new List<List<Move>>(100);
		foreach (PawnBase item in (side == BoardGameSide.AI) ? PlayerTwoUnits : PlayerOneUnits)
		{
			list.Add(CalculateValidMoves(item));
		}
		return list;
	}

	protected virtual void SwitchPlayerTurn()
	{
		MissionHandler.Handler.SwitchTurns();
	}

	protected virtual void MovePawnToTile(PawnBase pawn, TileBase tile, bool instantMove = false, bool displayMessage = true)
	{
		MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, 0f);
	}

	protected virtual void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
	{
		ClearValidMoves();
	}

	protected virtual void OnAfterDiceRollAnimation()
	{
		if (LastDice != -1)
		{
			MissionHandler.Handler.DiceRoll(LastDice);
		}
	}

	public void SetUserRay(Vec3 rayBegin, Vec3 rayEnd)
	{
		_userRayBegin = rayBegin;
		_userRayEnd = rayEnd;
	}

	public void SetStartingPlayer(PlayerTurn player)
	{
		HasToMovePawnsAcross = PlayerWhoStarted != player;
		switch (player)
		{
		case PlayerTurn.PlayerOne:
			_rotationTarget = 0f;
			break;
		case PlayerTurn.PlayerTwo:
			_rotationTarget = System.MathF.PI;
			break;
		default:
			Debug.FailedAssert("Unexpected starting player caught: " + player, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBase.cs", "SetStartingPlayer", 382);
			break;
		}
		PlayerTurn playerTurn = (PlayerWhoStarted = player);
		PlayerTurn = playerTurn;
	}

	public void SetGameOverInfo(GameOverEnum info)
	{
		GameOverInfo = info;
	}

	public bool HasMovesAvailable(ref List<List<Move>> moves)
	{
		foreach (List<Move> move in moves)
		{
			if (move != null && move.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	public int GetTotalMovesAvailable(ref List<List<Move>> moves)
	{
		int num = 0;
		foreach (List<Move> move in moves)
		{
			if (move != null)
			{
				num += move.Count;
			}
		}
		return num;
	}

	public void PlayDiceRollSound()
	{
		Vec3 globalPosition = DiceBoard.GlobalPosition;
		MissionHandler.Mission.MakeSound(DiceRollSoundCodeID, globalPosition, soundCanBePredicted: true, isReliable: false, -1, -1);
	}

	public int GetPlayerOneUnitsAlive()
	{
		int num = 0;
		foreach (PawnBase playerOneUnit in PlayerOneUnits)
		{
			if (!playerOneUnit.Captured)
			{
				num++;
			}
		}
		return num;
	}

	public int GetPlayerTwoUnitsAlive()
	{
		int num = 0;
		foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
		{
			if (!playerTwoUnit.Captured)
			{
				num++;
			}
		}
		return num;
	}

	public int GetPlayerOneUnitsDead()
	{
		int num = 0;
		foreach (PawnBase playerOneUnit in PlayerOneUnits)
		{
			if (playerOneUnit.Captured)
			{
				num++;
			}
		}
		return num;
	}

	public int GetPlayerTwoUnitsDead()
	{
		int num = 0;
		foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
		{
			if (playerTwoUnit.Captured)
			{
				num++;
			}
		}
		return num;
	}

	public void Initialize()
	{
		BoardEntity = Mission.Current.Scene.FindEntityWithTag("boardgame");
		InitializeUnits();
		InitializeTiles();
		InitializeCapturedUnitsZones();
		InitializeDiceBoard();
		InitializeSound();
		Reset();
	}

	protected void RemovePawnFromBoard(PawnBase pawn, float speed, bool instantMove = false)
	{
		CapturedPawnsPool capturedPawnsPool = (pawn.PlayerOne ? PlayerOnePool : PlayerTwoPool);
		IEnumerable<GameEntity> children = capturedPawnsPool.Entity.GetChildren();
		GameEntity gameEntity = null;
		foreach (GameEntity item in children)
		{
			if (item.HasTag("pawn_" + capturedPawnsPool.PawnCount))
			{
				gameEntity = item;
				break;
			}
		}
		capturedPawnsPool.PawnCount++;
		Vec3 origin = gameEntity.GetGlobalFrame().origin;
		float num = pawn.Entity.GlobalPosition.z - origin.z;
		float num2 = 0.001f;
		if (num > num2)
		{
			Vec3 goal = origin;
			goal.z = pawn.Entity.GlobalPosition.z;
			pawn.AddGoalPosition(goal);
		}
		else if (num < 0f - num2)
		{
			Vec3 globalPosition = pawn.Entity.GlobalPosition;
			globalPosition.z = origin.z;
			pawn.AddGoalPosition(globalPosition);
		}
		pawn.AddGoalPosition(origin);
		pawn.MovePawnToGoalPositions(instantMove, speed);
	}

	public bool Tick(float dt)
	{
		foreach (PawnBase playerOneUnit in PlayerOneUnits)
		{
			playerOneUnit.Tick(dt);
		}
		foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
		{
			playerTwoUnit.Tick(dt);
		}
		for (int i = 0; i < TileCount; i++)
		{
			Tiles[i].Tick(dt);
		}
		if (!MovingPawnPresent() && DoneSettingUpBoard() && ReadyToPlay)
		{
			if (_firstTickAfterReady)
			{
				_firstTickAfterReady = false;
				MissionHandler.Handler.Activate();
			}
			if (IsReady)
			{
				if (_draggingSelectedUnit)
				{
					Vec3 userRayBegin = _userRayBegin;
					Vec3 userRayEnd = _userRayEnd;
					Vec3 globalPosition = SelectedUnit.Entity.GlobalPosition;
					float length = (userRayEnd - userRayBegin).Length;
					float num = (globalPosition - userRayBegin).Length / length;
					Vec3 vecTo = new Vec3(userRayBegin.x + (userRayEnd.x - userRayBegin.x) * num, userRayBegin.y + (userRayEnd.y - userRayBegin.y) * num, SelectedUnit.PosBeforeMoving.z + 0.05f);
					Vec3 pawnAtPosition = MBMath.Lerp(globalPosition, vecTo, 1f, 0.005f);
					SelectedUnit.SetPawnAtPosition(pawnAtPosition);
				}
				if (DiceRollAnimationRunning)
				{
					if (DiceRollAnimationTimer < 1f)
					{
						DiceRollAnimationTimer += dt;
					}
					else
					{
						DiceRollAnimationRunning = false;
						OnAfterDiceRollAnimation();
					}
				}
				if (MovesLeftToEndTurn == 0)
				{
					EndTurn();
				}
				else
				{
					UpdateTurn(dt);
				}
				CheckSwitchPlayerTurn();
				return true;
			}
			return false;
		}
		return false;
	}

	public void ForceDice(int value)
	{
		LastDice = value;
	}

	protected PawnBase InitializeUnit(PawnBase pawnToInit)
	{
		pawnToInit.OnArrivedIntermediateGoalPosition = OnPawnArrivesGoalPosition;
		pawnToInit.OnArrivedFinalGoalPosition = OnPawnArrivesGoalPosition;
		return pawnToInit;
	}

	protected Move HandlePlayerInput(float dt)
	{
		Move result = new Move(null, null);
		if (InputManager.IsHotKeyPressed("BoardGamePawnSelect") && !_draggingSelectedUnit)
		{
			JustStoppedDraggingUnit = false;
			PawnBase hoveredPawnIfAny = GetHoveredPawnIfAny();
			TileBase hoveredTileIfAny = GetHoveredTileIfAny();
			if (hoveredPawnIfAny != null)
			{
				if (PawnSelectFilter.Count == 0 || PawnSelectFilter.Contains(hoveredPawnIfAny))
				{
					PawnBase selectedUnit = SelectedUnit;
					PawnBase pawnBase = SelectPawn(hoveredPawnIfAny);
					if (pawnBase.PlayerOne == (PlayerTurn == PlayerTurn.PlayerOne) || !pawnBase.PlayerOne == (PlayerTurn == PlayerTurn.PlayerTwo))
					{
						if (SelectedUnit != null && SelectedUnit == selectedUnit)
						{
							_deselectUnit = true;
						}
					}
					else if (hoveredTileIfAny == null)
					{
						SelectedUnit = null;
					}
				}
			}
			else if (hoveredTileIfAny == null)
			{
				SelectedUnit = null;
			}
		}
		else if (SelectedUnit != null && InputManager.IsHotKeyReleased("BoardGamePawnDeselect"))
		{
			if (_draggingSelectedUnit)
			{
				_draggingSelectedUnit = false;
				JustStoppedDraggingUnit = true;
			}
			else if (_deselectUnit)
			{
				PawnBase hoveredPawnIfAny2 = GetHoveredPawnIfAny();
				if (hoveredPawnIfAny2 != null && hoveredPawnIfAny2 == SelectedUnit)
				{
					SelectedUnit = null;
					_deselectUnit = false;
				}
			}
			if (_validMoves != null)
			{
				SelectedUnit.DisableCollisionBody();
				TileBase hoveredTileIfAny2 = GetHoveredTileIfAny();
				if (hoveredTileIfAny2 != null && (hoveredTileIfAny2.PawnOnTile == null || hoveredTileIfAny2.PawnOnTile != SelectedUnit))
				{
					foreach (Move validMove in _validMoves)
					{
						if (hoveredTileIfAny2.Entity == validMove.GoalTile.Entity)
						{
							result = validMove;
						}
					}
				}
				SelectedUnit.EnableCollisionBody();
			}
			if (!result.IsValid && SelectedUnit != null && JustStoppedDraggingUnit)
			{
				SelectedUnit.ClearGoalPositions();
				SelectedUnit.AddGoalPosition(SelectedUnit.PosBeforeMoving);
				SelectedUnit.MovePawnToGoalPositions(instantMove: false, 0.8f);
			}
			_draggingTimer = 0f;
		}
		if (SelectedUnit != null && InputManager.IsHotKeyDown("BoardGameDragPreview"))
		{
			_draggingTimer += dt;
			if (_draggingTimer >= 0.2f)
			{
				_draggingSelectedUnit = true;
				_deselectUnit = false;
			}
		}
		return result;
	}

	protected PawnBase GetHoveredPawnIfAny()
	{
		PawnBase pawnBase = null;
		Mission.Current.Scene.RayCastForClosestEntityOrTerrain(_userRayBegin, _userRayEnd, out float _, out WeakGameEntity collidedEntity, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
		if (collidedEntity.IsValid)
		{
			foreach (PawnBase playerOneUnit in PlayerOneUnits)
			{
				if (playerOneUnit.Entity.Name.Equals(collidedEntity.Name))
				{
					pawnBase = playerOneUnit;
					break;
				}
			}
			if (pawnBase == null)
			{
				foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
				{
					if (playerTwoUnit.Entity.Name.Equals(collidedEntity.Name))
					{
						pawnBase = playerTwoUnit;
						break;
					}
				}
			}
		}
		return pawnBase;
	}

	protected TileBase GetHoveredTileIfAny()
	{
		TileBase result = null;
		Mission.Current.Scene.RayCastForClosestEntityOrTerrain(_userRayBegin, _userRayEnd, out float _, out WeakGameEntity collidedEntity, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
		if (collidedEntity.IsValid)
		{
			for (int i = 0; i < TileCount; i++)
			{
				TileBase tileBase = Tiles[i];
				if (tileBase.Entity.Name.Equals(collidedEntity.Name))
				{
					result = tileBase;
					break;
				}
			}
		}
		return result;
	}

	protected void CheckSwitchPlayerTurn()
	{
		if (PlayerTurn != PlayerTurn.PlayerOneWaiting && PlayerTurn != PlayerTurn.PlayerTwoWaiting)
		{
			return;
		}
		bool flag = false;
		foreach (PawnBase playerOneUnit in PlayerOneUnits)
		{
			if (playerOneUnit.Moving)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
			{
				if (playerTwoUnit.Moving)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			SwitchPlayerTurn();
		}
	}

	protected void OnVictory(string message = "str_boardgame_victory_message")
	{
		MissionHandler.PlayerOneWon(message);
	}

	protected void OnAfterEndTurn()
	{
		ClearValidMoves();
		CheckGameEnded();
		MovesLeftToEndTurn = ((!InPreMovementStage) ? 1 : UnitsToPlacePerTurnInPreMovementStage);
	}

	protected void OnDefeat(string message = "str_boardgame_defeat_message")
	{
		MissionHandler.PlayerTwoWon(message);
	}

	protected void OnDraw(string message = "str_boardgame_draw_message")
	{
		MissionHandler.GameWasDraw(message);
	}

	private void OnBeforeSelectedUnitChanged(PawnBase oldSelectedUnit, PawnBase newSelectedUnit)
	{
		oldSelectedUnit?.Entity.GetMetaMesh(0).SetFactor1Linear(PawnUnselectedFactor);
		newSelectedUnit?.Entity.GetMetaMesh(0).SetFactor1Linear(PawnSelectedFactor);
		ClearValidMoves();
	}

	protected void EndTurn()
	{
		OnBeforeEndTurn();
		SwitchToWaiting();
		OnAfterEndTurn();
	}

	protected void ClearValidMoves()
	{
		HideAllValidTiles();
		if (_validMoves != null)
		{
			_validMoves.Clear();
			_validMoves = null;
		}
	}

	private void OnAfterSelectedUnitChanged()
	{
		if (SelectedUnit != null)
		{
			List<Move> list = CalculateValidMoves(SelectedUnit);
			if (list != null && list.Count > 0)
			{
				_validMoves = list;
			}
			if (SelectedUnit.PlayerOne || MissionHandler.AIOpponent == null)
			{
				SelectedUnit.PlayPawnSelectSound();
				ShowAllValidTiles();
			}
		}
	}

	private void UpdateTurn(float dt)
	{
		if (PlayerTurn == PlayerTurn.PlayerOne || (PlayerTurn == PlayerTurn.PlayerTwo && AIOpponent == null))
		{
			if (InPreMovementStage)
			{
				HandlePreMovementStage(dt);
			}
			else if (!DiceRollRequired || DiceRolled)
			{
				Move move = HandlePlayerInput(dt);
				if (move.IsValid)
				{
					MovePawnToTile(move.Unit, move.GoalTile);
				}
			}
		}
		else
		{
			if (PlayerTurn != PlayerTurn.PlayerTwo || AIOpponent == null || _waitingAIForfeitResponse)
			{
				return;
			}
			if (AIOpponent.WantsToForfeit())
			{
				OnAIWantsForfeit();
			}
			if (DiceRollRequired && !DiceRolled)
			{
				RollDice();
			}
			AIOpponent.UpdateThinkingAboutMove(dt);
			if (!AIOpponent.CanMakeMove())
			{
				return;
			}
			SelectedUnit = AIOpponent.RecentMoveCalculated.Unit;
			if (SelectedUnit != null)
			{
				if (InPreMovementStage)
				{
					HandlePreMovementStageAI(AIOpponent.RecentMoveCalculated);
				}
				else
				{
					TileBase goalTile = AIOpponent.RecentMoveCalculated.GoalTile;
					MovePawnToTile(SelectedUnit, goalTile);
				}
			}
			else
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_boardgame_no_available_moves_opponent"));
				EndTurn();
			}
			AIOpponent.ResetThinking();
		}
	}

	private bool DoneSettingUpBoard()
	{
		bool result = !SettingUpBoard;
		if (SettingUpBoard)
		{
			if (_rotationApplied != _rotationTarget && RotateBoard)
			{
				float value = _rotationTarget - _rotationApplied;
				float num = 0.05f;
				float num2 = TaleWorlds.Library.MathF.Clamp(value, 0f - num, num);
				MatrixFrame frame = BoardEntity.GetGlobalFrame();
				frame.rotation.RotateAboutUp(num2);
				BoardEntity.SetGlobalFrame(in frame);
				_rotationApplied += num2;
				if (TaleWorlds.Library.MathF.Abs(_rotationTarget - _rotationApplied) <= 1E-05f)
				{
					_rotationApplied = _rotationTarget;
					UpdateAllPawnsPositions();
					UpdateAllTilesPositions();
					return result;
				}
			}
			else
			{
				if (!_rotationCompleted)
				{
					_rotationCompleted = true;
					OnAfterBoardRotated();
					return result;
				}
				SettingUpBoard = false;
				OnAfterBoardSetUp();
			}
		}
		return result;
	}

	protected void HideAllValidTiles()
	{
		if (_validMoves == null || _validMoves.Count <= 0)
		{
			return;
		}
		foreach (Move validMove in _validMoves)
		{
			validMove.GoalTile.SetVisibility(isVisible: false);
		}
	}

	protected void ShowAllValidTiles()
	{
		if (_validMoves == null || _validMoves.Count <= 0)
		{
			return;
		}
		foreach (Move validMove in _validMoves)
		{
			validMove.GoalTile.SetVisibility(isVisible: true);
		}
	}

	private void UnfocusAllPawns()
	{
		if (PlayerOneUnits != null)
		{
			foreach (PawnBase playerOneUnit in PlayerOneUnits)
			{
				playerOneUnit.Entity.GetMetaMesh(0).SetFactor1Linear(PawnUnselectedFactor);
			}
		}
		if (PlayerTwoUnits == null)
		{
			return;
		}
		foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
		{
			playerTwoUnit.Entity.GetMetaMesh(0).SetFactor1Linear(PawnUnselectedFactor);
		}
	}

	private bool MovingPawnPresent()
	{
		bool flag = false;
		foreach (PawnBase playerOneUnit in PlayerOneUnits)
		{
			if (playerOneUnit.Moving || playerOneUnit.HasAnyGoalPosition)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
			{
				if (playerTwoUnit.Moving || playerTwoUnit.HasAnyGoalPosition)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}

	private void SwitchToWaiting()
	{
		if (PlayerTurn == PlayerTurn.PlayerOne)
		{
			PlayerTurn = PlayerTurn.PlayerOneWaiting;
		}
		else if (PlayerTurn == PlayerTurn.PlayerTwo)
		{
			PlayerTurn = PlayerTurn.PlayerTwoWaiting;
		}
		JustStoppedDraggingUnit = false;
	}

	protected void OnAIWantsForfeit()
	{
		if (!_waitingAIForfeitResponse)
		{
			_waitingAIForfeitResponse = true;
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_boardgame").ToString(), GameTexts.FindText("str_boardgame_forfeit_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_accept").ToString(), GameTexts.FindText("str_reject").ToString(), OnAIForfeitAccepted, OnAIForfeitRejected));
		}
	}

	private void UpdateAllPawnsPositions()
	{
		foreach (PawnBase playerOneUnit in PlayerOneUnits)
		{
			playerOneUnit.UpdatePawnPosition();
		}
		foreach (PawnBase playerTwoUnit in PlayerTwoUnits)
		{
			playerTwoUnit.UpdatePawnPosition();
		}
	}

	private void OnAIForfeitAccepted()
	{
		MissionHandler.AIForfeitGame();
		_waitingAIForfeitResponse = false;
	}

	private void OnAIForfeitRejected()
	{
		_waitingAIForfeitResponse = false;
	}
}
