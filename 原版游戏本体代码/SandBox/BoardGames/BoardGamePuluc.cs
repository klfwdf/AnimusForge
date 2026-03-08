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

namespace SandBox.BoardGames
{
	// Token: 0x020000EA RID: 234
	public class BoardGamePuluc : BoardGameBase
	{
		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000BC8 RID: 3016 RVA: 0x00057B65 File Offset: 0x00055D65
		public override int TileCount
		{
			get
			{
				return 13;
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000BC9 RID: 3017 RVA: 0x00057B69 File Offset: 0x00055D69
		protected override bool RotateBoard
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x06000BCA RID: 3018 RVA: 0x00057B6C File Offset: 0x00055D6C
		protected override bool PreMovementStagePresent
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x06000BCB RID: 3019 RVA: 0x00057B6F File Offset: 0x00055D6F
		protected override bool DiceRollRequired
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000BCC RID: 3020 RVA: 0x00057B72 File Offset: 0x00055D72
		public BoardGamePuluc(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
			: base(mission, new TextObject("{=Uh057UUb}Puluc", null), startingPlayer)
		{
			base.LastDice = -1;
			this.PawnUnselectedFactor = 4287395960U;
		}

		// Token: 0x06000BCD RID: 3021 RVA: 0x00057B9C File Offset: 0x00055D9C
		public override void InitializeUnits()
		{
			base.PlayerOneUnits.Clear();
			base.PlayerTwoUnits.Clear();
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			for (int i = 0; i < 6; i++)
			{
				GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
				list.Add(base.InitializeUnit(new PawnPuluc(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
			}
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int j = 0; j < 6; j++)
			{
				GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
				list2.Add(base.InitializeUnit(new PawnPuluc(entity2, base.PlayerWhoStarted > PlayerTurn.PlayerOne)));
			}
		}

		// Token: 0x06000BCE RID: 3022 RVA: 0x00057C84 File Offset: 0x00055E84
		public override void InitializeTiles()
		{
			if (base.Tiles == null)
			{
				base.Tiles = new TileBase[this.TileCount];
			}
			int x;
			IEnumerable<GameEntity> source = from x in this.BoardEntity.GetChildren()
				where x.Tags.Any((string t) => t.Contains("tile_"))
				select x;
			IEnumerable<GameEntity> source2 = from x in this.BoardEntity.GetChildren()
				where x.Tags.Any((string t) => t.Contains("decal_"))
				select x;
			int x2;
			for (x = 0; x < 11; x = x2)
			{
				GameEntity gameEntity = source.Single((GameEntity e) => e.HasTag("tile_" + x));
				BoardGameDecal firstScriptOfType = source2.Single((GameEntity e) => e.HasTag("decal_" + x)).GetFirstScriptOfType<BoardGameDecal>();
				base.Tiles[x] = new TilePuluc(gameEntity, firstScriptOfType, x);
				gameEntity.CreateVariableRatePhysics(true);
				x2 = x + 1;
			}
			GameEntity firstChildEntityWithTag = this.BoardEntity.GetFirstChildEntityWithTag("tile_homebase_player");
			BoardGameDecal firstScriptOfType2 = this.BoardEntity.GetFirstChildEntityWithTag("decal_homebase_player").GetFirstScriptOfType<BoardGameDecal>();
			base.Tiles[11] = new TilePuluc(firstChildEntityWithTag, firstScriptOfType2, 11);
			firstChildEntityWithTag.CreateVariableRatePhysics(true);
			GameEntity firstChildEntityWithTag2 = this.BoardEntity.GetFirstChildEntityWithTag("tile_homebase_opponent");
			BoardGameDecal firstScriptOfType3 = this.BoardEntity.GetFirstChildEntityWithTag("decal_homebase_opponent").GetFirstScriptOfType<BoardGameDecal>();
			base.Tiles[12] = new TilePuluc(firstChildEntityWithTag2, firstScriptOfType3, 12);
			firstChildEntityWithTag2.CreateVariableRatePhysics(true);
		}

		// Token: 0x06000BCF RID: 3023 RVA: 0x00057E14 File Offset: 0x00056014
		public override void InitializeSound()
		{
			PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
			PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
			PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_wood");
			PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
			this.DiceRollSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
		}

		// Token: 0x06000BD0 RID: 3024 RVA: 0x00057E6D File Offset: 0x0005606D
		public override void InitializeDiceBoard()
		{
			this.DiceBoard = Mission.Current.Scene.FindEntityWithTag("dice_board");
			this.DiceBoard.GetFirstScriptOfType<VertexAnimator>().Pause();
		}

		// Token: 0x06000BD1 RID: 3025 RVA: 0x00057E99 File Offset: 0x00056099
		public override void Reset()
		{
			base.Reset();
			base.LastDice = -1;
			this.SetPawnSides();
			if (this._startState.PawnInformation == null)
			{
				this._startState = this.TakeBoardSnapshot();
			}
			this.RestoreStartingBoard();
		}

		// Token: 0x06000BD2 RID: 3026 RVA: 0x00057ED0 File Offset: 0x000560D0
		public override List<Move> CalculateValidMoves(PawnBase pawn)
		{
			List<Move> list = null;
			PawnPuluc pawnPuluc = pawn as PawnPuluc;
			if (pawnPuluc != null && pawnPuluc.IsTopPawn)
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
				if (this.CanMovePawnToTile(pawnPuluc, num2))
				{
					Move item;
					item.Unit = pawnPuluc;
					item.GoalTile = base.Tiles[num2];
					list.Add(item);
				}
			}
			return list;
		}

		// Token: 0x06000BD3 RID: 3027 RVA: 0x00057FA8 File Offset: 0x000561A8
		public override void RollDice()
		{
			base.PlayDiceRollSound();
			int num = MBRandom.RandomInt(2) + MBRandom.RandomInt(2) + MBRandom.RandomInt(2) + MBRandom.RandomInt(2);
			if (num == 0)
			{
				num = 5;
			}
			VertexAnimator firstScriptOfType = this.DiceBoard.GetFirstScriptOfType<VertexAnimator>();
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
			this.DiceRollAnimationTimer = 0f;
			this.DiceRollAnimationRunning = true;
		}

		// Token: 0x06000BD4 RID: 3028 RVA: 0x0005808F File Offset: 0x0005628F
		protected override void OnAfterBoardSetUp()
		{
			this.ReadyToPlay = true;
		}

		// Token: 0x06000BD5 RID: 3029 RVA: 0x00058098 File Offset: 0x00056298
		protected override PawnBase SelectPawn(PawnBase pawn)
		{
			PawnPuluc pawnPuluc = pawn as PawnPuluc;
			if (pawnPuluc.CapturedBy != null)
			{
				pawn = pawnPuluc.CapturedBy;
			}
			if (pawn.PlayerOne == (base.PlayerTurn == PlayerTurn.PlayerOne) && !pawn.Captured)
			{
				this.SelectedUnit = pawn;
			}
			return pawn;
		}

		// Token: 0x06000BD6 RID: 3030 RVA: 0x000580E0 File Offset: 0x000562E0
		protected override void SwitchPlayerTurn()
		{
			if (this.SelectedUnit != null)
			{
				PawnPuluc pawnPuluc = this.SelectedUnit as PawnPuluc;
				if (pawnPuluc.InPlay && (base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting))
				{
					List<PawnPuluc> list = this.CheckIfPawnWillCapture(pawnPuluc, pawnPuluc.X);
					if (list != null && list.Count > 0)
					{
						pawnPuluc.State = PawnPuluc.MovementState.MovingBackward;
						pawnPuluc.PawnsBelow.AddRange(list);
						foreach (PawnPuluc pawnPuluc2 in list)
						{
							pawnPuluc2.IsTopPawn = false;
							pawnPuluc2.Captured = true;
							pawnPuluc2.CapturedBy = pawnPuluc;
						}
						TilePuluc tilePuluc = base.Tiles[pawnPuluc.X] as TilePuluc;
						Vec3 goal = (pawnPuluc.PlayerOne ? tilePuluc.PosRightMid : tilePuluc.PosLeftMid);
						pawnPuluc.AddGoalPosition(goal);
						pawnPuluc.MovePawnToGoalPositions(false, 0.5f, false);
					}
				}
			}
			this.SelectedUnit = null;
			if (base.PlayerTurn == PlayerTurn.PlayerOneWaiting)
			{
				base.PlayerTurn = PlayerTurn.PlayerTwo;
			}
			else if (base.PlayerTurn == PlayerTurn.PlayerTwoWaiting)
			{
				base.PlayerTurn = PlayerTurn.PlayerOne;
			}
			base.LastDice = -1;
			this.CheckGameEnded();
			base.SwitchPlayerTurn();
		}

		// Token: 0x06000BD7 RID: 3031 RVA: 0x00058220 File Offset: 0x00056420
		protected override bool CheckGameEnded()
		{
			bool result = false;
			if (base.GetPlayerOneUnitsAlive() <= 0)
			{
				base.OnDefeat("str_boardgame_defeat_message");
				this.ReadyToPlay = false;
				result = true;
			}
			if (base.GetPlayerTwoUnitsAlive() <= 0)
			{
				base.OnVictory("str_boardgame_victory_message");
				this.ReadyToPlay = false;
				result = true;
			}
			return result;
		}

		// Token: 0x06000BD8 RID: 3032 RVA: 0x0005826C File Offset: 0x0005646C
		protected override void UpdateAllTilesPositions()
		{
			TileBase[] tiles = base.Tiles;
			for (int i = 0; i < tiles.Length; i++)
			{
				((TilePuluc)tiles[i]).UpdateTilePosition();
			}
		}

		// Token: 0x06000BD9 RID: 3033 RVA: 0x0005829B File Offset: 0x0005649B
		protected override void OnBeforeEndTurn()
		{
			base.LastDice = -1;
		}

		// Token: 0x06000BDA RID: 3034 RVA: 0x000582A4 File Offset: 0x000564A4
		protected override void MovePawnToTile(PawnBase pawn, TileBase tile, bool instantMove = false, bool displayMessage = true)
		{
			base.MovePawnToTile(pawn, tile, instantMove, displayMessage);
			TilePuluc tilePuluc = tile as TilePuluc;
			PawnPuluc pawnPuluc = pawn as PawnPuluc;
			if (tilePuluc.PawnOnTile == null)
			{
				if (displayMessage)
				{
					if (base.PlayerTurn == PlayerTurn.PlayerOne)
					{
						InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_move_piece_player", null).ToString()));
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_move_piece_opponent", null).ToString()));
					}
				}
				int x = pawnPuluc.X;
				pawnPuluc.MovingToDifferentTile = x != tilePuluc.X || pawnPuluc.State == PawnPuluc.MovementState.ChangingDirection;
				pawnPuluc.X = tilePuluc.X;
				foreach (PawnPuluc pawnPuluc2 in pawnPuluc.PawnsBelow)
				{
					pawnPuluc2.X = pawnPuluc.X;
				}
				if (pawnPuluc.X == 12 || pawnPuluc.X == 11)
				{
					this.PawnHasReachedHomeBase(pawnPuluc, instantMove, false);
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
						TilePuluc tilePuluc2 = base.Tiles[num] as TilePuluc;
						goal = tilePuluc2.PosRight;
						goal2 = tilePuluc2.PosRightMid;
					}
					else
					{
						num = 0;
						TilePuluc tilePuluc3 = base.Tiles[num] as TilePuluc;
						goal = tilePuluc3.PosLeft;
						goal2 = tilePuluc3.PosLeftMid;
					}
					if (x != num)
					{
						pawn.AddGoalPosition(goal);
					}
					pawn.AddGoalPosition(goal2);
					pawnPuluc.State = PawnPuluc.MovementState.MovingBackward;
				}
				Vec3 goal3;
				if (pawnPuluc.State == PawnPuluc.MovementState.MovingForward)
				{
					goal3 = (pawn.PlayerOne ? tilePuluc.PosRight : tilePuluc.PosLeft);
				}
				else
				{
					goal3 = (pawn.PlayerOne ? tilePuluc.PosRightMid : tilePuluc.PosLeftMid);
				}
				pawn.AddGoalPosition(goal3);
				pawn.MovePawnToGoalPositions(false, 0.5f, this.JustStoppedDraggingUnit);
			}
		}

		// Token: 0x06000BDB RID: 3035 RVA: 0x00058470 File Offset: 0x00056670
		protected override void OnAfterDiceRollAnimation()
		{
			base.OnAfterDiceRollAnimation();
			if (base.LastDice != -1)
			{
				MBTextManager.SetTextVariable("DICE_ROLL", base.LastDice);
				if (base.PlayerTurn == PlayerTurn.PlayerOne)
				{
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_roll_dice_player", null).ToString()));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_boardgame_roll_dice_opponent", null).ToString()));
				}
				if (base.PlayerTurn == PlayerTurn.PlayerOne)
				{
					List<List<Move>> list = this.CalculateAllValidMoves(BoardGameSide.Player);
					if (!base.HasMovesAvailable(ref list))
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_boardgame_no_available_moves_player", null), 0, null, null, "");
						base.EndTurn();
					}
				}
			}
		}

		// Token: 0x06000BDC RID: 3036 RVA: 0x00058518 File Offset: 0x00056718
		public void AIMakeMove(Move move)
		{
			TilePuluc tilePuluc = move.GoalTile as TilePuluc;
			PawnPuluc pawnPuluc = move.Unit as PawnPuluc;
			pawnPuluc.X = tilePuluc.X;
			foreach (PawnPuluc pawnPuluc2 in pawnPuluc.PawnsBelow)
			{
				pawnPuluc2.X = pawnPuluc.X;
			}
			if (tilePuluc.X < 11)
			{
				List<PawnPuluc> list = this.CheckIfPawnWillCapture(pawnPuluc, tilePuluc.X);
				if (list != null && list.Count > 0)
				{
					pawnPuluc.State = PawnPuluc.MovementState.MovingBackward;
					pawnPuluc.PawnsBelow.AddRange(list);
					foreach (PawnPuluc pawnPuluc3 in list)
					{
						pawnPuluc3.IsTopPawn = false;
						pawnPuluc3.Captured = true;
						pawnPuluc3.CapturedBy = pawnPuluc;
					}
				}
			}
			if (pawnPuluc.X == 12 || pawnPuluc.X == 11)
			{
				this.PawnHasReachedHomeBase(pawnPuluc, true, true);
			}
		}

		// Token: 0x06000BDD RID: 3037 RVA: 0x00058630 File Offset: 0x00056830
		public BoardGamePuluc.BoardInformation TakeBoardSnapshot()
		{
			BoardGamePuluc.PawnInformation[] array = new BoardGamePuluc.PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnPuluc pawnPuluc = (PawnPuluc)pawnBase;
				List<PawnPuluc> list = new List<PawnPuluc>();
				if (pawnPuluc.PawnsBelow != null && pawnPuluc.PawnsBelow.Count > 0)
				{
					foreach (PawnPuluc item in pawnPuluc.PawnsBelow)
					{
						list.Add(item);
					}
				}
				array[num++] = new BoardGamePuluc.PawnInformation(pawnPuluc.X, pawnPuluc.IsInSpawn, pawnPuluc.IsTopPawn, pawnPuluc.State, list, pawnPuluc.Captured, pawnPuluc.Entity.GlobalPosition, pawnPuluc.CapturedBy);
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnPuluc pawnPuluc2 = (PawnPuluc)pawnBase2;
				List<PawnPuluc> list2 = new List<PawnPuluc>();
				if (pawnPuluc2.PawnsBelow != null && pawnPuluc2.PawnsBelow.Count > 0)
				{
					foreach (PawnPuluc item2 in pawnPuluc2.PawnsBelow)
					{
						list2.Add(item2);
					}
				}
				array[num++] = new BoardGamePuluc.PawnInformation(pawnPuluc2.X, pawnPuluc2.IsInSpawn, pawnPuluc2.IsTopPawn, pawnPuluc2.State, list2, pawnPuluc2.Captured, pawnPuluc2.Entity.GlobalPosition, pawnPuluc2.CapturedBy);
			}
			return new BoardGamePuluc.BoardInformation(ref array);
		}

		// Token: 0x06000BDE RID: 3038 RVA: 0x00058868 File Offset: 0x00056A68
		public void UndoMove(ref BoardGamePuluc.BoardInformation board)
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnPuluc pawnPuluc = (PawnPuluc)pawnBase;
				pawnPuluc.PawnsBelow.Clear();
				foreach (PawnPuluc item in board.PawnInformation[num].PawnsBelow)
				{
					pawnPuluc.PawnsBelow.Add(item);
				}
				pawnPuluc.IsTopPawn = board.PawnInformation[num].IsTopPawn;
				pawnPuluc.X = board.PawnInformation[num].X;
				pawnPuluc.IsInSpawn = board.PawnInformation[num].IsInSpawn;
				pawnPuluc.State = board.PawnInformation[num].State;
				pawnPuluc.Captured = board.PawnInformation[num].IsCaptured;
				pawnPuluc.CapturedBy = board.PawnInformation[num].CapturedBy;
				num++;
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnPuluc pawnPuluc2 = (PawnPuluc)pawnBase2;
				pawnPuluc2.PawnsBelow.Clear();
				foreach (PawnPuluc item2 in board.PawnInformation[num].PawnsBelow)
				{
					pawnPuluc2.PawnsBelow.Add(item2);
				}
				pawnPuluc2.IsTopPawn = board.PawnInformation[num].IsTopPawn;
				pawnPuluc2.X = board.PawnInformation[num].X;
				pawnPuluc2.IsInSpawn = board.PawnInformation[num].IsInSpawn;
				pawnPuluc2.State = board.PawnInformation[num].State;
				pawnPuluc2.Captured = board.PawnInformation[num].IsCaptured;
				pawnPuluc2.CapturedBy = board.PawnInformation[num].CapturedBy;
				num++;
			}
		}

		// Token: 0x06000BDF RID: 3039 RVA: 0x00058B3C File Offset: 0x00056D3C
		private bool CanMovePawnToTile(PawnPuluc pawn, int tileCoord)
		{
			bool result = false;
			if (tileCoord == 11)
			{
				result = true;
			}
			else if (tileCoord == 12)
			{
				result = true;
			}
			else
			{
				List<PawnPuluc> allPawnsForTileCoordinate = this.GetAllPawnsForTileCoordinate(tileCoord);
				if (allPawnsForTileCoordinate.Count == 0)
				{
					result = true;
				}
				else
				{
					List<PawnPuluc> topPawns = this.GetTopPawns(ref allPawnsForTileCoordinate);
					if (topPawns[0].PlayerOne != pawn.PlayerOne || topPawns[0] == pawn)
					{
						result = true;
					}
				}
			}
			return result;
		}

		// Token: 0x06000BE0 RID: 3040 RVA: 0x00058B9C File Offset: 0x00056D9C
		private List<PawnPuluc> GetAllPawnsForTileCoordinate(int x)
		{
			List<PawnPuluc> list = new List<PawnPuluc>();
			foreach (PawnBase pawnBase in base.PlayerOneUnits)
			{
				PawnPuluc pawnPuluc = (PawnPuluc)pawnBase;
				if (pawnPuluc.X == x)
				{
					list.Add(pawnPuluc);
				}
			}
			foreach (PawnBase pawnBase2 in base.PlayerTwoUnits)
			{
				PawnPuluc pawnPuluc2 = (PawnPuluc)pawnBase2;
				if (pawnPuluc2.X == x)
				{
					list.Add(pawnPuluc2);
				}
			}
			return list;
		}

		// Token: 0x06000BE1 RID: 3041 RVA: 0x00058C54 File Offset: 0x00056E54
		private List<PawnPuluc> GetTopPawns(ref List<PawnPuluc> pawns)
		{
			List<PawnPuluc> list = new List<PawnPuluc>();
			foreach (PawnPuluc pawnPuluc in pawns)
			{
				if (pawnPuluc.IsTopPawn)
				{
					list.Add(pawnPuluc);
				}
			}
			return list;
		}

		// Token: 0x06000BE2 RID: 3042 RVA: 0x00058CB4 File Offset: 0x00056EB4
		private List<PawnPuluc> CheckIfPawnWillCapture(PawnPuluc pawn, int tile)
		{
			List<PawnPuluc> allPawnsForTileCoordinate = this.GetAllPawnsForTileCoordinate(tile);
			if (allPawnsForTileCoordinate.Count > 0)
			{
				List<PawnPuluc> topPawns = this.GetTopPawns(ref allPawnsForTileCoordinate);
				if (topPawns.Count == 1)
				{
					return null;
				}
				foreach (PawnPuluc pawnPuluc in topPawns)
				{
					if (pawnPuluc != pawn)
					{
						List<PawnPuluc> list = new List<PawnPuluc>();
						list.Add(pawnPuluc);
						list.AddRange(pawnPuluc.PawnsBelow);
						return list;
					}
				}
			}
			return null;
		}

		// Token: 0x06000BE3 RID: 3043 RVA: 0x00058D44 File Offset: 0x00056F44
		private void RestoreStartingBoard()
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnPuluc pawnPuluc = (PawnPuluc)pawnBase;
				if (pawnPuluc.X != -1 && base.Tiles[pawnPuluc.X].PawnOnTile == pawnPuluc)
				{
					base.Tiles[pawnPuluc.X].PawnOnTile = null;
				}
				pawnPuluc.Reset();
				pawnPuluc.AddGoalPosition(pawnPuluc.SpawnPos);
				pawnPuluc.MovePawnToGoalPositions(false, 0.5f, false);
				num++;
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnPuluc pawnPuluc2 = (PawnPuluc)pawnBase2;
				if (pawnPuluc2.X != -1 && base.Tiles[pawnPuluc2.X].PawnOnTile == pawnPuluc2)
				{
					base.Tiles[pawnPuluc2.X].PawnOnTile = null;
				}
				pawnPuluc2.Reset();
				pawnPuluc2.AddGoalPosition(pawnPuluc2.SpawnPos);
				pawnPuluc2.MovePawnToGoalPositions(false, 0.5f, false);
				num++;
			}
		}

		// Token: 0x06000BE4 RID: 3044 RVA: 0x00058EA0 File Offset: 0x000570A0
		private void SetPawnSides()
		{
			if (this.HasToMovePawnsAcross)
			{
				CapturedPawnsPool playerOnePool = this.PlayerOnePool;
				this.PlayerOnePool = this.PlayerTwoPool;
				this.PlayerTwoPool = playerOnePool;
				if (this._startState.PawnInformation == null)
				{
					for (int i = 0; i < base.PlayerOneUnits.Count; i++)
					{
						PawnPuluc pawnPuluc = base.PlayerTwoUnits[base.PlayerTwoUnits.Count - i - 1] as PawnPuluc;
						PawnPuluc pawnPuluc2 = base.PlayerOneUnits[i] as PawnPuluc;
						Vec3 spawnPos = pawnPuluc.SpawnPos;
						pawnPuluc.SpawnPos = pawnPuluc2.SpawnPos;
						pawnPuluc2.SpawnPos = spawnPos;
					}
				}
			}
			if (this._startState.PawnInformation != null)
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
					(base.PlayerOneUnits[j] as PawnPuluc).SpawnPos = this._startState.PawnInformation[num].Position;
					num += num2;
				}
				if (base.PlayerWhoStarted != PlayerTurn.PlayerOne)
				{
					num = base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count - 1;
				}
				for (int k = 0; k < base.PlayerTwoUnits.Count; k++)
				{
					(base.PlayerTwoUnits[k] as PawnPuluc).SpawnPos = this._startState.PawnInformation[num].Position;
					num += num2;
				}
			}
		}

		// Token: 0x06000BE5 RID: 3045 RVA: 0x00059030 File Offset: 0x00057230
		private void PawnHasReachedHomeBase(PawnPuluc pawn, bool instantmove, bool fake = false)
		{
			foreach (PawnPuluc pawnPuluc in pawn.PawnsBelow)
			{
				if (pawnPuluc.PlayerOne == pawn.PlayerOne)
				{
					pawnPuluc.MovePawnBackToSpawn(instantmove, 0.6f, fake);
				}
				else
				{
					pawnPuluc.X = -1;
					pawnPuluc.IsInSpawn = false;
					if (!fake)
					{
						pawnPuluc.CapturedBy = null;
						base.RemovePawnFromBoard(pawnPuluc, 100f, true);
					}
				}
			}
			pawn.MovePawnBackToSpawn(instantmove, 0.6f, fake);
		}

		// Token: 0x04000526 RID: 1318
		public const int WhitePawnCount = 6;

		// Token: 0x04000527 RID: 1319
		public const int BlackPawnCount = 6;

		// Token: 0x04000528 RID: 1320
		public const int TrackTileCount = 11;

		// Token: 0x04000529 RID: 1321
		private const int PlayerHomebaseTileIndex = 11;

		// Token: 0x0400052A RID: 1322
		private const int OpponentHomebaseTileIndex = 12;

		// Token: 0x0400052B RID: 1323
		private BoardGamePuluc.BoardInformation _startState;

		// Token: 0x02000213 RID: 531
		public struct PawnInformation
		{
			// Token: 0x060013D0 RID: 5072 RVA: 0x00078064 File Offset: 0x00076264
			public PawnInformation(int x, bool inSpawn, bool topPawn, PawnPuluc.MovementState state, List<PawnPuluc> pawnsBelow, bool captured, Vec3 position, PawnPuluc capturedBy)
			{
				this.X = x;
				this.IsInSpawn = inSpawn;
				this.IsTopPawn = topPawn;
				this.State = state;
				this.PawnsBelow = pawnsBelow;
				this.IsCaptured = captured;
				this.CapturedBy = capturedBy;
				this.Position = position;
			}

			// Token: 0x04000974 RID: 2420
			public readonly int X;

			// Token: 0x04000975 RID: 2421
			public readonly bool IsInSpawn;

			// Token: 0x04000976 RID: 2422
			public readonly bool IsTopPawn;

			// Token: 0x04000977 RID: 2423
			public readonly bool IsCaptured;

			// Token: 0x04000978 RID: 2424
			public readonly PawnPuluc.MovementState State;

			// Token: 0x04000979 RID: 2425
			public readonly List<PawnPuluc> PawnsBelow;

			// Token: 0x0400097A RID: 2426
			public readonly Vec3 Position;

			// Token: 0x0400097B RID: 2427
			public readonly PawnPuluc CapturedBy;
		}

		// Token: 0x02000214 RID: 532
		public struct BoardInformation
		{
			// Token: 0x060013D1 RID: 5073 RVA: 0x000780A3 File Offset: 0x000762A3
			public BoardInformation(ref BoardGamePuluc.PawnInformation[] pawns)
			{
				this.PawnInformation = pawns;
			}

			// Token: 0x0400097C RID: 2428
			public readonly BoardGamePuluc.PawnInformation[] PawnInformation;
		}
	}
}
