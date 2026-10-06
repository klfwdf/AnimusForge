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

public class BoardGameTablut : BoardGameBase
{
	public struct PawnInformation
	{
		public int X;

		public int Y;

		public bool IsCaptured;

		public PawnInformation(int x, int y, bool captured)
		{
			X = x;
			Y = y;
			IsCaptured = captured;
		}
	}

	public struct BoardInformation
	{
		public readonly PawnInformation[] PawnInformation;

		public BoardInformation(ref PawnInformation[] pawns)
		{
			PawnInformation = pawns;
		}
	}

	public enum State
	{
		InProgress,
		Aborted,
		PlayerWon,
		AIWon
	}

	public const int BoardWidth = 9;

	public const int BoardHeight = 9;

	public const int AttackerPawnCount = 16;

	public const int DefenderPawnCount = 9;

	private BoardInformation _startState;

	public override int TileCount => 81;

	protected override bool RotateBoard => false;

	protected override bool PreMovementStagePresent => false;

	protected override bool DiceRollRequired => false;

	private PawnTablut King { get; set; }

	public BoardGameTablut(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
		: base(mission, new TextObject("{=qeKskdiY}Tablut"), startingPlayer)
	{
		SelectedUnit = null;
		PawnUnselectedFactor = 4287395960u;
	}

	public static bool IsCitadelTile(int tileX, int tileY)
	{
		if (tileX == 4)
		{
			return tileY == 4;
		}
		return false;
	}

	public override void InitializeUnits()
	{
		base.PlayerOneUnits.Clear();
		base.PlayerTwoUnits.Clear();
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		for (int i = 0; i < 16; i++)
		{
			GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
			list.Add(InitializeUnit(new PawnTablut(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
		}
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int j = 0; j < 9; j++)
		{
			GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
			list2.Add(InitializeUnit(new PawnTablut(entity2, base.PlayerWhoStarted != PlayerTurn.PlayerOne)));
		}
		King = list2[0] as PawnTablut;
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
		while (x < 9)
		{
			int y;
			int num;
			for (y = 0; y < 9; y = num)
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
		List<Move> list = new List<Move>(16);
		if (pawn.IsPlaced && !pawn.Captured)
		{
			PawnTablut pawnTablut = pawn as PawnTablut;
			int num = pawnTablut.X;
			int num2 = pawnTablut.Y;
			while (num > 0)
			{
				num--;
				if (!AddValidMove(list, pawn, num, num2))
				{
					break;
				}
			}
			num = pawnTablut.X;
			while (num < 8)
			{
				num++;
				if (!AddValidMove(list, pawn, num, num2))
				{
					break;
				}
			}
			num = pawnTablut.X;
			while (num2 < 8)
			{
				num2++;
				if (!AddValidMove(list, pawn, num, num2))
				{
					break;
				}
			}
			num2 = pawnTablut.Y;
			while (num2 > 0)
			{
				num2--;
				if (!AddValidMove(list, pawn, num, num2))
				{
					break;
				}
			}
			num2 = pawnTablut.Y;
		}
		return list;
	}

	public override void SetPawnCaptured(PawnBase pawn, bool fake = false)
	{
		base.SetPawnCaptured(pawn, fake);
		PawnTablut pawnTablut = pawn as PawnTablut;
		GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = null;
		pawnTablut.X = -1;
		pawnTablut.Y = -1;
		if (!fake)
		{
			RemovePawnFromBoard(pawnTablut, 0.6f);
		}
	}

	protected override void OnAfterBoardSetUp()
	{
		if (_startState.PawnInformation == null)
		{
			_startState = TakeBoardSnapshot();
		}
		ReadyToPlay = true;
	}

	protected override PawnBase SelectPawn(PawnBase pawn)
	{
		if (pawn.PlayerOne == (base.PlayerTurn == PlayerTurn.PlayerOne))
		{
			SelectedUnit = pawn;
		}
		return pawn;
	}

	protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
	{
		base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
		Tile2D tile2D = tile as Tile2D;
		PawnTablut pawnTablut = pawn as PawnTablut;
		if (tile2D.PawnOnTile != null)
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
		Vec3 globalPosition = pawnTablut.Entity.GlobalPosition;
		Vec3 globalPosition2 = tile2D.Entity.GlobalPosition;
		if (pawnTablut.X != -1 && pawnTablut.Y != -1)
		{
			GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = null;
		}
		pawnTablut.MovingToDifferentTile = pawnTablut.X != tile2D.X || pawnTablut.Y != tile2D.Y;
		pawnTablut.X = tile2D.X;
		pawnTablut.Y = tile2D.Y;
		tile2D.PawnOnTile = pawnTablut;
		if (SettingUpBoard && globalPosition2.z > globalPosition.z)
		{
			Vec3 goal = globalPosition;
			goal.z += 2f * (globalPosition2.z - globalPosition.z);
			pawnTablut.AddGoalPosition(goal);
			pawnTablut.MovePawnToGoalPositionsDelayed(instantMove, 0.5f, JustStoppedDraggingUnit, delay);
		}
		pawnTablut.AddGoalPosition(globalPosition2);
		pawnTablut.MovePawnToGoalPositionsDelayed(instantMove, 0.5f, JustStoppedDraggingUnit, delay);
		if (instantMove)
		{
			CheckIfPawnCaptures(SelectedUnit as PawnTablut);
		}
		if (pawnTablut == SelectedUnit && instantMove)
		{
			SelectedUnit = null;
		}
	}

	protected override void SwitchPlayerTurn()
	{
		if ((base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting) && SelectedUnit != null)
		{
			CheckIfPawnCaptures(SelectedUnit as PawnTablut);
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
		CheckGameEnded();
		base.SwitchPlayerTurn();
	}

	protected override bool CheckGameEnded()
	{
		State state = CheckGameState();
		bool result = true;
		switch (state)
		{
		case State.InProgress:
			result = false;
			break;
		case State.PlayerWon:
			OnVictory();
			ReadyToPlay = false;
			break;
		case State.AIWon:
			OnDefeat();
			ReadyToPlay = false;
			break;
		}
		return result;
	}

	public bool AIMakeMove(Move move)
	{
		Tile2D tile2D = move.GoalTile as Tile2D;
		PawnTablut pawnTablut = move.Unit as PawnTablut;
		if (tile2D.PawnOnTile == null)
		{
			if (pawnTablut.X != -1 && pawnTablut.Y != -1)
			{
				GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = null;
			}
			pawnTablut.X = tile2D.X;
			pawnTablut.Y = tile2D.Y;
			tile2D.PawnOnTile = pawnTablut;
			CheckIfPawnCaptures(pawnTablut, fake: true);
			return true;
		}
		return false;
	}

	public bool HasAvailableMoves(PawnTablut pawn)
	{
		bool result = false;
		if (pawn.IsPlaced && !pawn.Captured)
		{
			int x = pawn.X;
			int y = pawn.Y;
			result = (x > 0 && GetTile(x - 1, y).PawnOnTile == null && !IsCitadelTile(x - 1, y)) || (x < 8 && GetTile(x + 1, y).PawnOnTile == null && !IsCitadelTile(x + 1, y)) || (y > 0 && GetTile(x, y - 1).PawnOnTile == null && !IsCitadelTile(x, y - 1)) || (y < 8 && GetTile(x, y + 1).PawnOnTile == null && !IsCitadelTile(x, y + 1));
		}
		return result;
	}

	public Move GetRandomAvailableMove(PawnTablut pawn)
	{
		List<Move> list = CalculateValidMoves(pawn);
		return list[MBRandom.RandomInt(list.Count)];
	}

	public Move GetWinningMoveIfPresent(BoardGameSide side)
	{
		Move invalid = Move.Invalid;
		if ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && side == BoardGameSide.AI) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && side == BoardGameSide.Player))
		{
			bool flag = false;
			if (King.X <= 4)
			{
				bool flag2 = true;
				for (int num = King.X - 1; num >= 0; num--)
				{
					if (GetTile(num, King.Y).PawnOnTile != null)
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					invalid.Unit = King;
					invalid.GoalTile = GetTile(0, King.Y);
					flag = true;
				}
			}
			if (!flag && King.X >= 4)
			{
				bool flag3 = true;
				for (int i = King.X + 1; i < 9; i++)
				{
					if (GetTile(i, King.Y).PawnOnTile != null)
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					invalid.Unit = King;
					invalid.GoalTile = GetTile(8, King.Y);
					flag = true;
				}
			}
			if (!flag && King.Y <= 4)
			{
				bool flag4 = true;
				for (int num2 = King.Y - 1; num2 >= 0; num2--)
				{
					if (GetTile(King.X, num2).PawnOnTile != null)
					{
						flag4 = false;
						break;
					}
				}
				if (flag4)
				{
					invalid.Unit = King;
					invalid.GoalTile = GetTile(King.X, 0);
					flag = true;
				}
			}
			if (!flag && King.Y >= 4)
			{
				bool flag5 = true;
				for (int j = King.Y + 1; j < 9; j++)
				{
					if (GetTile(King.X, j).PawnOnTile != null)
					{
						flag5 = false;
						break;
					}
				}
				if (flag5)
				{
					invalid.Unit = King;
					invalid.GoalTile = GetTile(King.X, 8);
					flag = true;
				}
			}
		}
		else if (!IsCitadelTile(King.X, King.Y))
		{
			TileBase tile = GetTile(King.X + 1, King.Y);
			TileBase tile2 = GetTile(King.X - 1, King.Y);
			bool flag6 = IsCitadelTile(King.X + 1, King.Y);
			bool flag7 = IsCitadelTile(King.X - 1, King.Y);
			TileBase tile3 = GetTile(King.X, King.Y + 1);
			TileBase tile4 = GetTile(King.X, King.Y - 1);
			bool flag8 = IsCitadelTile(King.X, King.Y + 1);
			bool flag9 = IsCitadelTile(King.X, King.Y - 1);
			bool flag10 = false;
			if (tile2.PawnOnTile == null && !flag7 && (flag6 || (tile.PawnOnTile != null && ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && tile.PawnOnTile.PlayerOne) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && !tile.PawnOnTile.PlayerOne)))))
			{
				int num3 = King.X - 2;
				int num4 = ((num3 >= 4) ? 5 : 0);
				for (int num5 = num3; num5 >= num4; num5--)
				{
					PawnBase pawnOnTile = GetTile(num5, King.Y).PawnOnTile;
					if (pawnOnTile != null)
					{
						if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile.PlayerOne))
						{
							break;
						}
						invalid.Unit = pawnOnTile;
						invalid.GoalTile = tile2;
						flag10 = true;
					}
				}
				if (!flag10)
				{
					int num6 = King.Y - 1;
					int num7 = ((num6 >= 4) ? 5 : 0);
					for (int num8 = num6; num8 >= num7; num8--)
					{
						PawnBase pawnOnTile2 = GetTile(King.X - 1, num8).PawnOnTile;
						if (pawnOnTile2 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile2.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile2.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile2;
							invalid.GoalTile = tile2;
							flag10 = true;
						}
					}
				}
				if (!flag10)
				{
					int num9 = King.Y + 1;
					_ = 4;
					for (int k = num9; k < num9; k++)
					{
						PawnBase pawnOnTile3 = GetTile(King.X - 1, k).PawnOnTile;
						if (pawnOnTile3 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile3.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile3.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile3;
							invalid.GoalTile = tile2;
							flag10 = true;
						}
					}
				}
			}
			if (!flag10 && tile.PawnOnTile == null && !flag6 && (flag7 || (tile2.PawnOnTile != null && ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && tile2.PawnOnTile.PlayerOne) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && !tile2.PawnOnTile.PlayerOne)))))
			{
				int num10 = King.X + 2;
				int num11 = ((num10 > 4) ? 9 : 4);
				for (int l = num10; l < num11; l++)
				{
					PawnBase pawnOnTile4 = GetTile(l, King.Y).PawnOnTile;
					if (pawnOnTile4 != null)
					{
						if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile4.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile4.PlayerOne))
						{
							break;
						}
						invalid.Unit = pawnOnTile4;
						invalid.GoalTile = tile;
						flag10 = true;
					}
				}
				if (!flag10)
				{
					int num12 = King.Y - 1;
					int num13 = ((num12 >= 4) ? 5 : 0);
					for (int num14 = num12; num14 >= num13; num14--)
					{
						PawnBase pawnOnTile5 = GetTile(King.X + 1, num14).PawnOnTile;
						if (pawnOnTile5 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile5.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile5.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile5;
							invalid.GoalTile = tile;
							flag10 = true;
						}
					}
				}
				if (!flag10)
				{
					int num15 = King.Y + 1;
					int num16 = ((num15 > 4) ? 9 : 4);
					for (int m = num15; m < num16; m++)
					{
						PawnBase pawnOnTile6 = GetTile(King.X + 1, m).PawnOnTile;
						if (pawnOnTile6 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile6.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile6.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile6;
							invalid.GoalTile = tile;
							flag10 = true;
						}
					}
				}
			}
			if (!flag10 && tile4.PawnOnTile == null && !flag9 && (flag8 || (tile3.PawnOnTile != null && ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && tile3.PawnOnTile.PlayerOne) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && !tile3.PawnOnTile.PlayerOne)))))
			{
				int num17 = King.X - 1;
				int num18 = ((num17 >= 4) ? 5 : 0);
				for (int num19 = num17; num19 >= num18; num19--)
				{
					PawnBase pawnOnTile7 = GetTile(num19, King.Y - 1).PawnOnTile;
					if (pawnOnTile7 != null)
					{
						if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile7.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile7.PlayerOne))
						{
							break;
						}
						invalid.Unit = pawnOnTile7;
						invalid.GoalTile = tile4;
						flag10 = true;
					}
				}
				if (!flag10)
				{
					int num20 = King.X + 1;
					int num21 = ((num20 > 4) ? 9 : 4);
					for (int n = num20; n < num21; n++)
					{
						PawnBase pawnOnTile8 = GetTile(n, King.Y - 1).PawnOnTile;
						if (pawnOnTile8 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile8.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile8.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile8;
							invalid.GoalTile = tile4;
							flag10 = true;
						}
					}
				}
				if (!flag10)
				{
					int num22 = King.Y - 2;
					int num23 = ((num22 >= 4) ? 5 : 0);
					for (int num24 = num22; num24 >= num23; num24--)
					{
						PawnBase pawnOnTile9 = GetTile(King.X, num24).PawnOnTile;
						if (pawnOnTile9 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile9.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile9.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile9;
							invalid.GoalTile = tile4;
							flag10 = true;
						}
					}
				}
			}
			if (!flag10 && tile3.PawnOnTile == null && !flag8 && (flag9 || (tile4.PawnOnTile != null && ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && tile4.PawnOnTile.PlayerOne) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && !tile4.PawnOnTile.PlayerOne)))))
			{
				int num25 = King.X - 1;
				int num26 = ((num25 >= 4) ? 5 : 0);
				for (int num27 = num25; num27 >= num26; num27--)
				{
					PawnBase pawnOnTile10 = GetTile(num27, King.Y + 1).PawnOnTile;
					if (pawnOnTile10 != null)
					{
						if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile10.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile10.PlayerOne))
						{
							break;
						}
						invalid.Unit = pawnOnTile10;
						invalid.GoalTile = tile3;
						flag10 = true;
					}
				}
				if (!flag10)
				{
					int num28 = King.X + 1;
					int num29 = ((num28 > 4) ? 9 : 4);
					for (int num30 = num28; num30 < num29; num30++)
					{
						PawnBase pawnOnTile11 = GetTile(num30, King.Y + 1).PawnOnTile;
						if (pawnOnTile11 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile11.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile11.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile11;
							invalid.GoalTile = tile3;
							flag10 = true;
						}
					}
				}
				if (!flag10)
				{
					int num31 = King.Y + 2;
					int num32 = ((num31 > 4) ? 9 : 4);
					for (int num33 = num31; num33 < num32; num33++)
					{
						PawnBase pawnOnTile12 = GetTile(King.X, num33).PawnOnTile;
						if (pawnOnTile12 != null)
						{
							if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile12.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile12.PlayerOne))
							{
								break;
							}
							invalid.Unit = pawnOnTile12;
							invalid.GoalTile = tile3;
							flag10 = true;
						}
					}
				}
			}
		}
		return invalid;
	}

	public BoardInformation TakeBoardSnapshot()
	{
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		PawnInformation[] pawns = new PawnInformation[25];
		PawnInformation pawnInformation = default(PawnInformation);
		for (int i = 0; i < 25; i++)
		{
			PawnTablut pawnTablut = ((i >= 16) ? (list2[i - 16] as PawnTablut) : (list[i] as PawnTablut));
			pawnInformation.X = pawnTablut.X;
			pawnInformation.Y = pawnTablut.Y;
			pawnInformation.IsCaptured = pawnTablut.Captured;
			pawns[i] = pawnInformation;
		}
		return new BoardInformation(ref pawns);
	}

	public void UndoMove(ref BoardInformation board)
	{
		for (int i = 0; i < TileCount; i++)
		{
			base.Tiles[i].PawnOnTile = null;
		}
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int j = 0; j < 25; j++)
		{
			PawnInformation pawnInformation = board.PawnInformation[j];
			PawnTablut pawnTablut = ((j >= 16) ? (list2[j - 16] as PawnTablut) : (list[j] as PawnTablut));
			pawnTablut.X = pawnInformation.X;
			pawnTablut.Y = pawnInformation.Y;
			pawnTablut.Captured = pawnInformation.IsCaptured;
			if (pawnTablut.IsPlaced)
			{
				GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = pawnTablut;
			}
		}
	}

	public State CheckGameState()
	{
		State result;
		if (!base.AIOpponent.AbortRequested)
		{
			result = State.InProgress;
			if (base.PlayerTurn == PlayerTurn.PlayerOne || base.PlayerTurn == PlayerTurn.PlayerTwo)
			{
				bool flag = base.PlayerWhoStarted == PlayerTurn.PlayerOne;
				if (King.Captured)
				{
					result = (flag ? State.PlayerWon : State.AIWon);
				}
				else if (King.X == 0 || King.X == 8 || King.Y == 0 || King.Y == 8)
				{
					result = (flag ? State.AIWon : State.PlayerWon);
				}
				else
				{
					bool flag2 = false;
					bool flag3 = base.PlayerTurn == PlayerTurn.PlayerOne;
					List<PawnBase> list = (flag3 ? base.PlayerOneUnits : base.PlayerTwoUnits);
					int count = list.Count;
					for (int i = 0; i < count; i++)
					{
						PawnBase pawnBase = list[i];
						if (pawnBase.IsPlaced && !pawnBase.Captured && HasAvailableMoves(pawnBase as PawnTablut))
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						result = (flag3 ? State.AIWon : State.PlayerWon);
					}
				}
			}
		}
		else
		{
			result = State.Aborted;
		}
		return result;
	}

	private void SetTile(TileBase tile, int x, int y)
	{
		base.Tiles[y * 9 + x] = tile;
	}

	private TileBase GetTile(int x, int y)
	{
		return base.Tiles[y * 9 + x];
	}

	private void PreplaceUnits()
	{
		int[] array = new int[32]
		{
			3, 0, 4, 0, 5, 0, 4, 1, 0, 3,
			0, 4, 0, 5, 1, 4, 8, 3, 8, 4,
			8, 5, 7, 4, 3, 8, 4, 8, 5, 8,
			4, 7
		};
		int[] array2 = new int[18]
		{
			4, 4, 4, 3, 4, 2, 5, 4, 6, 4,
			3, 4, 2, 4, 4, 5, 4, 6
		};
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		int count = list.Count;
		for (int i = 0; i < count; i++)
		{
			int x = array[i * 2];
			int y = array[i * 2 + 1];
			MovePawnToTileDelayed(list[i], GetTile(x, y), instantMove: false, displayMessage: false, 0.15f * (float)(i + 1) + 0.25f);
		}
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		int count2 = list2.Count;
		for (int j = 0; j < count2; j++)
		{
			int x2 = array2[j * 2];
			int y2 = array2[j * 2 + 1];
			MovePawnToTileDelayed(list2[j], GetTile(x2, y2), instantMove: false, displayMessage: false, 0.15f * (float)(j + 1) + 0.25f);
		}
	}

	private void RestoreStartingBoard()
	{
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int i = 0; i < 25; i++)
		{
			PawnBase pawnBase = ((i >= 16) ? list2[i - 16] : list[i]);
			PawnInformation pawnInformation = _startState.PawnInformation[i];
			TileBase tile = GetTile(pawnInformation.X, pawnInformation.Y);
			pawnBase.Reset();
			MovePawnToTile(pawnBase, tile, instantMove: false, displayMessage: false);
		}
	}

	private bool AddValidMove(List<Move> moves, PawnBase pawn, int x, int y)
	{
		bool result = false;
		TileBase tile = GetTile(x, y);
		if (tile.PawnOnTile == null && !IsCitadelTile(x, y))
		{
			Move item = default(Move);
			item.Unit = pawn;
			item.GoalTile = tile;
			moves.Add(item);
			result = true;
		}
		return result;
	}

	private void CheckIfPawnCapturedEnemyPawn(PawnTablut pawn, bool fake, TileBase victimTile, Tile2D helperTile)
	{
		PawnBase pawnOnTile = victimTile.PawnOnTile;
		if (pawnOnTile == null || pawnOnTile.PlayerOne == pawn.PlayerOne)
		{
			return;
		}
		PawnBase pawnOnTile2 = helperTile.PawnOnTile;
		if (pawnOnTile2 != null)
		{
			if (pawnOnTile2.PlayerOne == pawn.PlayerOne)
			{
				SetPawnCaptured(pawnOnTile, fake);
			}
		}
		else if (IsCitadelTile(helperTile.X, helperTile.Y))
		{
			SetPawnCaptured(pawnOnTile, fake);
		}
	}

	private void CheckIfPawnCaptures(PawnTablut pawn, bool fake = false)
	{
		int x = pawn.X;
		int y = pawn.Y;
		if (x > 1)
		{
			CheckIfPawnCapturedEnemyPawn(pawn, fake, GetTile(x - 1, y), GetTile(x - 2, y) as Tile2D);
		}
		if (x < 7)
		{
			CheckIfPawnCapturedEnemyPawn(pawn, fake, GetTile(x + 1, y), GetTile(x + 2, y) as Tile2D);
		}
		if (y > 1)
		{
			CheckIfPawnCapturedEnemyPawn(pawn, fake, GetTile(x, y - 1), GetTile(x, y - 2) as Tile2D);
		}
		if (y < 7)
		{
			CheckIfPawnCapturedEnemyPawn(pawn, fake, GetTile(x, y + 1), GetTile(x, y + 2) as Tile2D);
		}
	}
}
