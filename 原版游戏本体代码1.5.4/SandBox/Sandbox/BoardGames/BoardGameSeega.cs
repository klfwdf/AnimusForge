using System.Collections.Generic;
using System.Linq;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Objects;
using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.BoardGames;

public class BoardGameSeega : BoardGameBase
{
	public class BarrierInfo
	{
		public bool IsHorizontal;

		public int Position;

		public bool PlayerOne;

		public BarrierInfo(bool isHor, int pos, bool playerOne)
		{
			IsHorizontal = isHor;
			Position = pos;
			PlayerOne = playerOne;
		}
	}

	public struct BoardInformation
	{
		public readonly PawnInformation[] PawnInformation;

		public readonly TileBaseInformation[,] TileInformation;

		public BoardInformation(ref PawnInformation[] pawns, ref TileBaseInformation[,] tiles)
		{
			PawnInformation = pawns;
			TileInformation = tiles;
		}
	}

	public struct PawnInformation
	{
		public readonly int X;

		public readonly int Y;

		public readonly int PrevX;

		public readonly int PrevY;

		public readonly bool MovedThisTurn;

		public readonly bool IsCaptured;

		public readonly Vec3 Position;

		public PawnInformation(int x, int y, int prevX, int prevY, bool movedThisTurn, bool captured, Vec3 position)
		{
			X = x;
			Y = y;
			PrevX = prevX;
			PrevY = prevY;
			MovedThisTurn = movedThisTurn;
			IsCaptured = captured;
			Position = position;
		}
	}

	private const int CentralTileX = 2;

	private const int CentralTileY = 2;

	public static readonly int BoardWidth = 5;

	public static readonly int BoardHeight = 5;

	private Dictionary<PawnBase, int> _blockingPawns = new Dictionary<PawnBase, int>();

	private BoardInformation _startState;

	private bool _placementStageOver;

	public override int TileCount => BoardWidth * BoardHeight;

	protected override int UnitsToPlacePerTurnInPreMovementStage => 2;

	protected override bool RotateBoard => false;

	protected override bool PreMovementStagePresent => true;

	protected override bool DiceRollRequired => false;

	public BoardGameSeega(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
		: base(mission, new TextObject("{=C4n1rgBC}Seega"), startingPlayer)
	{
		SelectedUnit = null;
	}

	public override void InitializeUnits()
	{
		base.PlayerOneUnits.Clear();
		base.PlayerTwoUnits.Clear();
		int num = 0;
		GameEntity gameEntity = null;
		do
		{
			gameEntity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + num);
			if (gameEntity != null)
			{
				base.PlayerOneUnits.Add(InitializeUnit(new PawnSeega(gameEntity, playerOne: true)));
				num++;
			}
		}
		while (gameEntity != null);
		num = 0;
		do
		{
			gameEntity = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + num);
			if (gameEntity != null)
			{
				base.PlayerTwoUnits.Add(InitializeUnit(new PawnSeega(gameEntity, playerOne: false)));
				num++;
			}
		}
		while (gameEntity != null);
	}

	public override void InitializeTiles()
	{
		IEnumerable<GameEntity> source = from gameEntity2 in BoardEntity.GetChildren()
			where gameEntity2.Tags.Any((string t) => t.Contains("tile_"))
			select gameEntity2;
		IEnumerable<GameEntity> source2 = from gameEntity2 in BoardEntity.GetChildren()
			where gameEntity2.Tags.Any((string t) => t.Contains("decal_"))
			select gameEntity2;
		if (base.Tiles == null)
		{
			base.Tiles = new TileBase[TileCount];
		}
		int x = 0;
		while (x < BoardWidth)
		{
			int y;
			int num;
			for (y = 0; y < BoardHeight; y = num)
			{
				GameEntity gameEntity = source.Single((GameEntity e) => e.HasTag("tile_" + x + "_" + y));
				BoardGameDecal firstScriptOfType = source2.Single((GameEntity e) => e.HasTag("decal_" + x + "_" + y)).GetFirstScriptOfType<BoardGameDecal>();
				Tile2D tile = new Tile2D(gameEntity, firstScriptOfType, x, y);
				gameEntity.CreateVariableRatePhysics(forChildren: true);
				SetTile(tile, x, y);
				num = y + 1;
			}
			num = x + 1;
			x = num;
		}
	}

	public override void InitializeSound()
	{
		PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
		PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
		PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_stone");
		PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
	}

	public override void Reset()
	{
		base.Reset();
		base.InPreMovementStage = true;
		_placementStageOver = false;
		if (_startState.PawnInformation == null)
		{
			PreplaceUnits();
		}
		else
		{
			RestoreStartingBoard();
		}
	}

	public override List<Move> CalculateValidMoves(PawnBase pawn)
	{
		List<Move> list = new List<Move>();
		PawnSeega pawnSeega = pawn as PawnSeega;
		if (pawn != null)
		{
			int x = pawnSeega.X;
			int y = pawnSeega.Y;
			if (!base.InPreMovementStage && pawnSeega.IsPlaced)
			{
				if (x > 0)
				{
					TileBase tile = GetTile(x - 1, y);
					if (tile.PawnOnTile == null && (pawnSeega.PrevX != x - 1 || pawnSeega.PrevY != y))
					{
						Move item = default(Move);
						item.Unit = pawn;
						item.GoalTile = tile;
						list.Add(item);
					}
				}
				if (x < BoardWidth - 1)
				{
					TileBase tile2 = GetTile(x + 1, y);
					if (tile2.PawnOnTile == null && (pawnSeega.PrevX != x + 1 || pawnSeega.PrevY != y))
					{
						Move item2 = default(Move);
						item2.Unit = pawn;
						item2.GoalTile = tile2;
						list.Add(item2);
					}
				}
				if (y > 0)
				{
					TileBase tile3 = GetTile(x, y - 1);
					if (tile3.PawnOnTile == null && (pawnSeega.PrevX != x || pawnSeega.PrevY != y - 1))
					{
						Move item3 = default(Move);
						item3.Unit = pawn;
						item3.GoalTile = tile3;
						list.Add(item3);
					}
				}
				if (y < BoardHeight - 1)
				{
					TileBase tile4 = GetTile(x, y + 1);
					if (tile4.PawnOnTile == null && (pawnSeega.PrevX != x || pawnSeega.PrevY != y + 1))
					{
						Move item4 = default(Move);
						item4.Unit = pawn;
						item4.GoalTile = tile4;
						list.Add(item4);
					}
				}
			}
			else if (base.InPreMovementStage && !pawnSeega.IsPlaced)
			{
				Move item5 = default(Move);
				for (int i = 0; i < TileCount; i++)
				{
					TileBase tileBase = base.Tiles[i];
					if (tileBase.PawnOnTile == null && !tileBase.Entity.HasTag("obstructed_at_start"))
					{
						item5.Unit = pawn;
						item5.GoalTile = tileBase;
						list.Add(item5);
					}
				}
			}
		}
		return list;
	}

	public override void SetPawnCaptured(PawnBase pawn, bool aiSimulation = false)
	{
		base.SetPawnCaptured(pawn, aiSimulation);
		PawnSeega pawnSeega = pawn as PawnSeega;
		GetTile(pawnSeega.X, pawnSeega.Y).PawnOnTile = null;
		pawnSeega.X = -1;
		pawnSeega.Y = -1;
		if (!aiSimulation)
		{
			RemovePawnFromBoard(pawnSeega, 0.8f);
			MBDebug.Print("Setting pawn captured: X: " + pawnSeega.X + ", Y: " + pawnSeega.Y);
		}
	}

	protected override void OnPawnArrivesGoalPosition(PawnBase pawn, Vec3 prevPos, Vec3 currentPos)
	{
		if (pawn.MovingToDifferentTile)
		{
			base.PawnSelectFilter.Clear();
			PawnSeega pawnSeega = SelectedUnit as PawnSeega;
			if (!base.InPreMovementStage && pawnSeega != null)
			{
				if (CheckIfPawnCaptures(pawnSeega) > 0)
				{
					PawnSeega pawnSeega2 = pawn as PawnSeega;
					bool flag = false;
					List<Move> list = CalculateValidMoves(pawn);
					int count = list.Count;
					for (int i = 0; i < count; i++)
					{
						Tile2D tile2D = GetTile(pawnSeega2.X, pawnSeega2.Y) as Tile2D;
						Tile2D tile2D2 = list[i].GoalTile as Tile2D;
						tile2D.PawnOnTile = null;
						pawnSeega2.X = tile2D2.X;
						pawnSeega2.Y = tile2D2.Y;
						tile2D2.PawnOnTile = pawnSeega2;
						int num = CheckIfPawnCaptures(pawnSeega2, aiSimulation: false, setCaptured: false);
						tile2D2.PawnOnTile = null;
						pawnSeega2.X = tile2D.X;
						pawnSeega2.Y = tile2D.Y;
						tile2D.PawnOnTile = pawnSeega2;
						if (num > 0)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						if (!base.PawnSelectFilter.Contains(pawn))
						{
							base.PawnSelectFilter.Add(pawn);
						}
						MovesLeftToEndTurn++;
					}
				}
				if (CheckIfUnitsIsolatedByBarrier(new Vec2i(pawnSeega.X, pawnSeega.Y)))
				{
					MBDebug.Print("Barrier was formed!");
					int playerOneUnitsAlive = GetPlayerOneUnitsAlive();
					int playerTwoUnitsAlive = GetPlayerTwoUnitsAlive();
					if (playerOneUnitsAlive > playerTwoUnitsAlive)
					{
						string message = (pawnSeega.PlayerOne ? "str_boardgame_seega_barrier_by_player_one_victory_message" : "str_boardgame_seega_barrier_by_player_two_victory_message");
						OnVictory(message);
					}
					else if (playerOneUnitsAlive < playerTwoUnitsAlive)
					{
						string message2 = (pawnSeega.PlayerOne ? "str_boardgame_seega_barrier_by_player_one_defeat_message" : "str_boardgame_seega_barrier_by_player_two_defeat_message");
						OnDefeat(message2);
					}
					else
					{
						string message3 = (pawnSeega.PlayerOne ? "str_boardgame_seega_barrier_by_player_one_draw_message" : "str_boardgame_seega_barrier_by_player_two_draw_message");
						OnDraw(message3);
					}
					ReadyToPlay = false;
				}
			}
			CheckGameEnded();
		}
		base.OnPawnArrivesGoalPosition(pawn, prevPos, currentPos);
	}

	protected override void SwitchPlayerTurn()
	{
		SelectedUnit = null;
		if (base.PlayerTurn == PlayerTurn.PlayerOneWaiting)
		{
			base.PlayerTurn = PlayerTurn.PlayerTwo;
			if (!base.InPreMovementStage)
			{
				if (MissionHandler.AIOpponent == null)
				{
					CheckIfPlayerIsStuck(playerOne: false);
				}
				foreach (PawnSeega playerTwoUnit in base.PlayerTwoUnits)
				{
					playerTwoUnit.UpdateMoveBackAvailable();
				}
			}
		}
		else if (base.PlayerTurn == PlayerTurn.PlayerTwoWaiting)
		{
			base.PlayerTurn = PlayerTurn.PlayerOne;
			if (!base.InPreMovementStage)
			{
				CheckIfPlayerIsStuck(playerOne: true);
				foreach (PawnSeega playerOneUnit in base.PlayerOneUnits)
				{
					playerOneUnit.UpdateMoveBackAvailable();
				}
			}
		}
		bool inPreMovementStage = base.InPreMovementStage;
		base.InPreMovementStage = !CheckPlacementStageOver() || (_blockingPawns != null && _blockingPawns.Count > 0);
		if (inPreMovementStage != base.InPreMovementStage && !base.InPreMovementStage)
		{
			EndTurn();
		}
		base.SwitchPlayerTurn();
	}

	protected override PawnBase SelectPawn(PawnBase pawn)
	{
		if (base.PlayerTurn == PlayerTurn.PlayerOne)
		{
			if (pawn.PlayerOne)
			{
				if (base.InPreMovementStage)
				{
					if (!pawn.IsPlaced)
					{
						SelectedUnit = pawn;
					}
				}
				else
				{
					SelectedUnit = pawn;
				}
			}
		}
		else if (base.AIOpponent == null && !pawn.PlayerOne)
		{
			if (base.InPreMovementStage)
			{
				if (!pawn.IsPlaced)
				{
					SelectedUnit = pawn;
				}
			}
			else
			{
				SelectedUnit = pawn;
			}
		}
		return pawn;
	}

	protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
	{
		base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
		Tile2D tile2D = tile as Tile2D;
		PawnSeega pawnSeega = pawn as PawnSeega;
		if (tile2D.PawnOnTile != null || pawnSeega == null)
		{
			return;
		}
		if (displayMessage)
		{
			if (base.PlayerTurn == PlayerTurn.PlayerOne)
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_move_piece_player").ToString()));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_move_piece_opponent").ToString()));
			}
		}
		Vec3 globalPosition = tile2D.Entity.GlobalPosition;
		float speed = 0.7f;
		if (!base.InPreMovementStage)
		{
			speed = 0.3f;
		}
		pawnSeega.MovingToDifferentTile = pawnSeega.X != tile2D.X || pawnSeega.Y != tile2D.Y;
		pawnSeega.AddGoalPosition(globalPosition);
		pawnSeega.MovePawnToGoalPositionsDelayed(instantMove, speed, JustStoppedDraggingUnit, delay);
		pawnSeega.PrevX = pawnSeega.X;
		pawnSeega.PrevY = pawnSeega.Y;
		pawnSeega.X = tile2D.X;
		pawnSeega.Y = tile2D.Y;
		if (pawnSeega.PrevX != -1 && pawnSeega.PrevY != -1)
		{
			GetTile(pawnSeega.PrevX, pawnSeega.PrevY).PawnOnTile = null;
		}
		tile2D.PawnOnTile = pawnSeega;
		if (instantMove && !base.InPreMovementStage)
		{
			CheckIfPawnCaptures(pawnSeega);
		}
		else if (pawnSeega == SelectedUnit && instantMove)
		{
			SelectedUnit = null;
		}
	}

	protected override void HandlePreMovementStage(float dt)
	{
		if (_blockingPawns != null && _blockingPawns.Count > 0)
		{
			if (base.InputManager.IsHotKeyPressed("BoardGamePawnSelect"))
			{
				PawnBase hoveredPawnIfAny = GetHoveredPawnIfAny();
				if (hoveredPawnIfAny != null && _blockingPawns.ContainsKey(hoveredPawnIfAny))
				{
					SetPawnCaptured(hoveredPawnIfAny);
					UnfocusBlockingPawns();
					base.InPreMovementStage = false;
				}
			}
			else
			{
				SelectedUnit = null;
			}
		}
		else
		{
			Move move = HandlePlayerInput(dt);
			if (move.IsValid)
			{
				MovePawnToTile(move.Unit, move.GoalTile);
			}
		}
	}

	protected override void HandlePreMovementStageAI(Move move)
	{
		MovePawnToTile(move.Unit, move.GoalTile);
	}

	protected override bool CheckGameEnded()
	{
		if (ReadyToPlay)
		{
			if (GetPlayerOneUnitsAlive() <= 1)
			{
				OnDefeat();
				ReadyToPlay = false;
			}
			else if (GetPlayerTwoUnitsAlive() <= 1)
			{
				OnVictory();
				ReadyToPlay = false;
			}
		}
		return !ReadyToPlay;
	}

	protected override void OnAfterBoardSetUp()
	{
		if (_startState.PawnInformation == null)
		{
			_startState = TakeBoardSnapshot();
		}
		ReadyToPlay = true;
	}

	public void AIMakeMove(Move move)
	{
		Tile2D tile2D = move.GoalTile as Tile2D;
		PawnSeega pawnSeega = move.Unit as PawnSeega;
		if (tile2D.PawnOnTile == null)
		{
			pawnSeega.PrevX = pawnSeega.X;
			pawnSeega.PrevY = pawnSeega.Y;
			pawnSeega.X = tile2D.X;
			pawnSeega.Y = tile2D.Y;
			GetTile(pawnSeega.PrevX, pawnSeega.PrevY).PawnOnTile = null;
			tile2D.PawnOnTile = pawnSeega;
			CheckIfPawnCaptures(pawnSeega, aiSimulation: true);
		}
	}

	public Dictionary<PawnBase, int> GetBlockingPawns(bool playerOneBlocked)
	{
		Dictionary<PawnBase, int> dictionary = new Dictionary<PawnBase, int>();
		foreach (PawnSeega item in playerOneBlocked ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			if (!item.IsPlaced || IsOnCentralTile(item))
			{
				continue;
			}
			BoardInformation board = TakeBoardSnapshot();
			SetPawnCaptured(item, aiSimulation: true);
			int num = 0;
			foreach (PawnSeega item2 in playerOneBlocked ? base.PlayerOneUnits : base.PlayerTwoUnits)
			{
				if (item2.IsPlaced)
				{
					num += CalculateValidMoves(item2).Count;
				}
			}
			if (num > 0)
			{
				dictionary.Add(item, num);
			}
			UndoMove(ref board);
		}
		return dictionary;
	}

	public BoardInformation TakeBoardSnapshot()
	{
		PawnInformation[] pawns = new PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
		TileBaseInformation[,] tiles = new TileBaseInformation[BoardWidth, BoardHeight];
		int num = 0;
		foreach (PawnSeega playerOneUnit in base.PlayerOneUnits)
		{
			pawns[num++] = new PawnInformation(playerOneUnit.X, playerOneUnit.Y, playerOneUnit.PrevX, playerOneUnit.PrevY, playerOneUnit.MovedThisTurn, playerOneUnit.Captured, playerOneUnit.Entity.GlobalPosition);
		}
		foreach (PawnSeega playerTwoUnit in base.PlayerTwoUnits)
		{
			pawns[num++] = new PawnInformation(playerTwoUnit.X, playerTwoUnit.Y, playerTwoUnit.PrevX, playerTwoUnit.PrevY, playerTwoUnit.MovedThisTurn, playerTwoUnit.Captured, playerTwoUnit.Entity.GlobalPosition);
		}
		for (int i = 0; i < BoardWidth; i++)
		{
			for (int j = 0; j < BoardHeight; j++)
			{
				tiles[i, j] = new TileBaseInformation(ref GetTile(i, j).PawnOnTile);
			}
		}
		return new BoardInformation(ref pawns, ref tiles);
	}

	public void UndoMove(ref BoardInformation board)
	{
		int num = 0;
		foreach (PawnSeega playerOneUnit in base.PlayerOneUnits)
		{
			playerOneUnit.X = board.PawnInformation[num].X;
			playerOneUnit.Y = board.PawnInformation[num].Y;
			playerOneUnit.PrevX = board.PawnInformation[num].PrevX;
			playerOneUnit.PrevY = board.PawnInformation[num].PrevY;
			playerOneUnit.Captured = board.PawnInformation[num].IsCaptured;
			playerOneUnit.AISetMovedThisTurn(board.PawnInformation[num].MovedThisTurn);
			num++;
		}
		foreach (PawnSeega playerTwoUnit in base.PlayerTwoUnits)
		{
			playerTwoUnit.X = board.PawnInformation[num].X;
			playerTwoUnit.Y = board.PawnInformation[num].Y;
			playerTwoUnit.PrevX = board.PawnInformation[num].PrevX;
			playerTwoUnit.PrevY = board.PawnInformation[num].PrevY;
			playerTwoUnit.Captured = board.PawnInformation[num].IsCaptured;
			playerTwoUnit.AISetMovedThisTurn(board.PawnInformation[num].MovedThisTurn);
			num++;
		}
		for (int i = 0; i < BoardWidth; i++)
		{
			for (int j = 0; j < BoardHeight; j++)
			{
				GetTile(i, j).PawnOnTile = board.TileInformation[i, j].PawnOnTile;
			}
		}
	}

	public TileBase GetTile(int x, int y)
	{
		return base.Tiles[y * BoardWidth + x];
	}

	private void SetTile(TileBase tile, int x, int y)
	{
		base.Tiles[y * BoardWidth + x] = tile;
	}

	private bool IsCentralTile(Tile2D tile)
	{
		if (tile.X == 2)
		{
			return tile.Y == 2;
		}
		return false;
	}

	private bool IsOnCentralTile(PawnSeega pawn)
	{
		if (pawn.X == 2)
		{
			return pawn.Y == 2;
		}
		return false;
	}

	private void PreplaceUnits()
	{
		MovePawnToTileDelayed(base.PlayerOneUnits[0], GetTile(0, 2), instantMove: false, displayMessage: false, 0.55f);
		MovePawnToTileDelayed(base.PlayerTwoUnits[0], GetTile(2, 0), instantMove: false, displayMessage: false, 0.70000005f);
		MovePawnToTileDelayed(base.PlayerOneUnits[1], GetTile(4, 2), instantMove: false, displayMessage: false, 0.85f);
		MovePawnToTileDelayed(base.PlayerTwoUnits[1], GetTile(2, 4), instantMove: false, displayMessage: false, 1f);
	}

	private void RestoreStartingBoard()
	{
		int num = 0;
		foreach (PawnSeega playerOneUnit in base.PlayerOneUnits)
		{
			if (_startState.PawnInformation[num].X != -1)
			{
				int x = _startState.PawnInformation[num].X;
				int y = _startState.PawnInformation[num].Y;
				if (_startState.PawnInformation[num].X != playerOneUnit.X && _startState.PawnInformation[num].Y != playerOneUnit.Y)
				{
					playerOneUnit.Reset();
					TileBase tile = GetTile(x, y);
					MovePawnToTile(playerOneUnit, tile);
				}
			}
			else if (!playerOneUnit.Entity.GlobalPosition.NearlyEquals(in _startState.PawnInformation[num].Position))
			{
				if (playerOneUnit.X != -1 && GetTile(playerOneUnit.X, playerOneUnit.Y).PawnOnTile == playerOneUnit)
				{
					GetTile(playerOneUnit.X, playerOneUnit.Y).PawnOnTile = null;
				}
				playerOneUnit.Reset();
				playerOneUnit.AddGoalPosition(_startState.PawnInformation[num].Position);
				playerOneUnit.MovePawnToGoalPositions(instantMove: false, 0.5f);
			}
			num++;
		}
		foreach (PawnSeega playerTwoUnit in base.PlayerTwoUnits)
		{
			if (_startState.PawnInformation[num].X != -1)
			{
				int x2 = _startState.PawnInformation[num].X;
				int y2 = _startState.PawnInformation[num].Y;
				if (_startState.PawnInformation[num].X != playerTwoUnit.X && _startState.PawnInformation[num].Y != playerTwoUnit.Y)
				{
					playerTwoUnit.Reset();
					TileBase tile2 = GetTile(x2, y2);
					MovePawnToTile(playerTwoUnit, tile2);
				}
			}
			else
			{
				if (playerTwoUnit.X != -1 && GetTile(playerTwoUnit.X, playerTwoUnit.Y).PawnOnTile == playerTwoUnit)
				{
					GetTile(playerTwoUnit.X, playerTwoUnit.Y).PawnOnTile = null;
				}
				playerTwoUnit.Reset();
				playerTwoUnit.AddGoalPosition(_startState.PawnInformation[num].Position);
				playerTwoUnit.MovePawnToGoalPositions(instantMove: false, 0.5f);
			}
			num++;
		}
	}

	private bool CheckPlacementStageOver()
	{
		if (!_placementStageOver)
		{
			bool flag = false;
			foreach (PawnSeega playerOneUnit in base.PlayerOneUnits)
			{
				if (!playerOneUnit.IsPlaced)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (PawnSeega playerTwoUnit in base.PlayerTwoUnits)
				{
					if (!playerTwoUnit.IsPlaced)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				_placementStageOver = true;
			}
		}
		return _placementStageOver;
	}

	private bool CheckIfPawnCapturedEnemyPawn(PawnSeega pawn, bool aiSimulation, Tile2D victimTile, TileBase helperTile, bool setCaptured)
	{
		bool result = false;
		PawnBase pawnOnTile = victimTile.PawnOnTile;
		if (pawnOnTile != null && !IsCentralTile(victimTile) && pawnOnTile.PlayerOne != pawn.PlayerOne)
		{
			PawnBase pawnOnTile2 = helperTile.PawnOnTile;
			if (pawnOnTile2 != null && pawnOnTile2.PlayerOne == pawn.PlayerOne)
			{
				result = true;
				if (setCaptured)
				{
					SetPawnCaptured(pawnOnTile, aiSimulation);
				}
			}
		}
		return result;
	}

	private int CheckIfPawnCaptures(PawnSeega pawn, bool aiSimulation = false, bool setCaptured = true)
	{
		int num = 0;
		int x = pawn.X;
		int y = pawn.Y;
		if (x > 1 && CheckIfPawnCapturedEnemyPawn(pawn, aiSimulation, GetTile(x - 1, y) as Tile2D, GetTile(x - 2, y), setCaptured))
		{
			num++;
		}
		if (x < BoardWidth - 2 && CheckIfPawnCapturedEnemyPawn(pawn, aiSimulation, GetTile(x + 1, y) as Tile2D, GetTile(x + 2, y), setCaptured))
		{
			num++;
		}
		if (y > 1 && CheckIfPawnCapturedEnemyPawn(pawn, aiSimulation, GetTile(x, y - 1) as Tile2D, GetTile(x, y - 2), setCaptured))
		{
			num++;
		}
		if (y < BoardHeight - 2 && CheckIfPawnCapturedEnemyPawn(pawn, aiSimulation, GetTile(x, y + 1) as Tile2D, GetTile(x, y + 2), setCaptured))
		{
			num++;
		}
		return num;
	}

	private void CheckIfPlayerIsStuck(bool playerOne)
	{
		List<List<Move>> moves = CalculateAllValidMoves(playerOne ? BoardGameSide.Player : BoardGameSide.AI);
		if (!HasMovesAvailable(ref moves))
		{
			MBDebug.Print("Player has no available moves! " + (playerOne ? "PLAYER ONE" : "PLAYER TWO"));
			_blockingPawns = GetBlockingPawns(playerOne);
			FocusBlockingPawns();
			if (playerOne)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=GwHPEgsv}You can't move. Chose one of the opponent's pawns to remove and make a move").ToString()));
			}
		}
	}

	private void FocusBlockingPawns()
	{
		foreach (KeyValuePair<PawnBase, int> blockingPawn in _blockingPawns)
		{
			blockingPawn.Key.Entity.GetMetaMesh(0).SetFactor1Linear(PawnSelectedFactor);
		}
	}

	private void UnfocusBlockingPawns()
	{
		foreach (KeyValuePair<PawnBase, int> blockingPawn in _blockingPawns)
		{
			blockingPawn.Key.Entity.GetMetaMesh(0).SetFactor1Linear(PawnUnselectedFactor);
		}
		_blockingPawns.Clear();
	}

	private BarrierInfo CheckIfBarrier(Vec2i pawnNewPos)
	{
		if (pawnNewPos.X > 0 && pawnNewPos.X < BoardWidth - 1)
		{
			PawnBase pawnOnTile = GetTile(pawnNewPos.X, 0).PawnOnTile;
			if (pawnOnTile != null)
			{
				for (int i = 1; i < BoardHeight; i++)
				{
					PawnBase pawnOnTile2 = GetTile(pawnNewPos.X, i).PawnOnTile;
					if (pawnOnTile2 == null || pawnOnTile2.PlayerOne != pawnOnTile.PlayerOne)
					{
						break;
					}
					if (i == BoardHeight - 1)
					{
						return new BarrierInfo(isHor: false, pawnNewPos.X, pawnOnTile.PlayerOne);
					}
				}
			}
		}
		if (pawnNewPos.Y > 0 && pawnNewPos.Y < BoardHeight - 1)
		{
			PawnBase pawnOnTile3 = GetTile(0, pawnNewPos.Y).PawnOnTile;
			if (pawnOnTile3 != null)
			{
				for (int j = 1; j < BoardWidth; j++)
				{
					PawnBase pawnOnTile4 = GetTile(j, pawnNewPos.Y).PawnOnTile;
					if (pawnOnTile4 == null || pawnOnTile4.PlayerOne != pawnOnTile3.PlayerOne)
					{
						break;
					}
					if (j == BoardWidth - 1)
					{
						return new BarrierInfo(isHor: true, pawnNewPos.Y, pawnOnTile3.PlayerOne);
					}
				}
			}
		}
		return null;
	}

	private bool CheckIfUnitsIsolatedByBarrier(Vec2i pawnNewPos)
	{
		BarrierInfo barrierInfo = CheckIfBarrier(pawnNewPos);
		if (barrierInfo != null)
		{
			bool flag = false;
			bool flag2 = false;
			foreach (PawnSeega item in barrierInfo.PlayerOne ? base.PlayerTwoUnits : base.PlayerOneUnits)
			{
				if (!item.Captured)
				{
					if ((barrierInfo.IsHorizontal ? item.Y : item.X) > barrierInfo.Position)
					{
						flag = true;
					}
					if ((barrierInfo.IsHorizontal ? item.Y : item.X) < barrierInfo.Position)
					{
						flag2 = true;
					}
				}
			}
			return !(flag && flag2);
		}
		return false;
	}
}
