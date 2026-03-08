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
	// Token: 0x020000EC RID: 236
	public class BoardGameTablut : BoardGameBase
	{
		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000C0D RID: 3085 RVA: 0x0005AAE6 File Offset: 0x00058CE6
		public override int TileCount
		{
			get
			{
				return 81;
			}
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000C0E RID: 3086 RVA: 0x0005AAEA File Offset: 0x00058CEA
		protected override bool RotateBoard
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000C0F RID: 3087 RVA: 0x0005AAED File Offset: 0x00058CED
		protected override bool PreMovementStagePresent
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x06000C10 RID: 3088 RVA: 0x0005AAF0 File Offset: 0x00058CF0
		protected override bool DiceRollRequired
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000C11 RID: 3089 RVA: 0x0005AAF3 File Offset: 0x00058CF3
		// (set) Token: 0x06000C12 RID: 3090 RVA: 0x0005AAFB File Offset: 0x00058CFB
		private PawnTablut King { get; set; }

		// Token: 0x06000C13 RID: 3091 RVA: 0x0005AB04 File Offset: 0x00058D04
		public BoardGameTablut(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
			: base(mission, new TextObject("{=qeKskdiY}Tablut", null), startingPlayer)
		{
			this.SelectedUnit = null;
			this.PawnUnselectedFactor = 4287395960U;
		}

		// Token: 0x06000C14 RID: 3092 RVA: 0x0005AB2B File Offset: 0x00058D2B
		public static bool IsCitadelTile(int tileX, int tileY)
		{
			return tileX == 4 && tileY == 4;
		}

		// Token: 0x06000C15 RID: 3093 RVA: 0x0005AB38 File Offset: 0x00058D38
		public override void InitializeUnits()
		{
			base.PlayerOneUnits.Clear();
			base.PlayerTwoUnits.Clear();
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			for (int i = 0; i < 16; i++)
			{
				GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
				list.Add(base.InitializeUnit(new PawnTablut(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
			}
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int j = 0; j < 9; j++)
			{
				GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
				list2.Add(base.InitializeUnit(new PawnTablut(entity2, base.PlayerWhoStarted > PlayerTurn.PlayerOne)));
			}
			this.King = list2[0] as PawnTablut;
		}

		// Token: 0x06000C16 RID: 3094 RVA: 0x0005AC34 File Offset: 0x00058E34
		public override void InitializeTiles()
		{
			int x;
			IEnumerable<GameEntity> source = from x in this.BoardEntity.GetChildren()
				where x.Tags.Any((string t) => t.Contains("tile_"))
				select x;
			IEnumerable<GameEntity> source2 = from x in this.BoardEntity.GetChildren()
				where x.Tags.Any((string t) => t.Contains("decal_"))
				select x;
			if (base.Tiles == null)
			{
				base.Tiles = new TileBase[this.TileCount];
			}
			int num;
			for (x = 0; x < 9; x = num)
			{
				int y;
				for (y = 0; y < 9; y = num)
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

		// Token: 0x06000C17 RID: 3095 RVA: 0x0005AD85 File Offset: 0x00058F85
		public override void InitializeSound()
		{
			PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
			PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
			PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_stone");
			PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
		}

		// Token: 0x06000C18 RID: 3096 RVA: 0x0005ADC3 File Offset: 0x00058FC3
		public override void Reset()
		{
			base.Reset();
			if (this._startState.PawnInformation == null)
			{
				this.PreplaceUnits();
				return;
			}
			this.RestoreStartingBoard();
		}

		// Token: 0x06000C19 RID: 3097 RVA: 0x0005ADE8 File Offset: 0x00058FE8
		public override List<Move> CalculateValidMoves(PawnBase pawn)
		{
			List<Move> list = new List<Move>(16);
			if (pawn.IsPlaced && !pawn.Captured)
			{
				PawnTablut pawnTablut = pawn as PawnTablut;
				int i = pawnTablut.X;
				int j = pawnTablut.Y;
				while (i > 0)
				{
					i--;
					if (!this.AddValidMove(list, pawn, i, j))
					{
						break;
					}
				}
				i = pawnTablut.X;
				while (i < 8)
				{
					i++;
					if (!this.AddValidMove(list, pawn, i, j))
					{
						break;
					}
				}
				i = pawnTablut.X;
				while (j < 8)
				{
					j++;
					if (!this.AddValidMove(list, pawn, i, j))
					{
						break;
					}
				}
				j = pawnTablut.Y;
				while (j > 0)
				{
					j--;
					if (!this.AddValidMove(list, pawn, i, j))
					{
						break;
					}
				}
				j = pawnTablut.Y;
			}
			return list;
		}

		// Token: 0x06000C1A RID: 3098 RVA: 0x0005AEA0 File Offset: 0x000590A0
		public override void SetPawnCaptured(PawnBase pawn, bool fake = false)
		{
			base.SetPawnCaptured(pawn, fake);
			PawnTablut pawnTablut = pawn as PawnTablut;
			this.GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = null;
			pawnTablut.X = -1;
			pawnTablut.Y = -1;
			if (!fake)
			{
				base.RemovePawnFromBoard(pawnTablut, 0.6f, false);
			}
		}

		// Token: 0x06000C1B RID: 3099 RVA: 0x0005AEF2 File Offset: 0x000590F2
		protected override void OnAfterBoardSetUp()
		{
			if (this._startState.PawnInformation == null)
			{
				this._startState = this.TakeBoardSnapshot();
			}
			this.ReadyToPlay = true;
		}

		// Token: 0x06000C1C RID: 3100 RVA: 0x0005AF14 File Offset: 0x00059114
		protected override PawnBase SelectPawn(PawnBase pawn)
		{
			if (pawn.PlayerOne == (base.PlayerTurn == PlayerTurn.PlayerOne))
			{
				this.SelectedUnit = pawn;
			}
			return pawn;
		}

		// Token: 0x06000C1D RID: 3101 RVA: 0x0005AF30 File Offset: 0x00059130
		protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
		{
			base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
			Tile2D tile2D = tile as Tile2D;
			PawnTablut pawnTablut = pawn as PawnTablut;
			if (tile2D.PawnOnTile == null)
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
				Vec3 globalPosition = pawnTablut.Entity.GlobalPosition;
				Vec3 globalPosition2 = tile2D.Entity.GlobalPosition;
				if (pawnTablut.X != -1 && pawnTablut.Y != -1)
				{
					this.GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = null;
				}
				pawnTablut.MovingToDifferentTile = pawnTablut.X != tile2D.X || pawnTablut.Y != tile2D.Y;
				pawnTablut.X = tile2D.X;
				pawnTablut.Y = tile2D.Y;
				tile2D.PawnOnTile = pawnTablut;
				if (this.SettingUpBoard && globalPosition2.z > globalPosition.z)
				{
					Vec3 goal = globalPosition;
					goal.z += 2f * (globalPosition2.z - globalPosition.z);
					pawnTablut.AddGoalPosition(goal);
					pawnTablut.MovePawnToGoalPositionsDelayed(instantMove, 0.5f, this.JustStoppedDraggingUnit, delay);
				}
				pawnTablut.AddGoalPosition(globalPosition2);
				pawnTablut.MovePawnToGoalPositionsDelayed(instantMove, 0.5f, this.JustStoppedDraggingUnit, delay);
				if (instantMove)
				{
					this.CheckIfPawnCaptures(this.SelectedUnit as PawnTablut, false);
				}
				if (pawnTablut == this.SelectedUnit && instantMove)
				{
					this.SelectedUnit = null;
				}
			}
		}

		// Token: 0x06000C1E RID: 3102 RVA: 0x0005B0C8 File Offset: 0x000592C8
		protected override void SwitchPlayerTurn()
		{
			if ((base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting) && this.SelectedUnit != null)
			{
				this.CheckIfPawnCaptures(this.SelectedUnit as PawnTablut, false);
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
			this.CheckGameEnded();
			base.SwitchPlayerTurn();
		}

		// Token: 0x06000C1F RID: 3103 RVA: 0x0005B138 File Offset: 0x00059338
		protected override bool CheckGameEnded()
		{
			BoardGameTablut.State state = this.CheckGameState();
			bool result = true;
			switch (state)
			{
			case BoardGameTablut.State.InProgress:
				result = false;
				break;
			case BoardGameTablut.State.PlayerWon:
				base.OnVictory("str_boardgame_victory_message");
				this.ReadyToPlay = false;
				break;
			case BoardGameTablut.State.AIWon:
				base.OnDefeat("str_boardgame_defeat_message");
				this.ReadyToPlay = false;
				break;
			}
			return result;
		}

		// Token: 0x06000C20 RID: 3104 RVA: 0x0005B194 File Offset: 0x00059394
		public bool AIMakeMove(Move move)
		{
			Tile2D tile2D = move.GoalTile as Tile2D;
			PawnTablut pawnTablut = move.Unit as PawnTablut;
			if (tile2D.PawnOnTile == null)
			{
				if (pawnTablut.X != -1 && pawnTablut.Y != -1)
				{
					this.GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = null;
				}
				pawnTablut.X = tile2D.X;
				pawnTablut.Y = tile2D.Y;
				tile2D.PawnOnTile = pawnTablut;
				this.CheckIfPawnCaptures(pawnTablut, true);
				return true;
			}
			return false;
		}

		// Token: 0x06000C21 RID: 3105 RVA: 0x0005B218 File Offset: 0x00059418
		public bool HasAvailableMoves(PawnTablut pawn)
		{
			bool result = false;
			if (pawn.IsPlaced && !pawn.Captured)
			{
				int x = pawn.X;
				int y = pawn.Y;
				result = (x > 0 && this.GetTile(x - 1, y).PawnOnTile == null && !BoardGameTablut.IsCitadelTile(x - 1, y)) || (x < 8 && this.GetTile(x + 1, y).PawnOnTile == null && !BoardGameTablut.IsCitadelTile(x + 1, y)) || (y > 0 && this.GetTile(x, y - 1).PawnOnTile == null && !BoardGameTablut.IsCitadelTile(x, y - 1)) || (y < 8 && this.GetTile(x, y + 1).PawnOnTile == null && !BoardGameTablut.IsCitadelTile(x, y + 1));
			}
			return result;
		}

		// Token: 0x06000C22 RID: 3106 RVA: 0x0005B2D4 File Offset: 0x000594D4
		public Move GetRandomAvailableMove(PawnTablut pawn)
		{
			List<Move> list = this.CalculateValidMoves(pawn);
			return list[MBRandom.RandomInt(list.Count)];
		}

		// Token: 0x06000C23 RID: 3107 RVA: 0x0005B2F0 File Offset: 0x000594F0
		public Move GetWinningMoveIfPresent(BoardGameSide side)
		{
			Move invalid = Move.Invalid;
			if ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && side == BoardGameSide.AI) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && side == BoardGameSide.Player))
			{
				bool flag = false;
				if (this.King.X <= 4)
				{
					bool flag2 = true;
					for (int i = this.King.X - 1; i >= 0; i--)
					{
						if (this.GetTile(i, this.King.Y).PawnOnTile != null)
						{
							flag2 = false;
							break;
						}
					}
					if (flag2)
					{
						invalid.Unit = this.King;
						invalid.GoalTile = this.GetTile(0, this.King.Y);
						flag = true;
					}
				}
				if (!flag && this.King.X >= 4)
				{
					bool flag3 = true;
					for (int j = this.King.X + 1; j < 9; j++)
					{
						if (this.GetTile(j, this.King.Y).PawnOnTile != null)
						{
							flag3 = false;
							break;
						}
					}
					if (flag3)
					{
						invalid.Unit = this.King;
						invalid.GoalTile = this.GetTile(8, this.King.Y);
						flag = true;
					}
				}
				if (!flag && this.King.Y <= 4)
				{
					bool flag4 = true;
					for (int k = this.King.Y - 1; k >= 0; k--)
					{
						if (this.GetTile(this.King.X, k).PawnOnTile != null)
						{
							flag4 = false;
							break;
						}
					}
					if (flag4)
					{
						invalid.Unit = this.King;
						invalid.GoalTile = this.GetTile(this.King.X, 0);
						flag = true;
					}
				}
				if (!flag && this.King.Y >= 4)
				{
					bool flag5 = true;
					for (int l = this.King.Y + 1; l < 9; l++)
					{
						if (this.GetTile(this.King.X, l).PawnOnTile != null)
						{
							flag5 = false;
							break;
						}
					}
					if (flag5)
					{
						invalid.Unit = this.King;
						invalid.GoalTile = this.GetTile(this.King.X, 8);
					}
				}
			}
			else if (!BoardGameTablut.IsCitadelTile(this.King.X, this.King.Y))
			{
				TileBase tile = this.GetTile(this.King.X + 1, this.King.Y);
				TileBase tile2 = this.GetTile(this.King.X - 1, this.King.Y);
				bool flag6 = BoardGameTablut.IsCitadelTile(this.King.X + 1, this.King.Y);
				bool flag7 = BoardGameTablut.IsCitadelTile(this.King.X - 1, this.King.Y);
				TileBase tile3 = this.GetTile(this.King.X, this.King.Y + 1);
				TileBase tile4 = this.GetTile(this.King.X, this.King.Y - 1);
				bool flag8 = BoardGameTablut.IsCitadelTile(this.King.X, this.King.Y + 1);
				bool flag9 = BoardGameTablut.IsCitadelTile(this.King.X, this.King.Y - 1);
				bool flag10 = false;
				if (tile2.PawnOnTile == null && !flag7 && (flag6 || (tile.PawnOnTile != null && ((base.PlayerWhoStarted == PlayerTurn.PlayerOne && tile.PawnOnTile.PlayerOne) || (base.PlayerWhoStarted == PlayerTurn.PlayerTwo && !tile.PawnOnTile.PlayerOne)))))
				{
					int num = this.King.X - 2;
					int num2 = ((num < 4) ? 0 : 5);
					for (int m = num; m >= num2; m--)
					{
						PawnBase pawnOnTile = this.GetTile(m, this.King.Y).PawnOnTile;
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
						int num3 = this.King.Y - 1;
						int num4 = ((num3 < 4) ? 0 : 5);
						for (int n = num3; n >= num4; n--)
						{
							PawnBase pawnOnTile2 = this.GetTile(this.King.X - 1, n).PawnOnTile;
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
						int num5 = this.King.Y + 1;
						for (int num6 = num5; num6 < num5; num6++)
						{
							PawnBase pawnOnTile3 = this.GetTile(this.King.X - 1, num6).PawnOnTile;
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
					int num7 = this.King.X + 2;
					int num8 = ((num7 > 4) ? 9 : 4);
					for (int num9 = num7; num9 < num8; num9++)
					{
						PawnBase pawnOnTile4 = this.GetTile(num9, this.King.Y).PawnOnTile;
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
						int num10 = this.King.Y - 1;
						int num11 = ((num10 < 4) ? 0 : 5);
						for (int num12 = num10; num12 >= num11; num12--)
						{
							PawnBase pawnOnTile5 = this.GetTile(this.King.X + 1, num12).PawnOnTile;
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
						int num13 = this.King.Y + 1;
						int num14 = ((num13 > 4) ? 9 : 4);
						for (int num15 = num13; num15 < num14; num15++)
						{
							PawnBase pawnOnTile6 = this.GetTile(this.King.X + 1, num15).PawnOnTile;
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
					int num16 = this.King.X - 1;
					int num17 = ((num16 < 4) ? 0 : 5);
					for (int num18 = num16; num18 >= num17; num18--)
					{
						PawnBase pawnOnTile7 = this.GetTile(num18, this.King.Y - 1).PawnOnTile;
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
						int num19 = this.King.X + 1;
						int num20 = ((num19 > 4) ? 9 : 4);
						for (int num21 = num19; num21 < num20; num21++)
						{
							PawnBase pawnOnTile8 = this.GetTile(num21, this.King.Y - 1).PawnOnTile;
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
						int num22 = this.King.Y - 2;
						int num23 = ((num22 < 4) ? 0 : 5);
						for (int num24 = num22; num24 >= num23; num24--)
						{
							PawnBase pawnOnTile9 = this.GetTile(this.King.X, num24).PawnOnTile;
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
					int num25 = this.King.X - 1;
					int num26 = ((num25 < 4) ? 0 : 5);
					for (int num27 = num25; num27 >= num26; num27--)
					{
						PawnBase pawnOnTile10 = this.GetTile(num27, this.King.Y + 1).PawnOnTile;
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
						int num28 = this.King.X + 1;
						int num29 = ((num28 > 4) ? 9 : 4);
						for (int num30 = num28; num30 < num29; num30++)
						{
							PawnBase pawnOnTile11 = this.GetTile(num30, this.King.Y + 1).PawnOnTile;
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
						int num31 = this.King.Y + 2;
						int num32 = ((num31 > 4) ? 9 : 4);
						for (int num33 = num31; num33 < num32; num33++)
						{
							PawnBase pawnOnTile12 = this.GetTile(this.King.X, num33).PawnOnTile;
							if (pawnOnTile12 != null)
							{
								if ((base.PlayerWhoStarted != PlayerTurn.PlayerOne || !pawnOnTile12.PlayerOne) && (base.PlayerWhoStarted != PlayerTurn.PlayerTwo || pawnOnTile12.PlayerOne))
								{
									break;
								}
								invalid.Unit = pawnOnTile12;
								invalid.GoalTile = tile3;
							}
						}
					}
				}
			}
			return invalid;
		}

		// Token: 0x06000C24 RID: 3108 RVA: 0x0005BDC0 File Offset: 0x00059FC0
		public BoardGameTablut.BoardInformation TakeBoardSnapshot()
		{
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			BoardGameTablut.PawnInformation[] array = new BoardGameTablut.PawnInformation[25];
			for (int i = 0; i < 25; i++)
			{
				PawnTablut pawnTablut;
				if (i < 16)
				{
					pawnTablut = list[i] as PawnTablut;
				}
				else
				{
					pawnTablut = list2[i - 16] as PawnTablut;
				}
				BoardGameTablut.PawnInformation pawnInformation;
				pawnInformation.X = pawnTablut.X;
				pawnInformation.Y = pawnTablut.Y;
				pawnInformation.IsCaptured = pawnTablut.Captured;
				array[i] = pawnInformation;
			}
			return new BoardGameTablut.BoardInformation(ref array);
		}

		// Token: 0x06000C25 RID: 3109 RVA: 0x0005BE70 File Offset: 0x0005A070
		public void UndoMove(ref BoardGameTablut.BoardInformation board)
		{
			for (int i = 0; i < this.TileCount; i++)
			{
				base.Tiles[i].PawnOnTile = null;
			}
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int j = 0; j < 25; j++)
			{
				BoardGameTablut.PawnInformation pawnInformation = board.PawnInformation[j];
				PawnTablut pawnTablut;
				if (j < 16)
				{
					pawnTablut = list[j] as PawnTablut;
				}
				else
				{
					pawnTablut = list2[j - 16] as PawnTablut;
				}
				pawnTablut.X = pawnInformation.X;
				pawnTablut.Y = pawnInformation.Y;
				pawnTablut.Captured = pawnInformation.IsCaptured;
				if (pawnTablut.IsPlaced)
				{
					this.GetTile(pawnTablut.X, pawnTablut.Y).PawnOnTile = pawnTablut;
				}
			}
		}

		// Token: 0x06000C26 RID: 3110 RVA: 0x0005BF60 File Offset: 0x0005A160
		public BoardGameTablut.State CheckGameState()
		{
			BoardGameTablut.State result;
			if (!base.AIOpponent.AbortRequested)
			{
				result = BoardGameTablut.State.InProgress;
				if (base.PlayerTurn == PlayerTurn.PlayerOne || base.PlayerTurn == PlayerTurn.PlayerTwo)
				{
					bool flag = base.PlayerWhoStarted == PlayerTurn.PlayerOne;
					if (this.King.Captured)
					{
						result = (flag ? BoardGameTablut.State.PlayerWon : BoardGameTablut.State.AIWon);
					}
					else if (this.King.X == 0 || this.King.X == 8 || this.King.Y == 0 || this.King.Y == 8)
					{
						result = (flag ? BoardGameTablut.State.AIWon : BoardGameTablut.State.PlayerWon);
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
							if (pawnBase.IsPlaced && !pawnBase.Captured && this.HasAvailableMoves(pawnBase as PawnTablut))
							{
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							result = (flag3 ? BoardGameTablut.State.AIWon : BoardGameTablut.State.PlayerWon);
						}
					}
				}
			}
			else
			{
				result = BoardGameTablut.State.Aborted;
			}
			return result;
		}

		// Token: 0x06000C27 RID: 3111 RVA: 0x0005C075 File Offset: 0x0005A275
		private void SetTile(TileBase tile, int x, int y)
		{
			base.Tiles[y * 9 + x] = tile;
		}

		// Token: 0x06000C28 RID: 3112 RVA: 0x0005C085 File Offset: 0x0005A285
		private TileBase GetTile(int x, int y)
		{
			return base.Tiles[y * 9 + x];
		}

		// Token: 0x06000C29 RID: 3113 RVA: 0x0005C094 File Offset: 0x0005A294
		private void PreplaceUnits()
		{
			int[] array = new int[]
			{
				3, 0, 4, 0, 5, 0, 4, 1, 0, 3,
				0, 4, 0, 5, 1, 4, 8, 3, 8, 4,
				8, 5, 7, 4, 3, 8, 4, 8, 5, 8,
				4, 7
			};
			int[] array2 = new int[]
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
				this.MovePawnToTileDelayed(list[i], this.GetTile(x, y), false, false, 0.15f * (float)(i + 1) + 0.25f);
			}
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			int count2 = list2.Count;
			for (int j = 0; j < count2; j++)
			{
				int x2 = array2[j * 2];
				int y2 = array2[j * 2 + 1];
				this.MovePawnToTileDelayed(list2[j], this.GetTile(x2, y2), false, false, 0.15f * (float)(j + 1) + 0.25f);
			}
		}

		// Token: 0x06000C2A RID: 3114 RVA: 0x0005C1A4 File Offset: 0x0005A3A4
		private void RestoreStartingBoard()
		{
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int i = 0; i < 25; i++)
			{
				PawnBase pawnBase;
				if (i < 16)
				{
					pawnBase = list[i];
				}
				else
				{
					pawnBase = list2[i - 16];
				}
				BoardGameTablut.PawnInformation pawnInformation = this._startState.PawnInformation[i];
				TileBase tile = this.GetTile(pawnInformation.X, pawnInformation.Y);
				pawnBase.Reset();
				this.MovePawnToTile(pawnBase, tile, false, false);
			}
		}

		// Token: 0x06000C2B RID: 3115 RVA: 0x0005C240 File Offset: 0x0005A440
		private bool AddValidMove(List<Move> moves, PawnBase pawn, int x, int y)
		{
			bool result = false;
			TileBase tile = this.GetTile(x, y);
			if (tile.PawnOnTile == null && !BoardGameTablut.IsCitadelTile(x, y))
			{
				Move item;
				item.Unit = pawn;
				item.GoalTile = tile;
				moves.Add(item);
				result = true;
			}
			return result;
		}

		// Token: 0x06000C2C RID: 3116 RVA: 0x0005C288 File Offset: 0x0005A488
		private void CheckIfPawnCapturedEnemyPawn(PawnTablut pawn, bool fake, TileBase victimTile, Tile2D helperTile)
		{
			PawnBase pawnOnTile = victimTile.PawnOnTile;
			if (pawnOnTile != null && pawnOnTile.PlayerOne != pawn.PlayerOne)
			{
				PawnBase pawnOnTile2 = helperTile.PawnOnTile;
				if (pawnOnTile2 != null)
				{
					if (pawnOnTile2.PlayerOne == pawn.PlayerOne)
					{
						this.SetPawnCaptured(pawnOnTile, fake);
						return;
					}
				}
				else if (BoardGameTablut.IsCitadelTile(helperTile.X, helperTile.Y))
				{
					this.SetPawnCaptured(pawnOnTile, fake);
				}
			}
		}

		// Token: 0x06000C2D RID: 3117 RVA: 0x0005C2F4 File Offset: 0x0005A4F4
		private void CheckIfPawnCaptures(PawnTablut pawn, bool fake = false)
		{
			int x = pawn.X;
			int y = pawn.Y;
			if (x > 1)
			{
				this.CheckIfPawnCapturedEnemyPawn(pawn, fake, this.GetTile(x - 1, y), this.GetTile(x - 2, y) as Tile2D);
			}
			if (x < 7)
			{
				this.CheckIfPawnCapturedEnemyPawn(pawn, fake, this.GetTile(x + 1, y), this.GetTile(x + 2, y) as Tile2D);
			}
			if (y > 1)
			{
				this.CheckIfPawnCapturedEnemyPawn(pawn, fake, this.GetTile(x, y - 1), this.GetTile(x, y - 2) as Tile2D);
			}
			if (y < 7)
			{
				this.CheckIfPawnCapturedEnemyPawn(pawn, fake, this.GetTile(x, y + 1), this.GetTile(x, y + 2) as Tile2D);
			}
		}

		// Token: 0x04000533 RID: 1331
		public const int BoardWidth = 9;

		// Token: 0x04000534 RID: 1332
		public const int BoardHeight = 9;

		// Token: 0x04000535 RID: 1333
		public const int AttackerPawnCount = 16;

		// Token: 0x04000536 RID: 1334
		public const int DefenderPawnCount = 9;

		// Token: 0x04000537 RID: 1335
		private BoardGameTablut.BoardInformation _startState;

		// Token: 0x0200021D RID: 541
		public struct PawnInformation
		{
			// Token: 0x060013E8 RID: 5096 RVA: 0x00078311 File Offset: 0x00076511
			public PawnInformation(int x, int y, bool captured)
			{
				this.X = x;
				this.Y = y;
				this.IsCaptured = captured;
			}

			// Token: 0x04000997 RID: 2455
			public int X;

			// Token: 0x04000998 RID: 2456
			public int Y;

			// Token: 0x04000999 RID: 2457
			public bool IsCaptured;
		}

		// Token: 0x0200021E RID: 542
		public struct BoardInformation
		{
			// Token: 0x060013E9 RID: 5097 RVA: 0x00078328 File Offset: 0x00076528
			public BoardInformation(ref BoardGameTablut.PawnInformation[] pawns)
			{
				this.PawnInformation = pawns;
			}

			// Token: 0x0400099A RID: 2458
			public readonly BoardGameTablut.PawnInformation[] PawnInformation;
		}

		// Token: 0x0200021F RID: 543
		public enum State
		{
			// Token: 0x0400099C RID: 2460
			InProgress,
			// Token: 0x0400099D RID: 2461
			Aborted,
			// Token: 0x0400099E RID: 2462
			PlayerWon,
			// Token: 0x0400099F RID: 2463
			AIWon
		}
	}
}
