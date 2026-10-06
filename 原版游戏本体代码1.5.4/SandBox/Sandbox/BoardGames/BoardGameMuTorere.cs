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

public class BoardGameMuTorere : BoardGameBase
{
	public struct BoardInformation
	{
		public readonly PawnInformation[] PawnInformation;

		public readonly TileBaseInformation[] TileInformation;

		public BoardInformation(ref PawnInformation[] pawns, ref TileBaseInformation[] tiles)
		{
			PawnInformation = pawns;
			TileInformation = tiles;
		}
	}

	public struct PawnInformation
	{
		public readonly int X;

		public PawnInformation(int x)
		{
			X = x;
		}
	}

	public const int WhitePawnCount = 4;

	public const int BlackPawnCount = 4;

	public override int TileCount => 9;

	protected override bool RotateBoard => true;

	protected override bool PreMovementStagePresent => false;

	protected override bool DiceRollRequired => false;

	public BoardGameMuTorere(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
		: base(mission, new TextObject("{=5siAbi69}Mu Torere"), startingPlayer)
	{
		PawnUnselectedFactor = 4288711820u;
	}

	public override void InitializeUnits()
	{
		base.PlayerOneUnits.Clear();
		base.PlayerTwoUnits.Clear();
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		for (int i = 0; i < 4; i++)
		{
			GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
			list.Add(InitializeUnit(new PawnMuTorere(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
		}
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int j = 0; j < 4; j++)
		{
			GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
			list2.Add(InitializeUnit(new PawnMuTorere(entity2, base.PlayerWhoStarted != PlayerTurn.PlayerOne)));
		}
	}

	public override void InitializeTiles()
	{
		if (base.Tiles == null)
		{
			base.Tiles = new TileBase[TileCount];
		}
		IEnumerable<GameEntity> source = from gameEntity2 in BoardEntity.GetChildren()
			where gameEntity2.Tags.Any((string t) => t.Contains("tile_"))
			select gameEntity2;
		IEnumerable<GameEntity> source2 = from gameEntity2 in BoardEntity.GetChildren()
			where gameEntity2.Tags.Any((string t) => t.Contains("decal_"))
			select gameEntity2;
		int x = 0;
		while (x < TileCount)
		{
			GameEntity gameEntity = source.Single((GameEntity e) => e.HasTag("tile_" + x));
			BoardGameDecal firstScriptOfType = source2.Single((GameEntity e) => e.HasTag("decal_" + x)).GetFirstScriptOfType<BoardGameDecal>();
			int xLeft;
			int xRight;
			switch (x)
			{
			case 0:
				xLeft = (xRight = -1);
				break;
			case 1:
				xLeft = 8;
				xRight = 2;
				break;
			case 8:
				xLeft = 7;
				xRight = 1;
				break;
			default:
				xLeft = x - 1;
				xRight = x + 1;
				break;
			}
			base.Tiles[x] = new TileMuTorere(gameEntity, firstScriptOfType, x, xLeft, xRight);
			gameEntity.CreateVariableRatePhysics(forChildren: true);
			int num = x + 1;
			x = num;
		}
	}

	public override void InitializeCapturedUnitsZones()
	{
	}

	public override void InitializeSound()
	{
		PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
		PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
		PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_wood");
		PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
	}

	public override void Reset()
	{
		base.Reset();
		PreplaceUnits();
	}

	public override List<Move> CalculateValidMoves(PawnBase pawn)
	{
		List<Move> list = new List<Move>();
		if (pawn is PawnMuTorere pawnMuTorere)
		{
			TileMuTorere tileMuTorere = FindAvailableTile() as TileMuTorere;
			if (pawnMuTorere.X == 0)
			{
				Move item = default(Move);
				item.Unit = pawn;
				item.GoalTile = tileMuTorere;
				list.Add(item);
			}
			else if (tileMuTorere.X != 0)
			{
				if (pawnMuTorere.X == tileMuTorere.XLeftTile || pawnMuTorere.X == tileMuTorere.XRightTile)
				{
					Move item2 = default(Move);
					item2.Unit = pawn;
					item2.GoalTile = tileMuTorere;
					list.Add(item2);
				}
			}
			else
			{
				TileMuTorere tileMuTorere2 = FindTileByCoordinate(pawnMuTorere.X);
				PawnBase pawnOnTile = base.Tiles[tileMuTorere2.XLeftTile].PawnOnTile;
				PawnBase pawnOnTile2 = base.Tiles[tileMuTorere2.XRightTile].PawnOnTile;
				if (pawnOnTile.PlayerOne != pawnMuTorere.PlayerOne || pawnOnTile2.PlayerOne != pawnMuTorere.PlayerOne)
				{
					Move item3 = default(Move);
					item3.Unit = pawn;
					item3.GoalTile = tileMuTorere;
					list.Add(item3);
				}
			}
		}
		return list;
	}

	protected override PawnBase SelectPawn(PawnBase pawn)
	{
		if (base.PlayerTurn == PlayerTurn.PlayerOne)
		{
			if (pawn.PlayerOne)
			{
				SelectedUnit = pawn;
			}
		}
		else if (base.AIOpponent == null && !pawn.PlayerOne)
		{
			SelectedUnit = pawn;
		}
		return pawn;
	}

	protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
	{
		base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
		TileMuTorere tileMuTorere = tile as TileMuTorere;
		PawnMuTorere pawnMuTorere = pawn as PawnMuTorere;
		if (tileMuTorere.PawnOnTile != null || pawnMuTorere == null)
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
		if (pawnMuTorere.X != -1)
		{
			base.Tiles[pawnMuTorere.X].PawnOnTile = null;
		}
		tileMuTorere.PawnOnTile = pawnMuTorere;
		pawnMuTorere.MovingToDifferentTile = pawnMuTorere.X != tileMuTorere.X;
		pawnMuTorere.X = tileMuTorere.X;
		Vec3 globalPosition = tileMuTorere.Entity.GlobalPosition;
		pawnMuTorere.AddGoalPosition(globalPosition);
		pawnMuTorere.MovePawnToGoalPositionsDelayed(instantMove, 0.6f, JustStoppedDraggingUnit, delay);
		if (pawnMuTorere == SelectedUnit)
		{
			SelectedUnit = null;
		}
	}

	protected override void SwitchPlayerTurn()
	{
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
		bool result = false;
		List<List<Move>> moves = CalculateAllValidMoves((base.PlayerTurn == PlayerTurn.PlayerOne) ? BoardGameSide.Player : BoardGameSide.AI);
		if (GetTotalMovesAvailable(ref moves) <= 0)
		{
			if (base.PlayerTurn == PlayerTurn.PlayerOne)
			{
				OnDefeat();
				ReadyToPlay = false;
				result = true;
			}
			else if (base.PlayerTurn == PlayerTurn.PlayerTwo)
			{
				OnVictory();
				ReadyToPlay = false;
				result = true;
			}
		}
		return result;
	}

	protected override void OnAfterBoardSetUp()
	{
		ReadyToPlay = true;
	}

	public TileMuTorere FindTileByCoordinate(int x)
	{
		TileMuTorere result = null;
		for (int i = 0; i < TileCount; i++)
		{
			TileMuTorere tileMuTorere = base.Tiles[i] as TileMuTorere;
			if (tileMuTorere.X == x)
			{
				result = tileMuTorere;
			}
		}
		return result;
	}

	public BoardInformation TakePawnsSnapshot()
	{
		PawnInformation[] pawns = new PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
		TileBaseInformation[] tiles = new TileBaseInformation[TileCount];
		int num = 0;
		foreach (PawnMuTorere item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			PawnInformation pawnInformation = new PawnInformation(item.X);
			pawns[num++] = pawnInformation;
		}
		foreach (PawnMuTorere item2 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			PawnInformation pawnInformation2 = new PawnInformation(item2.X);
			pawns[num++] = pawnInformation2;
		}
		for (int i = 0; i < TileCount; i++)
		{
			tiles[i] = new TileBaseInformation(ref base.Tiles[i].PawnOnTile);
		}
		return new BoardInformation(ref pawns, ref tiles);
	}

	public void UndoMove(ref BoardInformation board)
	{
		int num = 0;
		foreach (PawnMuTorere item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			item.X = board.PawnInformation[num++].X;
		}
		foreach (PawnMuTorere item2 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			item2.X = board.PawnInformation[num++].X;
		}
		for (int i = 0; i < TileCount; i++)
		{
			base.Tiles[i].PawnOnTile = board.TileInformation[i].PawnOnTile;
		}
	}

	public void AIMakeMove(Move move)
	{
		TileMuTorere tileMuTorere = move.GoalTile as TileMuTorere;
		PawnMuTorere pawnMuTorere = move.Unit as PawnMuTorere;
		base.Tiles[pawnMuTorere.X].PawnOnTile = null;
		tileMuTorere.PawnOnTile = pawnMuTorere;
		pawnMuTorere.X = tileMuTorere.X;
	}

	public TileBase FindAvailableTile()
	{
		TileBase[] tiles = base.Tiles;
		foreach (TileBase tileBase in tiles)
		{
			if (tileBase.PawnOnTile == null)
			{
				return tileBase;
			}
		}
		return null;
	}

	private void PreplaceUnits()
	{
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int i = 0; i < 4; i++)
		{
			MovePawnToTileDelayed(list[i], base.Tiles[i + 1], instantMove: false, displayMessage: false, 0.15f * (float)(i + 1) + 0.25f);
			MovePawnToTileDelayed(list2[i], base.Tiles[8 - i], instantMove: false, displayMessage: false, 0.15f * (float)(i + 1) + 0.5f);
		}
	}
}
