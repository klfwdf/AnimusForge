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
	// Token: 0x020000E1 RID: 225
	public class BoardGameBaghChal : BoardGameBase
	{
		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000B15 RID: 2837 RVA: 0x00052824 File Offset: 0x00050A24
		public override int TileCount
		{
			get
			{
				return BoardGameBaghChal.BoardWidth * BoardGameBaghChal.BoardHeight;
			}
		}

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000B16 RID: 2838 RVA: 0x00052831 File Offset: 0x00050A31
		protected override bool RotateBoard
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x06000B17 RID: 2839 RVA: 0x00052834 File Offset: 0x00050A34
		protected override bool PreMovementStagePresent
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x06000B18 RID: 2840 RVA: 0x00052837 File Offset: 0x00050A37
		protected override bool DiceRollRequired
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000B19 RID: 2841 RVA: 0x0005283A File Offset: 0x00050A3A
		public BoardGameBaghChal(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
			: base(mission, new TextObject("{=zWoj91XY}BaghChal", null), startingPlayer)
		{
			if (base.Tiles == null)
			{
				base.Tiles = new TileBase[this.TileCount];
			}
			this.SelectedUnit = null;
			this.PawnUnselectedFactor = 4287395960U;
		}

		// Token: 0x06000B1A RID: 2842 RVA: 0x0005287C File Offset: 0x00050A7C
		public override void InitializeUnits()
		{
			bool flag = base.PlayerWhoStarted == PlayerTurn.PlayerOne;
			if (this._goatUnits == null && this._tigerUnits == null)
			{
				this._goatUnits = (flag ? base.PlayerOneUnits : base.PlayerTwoUnits);
				for (int i = 0; i < 20; i++)
				{
					GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
					this._goatUnits.Add(base.InitializeUnit(new PawnBaghChal(entity, flag, false)));
				}
				this._tigerUnits = (flag ? base.PlayerTwoUnits : base.PlayerOneUnits);
				for (int j = 0; j < 4; j++)
				{
					GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
					this._tigerUnits.Add(base.InitializeUnit(new PawnBaghChal(entity2, !flag, true)));
				}
				return;
			}
			if (this._goatUnits == base.PlayerOneUnits != flag)
			{
				List<PawnBase> playerOneUnits = base.PlayerOneUnits;
				base.PlayerOneUnits = base.PlayerTwoUnits;
				base.PlayerTwoUnits = playerOneUnits;
			}
			this._goatUnits = (flag ? base.PlayerOneUnits : base.PlayerTwoUnits);
			this._tigerUnits = (flag ? base.PlayerTwoUnits : base.PlayerOneUnits);
			foreach (PawnBase pawnBase in this._goatUnits)
			{
				pawnBase.Reset();
				pawnBase.SetPlayerOne(flag);
			}
			foreach (PawnBase pawnBase2 in this._tigerUnits)
			{
				pawnBase2.Reset();
				pawnBase2.SetPlayerOne(!flag);
			}
		}

		// Token: 0x06000B1B RID: 2843 RVA: 0x00052A58 File Offset: 0x00050C58
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
			for (x = 0; x < BoardGameBaghChal.BoardWidth; x = num)
			{
				int y;
				for (y = 0; y < BoardGameBaghChal.BoardHeight; y = num)
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

		// Token: 0x06000B1C RID: 2844 RVA: 0x00052B96 File Offset: 0x00050D96
		public override void InitializeSound()
		{
			PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
			PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
			PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_stone");
			PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
		}

		// Token: 0x06000B1D RID: 2845 RVA: 0x00052BD4 File Offset: 0x00050DD4
		public override void Reset()
		{
			base.Reset();
			base.InPreMovementStage = true;
		}

		// Token: 0x06000B1E RID: 2846 RVA: 0x00052BE4 File Offset: 0x00050DE4
		public override List<List<Move>> CalculateAllValidMoves(BoardGameSide side)
		{
			List<List<Move>> list = new List<List<Move>>();
			bool flag = true;
			foreach (PawnBase pawnBase in ((side == BoardGameSide.AI) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnBaghChal pawnBaghChal = (PawnBaghChal)pawnBase;
				if ((flag || pawnBaghChal.IsPlaced) && !pawnBaghChal.Captured)
				{
					List<Move> list2 = this.CalculateValidMoves(pawnBaghChal);
					if (list2.Count > 0)
					{
						list.Add(list2);
					}
					if (pawnBaghChal.IsGoat && !pawnBaghChal.IsPlaced)
					{
						flag = false;
					}
				}
			}
			return list;
		}

		// Token: 0x06000B1F RID: 2847 RVA: 0x00052C8C File Offset: 0x00050E8C
		public override List<Move> CalculateValidMoves(PawnBase pawn)
		{
			List<Move> list = new List<Move>();
			PawnBaghChal pawnBaghChal = pawn as PawnBaghChal;
			if (pawn != null)
			{
				int x = pawnBaghChal.X;
				int y = pawnBaghChal.Y;
				bool isTiger = pawnBaghChal.IsTiger;
				if ((isTiger || !base.InPreMovementStage) && x >= 0 && x < BoardGameBaghChal.BoardWidth && y >= 0 && y < BoardGameBaghChal.BoardHeight)
				{
					if (x > 0 && this.GetTile(x - 1, y).PawnOnTile == null)
					{
						Move item;
						item.Unit = pawn;
						item.GoalTile = this.GetTile(x - 1, y);
						list.Add(item);
					}
					if (x < BoardGameBaghChal.BoardWidth - 1 && this.GetTile(x + 1, y).PawnOnTile == null)
					{
						Move item2;
						item2.Unit = pawn;
						item2.GoalTile = this.GetTile(x + 1, y);
						list.Add(item2);
					}
					if (y > 0 && this.GetTile(x, y - 1).PawnOnTile == null)
					{
						Move item3;
						item3.Unit = pawn;
						item3.GoalTile = this.GetTile(x, y - 1);
						list.Add(item3);
					}
					if (y < BoardGameBaghChal.BoardHeight - 1 && this.GetTile(x, y + 1).PawnOnTile == null)
					{
						Move item4;
						item4.Unit = pawn;
						item4.GoalTile = this.GetTile(x, y + 1);
						list.Add(item4);
					}
					if ((x + y) % 2 == 0)
					{
						Vec2i vec2i = new Vec2i(x + 1, y + 1);
						if (vec2i.X < BoardGameBaghChal.BoardWidth && vec2i.Y < BoardGameBaghChal.BoardHeight && this.GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
						{
							Move item5;
							item5.Unit = pawn;
							item5.GoalTile = this.GetTile(vec2i.X, vec2i.Y);
							list.Add(item5);
						}
						vec2i = new Vec2i(x - 1, y + 1);
						if (vec2i.X >= 0 && vec2i.Y < BoardGameBaghChal.BoardHeight && this.GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
						{
							Move item6;
							item6.Unit = pawn;
							item6.GoalTile = this.GetTile(vec2i.X, vec2i.Y);
							list.Add(item6);
						}
						vec2i = new Vec2i(x - 1, y - 1);
						if (vec2i.X >= 0 && vec2i.Y >= 0 && this.GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
						{
							Move item7;
							item7.Unit = pawn;
							item7.GoalTile = this.GetTile(vec2i.X, vec2i.Y);
							list.Add(item7);
						}
						vec2i = new Vec2i(x + 1, y - 1);
						if (vec2i.X < BoardGameBaghChal.BoardWidth && vec2i.Y >= 0 && this.GetTile(vec2i.X, vec2i.Y).PawnOnTile == null)
						{
							Move item8;
							item8.Unit = pawn;
							item8.GoalTile = this.GetTile(vec2i.X, vec2i.Y);
							list.Add(item8);
						}
					}
				}
				if (isTiger && x >= 0 && x < BoardGameBaghChal.BoardWidth && y >= 0 && y < BoardGameBaghChal.BoardHeight)
				{
					if (x > 1)
					{
						PawnBaghChal pawnBaghChal2 = this.GetTile(x - 1, y).PawnOnTile as PawnBaghChal;
						PawnBase pawnOnTile = this.GetTile(x - 2, y).PawnOnTile;
						if (pawnBaghChal2 != null && !pawnBaghChal2.IsTiger && pawnOnTile == null)
						{
							Move item9;
							item9.Unit = pawn;
							item9.GoalTile = this.GetTile(x - 2, y);
							list.Add(item9);
						}
					}
					if (x < BoardGameBaghChal.BoardWidth - 2)
					{
						PawnBaghChal pawnBaghChal3 = this.GetTile(x + 1, y).PawnOnTile as PawnBaghChal;
						PawnBase pawnOnTile2 = this.GetTile(x + 2, y).PawnOnTile;
						if (pawnBaghChal3 != null && !pawnBaghChal3.IsTiger && pawnOnTile2 == null)
						{
							Move item10;
							item10.Unit = pawn;
							item10.GoalTile = this.GetTile(x + 2, y);
							list.Add(item10);
						}
					}
					if (y > 1)
					{
						PawnBaghChal pawnBaghChal4 = this.GetTile(x, y - 1).PawnOnTile as PawnBaghChal;
						PawnBase pawnOnTile3 = this.GetTile(x, y - 2).PawnOnTile;
						if (pawnBaghChal4 != null && !pawnBaghChal4.IsTiger && pawnOnTile3 == null)
						{
							Move item11;
							item11.Unit = pawn;
							item11.GoalTile = this.GetTile(x, y - 2);
							list.Add(item11);
						}
					}
					if (y < BoardGameBaghChal.BoardHeight - 2)
					{
						PawnBaghChal pawnBaghChal5 = this.GetTile(x, y + 1).PawnOnTile as PawnBaghChal;
						PawnBase pawnOnTile4 = this.GetTile(x, y + 2).PawnOnTile;
						if (pawnBaghChal5 != null && !pawnBaghChal5.IsTiger && pawnOnTile4 == null)
						{
							Move item12;
							item12.Unit = pawn;
							item12.GoalTile = this.GetTile(x, y + 2);
							list.Add(item12);
						}
					}
					if ((x + y) % 2 == 0)
					{
						Vec2i vec2i2 = new Vec2i(x + 2, y + 2);
						if (vec2i2.X < BoardGameBaghChal.BoardWidth && vec2i2.Y < BoardGameBaghChal.BoardHeight)
						{
							PawnBaghChal pawnBaghChal6 = this.GetTile(x + 1, y + 1).PawnOnTile as PawnBaghChal;
							if (pawnBaghChal6 != null && !pawnBaghChal6.IsTiger && this.GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
							{
								Move item13;
								item13.Unit = pawn;
								item13.GoalTile = this.GetTile(vec2i2.X, vec2i2.Y);
								list.Add(item13);
							}
						}
						vec2i2 = new Vec2i(x - 2, y + 2);
						if (vec2i2.X >= 0 && vec2i2.Y < BoardGameBaghChal.BoardHeight)
						{
							PawnBaghChal pawnBaghChal7 = this.GetTile(x - 1, y + 1).PawnOnTile as PawnBaghChal;
							if (pawnBaghChal7 != null && !pawnBaghChal7.IsTiger && this.GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
							{
								Move item14;
								item14.Unit = pawn;
								item14.GoalTile = this.GetTile(vec2i2.X, vec2i2.Y);
								list.Add(item14);
							}
						}
						vec2i2 = new Vec2i(x - 2, y - 2);
						if (vec2i2.X >= 0 && vec2i2.Y >= 0)
						{
							PawnBaghChal pawnBaghChal8 = this.GetTile(x - 1, y - 1).PawnOnTile as PawnBaghChal;
							if (pawnBaghChal8 != null && !pawnBaghChal8.IsTiger && this.GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
							{
								Move item15;
								item15.Unit = pawn;
								item15.GoalTile = this.GetTile(vec2i2.X, vec2i2.Y);
								list.Add(item15);
							}
						}
						vec2i2 = new Vec2i(x + 2, y - 2);
						if (vec2i2.X < BoardGameBaghChal.BoardWidth && vec2i2.Y >= 0)
						{
							PawnBaghChal pawnBaghChal9 = this.GetTile(x + 1, y - 1).PawnOnTile as PawnBaghChal;
							if (pawnBaghChal9 != null && !pawnBaghChal9.IsTiger && this.GetTile(vec2i2.X, vec2i2.Y).PawnOnTile == null)
							{
								Move item16;
								item16.Unit = pawn;
								item16.GoalTile = this.GetTile(vec2i2.X, vec2i2.Y);
								list.Add(item16);
							}
						}
					}
				}
				if (!isTiger && base.InPreMovementStage && x == -1 && y == -1)
				{
					for (int i = 0; i < this.TileCount; i++)
					{
						if (base.Tiles[i].PawnOnTile == null)
						{
							Move item17;
							item17.Unit = pawn;
							item17.GoalTile = base.Tiles[i];
							list.Add(item17);
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000B20 RID: 2848 RVA: 0x000533D8 File Offset: 0x000515D8
		public override void SetPawnCaptured(PawnBase pawn, bool fake = false)
		{
			base.SetPawnCaptured(pawn, fake);
			PawnBaghChal pawnBaghChal = pawn as PawnBaghChal;
			this.GetTile(pawnBaghChal.X, pawnBaghChal.Y).PawnOnTile = null;
			pawnBaghChal.PrevX = pawnBaghChal.X;
			pawnBaghChal.PrevY = pawnBaghChal.Y;
			pawnBaghChal.X = -1;
			pawnBaghChal.Y = -1;
			if (!fake)
			{
				base.RemovePawnFromBoard(pawnBaghChal, 0.6f, false);
			}
		}

		// Token: 0x06000B21 RID: 2849 RVA: 0x00053444 File Offset: 0x00051644
		protected override void HandlePreMovementStage(float dt)
		{
			Move move = base.HandlePlayerInput(dt);
			if (move.IsValid)
			{
				this.MovePawnToTile(move.Unit, move.GoalTile, false, true);
			}
		}

		// Token: 0x06000B22 RID: 2850 RVA: 0x00053476 File Offset: 0x00051676
		protected override void HandlePreMovementStageAI(Move move)
		{
			this.MovePawnToTile(move.Unit, move.GoalTile, false, true);
		}

		// Token: 0x06000B23 RID: 2851 RVA: 0x0005348C File Offset: 0x0005168C
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
							this.SelectedUnit = pawn;
						}
					}
					else
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

		// Token: 0x06000B24 RID: 2852 RVA: 0x000534EC File Offset: 0x000516EC
		protected override void SwitchPlayerTurn()
		{
			if ((base.PlayerTurn == PlayerTurn.PlayerOneWaiting || base.PlayerTurn == PlayerTurn.PlayerTwoWaiting) && this.SelectedUnit != null)
			{
				this.CheckIfPawnCaptures(this.SelectedUnit as PawnBaghChal, false);
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
			if (base.InPreMovementStage)
			{
				base.InPreMovementStage = !this.CheckPlacementStageOver();
			}
			this.CheckGameEnded();
			base.SwitchPlayerTurn();
		}

		// Token: 0x06000B25 RID: 2853 RVA: 0x00053574 File Offset: 0x00051774
		protected override bool CheckGameEnded()
		{
			bool result = false;
			if (base.PlayerTurn == PlayerTurn.PlayerTwo || base.PlayerTurn == PlayerTurn.PlayerOne)
			{
				List<List<Move>> list = this.CalculateAllValidMoves((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? BoardGameSide.AI : BoardGameSide.Player);
				if (!base.HasMovesAvailable(ref list))
				{
					if (base.PlayerWhoStarted == PlayerTurn.PlayerOne)
					{
						base.OnVictory("str_boardgame_victory_message");
					}
					else
					{
						base.OnDefeat("str_boardgame_defeat_message");
					}
					this.ReadyToPlay = false;
					result = true;
				}
				else
				{
					int num = 0;
					using (List<PawnBase>.Enumerator enumerator = this._goatUnits.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (((PawnBaghChal)enumerator.Current).Captured)
							{
								num++;
							}
						}
					}
					if (num >= 5)
					{
						if (base.PlayerWhoStarted == PlayerTurn.PlayerOne)
						{
							base.OnDefeat("str_boardgame_defeat_message");
						}
						else
						{
							base.OnVictory("str_boardgame_victory_message");
						}
						this.ReadyToPlay = false;
						result = true;
					}
				}
			}
			return result;
		}

		// Token: 0x06000B26 RID: 2854 RVA: 0x00053660 File Offset: 0x00051860
		protected override void OnAfterBoardRotated()
		{
			this.PreplaceUnits();
		}

		// Token: 0x06000B27 RID: 2855 RVA: 0x00053668 File Offset: 0x00051868
		protected override void OnAfterBoardSetUp()
		{
			this.ReadyToPlay = true;
		}

		// Token: 0x06000B28 RID: 2856 RVA: 0x00053674 File Offset: 0x00051874
		protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
		{
			base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
			Tile2D tile2D = tile as Tile2D;
			PawnBaghChal pawnBaghChal = pawn as PawnBaghChal;
			if (tile2D.PawnOnTile == null && pawnBaghChal != null)
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
				pawnBaghChal.MovingToDifferentTile = pawnBaghChal.X != tile2D.X || pawnBaghChal.Y != tile2D.Y;
				pawnBaghChal.PrevX = pawnBaghChal.X;
				pawnBaghChal.PrevY = pawnBaghChal.Y;
				pawnBaghChal.X = tile2D.X;
				pawnBaghChal.Y = tile2D.Y;
				if (pawnBaghChal.PrevX != -1 && pawnBaghChal.PrevY != -1)
				{
					this.GetTile(pawnBaghChal.PrevX, pawnBaghChal.PrevY).PawnOnTile = null;
				}
				tile2D.PawnOnTile = pawnBaghChal;
				if (pawnBaghChal.Entity.GlobalPosition.z < globalPosition.z)
				{
					Vec3 globalPosition2 = pawnBaghChal.Entity.GlobalPosition;
					globalPosition2.z = globalPosition.z;
					pawnBaghChal.AddGoalPosition(globalPosition2);
				}
				pawnBaghChal.AddGoalPosition(globalPosition);
				pawnBaghChal.MovePawnToGoalPositionsDelayed(instantMove, speed, this.JustStoppedDraggingUnit, delay);
				if (instantMove && !base.InPreMovementStage)
				{
					this.CheckIfPawnCaptures(this.SelectedUnit as PawnBaghChal, false);
					return;
				}
				if (pawnBaghChal == this.SelectedUnit && instantMove)
				{
					this.SelectedUnit = null;
				}
			}
		}

		// Token: 0x06000B29 RID: 2857 RVA: 0x0005381C File Offset: 0x00051A1C
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
					this.GetTile(pawnBaghChal.PrevX, pawnBaghChal.PrevY).PawnOnTile = null;
				}
				tile2D.PawnOnTile = pawnBaghChal;
				this.CheckIfPawnCaptures(pawnBaghChal, true);
			}
		}

		// Token: 0x06000B2A RID: 2858 RVA: 0x000538B4 File Offset: 0x00051AB4
		public BoardGameBaghChal.BoardInformation TakeBoardSnapshot()
		{
			BoardGameBaghChal.PawnInformation[] array = new BoardGameBaghChal.PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
			TileBaseInformation[,] array2 = new TileBaseInformation[BoardGameBaghChal.BoardWidth, BoardGameBaghChal.BoardHeight];
			int num = 0;
			foreach (PawnBase pawnBase in this._goatUnits)
			{
				PawnBaghChal pawnBaghChal = (PawnBaghChal)pawnBase;
				array[num++] = new BoardGameBaghChal.PawnInformation(pawnBaghChal.X, pawnBaghChal.Y, pawnBaghChal.PrevX, pawnBaghChal.PrevY, pawnBaghChal.Captured, pawnBaghChal.Entity.GlobalPosition);
			}
			foreach (PawnBase pawnBase2 in this._tigerUnits)
			{
				PawnBaghChal pawnBaghChal2 = (PawnBaghChal)pawnBase2;
				array[num++] = new BoardGameBaghChal.PawnInformation(pawnBaghChal2.X, pawnBaghChal2.Y, pawnBaghChal2.PrevX, pawnBaghChal2.PrevY, pawnBaghChal2.Captured, pawnBaghChal2.Entity.GlobalPosition);
			}
			for (int i = 0; i < BoardGameBaghChal.BoardWidth; i++)
			{
				for (int j = 0; j < BoardGameBaghChal.BoardHeight; j++)
				{
					array2[i, j] = new TileBaseInformation(ref this.GetTile(i, j).PawnOnTile);
				}
			}
			return new BoardGameBaghChal.BoardInformation(ref array, ref array2);
		}

		// Token: 0x06000B2B RID: 2859 RVA: 0x00053A44 File Offset: 0x00051C44
		public void UndoMove(ref BoardGameBaghChal.BoardInformation board)
		{
			int num = 0;
			foreach (PawnBase pawnBase in this._goatUnits)
			{
				PawnBaghChal pawnBaghChal = (PawnBaghChal)pawnBase;
				pawnBaghChal.X = board.PawnInformation[num].X;
				pawnBaghChal.Y = board.PawnInformation[num].Y;
				pawnBaghChal.PrevX = board.PawnInformation[num].PrevX;
				pawnBaghChal.PrevY = board.PawnInformation[num].PrevY;
				pawnBaghChal.Captured = board.PawnInformation[num].Captured;
				num++;
			}
			foreach (PawnBase pawnBase2 in this._tigerUnits)
			{
				PawnBaghChal pawnBaghChal2 = (PawnBaghChal)pawnBase2;
				pawnBaghChal2.X = board.PawnInformation[num].X;
				pawnBaghChal2.Y = board.PawnInformation[num].Y;
				pawnBaghChal2.PrevX = board.PawnInformation[num].PrevX;
				pawnBaghChal2.PrevY = board.PawnInformation[num].PrevY;
				pawnBaghChal2.Captured = board.PawnInformation[num].Captured;
				num++;
			}
			for (int i = 0; i < BoardGameBaghChal.BoardWidth; i++)
			{
				for (int j = 0; j < BoardGameBaghChal.BoardHeight; j++)
				{
					this.GetTile(i, j).PawnOnTile = board.TileInformation[i, j].PawnOnTile;
				}
			}
		}

		// Token: 0x06000B2C RID: 2860 RVA: 0x00053C0C File Offset: 0x00051E0C
		public PawnBaghChal GetANonePlacedGoat()
		{
			foreach (PawnBase pawnBase in this._goatUnits)
			{
				PawnBaghChal pawnBaghChal = (PawnBaghChal)pawnBase;
				if (!pawnBaghChal.Captured && !pawnBaghChal.IsPlaced)
				{
					return pawnBaghChal;
				}
			}
			return null;
		}

		// Token: 0x06000B2D RID: 2861 RVA: 0x00053C74 File Offset: 0x00051E74
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
			this.SetPawnCaptured(this.GetTile(vec2i3.X, vec2i3.Y).PawnOnTile, fake);
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x00053D68 File Offset: 0x00051F68
		private void PreplaceUnits()
		{
			this.MovePawnToTileDelayed(this._tigerUnits[0], this.GetTile(0, 0), false, false, 0.4f);
			this.MovePawnToTileDelayed(this._tigerUnits[1], this.GetTile(4, 0), false, false, 0.55f);
			this.MovePawnToTileDelayed(this._tigerUnits[2], this.GetTile(0, 4), false, false, 0.70000005f);
			this.MovePawnToTileDelayed(this._tigerUnits[3], this.GetTile(4, 4), false, false, 0.85f);
			for (int i = 0; i < 20; i++)
			{
				PawnBaghChal pawnBaghChal = this._goatUnits[i] as PawnBaghChal;
				MatrixFrame globalFrame = pawnBaghChal.Entity.GetGlobalFrame();
				MatrixFrame initialFrame = pawnBaghChal.InitialFrame;
				if (base.PlayerWhoStarted != PlayerTurn.PlayerOne)
				{
					initialFrame.rotation.RotateAboutUp(3.1415927f);
				}
				pawnBaghChal.Entity.SetFrame(ref initialFrame, true);
				Vec3 origin = pawnBaghChal.Entity.GetGlobalFrame().origin;
				pawnBaghChal.Entity.SetGlobalFrame(globalFrame, true);
				if (!pawnBaghChal.Entity.GlobalPosition.NearlyEquals(origin, 1E-05f))
				{
					Vec3 globalPosition = pawnBaghChal.Entity.GlobalPosition;
					globalPosition.z = this.BoardEntity.GlobalBoxMax.z;
					pawnBaghChal.AddGoalPosition(globalPosition);
					globalPosition.x = origin.x;
					globalPosition.y = origin.y;
					pawnBaghChal.AddGoalPosition(globalPosition);
					pawnBaghChal.AddGoalPosition(origin);
					pawnBaghChal.MovePawnToGoalPositions(false, 0.5f, false);
				}
			}
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x00053EFC File Offset: 0x000520FC
		private bool CheckPlacementStageOver()
		{
			bool result = false;
			int num = 0;
			foreach (PawnBase pawnBase in this._goatUnits)
			{
				PawnBaghChal pawnBaghChal = (PawnBaghChal)pawnBase;
				if (pawnBaghChal.Captured || pawnBaghChal.IsPlaced)
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

		// Token: 0x06000B30 RID: 2864 RVA: 0x00053F70 File Offset: 0x00052170
		private void SetTile(TileBase tile, int x, int y)
		{
			base.Tiles[y * BoardGameBaghChal.BoardWidth + x] = tile;
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x00053F83 File Offset: 0x00052183
		private TileBase GetTile(int x, int y)
		{
			return base.Tiles[y * BoardGameBaghChal.BoardWidth + x];
		}

		// Token: 0x040004C6 RID: 1222
		public const int UnitCountTiger = 4;

		// Token: 0x040004C7 RID: 1223
		public const int UnitCountGoat = 20;

		// Token: 0x040004C8 RID: 1224
		public static readonly int BoardWidth = 5;

		// Token: 0x040004C9 RID: 1225
		public static readonly int BoardHeight = 5;

		// Token: 0x040004CA RID: 1226
		private List<PawnBase> _goatUnits;

		// Token: 0x040004CB RID: 1227
		private List<PawnBase> _tigerUnits;

		// Token: 0x02000205 RID: 517
		public struct BoardInformation
		{
			// Token: 0x060013AD RID: 5037 RVA: 0x00077C90 File Offset: 0x00075E90
			public BoardInformation(ref BoardGameBaghChal.PawnInformation[] pawns, ref TileBaseInformation[,] tiles)
			{
				this.PawnInformation = pawns;
				this.TileInformation = tiles;
			}

			// Token: 0x0400094B RID: 2379
			public readonly BoardGameBaghChal.PawnInformation[] PawnInformation;

			// Token: 0x0400094C RID: 2380
			public readonly TileBaseInformation[,] TileInformation;
		}

		// Token: 0x02000206 RID: 518
		public struct PawnInformation
		{
			// Token: 0x060013AE RID: 5038 RVA: 0x00077CA2 File Offset: 0x00075EA2
			public PawnInformation(int x, int y, int prevX, int prevY, bool captured, Vec3 position)
			{
				this.X = x;
				this.Y = y;
				this.PrevX = prevX;
				this.PrevY = prevY;
				this.Captured = captured;
				this.Position = position;
			}

			// Token: 0x0400094D RID: 2381
			public readonly int X;

			// Token: 0x0400094E RID: 2382
			public readonly int Y;

			// Token: 0x0400094F RID: 2383
			public readonly int PrevX;

			// Token: 0x04000950 RID: 2384
			public readonly int PrevY;

			// Token: 0x04000951 RID: 2385
			public readonly bool Captured;

			// Token: 0x04000952 RID: 2386
			public readonly Vec3 Position;
		}
	}
}
