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

public class BoardGamePuluc : BoardGameBase
{
	public struct PawnInformation
	{
		public readonly int X;

		public readonly bool IsInSpawn;

		public readonly bool IsTopPawn;

		public readonly bool IsCaptured;

		public readonly PawnPuluc.MovementState State;

		public readonly List<PawnPuluc> PawnsBelow;

		public readonly Vec3 Position;

		public readonly PawnPuluc CapturedBy;

		public PawnInformation(int x, bool inSpawn, bool topPawn, PawnPuluc.MovementState state, List<PawnPuluc> pawnsBelow, bool captured, Vec3 position, PawnPuluc capturedBy)
		{
			X = x;
			IsInSpawn = inSpawn;
			IsTopPawn = topPawn;
			State = state;
			PawnsBelow = pawnsBelow;
			IsCaptured = captured;
			CapturedBy = capturedBy;
			Position = position;
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

	public const int WhitePawnCount = 6;

	public const int BlackPawnCount = 6;

	public const int TrackTileCount = 11;

	private const int PlayerHomebaseTileIndex = 11;

	private const int OpponentHomebaseTileIndex = 12;

	private BoardInformation _startState;

	public override int TileCount => 13;

	protected override bool RotateBoard => false;

	protected override bool PreMovementStagePresent => false;

	protected override bool DiceRollRequired => true;

	public BoardGamePuluc(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
		: base(mission, new TextObject("{=Uh057UUb}Puluc"), startingPlayer)
	{
		base.LastDice = -1;
		PawnUnselectedFactor = 4287395960u;
	}

	public override void InitializeUnits()
	{
		base.PlayerOneUnits.Clear();
		base.PlayerTwoUnits.Clear();
		List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
		for (int i = 0; i < 6; i++)
		{
			GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
			list.Add(InitializeUnit(new PawnPuluc(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
		}
		List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
		for (int j = 0; j < 6; j++)
		{
			GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
			list2.Add(InitializeUnit(new PawnPuluc(entity2, base.PlayerWhoStarted != PlayerTurn.PlayerOne)));
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
		while (x < 11)
		{
			GameEntity gameEntity = source.Single((GameEntity e) => e.HasTag("tile_" + x));
			BoardGameDecal firstScriptOfType = source2.Single((GameEntity e) => e.HasTag("decal_" + x)).GetFirstScriptOfType<BoardGameDecal>();
			base.Tiles[x] = new TilePuluc(gameEntity, firstScriptOfType, x);
			gameEntity.CreateVariableRatePhysics(forChildren: true);
			int num = x + 1;
			x = num;
		}
		GameEntity firstChildEntityWithTag = BoardEntity.GetFirstChildEntityWithTag("tile_homebase_player");
		BoardGameDecal firstScriptOfType2 = BoardEntity.GetFirstChildEntityWithTag("decal_homebase_player").GetFirstScriptOfType<BoardGameDecal>();
		base.Tiles[11] = new TilePuluc(firstChildEntityWithTag, firstScriptOfType2, 11);
		firstChildEntityWithTag.CreateVariableRatePhysics(forChildren: true);
		GameEntity firstChildEntityWithTag2 = BoardEntity.GetFirstChildEntityWithTag("tile_homebase_opponent");
		BoardGameDecal firstScriptOfType3 = BoardEntity.GetFirstChildEntityWithTag("decal_homebase_opponent").GetFirstScriptOfType<BoardGameDecal>();
		base.Tiles[12] = new TilePuluc(firstChildEntityWithTag2, firstScriptOfType3, 12);
		firstChildEntityWithTag2.CreateVariableRatePhysics(forChildren: true);
	}

	public override void InitializeSound()
	{
		PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
		PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
		PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_wood");
		PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
		DiceRollSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
	}

	public override void InitializeDiceBoard()
	{
		DiceBoard = Mission.Current.Scene.FindEntityWithTag("dice_board");
		DiceBoard.GetFirstScriptOfType<VertexAnimator>().Pause();
	}

	public override void Reset()
	{
		base.Reset();
		base.LastDice = -1;
		SetPawnSides();
		if (_startState.PawnInformation == null)
		{
			_startState = TakeBoardSnapshot();
		}
		RestoreStartingBoard();
	}

	public override List<Move> CalculateValidMoves(PawnBase pawn)
	{
		List<Move> list = null;
		if (pawn is PawnPuluc { IsTopPawn: not false } pawnPuluc)
		{
			list = new List<Move>();
			int num = ((pawnPuluc.IsInSpawn && !pawnPuluc.PlayerOne) ? 11 : pawnPuluc.X);
			bool flag = pawnPuluc.State == PawnPuluc.MovementState.MovingBackward;
			int num2 = ((pawnPuluc.PlayerOne ^ flag) ? (num + base.LastDice) : (num - base.LastDice));
			if (num2 < 0)
			{
				if (flag)
				{
					num2 = 11;
				}
				else
				{
					pawnPuluc.State = PawnPuluc.MovementState.ChangingDirection;
					num2 = -num2;
				}
			}
			else if (num2 > 10)
			{
				if (flag)
				{
					num2 = 12;
				}
				else
				{
					pawnPuluc.State = PawnPuluc.MovementState.ChangingDirection;
					num2 = 20 - num2;
				}
			}
			if (CanMovePawnToTile(pawnPuluc, num2))
			{
				Move item = default(Move);
				item.Unit = pawnPuluc;
				item.GoalTile = base.Tiles[num2];
				list.Add(item);
			}
		}
		return list;
	}

	public override void RollDice()
	{
		PlayDiceRollSound();
		int num = MBRandom.RandomInt(2) + MBRandom.RandomInt(2) + MBRandom.RandomInt(2) + MBRandom.RandomInt(2);
		if (num == 0)
		{
			num = 5;
		}
		VertexAnimator firstScriptOfType = DiceBoard.GetFirstScriptOfType<VertexAnimator>();
		switch (num)
		{
		case 1:
			firstScriptOfType.SetAnimation(1, 125, 70f);
			break;
		case 2:
			firstScriptOfType.SetAnimation(129, 248, 70f);
			break;
		case 3:
			firstScriptOfType.SetAnimation(251, 373, 70f);
			break;
		case 4:
			firstScriptOfType.SetAnimation(379, 496, 70f);
			break;
		case 5:
			firstScriptOfType.SetAnimation(501, 626, 70f);
			break;
		}
		firstScriptOfType.PlayOnce();
		base.LastDice = num;
		DiceRollAnimationTimer = 0f;
		DiceRollAnimationRunning = true;
	}

	protected override void OnAfterBoardSetUp()
	{
		ReadyToPlay = true;
	}

	protected override PawnBase SelectPawn(PawnBase pawn)
	{
		PawnPuluc pawnPuluc = pawn as PawnPuluc;
		if (pawnPuluc.CapturedBy != null)
		{
			pawn = pawnPuluc.CapturedBy;
		}
		if (pawn.PlayerOne == (base.PlayerTurn == PlayerTurn.PlayerOne) && !pawn.Captured)
		{
			SelectedUnit = pawn;
		}
		return pawn;
	}

	protected override void SwitchPlayerTurn()
	{
		if (SelectedUnit != null)
		{
			PawnPuluc pawnPuluc = SelectedUnit as PawnPuluc;
			if (pawnPuluc.InPlay && (base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting))
			{
				List<PawnPuluc> list = CheckIfPawnWillCapture(pawnPuluc, pawnPuluc.X);
				if (list != null && list.Count > 0)
				{
					pawnPuluc.State = PawnPuluc.MovementState.MovingBackward;
					pawnPuluc.PawnsBelow.AddRange(list);
					foreach (PawnPuluc item in list)
					{
						item.IsTopPawn = false;
						item.Captured = true;
						item.CapturedBy = pawnPuluc;
					}
					TilePuluc tilePuluc = base.Tiles[pawnPuluc.X] as TilePuluc;
					Vec3 goal = (pawnPuluc.PlayerOne ? tilePuluc.PosRightMid : tilePuluc.PosLeftMid);
					pawnPuluc.AddGoalPosition(goal);
					pawnPuluc.MovePawnToGoalPositions(instantMove: false, 0.5f);
				}
			}
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
		base.LastDice = -1;
		CheckGameEnded();
		base.SwitchPlayerTurn();
	}

	protected override bool CheckGameEnded()
	{
		bool result = false;
		if (GetPlayerOneUnitsAlive() <= 0)
		{
			OnDefeat();
			ReadyToPlay = false;
			result = true;
		}
		if (GetPlayerTwoUnitsAlive() <= 0)
		{
			OnVictory();
			ReadyToPlay = false;
			result = true;
		}
		return result;
	}

	protected override void UpdateAllTilesPositions()
	{
		TileBase[] tiles = base.Tiles;
		for (int i = 0; i < tiles.Length; i++)
		{
			((TilePuluc)tiles[i]).UpdateTilePosition();
		}
	}

	protected override void OnBeforeEndTurn()
	{
		base.LastDice = -1;
	}

	protected override void MovePawnToTile(PawnBase pawn, TileBase tile, bool instantMove = false, bool displayMessage = true)
	{
		base.MovePawnToTile(pawn, tile, instantMove, displayMessage);
		TilePuluc tilePuluc = tile as TilePuluc;
		PawnPuluc pawnPuluc = pawn as PawnPuluc;
		if (tilePuluc.PawnOnTile != null)
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
		int x = pawnPuluc.X;
		pawnPuluc.MovingToDifferentTile = x != tilePuluc.X || pawnPuluc.State == PawnPuluc.MovementState.ChangingDirection;
		pawnPuluc.X = tilePuluc.X;
		foreach (PawnPuluc item in pawnPuluc.PawnsBelow)
		{
			item.X = pawnPuluc.X;
		}
		if (pawnPuluc.X == 12 || pawnPuluc.X == 11)
		{
			PawnHasReachedHomeBase(pawnPuluc, instantMove);
			return;
		}
		if (pawnPuluc.State == PawnPuluc.MovementState.ChangingDirection)
		{
			int num;
			Vec3 goal;
			Vec3 goal2;
			if (pawn.PlayerOne)
			{
				num = 10;
				TilePuluc obj = base.Tiles[num] as TilePuluc;
				goal = obj.PosRight;
				goal2 = obj.PosRightMid;
			}
			else
			{
				num = 0;
				TilePuluc obj2 = base.Tiles[num] as TilePuluc;
				goal = obj2.PosLeft;
				goal2 = obj2.PosLeftMid;
			}
			if (x != num)
			{
				pawn.AddGoalPosition(goal);
			}
			pawn.AddGoalPosition(goal2);
			pawnPuluc.State = PawnPuluc.MovementState.MovingBackward;
		}
		Vec3 goal3 = ((pawnPuluc.State != PawnPuluc.MovementState.MovingForward) ? (pawn.PlayerOne ? tilePuluc.PosRightMid : tilePuluc.PosLeftMid) : (pawn.PlayerOne ? tilePuluc.PosRight : tilePuluc.PosLeft));
		pawn.AddGoalPosition(goal3);
		pawn.MovePawnToGoalPositions(instantMove: false, 0.5f, JustStoppedDraggingUnit);
	}

	protected override void OnAfterDiceRollAnimation()
	{
		base.OnAfterDiceRollAnimation();
		if (base.LastDice == -1)
		{
			return;
		}
		MBTextManager.SetTextVariable("DICE_ROLL", base.LastDice);
		if (base.PlayerTurn == PlayerTurn.PlayerOne)
		{
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_roll_dice_player").ToString()));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_roll_dice_opponent").ToString()));
		}
		if (base.PlayerTurn == PlayerTurn.PlayerOne)
		{
			List<List<Move>> moves = CalculateAllValidMoves(BoardGameSide.Player);
			if (!HasMovesAvailable(ref moves))
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_boardgame_no_available_moves_player"));
				EndTurn();
			}
		}
	}

	public void AIMakeMove(Move move)
	{
		TilePuluc tilePuluc = move.GoalTile as TilePuluc;
		PawnPuluc pawnPuluc = move.Unit as PawnPuluc;
		pawnPuluc.X = tilePuluc.X;
		foreach (PawnPuluc item in pawnPuluc.PawnsBelow)
		{
			item.X = pawnPuluc.X;
		}
		if (tilePuluc.X < 11)
		{
			List<PawnPuluc> list = CheckIfPawnWillCapture(pawnPuluc, tilePuluc.X);
			if (list != null && list.Count > 0)
			{
				pawnPuluc.State = PawnPuluc.MovementState.MovingBackward;
				pawnPuluc.PawnsBelow.AddRange(list);
				foreach (PawnPuluc item2 in list)
				{
					item2.IsTopPawn = false;
					item2.Captured = true;
					item2.CapturedBy = pawnPuluc;
				}
			}
		}
		if (pawnPuluc.X == 12 || pawnPuluc.X == 11)
		{
			PawnHasReachedHomeBase(pawnPuluc, instantmove: true, fake: true);
		}
	}

	public BoardInformation TakeBoardSnapshot()
	{
		PawnInformation[] pawns = new PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
		int num = 0;
		foreach (PawnPuluc item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			List<PawnPuluc> list = new List<PawnPuluc>();
			if (item.PawnsBelow != null && item.PawnsBelow.Count > 0)
			{
				foreach (PawnPuluc item2 in item.PawnsBelow)
				{
					list.Add(item2);
				}
			}
			pawns[num++] = new PawnInformation(item.X, item.IsInSpawn, item.IsTopPawn, item.State, list, item.Captured, item.Entity.GlobalPosition, item.CapturedBy);
		}
		foreach (PawnPuluc item3 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			List<PawnPuluc> list2 = new List<PawnPuluc>();
			if (item3.PawnsBelow != null && item3.PawnsBelow.Count > 0)
			{
				foreach (PawnPuluc item4 in item3.PawnsBelow)
				{
					list2.Add(item4);
				}
			}
			pawns[num++] = new PawnInformation(item3.X, item3.IsInSpawn, item3.IsTopPawn, item3.State, list2, item3.Captured, item3.Entity.GlobalPosition, item3.CapturedBy);
		}
		return new BoardInformation(ref pawns);
	}

	public void UndoMove(ref BoardInformation board)
	{
		int num = 0;
		foreach (PawnPuluc item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			item.PawnsBelow.Clear();
			foreach (PawnPuluc item2 in board.PawnInformation[num].PawnsBelow)
			{
				item.PawnsBelow.Add(item2);
			}
			item.IsTopPawn = board.PawnInformation[num].IsTopPawn;
			item.X = board.PawnInformation[num].X;
			item.IsInSpawn = board.PawnInformation[num].IsInSpawn;
			item.State = board.PawnInformation[num].State;
			item.Captured = board.PawnInformation[num].IsCaptured;
			item.CapturedBy = board.PawnInformation[num].CapturedBy;
			num++;
		}
		foreach (PawnPuluc item3 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			item3.PawnsBelow.Clear();
			foreach (PawnPuluc item4 in board.PawnInformation[num].PawnsBelow)
			{
				item3.PawnsBelow.Add(item4);
			}
			item3.IsTopPawn = board.PawnInformation[num].IsTopPawn;
			item3.X = board.PawnInformation[num].X;
			item3.IsInSpawn = board.PawnInformation[num].IsInSpawn;
			item3.State = board.PawnInformation[num].State;
			item3.Captured = board.PawnInformation[num].IsCaptured;
			item3.CapturedBy = board.PawnInformation[num].CapturedBy;
			num++;
		}
	}

	private bool CanMovePawnToTile(PawnPuluc pawn, int tileCoord)
	{
		bool result = false;
		switch (tileCoord)
		{
		case 11:
			result = true;
			break;
		case 12:
			result = true;
			break;
		default:
		{
			List<PawnPuluc> pawns = GetAllPawnsForTileCoordinate(tileCoord);
			if (pawns.Count == 0)
			{
				result = true;
				break;
			}
			List<PawnPuluc> topPawns = GetTopPawns(ref pawns);
			if (topPawns[0].PlayerOne != pawn.PlayerOne || topPawns[0] == pawn)
			{
				result = true;
			}
			break;
		}
		}
		return result;
	}

	private List<PawnPuluc> GetAllPawnsForTileCoordinate(int x)
	{
		List<PawnPuluc> list = new List<PawnPuluc>();
		foreach (PawnPuluc playerOneUnit in base.PlayerOneUnits)
		{
			if (playerOneUnit.X == x)
			{
				list.Add(playerOneUnit);
			}
		}
		foreach (PawnPuluc playerTwoUnit in base.PlayerTwoUnits)
		{
			if (playerTwoUnit.X == x)
			{
				list.Add(playerTwoUnit);
			}
		}
		return list;
	}

	private List<PawnPuluc> GetTopPawns(ref List<PawnPuluc> pawns)
	{
		List<PawnPuluc> list = new List<PawnPuluc>();
		foreach (PawnPuluc pawn in pawns)
		{
			if (pawn.IsTopPawn)
			{
				list.Add(pawn);
			}
		}
		return list;
	}

	private List<PawnPuluc> CheckIfPawnWillCapture(PawnPuluc pawn, int tile)
	{
		List<PawnPuluc> pawns = GetAllPawnsForTileCoordinate(tile);
		if (pawns.Count > 0)
		{
			List<PawnPuluc> topPawns = GetTopPawns(ref pawns);
			if (topPawns.Count == 1)
			{
				return null;
			}
			foreach (PawnPuluc item in topPawns)
			{
				if (item != pawn)
				{
					List<PawnPuluc> list = new List<PawnPuluc>();
					list.Add(item);
					list.AddRange(item.PawnsBelow);
					return list;
				}
			}
		}
		return null;
	}

	private void RestoreStartingBoard()
	{
		int num = 0;
		foreach (PawnPuluc item in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits)
		{
			if (item.X != -1 && base.Tiles[item.X].PawnOnTile == item)
			{
				base.Tiles[item.X].PawnOnTile = null;
			}
			item.Reset();
			item.AddGoalPosition(item.SpawnPos);
			item.MovePawnToGoalPositions(instantMove: false, 0.5f);
			num++;
		}
		foreach (PawnPuluc item2 in (base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits)
		{
			if (item2.X != -1 && base.Tiles[item2.X].PawnOnTile == item2)
			{
				base.Tiles[item2.X].PawnOnTile = null;
			}
			item2.Reset();
			item2.AddGoalPosition(item2.SpawnPos);
			item2.MovePawnToGoalPositions(instantMove: false, 0.5f);
			num++;
		}
	}

	private void SetPawnSides()
	{
		if (HasToMovePawnsAcross)
		{
			CapturedPawnsPool playerOnePool = PlayerOnePool;
			PlayerOnePool = PlayerTwoPool;
			PlayerTwoPool = playerOnePool;
			if (_startState.PawnInformation == null)
			{
				for (int i = 0; i < base.PlayerOneUnits.Count; i++)
				{
					PawnPuluc obj = base.PlayerTwoUnits[base.PlayerTwoUnits.Count - i - 1] as PawnPuluc;
					PawnPuluc pawnPuluc = base.PlayerOneUnits[i] as PawnPuluc;
					Vec3 spawnPos = obj.SpawnPos;
					obj.SpawnPos = pawnPuluc.SpawnPos;
					pawnPuluc.SpawnPos = spawnPos;
				}
			}
		}
		if (_startState.PawnInformation != null)
		{
			int num = 0;
			int num2 = 1;
			if (base.PlayerWhoStarted != PlayerTurn.PlayerOne)
			{
				num = base.PlayerTwoUnits.Count - 1;
				num2 = -1;
			}
			for (int j = 0; j < base.PlayerOneUnits.Count; j++)
			{
				(base.PlayerOneUnits[j] as PawnPuluc).SpawnPos = _startState.PawnInformation[num].Position;
				num += num2;
			}
			if (base.PlayerWhoStarted != PlayerTurn.PlayerOne)
			{
				num = base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count - 1;
			}
			for (int k = 0; k < base.PlayerTwoUnits.Count; k++)
			{
				(base.PlayerTwoUnits[k] as PawnPuluc).SpawnPos = _startState.PawnInformation[num].Position;
				num += num2;
			}
		}
	}

	private void PawnHasReachedHomeBase(PawnPuluc pawn, bool instantmove, bool fake = false)
	{
		foreach (PawnPuluc item in pawn.PawnsBelow)
		{
			if (item.PlayerOne == pawn.PlayerOne)
			{
				item.MovePawnBackToSpawn(instantmove, 0.6f, fake);
				continue;
			}
			item.X = -1;
			item.IsInSpawn = false;
			if (!fake)
			{
				item.CapturedBy = null;
				RemovePawnFromBoard(item, 100f, instantMove: true);
			}
		}
		pawn.MovePawnBackToSpawn(instantmove, 0.6f, fake);
	}
}
