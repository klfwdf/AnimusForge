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
	// Token: 0x020000E8 RID: 232
	public class BoardGameKonane : BoardGameBase
	{
		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x06000B92 RID: 2962 RVA: 0x000556E1 File Offset: 0x000538E1
		public override int TileCount
		{
			get
			{
				return BoardGameKonane.BoardWidth * BoardGameKonane.BoardHeight;
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x06000B93 RID: 2963 RVA: 0x000556EE File Offset: 0x000538EE
		protected override bool RotateBoard
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x06000B94 RID: 2964 RVA: 0x000556F1 File Offset: 0x000538F1
		protected override bool PreMovementStagePresent
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000B95 RID: 2965 RVA: 0x000556F4 File Offset: 0x000538F4
		protected override bool DiceRollRequired
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000B96 RID: 2966 RVA: 0x000556F8 File Offset: 0x000538F8
		public BoardGameKonane(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
			: base(mission, new TextObject("{=5DSafcSC}Konane", null), startingPlayer)
		{
			if (base.Tiles == null)
			{
				base.Tiles = new TileBase[this.TileCount];
			}
			this.SelectedUnit = null;
			this.PawnUnselectedFactor = 4287395960U;
		}

		// Token: 0x06000B97 RID: 2967 RVA: 0x00055750 File Offset: 0x00053950
		public override void InitializeUnits()
		{
			base.PlayerOneUnits.Clear();
			base.PlayerTwoUnits.Clear();
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			for (int i = 0; i < 18; i++)
			{
				GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
				list.Add(base.InitializeUnit(new PawnKonane(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
			}
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int j = 0; j < 18; j++)
			{
				GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
				list2.Add(base.InitializeUnit(new PawnKonane(entity2, base.PlayerWhoStarted > PlayerTurn.PlayerOne)));
			}
		}

		// Token: 0x06000B98 RID: 2968 RVA: 0x00055838 File Offset: 0x00053A38
		public override void InitializeTiles()
		{
			int x;
			IEnumerable<GameEntity> source = from x in this.BoardEntity.GetChildren()
				where x.Tags.Any((string t) => t.Contains("tile_"))
				select x;
			IEnumerable<GameEntity> source2 = from x in this.BoardEntity.GetChildren()
				where x.Tags.Any((string t) => t.Contains("decal_"))
				select x;
			int num;
			for (x = 0; x < BoardGameKonane.BoardWidth; x = num)
			{
				int y;
				for (y = 0; y < BoardGameKonane.BoardHeight; y = num)
				{
					GameEntity gameEntity = source.Single((GameEntity e) => e.HasTag(string.Concat(new object[] { "tile_", x, "_", y })));
					BoardGameDecal firstScriptOfType = source2.Single((GameEntity e) => e.HasTag(string.Concat(new object[] { "decal_", x, "_", y }))).GetFirstScriptOfType<BoardGameDecal>();
					Tile2D tile = new Tile2D(gameEntity, firstScriptOfType, x, y);
					gameEntity.CreateVariableRatePhysics(true);
					this.SetTile(tile, x, y);
					num = y + 1;
				}
				num = x + 1;
			}
		}

		// Token: 0x06000B99 RID: 2969 RVA: 0x00055976 File Offset: 0x00053B76
		public override void InitializeSound()
		{
			PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
			PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
			PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_stone");
			PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
		}

		// Token: 0x06000B9A RID: 2970 RVA: 0x000559B4 File Offset: 0x00053BB4
		public override void Reset()
		{
			base.Reset();
			base.InPreMovementStage = true;
			if (this._startState.PawnInformation == null)
			{
				this.PreplaceUnits();
				return;
			}
			this.RestoreStartingBoard();
		}

		// Token: 0x06000B9B RID: 2971 RVA: 0x000559E0 File Offset: 0x00053BE0
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
						PawnBase pawnOnTile = this.GetTile(x - 1, y).PawnOnTile;
						PawnBase pawnOnTile2 = this.GetTile(x - 2, y).PawnOnTile;
						if (pawnOnTile != null && pawnOnTile2 == null && pawnOnTile.PlayerOne != pawn.PlayerOne)
						{
							Move item;
							item.Unit = pawn;
							item.GoalTile = this.GetTile(x - 2, y);
							list.Add(item);
							if (x > 3)
							{
								PawnBase pawnOnTile3 = this.GetTile(x - 3, y).PawnOnTile;
								PawnBase pawnOnTile4 = this.GetTile(x - 4, y).PawnOnTile;
								if (pawnOnTile3 != null && pawnOnTile4 == null && pawnOnTile3.PlayerOne != pawn.PlayerOne)
								{
									Move item2;
									item2.Unit = pawn;
									item2.GoalTile = this.GetTile(x - 4, y);
									list.Add(item2);
								}
							}
						}
					}
					if (x < BoardGameKonane.BoardWidth - 2)
					{
						PawnBase pawnOnTile5 = this.GetTile(x + 1, y).PawnOnTile;
						PawnBase pawnOnTile6 = this.GetTile(x + 2, y).PawnOnTile;
						if (pawnOnTile5 != null && pawnOnTile6 == null && pawnOnTile5.PlayerOne != pawn.PlayerOne)
						{
							Move item3;
							item3.Unit = pawn;
							item3.GoalTile = this.GetTile(x + 2, y);
							list.Add(item3);
							if (x < 2)
							{
								PawnBase pawnOnTile7 = this.GetTile(x + 3, y).PawnOnTile;
								PawnBase pawnOnTile8 = this.GetTile(x + 4, y).PawnOnTile;
								if (pawnOnTile7 != null && pawnOnTile8 == null && pawnOnTile7.PlayerOne != pawn.PlayerOne)
								{
									Move item4;
									item4.Unit = pawn;
									item4.GoalTile = this.GetTile(x + 4, y);
									list.Add(item4);
								}
							}
						}
					}
					if (y > 1)
					{
						PawnBase pawnOnTile9 = this.GetTile(x, y - 1).PawnOnTile;
						PawnBase pawnOnTile10 = this.GetTile(x, y - 2).PawnOnTile;
						if (pawnOnTile9 != null && pawnOnTile10 == null && pawnOnTile9.PlayerOne != pawn.PlayerOne)
						{
							Move item5;
							item5.Unit = pawn;
							item5.GoalTile = this.GetTile(x, y - 2);
							list.Add(item5);
							if (y > 3)
							{
								PawnBase pawnOnTile11 = this.GetTile(x, y - 3).PawnOnTile;
								PawnBase pawnOnTile12 = this.GetTile(x, y - 4).PawnOnTile;
								if (pawnOnTile11 != null && pawnOnTile12 == null && pawnOnTile11.PlayerOne != pawn.PlayerOne)
								{
									Move item6;
									item6.Unit = pawn;
									item6.GoalTile = this.GetTile(x, y - 4);
									list.Add(item6);
								}
							}
						}
					}
					if (y < BoardGameKonane.BoardHeight - 2)
					{
						PawnBase pawnOnTile13 = this.GetTile(x, y + 1).PawnOnTile;
						PawnBase pawnOnTile14 = this.GetTile(x, y + 2).PawnOnTile;
						if (pawnOnTile13 != null && pawnOnTile14 == null && pawnOnTile13.PlayerOne != pawn.PlayerOne)
						{
							Move item7;
							item7.Unit = pawn;
							item7.GoalTile = this.GetTile(x, y + 2);
							list.Add(item7);
							if (y < 2)
							{
								PawnBase pawnOnTile15 = this.GetTile(x, y + 3).PawnOnTile;
								PawnBase pawnOnTile16 = this.GetTile(x, y + 4).PawnOnTile;
								if (pawnOnTile15 != null && pawnOnTile16 == null && pawnOnTile15.PlayerOne != pawn.PlayerOne)
								{
									Move item8;
									item8.Unit = pawn;
									item8.GoalTile = this.GetTile(x, y + 4);
									list.Add(item8);
								}
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000B9C RID: 2972 RVA: 0x00055D48 File Offset: 0x00053F48
		public override void SetPawnCaptured(PawnBase pawn, bool fake = false)
		{
			base.SetPawnCaptured(pawn, fake);
			PawnKonane pawnKonane = pawn as PawnKonane;
			this.GetTile(pawnKonane.X, pawnKonane.Y).PawnOnTile = null;
			pawnKonane.PrevX = pawnKonane.X;
			pawnKonane.PrevY = pawnKonane.Y;
			pawnKonane.X = -1;
			pawnKonane.Y = -1;
			if (!fake)
			{
				base.RemovePawnFromBoard(pawnKonane, 0.6f, false);
			}
		}

		// Token: 0x06000B9D RID: 2973 RVA: 0x00055DB4 File Offset: 0x00053FB4
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
							this.SelectedUnit = pawn;
						}
					}
					else
					{
						this.SelectedUnit = pawn;
					}
				}
			}
			else if (base.AIOpponent == null && !pawn.PlayerOne)
			{
				if (base.InPreMovementStage)
				{
					if (!pawn.IsPlaced)
					{
						this.SelectedUnit = pawn;
					}
				}
				else
				{
					this.SelectedUnit = pawn;
				}
			}
			return pawn;
		}

		// Token: 0x06000B9E RID: 2974 RVA: 0x00055E24 File Offset: 0x00054024
		protected override void HandlePreMovementStage(float dt)
		{
			if (base.InputManager.IsHotKeyPressed("BoardGamePawnSelect"))
			{
				PawnBase hoveredPawnIfAny = base.GetHoveredPawnIfAny();
				if (hoveredPawnIfAny != null && this.RemovablePawns.Contains(hoveredPawnIfAny))
				{
					this.SetPawnCaptured(hoveredPawnIfAny, false);
					this.UnFocusRemovablePawns();
					base.EndTurn();
					return;
				}
			}
			else
			{
				this.SelectedUnit = null;
			}
		}

		// Token: 0x06000B9F RID: 2975 RVA: 0x00055E77 File Offset: 0x00054077
		protected override void HandlePreMovementStageAI(Move move)
		{
			this.SetPawnCaptured(move.Unit, false);
			base.EndTurn();
		}

		// Token: 0x06000BA0 RID: 2976 RVA: 0x00055E8C File Offset: 0x0005408C
		protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
		{
			base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
			Tile2D tile2D = tile as Tile2D;
			PawnKonane pawnKonane = pawn as PawnKonane;
			if (tile2D.PawnOnTile == null && pawnKonane != null)
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
					this.GetTile(pawnKonane.PrevX, pawnKonane.PrevY).PawnOnTile = null;
				}
				tile.PawnOnTile = pawnKonane;
				if (instantMove || base.InPreMovementStage || this.JustStoppedDraggingUnit)
				{
					pawnKonane.AddGoalPosition(globalPosition);
					pawnKonane.MovePawnToGoalPositionsDelayed(instantMove, speed, true, delay);
				}
				else
				{
					Tile2D prevTile = this.GetTile(pawnKonane.PrevX, pawnKonane.PrevY) as Tile2D;
					this.SetAllGoalPositions(pawnKonane, prevTile, speed);
				}
				if (instantMove && !base.InPreMovementStage)
				{
					this.CheckWhichPawnsAreCaptured(pawnKonane, false);
				}
				else if (pawnKonane == this.SelectedUnit && instantMove)
				{
					this.SelectedUnit = null;
				}
				base.ClearValidMoves();
			}
		}

		// Token: 0x06000BA1 RID: 2977 RVA: 0x0005602C File Offset: 0x0005422C
		protected override void SwitchPlayerTurn()
		{
			if ((base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting) && !base.InPreMovementStage && this.SelectedUnit != null)
			{
				this.CheckWhichPawnsAreCaptured(this.SelectedUnit as PawnKonane, false);
			}
			this.SelectedUnit = null;
			bool flag = false;
			if (base.InPreMovementStage)
			{
				base.InPreMovementStage = !this.CheckPlacementStageOver();
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
					this.CheckForRemovablePawns(true);
				}
				else if (base.PlayerTurn == PlayerTurn.PlayerTwo)
				{
					this.CheckForRemovablePawns(false);
				}
			}
			else if (flag)
			{
				base.EndTurn();
			}
			else
			{
				this.CheckGameEnded();
			}
			base.SwitchPlayerTurn();
		}

		// Token: 0x06000BA2 RID: 2978 RVA: 0x00056104 File Offset: 0x00054304
		protected override bool CheckGameEnded()
		{
			bool result = false;
			if (base.PlayerTurn == PlayerTurn.PlayerTwo)
			{
				List<List<Move>> list = this.CalculateAllValidMoves(BoardGameSide.AI);
				if (!base.HasMovesAvailable(ref list))
				{
					base.OnVictory("str_boardgame_victory_message");
					this.ReadyToPlay = false;
					result = true;
				}
			}
			else if (base.PlayerTurn == PlayerTurn.PlayerOne)
			{
				List<List<Move>> list2 = this.CalculateAllValidMoves(BoardGameSide.None);
				if (!base.HasMovesAvailable(ref list2))
				{
					base.OnDefeat("str_boardgame_defeat_message");
					this.ReadyToPlay = false;
					result = true;
				}
			}
			return result;
		}

		// Token: 0x06000BA3 RID: 2979 RVA: 0x00056173 File Offset: 0x00054373
		protected override void OnAfterBoardSetUp()
		{
			if (this._startState.PawnInformation == null)
			{
				this._startState = this.TakeBoardSnapshot();
			}
			this.ReadyToPlay = true;
			this.CheckForRemovablePawns(base.PlayerWhoStarted == PlayerTurn.PlayerOne);
		}

		// Token: 0x06000BA4 RID: 2980 RVA: 0x000561A8 File Offset: 0x000543A8
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
				this.GetTile(pawnKonane.PrevX, pawnKonane.PrevY).PawnOnTile = null;
				tile2D.PawnOnTile = pawnKonane;
				this.CheckWhichPawnsAreCaptured(pawnKonane, true);
			}
		}

		// Token: 0x06000BA5 RID: 2981 RVA: 0x0005622C File Offset: 0x0005442C
		public int CheckForRemovablePawns(bool playerOne)
		{
			this.UnFocusRemovablePawns();
			int num = (playerOne ? base.GetPlayerTwoUnitsDead() : base.GetPlayerOneUnitsDead());
			if (num == 0)
			{
				using (List<PawnBase>.Enumerator enumerator = (playerOne ? base.PlayerOneUnits : base.PlayerTwoUnits).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						PawnBase pawnBase = enumerator.Current;
						PawnKonane pawnKonane = (PawnKonane)pawnBase;
						if (pawnKonane.X == 0 && pawnKonane.Y == 0)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 5 && pawnKonane.Y == 0)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 0 && pawnKonane.Y == 5)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 5 && pawnKonane.Y == 5)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 2 && pawnKonane.Y == 2)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 3 && pawnKonane.Y == 2)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 2 && pawnKonane.Y == 3)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
						else if (pawnKonane.X == 3 && pawnKonane.Y == 3)
						{
							this.RemovablePawns.Add(pawnKonane);
						}
					}
					goto IL_40C;
				}
			}
			if (num == 1)
			{
				using (List<PawnBase>.Enumerator enumerator = (playerOne ? base.PlayerTwoUnits : base.PlayerOneUnits).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						PawnBase pawnBase2 = enumerator.Current;
						PawnKonane pawnKonane2 = (PawnKonane)pawnBase2;
						if (pawnKonane2.X == -1 && pawnKonane2.Y == -1)
						{
							if (pawnKonane2.PrevX == 0 && pawnKonane2.PrevY == 0)
							{
								this.RemovablePawns.Add(this.GetTile(1, 0).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(0, 1).PawnOnTile);
							}
							else if (pawnKonane2.PrevX == 5 && pawnKonane2.PrevY == 0)
							{
								this.RemovablePawns.Add(this.GetTile(4, 0).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(5, 1).PawnOnTile);
							}
							else if (pawnKonane2.PrevX == 0 && pawnKonane2.PrevY == 5)
							{
								this.RemovablePawns.Add(this.GetTile(0, 4).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(1, 5).PawnOnTile);
							}
							else if (pawnKonane2.PrevX == 5 && pawnKonane2.PrevY == 5)
							{
								this.RemovablePawns.Add(this.GetTile(5, 4).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(4, 5).PawnOnTile);
							}
							if (pawnKonane2.PrevX == 2 && pawnKonane2.PrevY == 2)
							{
								this.RemovablePawns.Add(this.GetTile(2, 3).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(3, 2).PawnOnTile);
								break;
							}
							if (pawnKonane2.PrevX == 3 && pawnKonane2.PrevY == 2)
							{
								this.RemovablePawns.Add(this.GetTile(2, 2).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(3, 3).PawnOnTile);
								break;
							}
							if (pawnKonane2.PrevX == 2 && pawnKonane2.PrevY == 3)
							{
								this.RemovablePawns.Add(this.GetTile(3, 3).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(2, 2).PawnOnTile);
								break;
							}
							if (pawnKonane2.PrevX == 3 && pawnKonane2.PrevY == 3)
							{
								this.RemovablePawns.Add(this.GetTile(2, 3).PawnOnTile);
								this.RemovablePawns.Add(this.GetTile(3, 2).PawnOnTile);
								break;
							}
							break;
						}
					}
					goto IL_40C;
				}
			}
			Debug.FailedAssert("[DEBUG]This should not be reached!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameKonane.cs", "CheckForRemovablePawns", 655);
			IL_40C:
			this.FocusRemovablePawns();
			return this.RemovablePawns.Count;
		}

		// Token: 0x06000BA6 RID: 2982 RVA: 0x0005668C File Offset: 0x0005488C
		public BoardGameKonane.BoardInformation TakeBoardSnapshot()
		{
			BoardGameKonane.PawnInformation[] array = new BoardGameKonane.PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
			TileBaseInformation[,] array2 = new TileBaseInformation[BoardGameKonane.BoardWidth, BoardGameKonane.BoardHeight];
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnKonane pawnKonane = (PawnKonane)pawnBase;
				array[num++] = new BoardGameKonane.PawnInformation(pawnKonane.X, pawnKonane.Y, pawnKonane.PrevX, pawnKonane.PrevY, pawnKonane.Captured, pawnKonane.Entity.GlobalPosition);
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnKonane pawnKonane2 = (PawnKonane)pawnBase2;
				array[num++] = new BoardGameKonane.PawnInformation(pawnKonane2.X, pawnKonane2.Y, pawnKonane2.PrevX, pawnKonane2.PrevY, pawnKonane2.Captured, pawnKonane2.Entity.GlobalPosition);
			}
			for (int i = 0; i < BoardGameKonane.BoardWidth; i++)
			{
				for (int j = 0; j < BoardGameKonane.BoardHeight; j++)
				{
					array2[i, j] = new TileBaseInformation(ref this.GetTile(i, j).PawnOnTile);
				}
			}
			return new BoardGameKonane.BoardInformation(ref array, ref array2);
		}

		// Token: 0x06000BA7 RID: 2983 RVA: 0x0005683C File Offset: 0x00054A3C
		public void UndoMove(ref BoardGameKonane.BoardInformation board)
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnKonane pawnKonane = (PawnKonane)pawnBase;
				pawnKonane.X = board.PawnInformation[num].X;
				pawnKonane.Y = board.PawnInformation[num].Y;
				pawnKonane.PrevX = board.PawnInformation[num].PrevX;
				pawnKonane.PrevY = board.PawnInformation[num].PrevY;
				pawnKonane.Captured = board.PawnInformation[num].IsCaptured;
				num++;
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnKonane pawnKonane2 = (PawnKonane)pawnBase2;
				pawnKonane2.X = board.PawnInformation[num].X;
				pawnKonane2.Y = board.PawnInformation[num].Y;
				pawnKonane2.PrevX = board.PawnInformation[num].PrevX;
				pawnKonane2.PrevY = board.PawnInformation[num].PrevY;
				pawnKonane2.Captured = board.PawnInformation[num].IsCaptured;
				num++;
			}
			for (int i = 0; i < BoardGameKonane.BoardWidth; i++)
			{
				for (int j = 0; j < BoardGameKonane.BoardHeight; j++)
				{
					this.GetTile(i, j).PawnOnTile = board.TileInformation[i, j].PawnOnTile;
				}
			}
		}

		// Token: 0x06000BA8 RID: 2984 RVA: 0x00056A24 File Offset: 0x00054C24
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
				this.SetPawnCaptured(this.GetTile(vec2i3.X, vec2i3.Y).PawnOnTile, fake);
				return;
			}
			Vec2i vec2i4 = new Vec2i(vec2i.X / 4, vec2i.Y / 4);
			Vec2i vec2i5 = new Vec2i(x - vec2i4.X, y - vec2i4.Y);
			Vec2i vec2i6 = new Vec2i(x - vec2i4.X - vec2i4.X * 2, y - vec2i4.Y - vec2i4.Y * 2);
			this.SetPawnCaptured(this.GetTile(vec2i5.X, vec2i5.Y).PawnOnTile, fake);
			this.SetPawnCaptured(this.GetTile(vec2i6.X, vec2i6.Y).PawnOnTile, fake);
		}

		// Token: 0x06000BA9 RID: 2985 RVA: 0x00056BF3 File Offset: 0x00054DF3
		private void SetTile(TileBase tile, int x, int y)
		{
			base.Tiles[y * BoardGameKonane.BoardWidth + x] = tile;
		}

		// Token: 0x06000BAA RID: 2986 RVA: 0x00056C06 File Offset: 0x00054E06
		private TileBase GetTile(int x, int y)
		{
			return base.Tiles[y * BoardGameKonane.BoardWidth + x];
		}

		// Token: 0x06000BAB RID: 2987 RVA: 0x00056C18 File Offset: 0x00054E18
		private void FocusRemovablePawns()
		{
			foreach (PawnBase pawnBase in this.RemovablePawns)
			{
				((PawnKonane)pawnBase).Entity.GetMetaMesh(0).SetFactor1Linear(this.PawnSelectedFactor);
			}
		}

		// Token: 0x06000BAC RID: 2988 RVA: 0x00056C80 File Offset: 0x00054E80
		private void UnFocusRemovablePawns()
		{
			foreach (PawnBase pawnBase in this.RemovablePawns)
			{
				((PawnKonane)pawnBase).Entity.GetMetaMesh(0).SetFactor1Linear(this.PawnUnselectedFactor);
			}
			this.RemovablePawns.Clear();
		}

		// Token: 0x06000BAD RID: 2989 RVA: 0x00056CF4 File Offset: 0x00054EF4
		private void SetAllGoalPositions(PawnKonane pawn, Tile2D prevTile, float speed)
		{
			Vec3 globalPosition = prevTile.Entity.GlobalPosition;
			Vec3 globalPosition2 = this.GetTile(pawn.X, pawn.Y).Entity.GlobalPosition;
			bool flag = false;
			Vec2i vec2i = new Vec2i(pawn.X - prevTile.X, pawn.Y - prevTile.Y);
			if (vec2i.X == 4 || vec2i.Y == 4 || vec2i.X == -4 || vec2i.Y == -4)
			{
				flag = true;
			}
			if (!flag)
			{
				pawn.AddGoalPosition(globalPosition2);
			}
			else
			{
				Vec2i vec2i2 = new Vec2i(vec2i.X / 4, vec2i.Y / 4);
				pawn.AddGoalPosition(this.GetTile(prevTile.X + 2 * vec2i2.X, prevTile.Y + 2 * vec2i2.Y).Entity.GlobalPosition);
				pawn.AddGoalPosition(globalPosition2);
			}
			pawn.MovePawnToGoalPositions(false, speed, false);
		}

		// Token: 0x06000BAE RID: 2990 RVA: 0x00056DDC File Offset: 0x00054FDC
		private bool CheckPlacementStageOver()
		{
			bool result = false;
			if (base.GetPlayerOneUnitsDead() + base.GetPlayerTwoUnitsDead() == 2)
			{
				result = true;
			}
			return result;
		}

		// Token: 0x06000BAF RID: 2991 RVA: 0x00056E00 File Offset: 0x00055000
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
					this.MovePawnToTileDelayed(list[i], this.GetTile(num, num2), false, false, delay);
					this.MovePawnToTileDelayed(list2[i], this.GetTile(num + 1, num2), false, false, delay);
				}
				else
				{
					this.MovePawnToTileDelayed(list[i], this.GetTile(num + 1, num2), false, false, delay);
					this.MovePawnToTileDelayed(list2[i], this.GetTile(num, num2), false, false, delay);
				}
			}
		}

		// Token: 0x06000BB0 RID: 2992 RVA: 0x00056EE0 File Offset: 0x000550E0
		private void RestoreStartingBoard()
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnKonane pawnKonane = (PawnKonane)pawnBase;
				if (this._startState.PawnInformation[num].X != -1)
				{
					if (this._startState.PawnInformation[num].X != pawnKonane.X && this._startState.PawnInformation[num].Y != pawnKonane.Y)
					{
						pawnKonane.Reset();
						TileBase tile = this.GetTile(this._startState.PawnInformation[num].X, this._startState.PawnInformation[num].Y);
						this.MovePawnToTile(pawnKonane, tile, false, true);
					}
				}
				else if (!pawnKonane.Entity.GlobalPosition.NearlyEquals(this._startState.PawnInformation[num].Position, 1E-05f))
				{
					if (pawnKonane.X != -1 && this.GetTile(pawnKonane.X, pawnKonane.Y).PawnOnTile == pawnKonane)
					{
						this.GetTile(pawnKonane.X, pawnKonane.Y).PawnOnTile = null;
					}
					pawnKonane.Reset();
					pawnKonane.AddGoalPosition(this._startState.PawnInformation[num].Position);
					pawnKonane.MovePawnToGoalPositions(false, 0.5f, false);
				}
				num++;
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnKonane pawnKonane2 = (PawnKonane)pawnBase2;
				if (this._startState.PawnInformation[num].X != -1)
				{
					if (this._startState.PawnInformation[num].X != pawnKonane2.X && this._startState.PawnInformation[num].Y != pawnKonane2.Y)
					{
						TileBase tile2 = this.GetTile(this._startState.PawnInformation[num].X, this._startState.PawnInformation[num].Y);
						this.MovePawnToTile(pawnKonane2, tile2, false, true);
					}
				}
				else
				{
					if (pawnKonane2.X != -1 && this.GetTile(pawnKonane2.X, pawnKonane2.Y).PawnOnTile == pawnKonane2)
					{
						this.GetTile(pawnKonane2.X, pawnKonane2.Y).PawnOnTile = null;
					}
					pawnKonane2.Reset();
					pawnKonane2.AddGoalPosition(this._startState.PawnInformation[num].Position);
					pawnKonane2.MovePawnToGoalPositions(false, 0.5f, false);
				}
				num++;
			}
		}

		// Token: 0x0400051E RID: 1310
		public const int WhitePawnCount = 18;

		// Token: 0x0400051F RID: 1311
		public const int BlackPawnCount = 18;

		// Token: 0x04000520 RID: 1312
		public static readonly int BoardWidth = 6;

		// Token: 0x04000521 RID: 1313
		public static readonly int BoardHeight = 6;

		// Token: 0x04000522 RID: 1314
		public List<PawnBase> RemovablePawns = new List<PawnBase>();

		// Token: 0x04000523 RID: 1315
		private BoardGameKonane.BoardInformation _startState;

		// Token: 0x0200020A RID: 522
		public struct BoardInformation
		{
			// Token: 0x060013B9 RID: 5049 RVA: 0x00077E09 File Offset: 0x00076009
			public BoardInformation(ref BoardGameKonane.PawnInformation[] pawns, ref TileBaseInformation[,] tiles)
			{
				this.PawnInformation = pawns;
				this.TileInformation = tiles;
			}

			// Token: 0x0400095B RID: 2395
			public readonly BoardGameKonane.PawnInformation[] PawnInformation;

			// Token: 0x0400095C RID: 2396
			public readonly TileBaseInformation[,] TileInformation;
		}

		// Token: 0x0200020B RID: 523
		public struct PawnInformation
		{
			// Token: 0x060013BA RID: 5050 RVA: 0x00077E1B File Offset: 0x0007601B
			public PawnInformation(int x, int y, int prevX, int prevY, bool captured, Vec3 position)
			{
				this.X = x;
				this.Y = y;
				this.PrevX = prevX;
				this.PrevY = prevY;
				this.IsCaptured = captured;
				this.Position = position;
			}

			// Token: 0x0400095D RID: 2397
			public readonly int X;

			// Token: 0x0400095E RID: 2398
			public readonly int Y;

			// Token: 0x0400095F RID: 2399
			public readonly int PrevX;

			// Token: 0x04000960 RID: 2400
			public readonly int PrevY;

			// Token: 0x04000961 RID: 2401
			public readonly bool IsCaptured;

			// Token: 0x04000962 RID: 2402
			public readonly Vec3 Position;
		}
	}
}
