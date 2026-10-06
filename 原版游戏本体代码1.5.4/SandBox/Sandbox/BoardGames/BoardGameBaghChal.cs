using System;
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

public class BoardGameBaghChal : BoardGameBase
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

		public readonly bool Captured;

		public readonly Vec3 Position;

		public PawnInformation(int x, int y, int prevX, int prevY, bool captured, Vec3 position)
		{
			X = x;
			Y = y;
			PrevX = prevX;
			PrevY = prevY;
			Captured = captured;
			Position = position;
		}
	}

	public const int UnitCountTiger = 4;

	public const int UnitCountGoat = 20;

	public static readonly int BoardWidth = 5;

	public static readonly int BoardHeight = 5;

	private List<PawnBase> _goatUnits;

	private List<PawnBase> _tigerUnits;

	public override int TileCount => BoardWidth * BoardHeight;

	protected override bool RotateBoard => true;

	protected override bool PreMovementStagePresent => true;

	protected override bool DiceRollRequired => false;

	public BoardGameBaghChal(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
		: base(mission, new TextObject("{=zWoj91XY}BaghChal"), startingPlayer)
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
		bool flag = base.PlayerWhoStarted == PlayerTurn.PlayerOne;
		if (_goatUnits == null && _tigerUnits == null)
		{
			_goatUnits = (flag ? base.PlayerOneUnits : base.PlayerTwoUnits);
			for (int i = 0; i < 20; i++)
			{
				GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
				_goatUnits.Add(InitializeUnit(new PawnBaghChal(entity, flag, isTiger: false)));
			}
			_tigerUnits = (flag ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int j = 0; j < 4; j++)
			{
				GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
				_tigerUnits.Add(InitializeUnit(new PawnBaghChal(entity2, !flag, isTiger: true)));
			}
			return;
		}
		if (_goatUnits == base.PlayerOneUnits != flag)
		{
			List<PawnBase> playerOneUnits = base.PlayerOneUnits;
			base.PlayerOneUnits = base.PlayerTwoUnits;
			base.PlayerTwoUnits = playerOneUnits;
		}
		_goatUnits = (flag ? base.PlayerOneUnits : base.PlayerTwoUnits);
		_tigerUnits = (flag ? base.PlayerTwoUnits : base.PlayerOneUnits);
		foreach (PawnBase goatUnit in _goatUnits)
		{
			goatUnit.Reset();
			goatUnit.SetPlayerOne(flag);
		}
		foreach (PawnBase tigerUnit in _tigerUnits)
		{
			tigerUnit.Reset();
			tigerUnit.SetPlayerOne(!flag);
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
	}

	public override List<List<Move>> CalculateAllValidMoves(BoardGameSide side)
	{
		List<List<Move>> list = new List<List<Move>>();
		bool flag = true;
		foreach (PawnBaghChal item in (side == BoardGameSide.AI) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			if ((flag || item.IsPlaced) && !item.Captured)
			{
				List<Move> list2 = CalculateValidMoves(item);
				if (list2.Count > 0)
				{
					list.Add(list2);
				}
				if (item.IsGoat && !item.IsPlaced)
				{
					flag = false;
				}
			}
		}
		return list;
	}

	public override List<Move> CalculateValidMoves(PawnBase pawn)
	{
		List<Move> list = new List<Move>();
		PawnBaghChal pawnBaghChal = pawn as PawnBaghChal;
		if (pawn != null)
		{
			int x = pawnBaghChal.X;
			int y = pawnBaghChal.Y;
			bool isTiger = pawnBaghChal.IsTiger;
			if ((isTiger || !base.InPreMovementStage) && x >= 0 && x < BoardWidth && y >= 0 && y < BoardHeight)
			{
				if (x > 0 && GetTile(x - 1, y).PawnOnTile == null)
				{
					Move item = default(Move);
					item.Unit = pawn;
					item.GoalTile = GetTile(x - 1, y);
					list.Add(item);
				}
				if (x < BoardWidth - 1 && GetTile(x + 1, y).PawnOnTile == null)
				{
					Move item2 = default(Move);
					item2.Unit = pawn;
					item2.GoalTile = GetTile(x + 1, y);
					list.Add(item2);
				}
				if (y > 0 && GetTile(x, y - 1).PawnOnTile == null)
				{
					Move item3 = default(Move);
					item3.Unit = pawn;
					item3.GoalTile = GetTile(x, y - 1);
					list.Add(item3);
				}
				if (y < BoardHeight - 1 && GetTile(x, y + 1).PawnOnTile == null)
				{
					Move item4 = default(Move);
					item4.Unit = pawn;
					item4.GoalTile = GetTile(x, y + 1);
					list.Add(item4);
				}
				if ((x + y) % 2 == 0)
				{
					Vec2i vec2i = new Vec2i(x + 1, y + 1);
					if (vec2i.X < BoardWidth && vec2i.Y < BoardHeight && GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
					{
						Move item5 = default(Move);
						item5.Unit = pawn;
						item5.GoalTile = GetTile(vec2i.X, vec2i.Y);
						list.Add(item5);
					}
					vec2i = new Vec2i(x - 1, y + 1);
					if (vec2i.X >= 0 && vec2i.Y < BoardHeight && GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
					{
						Move item6 = default(Move);
						item6.Unit = pawn;
						item6.GoalTile = GetTile(vec2i.X, vec2i.Y);
						list.Add(item6);
					}
					vec2i = new Vec2i(x - 1, y - 1);
					if (vec2i.X >= 0 && vec2i.Y >= 0 && GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
					{
						Move item7 = default(Move);
						item7.Unit = pawn;
						item7.GoalTile = GetTile(vec2i.X, vec2i.Y);
						list.Add(item7);
					}
					vec2i = new Vec2i(x + 1, y - 1);
					if (vec2i.X < BoardWidth && vec2i.Y >= 0 && GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
					{
						Move item8 = default(Move);
						item8.Unit = pawn;
						item8.GoalTile = GetTile(vec2i.X, vec2i.Y);
						list.Add(item8);
					}
				}
			}
			if (isTiger && x >= 0 && x < BoardWidth && y >= 0 && y < BoardHeight)
			{
				if (x > 1)
				{
					PawnBaghChal pawnBaghChal2 = GetTile(x - 1, y).PawnOnTile as PawnBaghChal;
					PawnBase pawnOnTile = GetTile(x - 2, y).PawnOnTile;
					if (pawnBaghChal2 != null && !pawnBaghChal2.IsTiger && pawnOnTile == null)
					{
						Move item9 = default(Move);
						item9.Unit = pawn;
						item9.GoalTile = GetTile(x - 2, y);
						list.Add(item9);
					}
				}
				if (x < BoardWidth - 2)
				{
					PawnBaghChal pawnBaghChal3 = GetTile(x + 1, y).PawnOnTile as PawnBaghChal;
					PawnBase pawnOnTile2 = GetTile(x + 2, y).PawnOnTile;
					if (pawnBaghChal3 != null && !pawnBaghChal3.IsTiger && pawnOnTile2 == null)
					{
						Move item10 = default(Move);
						item10.Unit = pawn;
						item10.GoalTile = GetTile(x + 2, y);
						list.Add(item10);
					}
				}
				if (y > 1)
				{
					PawnBaghChal pawnBaghChal4 = GetTile(x, y - 1).PawnOnTile as PawnBaghChal;
					PawnBase pawnOnTile3 = GetTile(x, y - 2).PawnOnTile;
					if (pawnBaghChal4 != null && !pawnBaghChal4.IsTiger && pawnOnTile3 == null)
					{
						Move item11 = default(Move);
						item11.Unit = pawn;
						item11.GoalTile = GetTile(x, y - 2);
						list.Add(item11);
					}
				}
				if (y < BoardHeight - 2)
				{
					PawnBaghChal pawnBaghChal5 = GetTile(x, y + 1).PawnOnTile as PawnBaghChal;
					PawnBase pawnOnTile4 = GetTile(x, y + 2).PawnOnTile;
					if (pawnBaghChal5 != null && !pawnBaghChal5.IsTiger && pawnOnTile4 == null)
					{
						Move item12 = default(Move);
						item12.Unit = pawn;
						item12.GoalTile = GetTile(x, y + 2);
						list.Add(item12);
					}
				}
				if ((x + y) % 2 == 0)
				{
					Vec2i vec2i2 = new Vec2i(x + 2, y + 2);
					if (vec2i2.X < BoardWidth && vec2i2.Y < BoardHeight && GetTile(x + 1, y + 1).PawnOnTile is PawnBaghChal { IsTiger: false } && GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
					{
						Move item13 = default(Move);
						item13.Unit = pawn;
						item13.GoalTile = GetTile(vec2i2.X, vec2i2.Y);
						list.Add(item13);
					}
					vec2i2 = new Vec2i(x - 2, y + 2);
					if (vec2i2.X >= 0 && vec2i2.Y < BoardHeight && GetTile(x - 1, y + 1).PawnOnTile is PawnBaghChal { IsTiger: false } && GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
					{
						Move item14 = default(Move);
						item14.Unit = pawn;
						item14.GoalTile = GetTile(vec2i2.X, vec2i2.Y);
						list.Add(item14);
					}
					vec2i2 = new Vec2i(x - 2, y - 2);
					if (vec2i2.X >= 0 && vec2i2.Y >= 0 && GetTile(x - 1, y - 1).PawnOnTile is PawnBaghChal { IsTiger: false } && GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
					{
						Move item15 = default(Move);
						item15.Unit = pawn;
						item15.GoalTile = GetTile(vec2i2.X, vec2i2.Y);
						list.Add(item15);
					}
					vec2i2 = new Vec2i(x + 2, y - 2);
					if (vec2i2.X < BoardWidth && vec2i2.Y >= 0 && GetTile(x + 1, y - 1).PawnOnTile is PawnBaghChal { IsTiger: false } && GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
					{
						Move item16 = default(Move);
						item16.Unit = pawn;
						item16.GoalTile = GetTile(vec2i2.X, vec2i2.Y);
						list.Add(item16);
					}
				}
			}
			if (!isTiger && base.InPreMovementStage && x == -1 && y == -1)
			{
				Move item17 = default(Move);
				for (int i = 0; i < TileCount; i++)
				{
					if (base.Tiles[i].PawnOnTile == null)
					{
						item17.Unit = pawn;
						item17.GoalTile = base.Tiles[i];
						list.Add(item17);
					}
				}
			}
		}
		return list;
	}

	public override void SetPawnCaptured(PawnBase pawn, bool fake = false)
	{
		base.SetPawnCaptured(pawn, fake);
		PawnBaghChal pawnBaghChal = pawn as PawnBaghChal;
		GetTile(pawnBaghChal.X, pawnBaghChal.Y).PawnOnTile = null;
		pawnBaghChal.PrevX = pawnBaghChal.X;
		pawnBaghChal.PrevY = pawnBaghChal.Y;
		pawnBaghChal.X = -1;
		pawnBaghChal.Y = -1;
		if (!fake)
		{
			RemovePawnFromBoard(pawnBaghChal, 0.6f);
		}
	}

	protected override void HandlePreMovementStage(float dt)
	{
		Move move = HandlePlayerInput(dt);
		if (move.IsValid)
		{
			MovePawnToTile(move.Unit, move.GoalTile);
		}
	}

	protected override void HandlePreMovementStageAI(Move move)
	{
		MovePawnToTile(move.Unit, move.GoalTile);
	}

	protected override PawnBase SelectPawn(PawnBase pawn)
	{
		if (pawn.PlayerOne == (base.PlayerTurn == PlayerTurn.PlayerOne))
		{
			if (base.PlayerTurn == base.PlayerWhoStarted)
			{
				if (base.InPreMovementStage)
				{
					if (!pawn.IsPlaced && !pawn.Captured)
					{
						SelectedUnit = pawn;
					}
				}
				else
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

	protected override void SwitchPlayerTurn()
	{
		if ((base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting) && SelectedUnit != null)
		{
			CheckIfPawnCaptures(SelectedUnit as PawnBaghChal);
		}
		SelectedUnit = null;
		if (base.PlayerTurn == PlayerTurn.PlayerOneWaiting)
		{
			base.PlayerTurn = PlayerTurn.PlayerTwo;
		}
		else if (base.PlayerTurn == PlayerTurn.PlayerTwoWaiting)
		{
			base.PlayerTurn = PlayerTurn.PlayerOne;
		}
		if (base.InPreMovementStage)
		{
			base.InPreMovementStage = !CheckPlacementStageOver();
		}
		CheckGameEnded();
		base.SwitchPlayerTurn();
	}

	protected override bool CheckGameEnded()
	{
		bool result = false;
		if (base.PlayerTurn == PlayerTurn.PlayerTwo || base.PlayerTurn == PlayerTurn.PlayerOne)
		{
			List<List<Move>> moves = CalculateAllValidMoves((base.PlayerWhoStarted != PlayerTurn.PlayerOne) ? BoardGameSide.Player : BoardGameSide.AI);
			if (!HasMovesAvailable(ref moves))
			{
				if (base.PlayerWhoStarted == PlayerTurn.PlayerOne)
				{
					OnVictory();
				}
				else
				{
					OnDefeat();
				}
				ReadyToPlay = false;
				result = true;
			}
			else
			{
				int num = 0;
				foreach (PawnBaghChal goatUnit in _goatUnits)
				{
					if (goatUnit.Captured)
					{
						num++;
					}
				}
				if (num >= 5)
				{
					if (base.PlayerWhoStarted == PlayerTurn.PlayerOne)
					{
						OnDefeat();
					}
					else
					{
						OnVictory();
					}
					ReadyToPlay = false;
					result = true;
				}
			}
		}
		return result;
	}

	protected override void OnAfterBoardRotated()
	{
		PreplaceUnits();
	}

	protected override void OnAfterBoardSetUp()
	{
		ReadyToPlay = true;
	}

	protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
	{
		base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
		Tile2D tile2D = tile as Tile2D;
		PawnBaghChal pawnBaghChal = pawn as PawnBaghChal;
		if (tile2D.PawnOnTile != null || pawnBaghChal == null)
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
		pawnBaghChal.MovingToDifferentTile = pawnBaghChal.X != tile2D.X || pawnBaghChal.Y != tile2D.Y;
		pawnBaghChal.PrevX = pawnBaghChal.X;
		pawnBaghChal.PrevY = pawnBaghChal.Y;
		pawnBaghChal.X = tile2D.X;
		pawnBaghChal.Y = tile2D.Y;
		if (pawnBaghChal.PrevX != -1 && pawnBaghChal.PrevY != -1)
		{
			GetTile(pawnBaghChal.PrevX, pawnBaghChal.PrevY).PawnOnTile = null;
		}
		tile2D.PawnOnTile = pawnBaghChal;
		if (pawnBaghChal.Entity.GlobalPosition.z < globalPosition.z)
		{
			Vec3 globalPosition2 = pawnBaghChal.Entity.GlobalPosition;
			globalPosition2.z = globalPosition.z;
			pawnBaghChal.AddGoalPosition(globalPosition2);
		}
		pawnBaghChal.AddGoalPosition(globalPosition);
		pawnBaghChal.MovePawnToGoalPositionsDelayed(instantMove, speed, JustStoppedDraggingUnit, delay);
		if (instantMove && !base.InPreMovementStage)
		{
			CheckIfPawnCaptures(SelectedUnit as PawnBaghChal);
		}
		else if (pawnBaghChal == SelectedUnit && instantMove)
		{
			SelectedUnit = null;
		}
	}

	public void AIMakeMove(Move move)
	{
		Tile2D tile2D = move.GoalTile as Tile2D;
		PawnBaghChal pawnBaghChal = move.Unit as PawnBaghChal;
		if (tile2D.PawnOnTile == null)
		{
			pawnBaghChal.PrevX = pawnBaghChal.X;
			pawnBaghChal.PrevY = pawnBaghChal.Y;
			pawnBaghChal.X = tile2D.X;
			pawnBaghChal.Y = tile2D.Y;
			if (pawnBaghChal.PrevX != -1 && pawnBaghChal.PrevY != -1)
			{
				GetTile(pawnBaghChal.PrevX, pawnBaghChal.PrevY).PawnOnTile = null;
			}
			tile2D.PawnOnTile = pawnBaghChal;
			CheckIfPawnCaptures(pawnBaghChal, fake: true);
		}
	}

	public BoardInformation TakeBoardSnapshot()
	{
		PawnInformation[] pawns = new PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
		TileBaseInformation[,] tiles = new TileBaseInformation[BoardWidth, BoardHeight];
		int num = 0;
		foreach (PawnBaghChal goatUnit in _goatUnits)
		{
			pawns[num++] = new PawnInformation(goatUnit.X, goatUnit.Y, goatUnit.PrevX, goatUnit.PrevY, goatUnit.Captured, goatUnit.Entity.GlobalPosition);
		}
		foreach (PawnBaghChal tigerUnit in _tigerUnits)
		{
			pawns[num++] = new PawnInformation(tigerUnit.X, tigerUnit.Y, tigerUnit.PrevX, tigerUnit.PrevY, tigerUnit.Captured, tigerUnit.Entity.GlobalPosition);
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
		foreach (PawnBaghChal goatUnit in _goatUnits)
		{
			goatUnit.X = board.PawnInformation[num].X;
			goatUnit.Y = board.PawnInformation[num].Y;
			goatUnit.PrevX = board.PawnInformation[num].PrevX;
			goatUnit.PrevY = board.PawnInformation[num].PrevY;
			goatUnit.Captured = board.PawnInformation[num].Captured;
			num++;
		}
		foreach (PawnBaghChal tigerUnit in _tigerUnits)
		{
			tigerUnit.X = board.PawnInformation[num].X;
			tigerUnit.Y = board.PawnInformation[num].Y;
			tigerUnit.PrevX = board.PawnInformation[num].PrevX;
			tigerUnit.PrevY = board.PawnInformation[num].PrevY;
			tigerUnit.Captured = board.PawnInformation[num].Captured;
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

	public PawnBaghChal GetANonePlacedGoat()
	{
		foreach (PawnBaghChal goatUnit in _goatUnits)
		{
			if (!goatUnit.Captured && !goatUnit.IsPlaced)
			{
				return goatUnit;
			}
		}
		return null;
	}

	protected void CheckIfPawnCaptures(PawnBaghChal pawn, bool fake = false)
	{
		if (!pawn.IsTiger)
		{
			return;
		}
		int x = pawn.X;
		int y = pawn.Y;
		int prevX = pawn.PrevX;
		int prevY = pawn.PrevY;
		if (x == -1 || y == -1 || prevX == -1 || prevY == -1)
		{
			Debug.FailedAssert("x == -1 || y == -1 || prevX == -1 || prevY == -1", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBaghChal.cs", "CheckIfPawnCaptures", 819);
		}
		Vec2i vec2i = new Vec2i(x - prevX, y - prevY);
		Vec2i vec2i2 = new Vec2i(vec2i.X / 2, vec2i.Y / 2);
		int num = vec2i.X + vec2i.Y;
		if (x == prevX || y == prevY)
		{
			if (num == 1 || num == -1)
			{
				return;
			}
		}
		else if (vec2i.X == 1 || vec2i.X == -1)
		{
			return;
		}
		Vec2i vec2i3 = new Vec2i(x - vec2i2.X, y - vec2i2.Y);
		SetPawnCaptured(GetTile(vec2i3.X, vec2i3.Y).PawnOnTile, fake);
	}

	private void PreplaceUnits()
	{
		MovePawnToTileDelayed(_tigerUnits[0], GetTile(0, 0), instantMove: false, displayMessage: false, 0.4f);
		MovePawnToTileDelayed(_tigerUnits[1], GetTile(4, 0), instantMove: false, displayMessage: false, 0.55f);
		MovePawnToTileDelayed(_tigerUnits[2], GetTile(0, 4), instantMove: false, displayMessage: false, 0.70000005f);
		MovePawnToTileDelayed(_tigerUnits[3], GetTile(4, 4), instantMove: false, displayMessage: false, 0.85f);
		for (int i = 0; i < 20; i++)
		{
			PawnBaghChal pawnBaghChal = _goatUnits[i] as PawnBaghChal;
			MatrixFrame frame = pawnBaghChal.Entity.GetGlobalFrame();
			MatrixFrame frame2 = pawnBaghChal.InitialFrame;
			if (base.PlayerWhoStarted != PlayerTurn.PlayerOne)
			{
				frame2.rotation.RotateAboutUp(System.MathF.PI);
			}
			pawnBaghChal.Entity.SetFrame(ref frame2);
			Vec3 v = pawnBaghChal.Entity.GetGlobalFrame().origin;
			pawnBaghChal.Entity.SetGlobalFrame(in frame);
			if (!pawnBaghChal.Entity.GlobalPosition.NearlyEquals(in v))
			{
				Vec3 globalPosition = pawnBaghChal.Entity.GlobalPosition;
				globalPosition.z = BoardEntity.GlobalBoxMax.z;
				pawnBaghChal.AddGoalPosition(globalPosition);
				globalPosition.x = v.x;
				globalPosition.y = v.y;
				pawnBaghChal.AddGoalPosition(globalPosition);
				pawnBaghChal.AddGoalPosition(v);
				pawnBaghChal.MovePawnToGoalPositions(instantMove: false, 0.5f);
			}
		}
	}

	private bool CheckPlacementStageOver()
	{
		bool result = false;
		int num = 0;
		foreach (PawnBaghChal goatUnit in _goatUnits)
		{
			if (goatUnit.Captured || goatUnit.IsPlaced)
			{
				num++;
			}
		}
		if (num == 20)
		{
			result = true;
		}
		return result;
	}

	private void SetTile(TileBase tile, int x, int y)
	{
		base.Tiles[y * BoardWidth + x] = tile;
	}

	private TileBase GetTile(int x, int y)
	{
		return base.Tiles[y * BoardWidth + x];
	}
}
