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

public class BoardGameKonane : BoardGameBase
{
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

		public readonly bool IsCaptured;

		public readonly Vec3 Position;

		public PawnInformation(int x, int y, int prevX, int prevY, bool captured, Vec3 position)
		{
			X = x;
			Y = y;
			PrevX = prevX;
			PrevY = prevY;
			IsCaptured = captured;
			Position = position;
		}
	}

	public const int WhitePawnCount = 18;

	public const int BlackPawnCount = 18;

	public static readonly int BoardWidth = 6;

	public static readonly int BoardHeight = 6;

	public List<PawnBase> RemovablePawns = new List<PawnBase>();

	private BoardInformation _startState;

	public override int TileCount => BoardWidth * BoardHeight;

	protected override bool RotateBoard => true;

	protected override bool PreMovementStagePresent => true;

	protected override bool DiceRollRequired => false;

	public BoardGameKonane(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
		: base(mission, new TextObject("{=5DSafcSC}Konane"), startingPlayer)
	{
		if (base.Tiles == null)
		{
			base.Tiles = new TileBase[TileCount];
		}
		SelectedUnit = null;
		PawnUnselectedFactor = 4287395960u;
	}

	public override void InitializeUnits()
	{
		base.PlayerOneUnits.Clear();
		base.PlayerTwoUnits.Clear();
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		for (int i = 0; i < 18; i++)
		{
			GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
			list.Add(InitializeUnit(new PawnKonane(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
		}
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int j = 0; j < 18; j++)
		{
			GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
			list2.Add(InitializeUnit(new PawnKonane(entity2, base.PlayerWhoStarted != PlayerTurn.PlayerOne)));
		}
	}

	public override void InitializeTiles()
	{
		IEnumerable<GameEntity> source = from gameEntity2 in BoardEntity.GetChildren()
			where gameEntity2.Tags.Any((string t) => t.Contains("tile_"))
			select gameEntity2;
		IEnumerable<GameEntity> source2 = from gameEntity2 in BoardEntity.GetChildren()
			where gameEntity2.Tags.Any((string t) => t.Contains("decal_"))
			select gameEntity2;
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
		PawnKonane pawnKonane = pawn as PawnKonane;
		if (pawn != null)
		{
			int x = pawnKonane.X;
			int y = pawnKonane.Y;
			if (!base.InPreMovementStage && pawn.IsPlaced)
			{
				if (x > 1)
				{
					PawnBase pawnOnTile = GetTile(x - 1, y).PawnOnTile;
					PawnBase pawnOnTile2 = GetTile(x - 2, y).PawnOnTile;
					if (pawnOnTile != null && pawnOnTile2 == null && pawnOnTile.PlayerOne != pawn.PlayerOne)
					{
						Move item = default(Move);
						item.Unit = pawn;
						item.GoalTile = GetTile(x - 2, y);
						list.Add(item);
						if (x > 3)
						{
							PawnBase pawnOnTile3 = GetTile(x - 3, y).PawnOnTile;
							PawnBase pawnOnTile4 = GetTile(x - 4, y).PawnOnTile;
							if (pawnOnTile3 != null && pawnOnTile4 == null && pawnOnTile3.PlayerOne != pawn.PlayerOne)
							{
								Move item2 = default(Move);
								item2.Unit = pawn;
								item2.GoalTile = GetTile(x - 4, y);
								list.Add(item2);
							}
						}
					}
				}
				if (x < BoardWidth - 2)
				{
					PawnBase pawnOnTile5 = GetTile(x + 1, y).PawnOnTile;
					PawnBase pawnOnTile6 = GetTile(x + 2, y).PawnOnTile;
					if (pawnOnTile5 != null && pawnOnTile6 == null && pawnOnTile5.PlayerOne != pawn.PlayerOne)
					{
						Move item3 = default(Move);
						item3.Unit = pawn;
						item3.GoalTile = GetTile(x + 2, y);
						list.Add(item3);
						if (x < 2)
						{
							PawnBase pawnOnTile7 = GetTile(x + 3, y).PawnOnTile;
							PawnBase pawnOnTile8 = GetTile(x + 4, y).PawnOnTile;
							if (pawnOnTile7 != null && pawnOnTile8 == null && pawnOnTile7.PlayerOne != pawn.PlayerOne)
							{
								Move item4 = default(Move);
								item4.Unit = pawn;
								item4.GoalTile = GetTile(x + 4, y);
								list.Add(item4);
							}
						}
					}
				}
				if (y > 1)
				{
					PawnBase pawnOnTile9 = GetTile(x, y - 1).PawnOnTile;
					PawnBase pawnOnTile10 = GetTile(x, y - 2).PawnOnTile;
					if (pawnOnTile9 != null && pawnOnTile10 == null && pawnOnTile9.PlayerOne != pawn.PlayerOne)
					{
						Move item5 = default(Move);
						item5.Unit = pawn;
						item5.GoalTile = GetTile(x, y - 2);
						list.Add(item5);
						if (y > 3)
						{
							PawnBase pawnOnTile11 = GetTile(x, y - 3).PawnOnTile;
							PawnBase pawnOnTile12 = GetTile(x, y - 4).PawnOnTile;
							if (pawnOnTile11 != null && pawnOnTile12 == null && pawnOnTile11.PlayerOne != pawn.PlayerOne)
							{
								Move item6 = default(Move);
								item6.Unit = pawn;
								item6.GoalTile = GetTile(x, y - 4);
								list.Add(item6);
							}
						}
					}
				}
				if (y < BoardHeight - 2)
				{
					PawnBase pawnOnTile13 = GetTile(x, y + 1).PawnOnTile;
					PawnBase pawnOnTile14 = GetTile(x, y + 2).PawnOnTile;
					if (pawnOnTile13 != null && pawnOnTile14 == null && pawnOnTile13.PlayerOne != pawn.PlayerOne)
					{
						Move item7 = default(Move);
						item7.Unit = pawn;
						item7.GoalTile = GetTile(x, y + 2);
						list.Add(item7);
						if (y < 2)
						{
							PawnBase pawnOnTile15 = GetTile(x, y + 3).PawnOnTile;
							PawnBase pawnOnTile16 = GetTile(x, y + 4).PawnOnTile;
							if (pawnOnTile15 != null && pawnOnTile16 == null && pawnOnTile15.PlayerOne != pawn.PlayerOne)
							{
								Move item8 = default(Move);
								item8.Unit = pawn;
								item8.GoalTile = GetTile(x, y + 4);
								list.Add(item8);
							}
						}
					}
				}
			}
		}
		return list;
	}

	public override void SetPawnCaptured(PawnBase pawn, bool fake = false)
	{
		base.SetPawnCaptured(pawn, fake);
		PawnKonane pawnKonane = pawn as PawnKonane;
		GetTile(pawnKonane.X, pawnKonane.Y).PawnOnTile = null;
		pawnKonane.PrevX = pawnKonane.X;
		pawnKonane.PrevY = pawnKonane.Y;
		pawnKonane.X = -1;
		pawnKonane.Y = -1;
		if (!fake)
		{
			RemovePawnFromBoard(pawnKonane, 0.6f);
		}
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

	protected override void HandlePreMovementStage(float dt)
	{
		if (base.InputManager.IsHotKeyPressed("BoardGamePawnSelect"))
		{
			PawnBase hoveredPawnIfAny = GetHoveredPawnIfAny();
			if (hoveredPawnIfAny != null && RemovablePawns.Contains(hoveredPawnIfAny))
			{
				SetPawnCaptured(hoveredPawnIfAny);
				UnFocusRemovablePawns();
				EndTurn();
			}
		}
		else
		{
			SelectedUnit = null;
		}
	}

	protected override void HandlePreMovementStageAI(Move move)
	{
		SetPawnCaptured(move.Unit);
		EndTurn();
	}

	protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
	{
		base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
		Tile2D tile2D = tile as Tile2D;
		PawnKonane pawnKonane = pawn as PawnKonane;
		if (tile2D.PawnOnTile != null || pawnKonane == null)
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
		float speed = 0.5f;
		if (!base.InPreMovementStage)
		{
			speed = 0.3f;
		}
		pawnKonane.MovingToDifferentTile = pawnKonane.X != tile2D.X || pawnKonane.Y != tile2D.Y;
		pawnKonane.PrevX = pawnKonane.X;
		pawnKonane.PrevY = pawnKonane.Y;
		pawnKonane.X = tile2D.X;
		pawnKonane.Y = tile2D.Y;
		if (pawnKonane.PrevX != -1 && pawnKonane.PrevY != -1)
		{
			GetTile(pawnKonane.PrevX, pawnKonane.PrevY).PawnOnTile = null;
		}
		tile.PawnOnTile = pawnKonane;
		if (instantMove || base.InPreMovementStage || JustStoppedDraggingUnit)
		{
			pawnKonane.AddGoalPosition(globalPosition);
			pawnKonane.MovePawnToGoalPositionsDelayed(instantMove, speed, dragged: true, delay);
		}
		else
		{
			Tile2D prevTile = GetTile(pawnKonane.PrevX, pawnKonane.PrevY) as Tile2D;
			SetAllGoalPositions(pawnKonane, prevTile, speed);
		}
		if (instantMove && !base.InPreMovementStage)
		{
			CheckWhichPawnsAreCaptured(pawnKonane);
		}
		else if (pawnKonane == SelectedUnit && instantMove)
		{
			SelectedUnit = null;
		}
		ClearValidMoves();
	}

	protected override void SwitchPlayerTurn()
	{
		if ((base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting) && !base.InPreMovementStage && SelectedUnit != null)
		{
			CheckWhichPawnsAreCaptured(SelectedUnit as PawnKonane);
		}
		SelectedUnit = null;
		bool flag = false;
		if (base.InPreMovementStage)
		{
			base.InPreMovementStage = !CheckPlacementStageOver();
			flag = !base.InPreMovementStage;
		}
		if (!flag)
		{
			if (base.PlayerTurn == PlayerTurn.PlayerOneWaiting)
			{
				base.PlayerTurn = PlayerTurn.PlayerTwo;
			}
			else if (base.PlayerTurn == PlayerTurn.PlayerTwoWaiting)
			{
				base.PlayerTurn = PlayerTurn.PlayerOne;
			}
		}
		if (base.InPreMovementStage)
		{
			if (base.PlayerTurn == PlayerTurn.PlayerOne)
			{
				CheckForRemovablePawns(playerOne: true);
			}
			else if (base.PlayerTurn == PlayerTurn.PlayerTwo)
			{
				CheckForRemovablePawns(playerOne: false);
			}
		}
		else if (flag)
		{
			EndTurn();
		}
		else
		{
			CheckGameEnded();
		}
		base.SwitchPlayerTurn();
	}

	protected override bool CheckGameEnded()
	{
		bool result = false;
		if (base.PlayerTurn == PlayerTurn.PlayerTwo)
		{
			List<List<Move>> moves = CalculateAllValidMoves(BoardGameSide.AI);
			if (!HasMovesAvailable(ref moves))
			{
				OnVictory();
				ReadyToPlay = false;
				result = true;
			}
		}
		else if (base.PlayerTurn == PlayerTurn.PlayerOne)
		{
			List<List<Move>> moves2 = CalculateAllValidMoves(BoardGameSide.None);
			if (!HasMovesAvailable(ref moves2))
			{
				OnDefeat();
				ReadyToPlay = false;
				result = true;
			}
		}
		return result;
	}

	protected override void OnAfterBoardSetUp()
	{
		if (_startState.PawnInformation == null)
		{
			_startState = TakeBoardSnapshot();
		}
		ReadyToPlay = true;
		CheckForRemovablePawns(base.PlayerWhoStarted == PlayerTurn.PlayerOne);
	}

	public void AIMakeMove(Move move)
	{
		Tile2D tile2D = move.GoalTile as Tile2D;
		PawnKonane pawnKonane = move.Unit as PawnKonane;
		if (tile2D.PawnOnTile == null)
		{
			pawnKonane.PrevX = pawnKonane.X;
			pawnKonane.PrevY = pawnKonane.Y;
			pawnKonane.X = tile2D.X;
			pawnKonane.Y = tile2D.Y;
			GetTile(pawnKonane.PrevX, pawnKonane.PrevY).PawnOnTile = null;
			tile2D.PawnOnTile = pawnKonane;
			CheckWhichPawnsAreCaptured(pawnKonane, fake: true);
		}
	}

	public int CheckForRemovablePawns(bool playerOne)
	{
		UnFocusRemovablePawns();
		switch (playerOne ? GetPlayerTwoUnitsDead() : GetPlayerOneUnitsDead())
		{
		case 0:
			foreach (PawnKonane item in playerOne ? base.PlayerOneUnits : base.PlayerTwoUnits)
			{
				if (item.X == 0 && item.Y == 0)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 5 && item.Y == 0)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 0 && item.Y == 5)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 5 && item.Y == 5)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 2 && item.Y == 2)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 3 && item.Y == 2)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 2 && item.Y == 3)
				{
					RemovablePawns.Add(item);
				}
				else if (item.X == 3 && item.Y == 3)
				{
					RemovablePawns.Add(item);
				}
			}
			break;
		case 1:
			foreach (PawnKonane item2 in playerOne ? base.PlayerTwoUnits : base.PlayerOneUnits)
			{
				if (item2.X == -1 && item2.Y == -1)
				{
					if (item2.PrevX == 0 && item2.PrevY == 0)
					{
						RemovablePawns.Add(GetTile(1, 0).PawnOnTile);
						RemovablePawns.Add(GetTile(0, 1).PawnOnTile);
					}
					else if (item2.PrevX == 5 && item2.PrevY == 0)
					{
						RemovablePawns.Add(GetTile(4, 0).PawnOnTile);
						RemovablePawns.Add(GetTile(5, 1).PawnOnTile);
					}
					else if (item2.PrevX == 0 && item2.PrevY == 5)
					{
						RemovablePawns.Add(GetTile(0, 4).PawnOnTile);
						RemovablePawns.Add(GetTile(1, 5).PawnOnTile);
					}
					else if (item2.PrevX == 5 && item2.PrevY == 5)
					{
						RemovablePawns.Add(GetTile(5, 4).PawnOnTile);
						RemovablePawns.Add(GetTile(4, 5).PawnOnTile);
					}
					if (item2.PrevX == 2 && item2.PrevY == 2)
					{
						RemovablePawns.Add(GetTile(2, 3).PawnOnTile);
						RemovablePawns.Add(GetTile(3, 2).PawnOnTile);
					}
					else if (item2.PrevX == 3 && item2.PrevY == 2)
					{
						RemovablePawns.Add(GetTile(2, 2).PawnOnTile);
						RemovablePawns.Add(GetTile(3, 3).PawnOnTile);
					}
					else if (item2.PrevX == 2 && item2.PrevY == 3)
					{
						RemovablePawns.Add(GetTile(3, 3).PawnOnTile);
						RemovablePawns.Add(GetTile(2, 2).PawnOnTile);
					}
					else if (item2.PrevX == 3 && item2.PrevY == 3)
					{
						RemovablePawns.Add(GetTile(2, 3).PawnOnTile);
						RemovablePawns.Add(GetTile(3, 2).PawnOnTile);
					}
					break;
				}
			}
			break;
		default:
			Debug.FailedAssert("[DEBUG]This should not be reached!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameKonane.cs", "CheckForRemovablePawns", 655);
			break;
		}
		FocusRemovablePawns();
		return RemovablePawns.Count;
	}

	public BoardInformation TakeBoardSnapshot()
	{
		PawnInformation[] pawns = new PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
		TileBaseInformation[,] tiles = new TileBaseInformation[BoardWidth, BoardHeight];
		int num = 0;
		foreach (PawnKonane item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			pawns[num++] = new PawnInformation(item.X, item.Y, item.PrevX, item.PrevY, item.Captured, item.Entity.GlobalPosition);
		}
		foreach (PawnKonane item2 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			pawns[num++] = new PawnInformation(item2.X, item2.Y, item2.PrevX, item2.PrevY, item2.Captured, item2.Entity.GlobalPosition);
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
		foreach (PawnKonane item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			item.X = board.PawnInformation[num].X;
			item.Y = board.PawnInformation[num].Y;
			item.PrevX = board.PawnInformation[num].PrevX;
			item.PrevY = board.PawnInformation[num].PrevY;
			item.Captured = board.PawnInformation[num].IsCaptured;
			num++;
		}
		foreach (PawnKonane item2 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			item2.X = board.PawnInformation[num].X;
			item2.Y = board.PawnInformation[num].Y;
			item2.PrevX = board.PawnInformation[num].PrevX;
			item2.PrevY = board.PawnInformation[num].PrevY;
			item2.Captured = board.PawnInformation[num].IsCaptured;
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

	protected void CheckWhichPawnsAreCaptured(PawnKonane pawn, bool fake = false)
	{
		int x = pawn.X;
		int y = pawn.Y;
		int prevX = pawn.PrevX;
		int prevY = pawn.PrevY;
		bool flag = false;
		if (x == -1 || y == -1 || prevX == -1 || prevY == -1)
		{
			Debug.FailedAssert("x == -1 || y == -1 || prevX == -1 || prevY == -1", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameKonane.cs", "CheckWhichPawnsAreCaptured", 738);
		}
		Vec2i vec2i = new Vec2i(x - prevX, y - prevY);
		if (vec2i.X == 4 || vec2i.Y == 4 || vec2i.X == -4 || vec2i.Y == -4)
		{
			flag = true;
		}
		else if (vec2i.X == 2 || vec2i.Y == 2 || vec2i.X == -2 || vec2i.Y == -2)
		{
			flag = false;
		}
		else
		{
			Debug.FailedAssert("CheckWhichPawnsAreCaptured", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameKonane.cs", "CheckWhichPawnsAreCaptured", 753);
		}
		if (!flag)
		{
			Vec2i vec2i2 = new Vec2i(vec2i.X / 2, vec2i.Y / 2);
			Vec2i vec2i3 = new Vec2i(x - vec2i2.X, y - vec2i2.Y);
			SetPawnCaptured(GetTile(vec2i3.X, vec2i3.Y).PawnOnTile, fake);
		}
		else
		{
			Vec2i vec2i4 = new Vec2i(vec2i.X / 4, vec2i.Y / 4);
			Vec2i vec2i5 = new Vec2i(x - vec2i4.X, y - vec2i4.Y);
			Vec2i vec2i6 = new Vec2i(x - vec2i4.X - vec2i4.X * 2, y - vec2i4.Y - vec2i4.Y * 2);
			SetPawnCaptured(GetTile(vec2i5.X, vec2i5.Y).PawnOnTile, fake);
			SetPawnCaptured(GetTile(vec2i6.X, vec2i6.Y).PawnOnTile, fake);
		}
	}

	private void SetTile(TileBase tile, int x, int y)
	{
		base.Tiles[y * BoardWidth + x] = tile;
	}

	private TileBase GetTile(int x, int y)
	{
		return base.Tiles[y * BoardWidth + x];
	}

	private void FocusRemovablePawns()
	{
		foreach (PawnKonane removablePawn in RemovablePawns)
		{
			removablePawn.Entity.GetMetaMesh(0).SetFactor1Linear(PawnSelectedFactor);
		}
	}

	private void UnFocusRemovablePawns()
	{
		foreach (PawnKonane removablePawn in RemovablePawns)
		{
			removablePawn.Entity.GetMetaMesh(0).SetFactor1Linear(PawnUnselectedFactor);
		}
		RemovablePawns.Clear();
	}

	private void SetAllGoalPositions(PawnKonane pawn, Tile2D prevTile, float speed)
	{
		_ = prevTile.Entity.GlobalPosition;
		Vec3 globalPosition = GetTile(pawn.X, pawn.Y).Entity.GlobalPosition;
		bool flag = false;
		Vec2i vec2i = new Vec2i(pawn.X - prevTile.X, pawn.Y - prevTile.Y);
		if (vec2i.X == 4 || vec2i.Y == 4 || vec2i.X == -4 || vec2i.Y == -4)
		{
			flag = true;
		}
		if (!flag)
		{
			pawn.AddGoalPosition(globalPosition);
		}
		else
		{
			Vec2i vec2i2 = new Vec2i(vec2i.X / 4, vec2i.Y / 4);
			pawn.AddGoalPosition(GetTile(prevTile.X + 2 * vec2i2.X, prevTile.Y + 2 * vec2i2.Y).Entity.GlobalPosition);
			pawn.AddGoalPosition(globalPosition);
		}
		pawn.MovePawnToGoalPositions(instantMove: false, speed);
	}

	private bool CheckPlacementStageOver()
	{
		bool result = false;
		if (GetPlayerOneUnitsDead() + GetPlayerTwoUnitsDead() == 2)
		{
			result = true;
		}
		return result;
	}

	private void PreplaceUnits()
	{
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int i = 0; i < 18; i++)
		{
			int num = i % 3 * 2;
			int num2 = i / 3;
			float delay = 0.15f * (float)(i + 1) + 0.25f;
			if (num2 % 2 == 0)
			{
				MovePawnToTileDelayed(list[i], GetTile(num, num2), instantMove: false, displayMessage: false, delay);
				MovePawnToTileDelayed(list2[i], GetTile(num + 1, num2), instantMove: false, displayMessage: false, delay);
			}
			else
			{
				MovePawnToTileDelayed(list[i], GetTile(num + 1, num2), instantMove: false, displayMessage: false, delay);
				MovePawnToTileDelayed(list2[i], GetTile(num, num2), instantMove: false, displayMessage: false, delay);
			}
		}
	}

	private void RestoreStartingBoard()
	{
		int num = 0;
		foreach (PawnKonane item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			if (_startState.PawnInformation[num].X != -1)
			{
				if (_startState.PawnInformation[num].X != item.X && _startState.PawnInformation[num].Y != item.Y)
				{
					item.Reset();
					TileBase tile = GetTile(_startState.PawnInformation[num].X, _startState.PawnInformation[num].Y);
					MovePawnToTile(item, tile);
				}
			}
			else if (!item.Entity.GlobalPosition.NearlyEquals(in _startState.PawnInformation[num].Position))
			{
				if (item.X != -1 && GetTile(item.X, item.Y).PawnOnTile == item)
				{
					GetTile(item.X, item.Y).PawnOnTile = null;
				}
				item.Reset();
				item.AddGoalPosition(_startState.PawnInformation[num].Position);
				item.MovePawnToGoalPositions(instantMove: false, 0.5f);
			}
			num++;
		}
		foreach (PawnKonane item2 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			if (_startState.PawnInformation[num].X != -1)
			{
				if (_startState.PawnInformation[num].X != item2.X && _startState.PawnInformation[num].Y != item2.Y)
				{
					TileBase tile2 = GetTile(_startState.PawnInformation[num].X, _startState.PawnInformation[num].Y);
					MovePawnToTile(item2, tile2);
				}
			}
			else
			{
				if (item2.X != -1 && GetTile(item2.X, item2.Y).PawnOnTile == item2)
				{
					GetTile(item2.X, item2.Y).PawnOnTile = null;
				}
				item2.Reset();
				item2.AddGoalPosition(_startState.PawnInformation[num].Position);
				item2.MovePawnToGoalPositions(instantMove: false, 0.5f);
			}
			num++;
		}
	}
}
