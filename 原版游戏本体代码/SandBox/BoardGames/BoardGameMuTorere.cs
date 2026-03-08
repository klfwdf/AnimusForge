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
	// Token: 0x020000E9 RID: 233
	public class BoardGameMuTorere : BoardGameBase
	{
		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000BB2 RID: 2994 RVA: 0x00057226 File Offset: 0x00055426
		public override int TileCount
		{
			get
			{
				return 9;
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x06000BB3 RID: 2995 RVA: 0x0005722A File Offset: 0x0005542A
		protected override bool RotateBoard
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000BB4 RID: 2996 RVA: 0x0005722D File Offset: 0x0005542D
		protected override bool PreMovementStagePresent
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000BB5 RID: 2997 RVA: 0x00057230 File Offset: 0x00055430
		protected override bool DiceRollRequired
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000BB6 RID: 2998 RVA: 0x00057233 File Offset: 0x00055433
		public BoardGameMuTorere(MissionBoardGameLogic mission, PlayerTurn startingPlayer)
			: base(mission, new TextObject("{=5siAbi69}Mu Torere", null), startingPlayer)
		{
			this.PawnUnselectedFactor = 4288711820U;
		}

		// Token: 0x06000BB7 RID: 2999 RVA: 0x00057254 File Offset: 0x00055454
		public override void InitializeUnits()
		{
			base.PlayerOneUnits.Clear();
			base.PlayerTwoUnits.Clear();
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			for (int i = 0; i < 4; i++)
			{
				GameEntity entity = Mission.Current.Scene.FindEntityWithTag("player_one_unit_" + i);
				list.Add(base.InitializeUnit(new PawnMuTorere(entity, base.PlayerWhoStarted == PlayerTurn.PlayerOne)));
			}
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int j = 0; j < 4; j++)
			{
				GameEntity entity2 = Mission.Current.Scene.FindEntityWithTag("player_two_unit_" + j);
				list2.Add(base.InitializeUnit(new PawnMuTorere(entity2, base.PlayerWhoStarted > PlayerTurn.PlayerOne)));
			}
		}

		// Token: 0x06000BB8 RID: 3000 RVA: 0x0005733C File Offset: 0x0005553C
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
			int num;
			for (x = 0; x < this.TileCount; x = num)
			{
				GameEntity gameEntity = source.Single((GameEntity e) => e.HasTag("tile_" + x));
				BoardGameDecal firstScriptOfType = source2.Single((GameEntity e) => e.HasTag("decal_" + x)).GetFirstScriptOfType<BoardGameDecal>();
				num = x;
				int xLeft;
				int xRight;
				if (num != 0)
				{
					if (num != 1)
					{
						if (num != 8)
						{
							xLeft = x - 1;
							xRight = x + 1;
						}
						else
						{
							xLeft = 7;
							xRight = 1;
						}
					}
					else
					{
						xLeft = 8;
						xRight = 2;
					}
				}
				else
				{
					xRight = (xLeft = -1);
				}
				base.Tiles[x] = new TileMuTorere(gameEntity, firstScriptOfType, x, xLeft, xRight);
				gameEntity.CreateVariableRatePhysics(true);
				num = x + 1;
			}
		}

		// Token: 0x06000BB9 RID: 3001 RVA: 0x0005748D File Offset: 0x0005568D
		public override void InitializeCapturedUnitsZones()
		{
		}

		// Token: 0x06000BBA RID: 3002 RVA: 0x0005748F File Offset: 0x0005568F
		public override void InitializeSound()
		{
			PawnBase.PawnMoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/move_stone");
			PawnBase.PawnSelectSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/pick_stone");
			PawnBase.PawnTapSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/drop_wood");
			PawnBase.PawnRemoveSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/movement/foley/minigame/out_stone");
		}

		// Token: 0x06000BBB RID: 3003 RVA: 0x000574CD File Offset: 0x000556CD
		public override void Reset()
		{
			base.Reset();
			this.PreplaceUnits();
		}

		// Token: 0x06000BBC RID: 3004 RVA: 0x000574DC File Offset: 0x000556DC
		public override List<Move> CalculateValidMoves(PawnBase pawn)
		{
			List<Move> list = new List<Move>();
			PawnMuTorere pawnMuTorere = pawn as PawnMuTorere;
			if (pawnMuTorere != null)
			{
				TileMuTorere tileMuTorere = this.FindAvailableTile() as TileMuTorere;
				if (pawnMuTorere.X == 0)
				{
					Move item;
					item.Unit = pawn;
					item.GoalTile = tileMuTorere;
					list.Add(item);
				}
				else if (tileMuTorere.X != 0)
				{
					if (pawnMuTorere.X == tileMuTorere.XLeftTile || pawnMuTorere.X == tileMuTorere.XRightTile)
					{
						Move item2;
						item2.Unit = pawn;
						item2.GoalTile = tileMuTorere;
						list.Add(item2);
					}
				}
				else
				{
					TileMuTorere tileMuTorere2 = this.FindTileByCoordinate(pawnMuTorere.X);
					PawnBase pawnOnTile = base.Tiles[tileMuTorere2.XLeftTile].PawnOnTile;
					PawnBase pawnOnTile2 = base.Tiles[tileMuTorere2.XRightTile].PawnOnTile;
					if (pawnOnTile.PlayerOne != pawnMuTorere.PlayerOne || pawnOnTile2.PlayerOne != pawnMuTorere.PlayerOne)
					{
						Move item3;
						item3.Unit = pawn;
						item3.GoalTile = tileMuTorere;
						list.Add(item3);
					}
				}
			}
			return list;
		}

		// Token: 0x06000BBD RID: 3005 RVA: 0x000575D8 File Offset: 0x000557D8
		protected override PawnBase SelectPawn(PawnBase pawn)
		{
			if (base.PlayerTurn == PlayerTurn.PlayerOne)
			{
				if (pawn.PlayerOne)
				{
					this.SelectedUnit = pawn;
				}
			}
			else if (base.AIOpponent == null && !pawn.PlayerOne)
			{
				this.SelectedUnit = pawn;
			}
			return pawn;
		}

		// Token: 0x06000BBE RID: 3006 RVA: 0x0005760C File Offset: 0x0005580C
		protected override void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
		{
			base.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, delay);
			TileMuTorere tileMuTorere = tile as TileMuTorere;
			PawnMuTorere pawnMuTorere = pawn as PawnMuTorere;
			if (tileMuTorere.PawnOnTile == null && pawnMuTorere != null)
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
				if (pawnMuTorere.X != -1)
				{
					base.Tiles[pawnMuTorere.X].PawnOnTile = null;
				}
				tileMuTorere.PawnOnTile = pawnMuTorere;
				pawnMuTorere.MovingToDifferentTile = pawnMuTorere.X != tileMuTorere.X;
				pawnMuTorere.X = tileMuTorere.X;
				Vec3 globalPosition = tileMuTorere.Entity.GlobalPosition;
				pawnMuTorere.AddGoalPosition(globalPosition);
				pawnMuTorere.MovePawnToGoalPositionsDelayed(instantMove, 0.6f, this.JustStoppedDraggingUnit, delay);
				if (pawnMuTorere == this.SelectedUnit)
				{
					this.SelectedUnit = null;
				}
			}
		}

		// Token: 0x06000BBF RID: 3007 RVA: 0x00057704 File Offset: 0x00055904
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
			this.CheckGameEnded();
			base.SwitchPlayerTurn();
		}

		// Token: 0x06000BC0 RID: 3008 RVA: 0x00057738 File Offset: 0x00055938
		protected override bool CheckGameEnded()
		{
			bool result = false;
			List<List<Move>> list = this.CalculateAllValidMoves((base.PlayerTurn == PlayerTurn.PlayerOne) ? BoardGameSide.Player : BoardGameSide.AI);
			if (base.GetTotalMovesAvailable(ref list) <= 0)
			{
				if (base.PlayerTurn == PlayerTurn.PlayerOne)
				{
					base.OnDefeat("str_boardgame_defeat_message");
					this.ReadyToPlay = false;
					result = true;
				}
				else if (base.PlayerTurn == PlayerTurn.PlayerTwo)
				{
					base.OnVictory("str_boardgame_victory_message");
					this.ReadyToPlay = false;
					result = true;
				}
			}
			return result;
		}

		// Token: 0x06000BC1 RID: 3009 RVA: 0x000577A1 File Offset: 0x000559A1
		protected override void OnAfterBoardSetUp()
		{
			this.ReadyToPlay = true;
		}

		// Token: 0x06000BC2 RID: 3010 RVA: 0x000577AC File Offset: 0x000559AC
		public TileMuTorere FindTileByCoordinate(int x)
		{
			TileMuTorere result = null;
			for (int i = 0; i < this.TileCount; i++)
			{
				TileMuTorere tileMuTorere = base.Tiles[i] as TileMuTorere;
				if (tileMuTorere.X == x)
				{
					result = tileMuTorere;
				}
			}
			return result;
		}

		// Token: 0x06000BC3 RID: 3011 RVA: 0x000577E8 File Offset: 0x000559E8
		public BoardGameMuTorere.BoardInformation TakePawnsSnapshot()
		{
			BoardGameMuTorere.PawnInformation[] array = new BoardGameMuTorere.PawnInformation[base.PlayerOneUnits.Count + base.PlayerTwoUnits.Count];
			TileBaseInformation[] array2 = new TileBaseInformation[this.TileCount];
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				PawnMuTorere pawnMuTorere = (PawnMuTorere)pawnBase;
				BoardGameMuTorere.PawnInformation pawnInformation = new BoardGameMuTorere.PawnInformation(pawnMuTorere.X);
				array[num++] = pawnInformation;
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				PawnMuTorere pawnMuTorere2 = (PawnMuTorere)pawnBase2;
				BoardGameMuTorere.PawnInformation pawnInformation2 = new BoardGameMuTorere.PawnInformation(pawnMuTorere2.X);
				array[num++] = pawnInformation2;
			}
			for (int i = 0; i < this.TileCount; i++)
			{
				array2[i] = new TileBaseInformation(ref base.Tiles[i].PawnOnTile);
			}
			return new BoardGameMuTorere.BoardInformation(ref array, ref array2);
		}

		// Token: 0x06000BC4 RID: 3012 RVA: 0x00057938 File Offset: 0x00055B38
		public void UndoMove(ref BoardGameMuTorere.BoardInformation board)
		{
			int num = 0;
			foreach (PawnBase pawnBase in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits))
			{
				((PawnMuTorere)pawnBase).X = board.PawnInformation[num++].X;
			}
			foreach (PawnBase pawnBase2 in ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits))
			{
				((PawnMuTorere)pawnBase2).X = board.PawnInformation[num++].X;
			}
			for (int i = 0; i < this.TileCount; i++)
			{
				base.Tiles[i].PawnOnTile = board.TileInformation[i].PawnOnTile;
			}
		}

		// Token: 0x06000BC5 RID: 3013 RVA: 0x00057A4C File Offset: 0x00055C4C
		public void AIMakeMove(Move move)
		{
			TileMuTorere tileMuTorere = move.GoalTile as TileMuTorere;
			PawnMuTorere pawnMuTorere = move.Unit as PawnMuTorere;
			base.Tiles[pawnMuTorere.X].PawnOnTile = null;
			tileMuTorere.PawnOnTile = pawnMuTorere;
			pawnMuTorere.X = tileMuTorere.X;
		}

		// Token: 0x06000BC6 RID: 3014 RVA: 0x00057A98 File Offset: 0x00055C98
		public TileBase FindAvailableTile()
		{
			foreach (TileBase tileBase in base.Tiles)
			{
				if (tileBase.PawnOnTile == null)
				{
					return tileBase;
				}
			}
			return null;
		}

		// Token: 0x06000BC7 RID: 3015 RVA: 0x00057ACC File Offset: 0x00055CCC
		private void PreplaceUnits()
		{
			List<PawnBase> list = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerOneUnits : base.PlayerTwoUnits);
			List<PawnBase> list2 = ((base.PlayerWhoStarted == PlayerTurn.PlayerOne) ? base.PlayerTwoUnits : base.PlayerOneUnits);
			for (int i = 0; i < 4; i++)
			{
				this.MovePawnToTileDelayed(list[i], base.Tiles[i + 1], false, false, 0.15f * (float)(i + 1) + 0.25f);
				this.MovePawnToTileDelayed(list2[i], base.Tiles[8 - i], false, false, 0.15f * (float)(i + 1) + 0.5f);
			}
		}

		// Token: 0x04000524 RID: 1316
		public const int WhitePawnCount = 4;

		// Token: 0x04000525 RID: 1317
		public const int BlackPawnCount = 4;

		// Token: 0x0200020F RID: 527
		public struct BoardInformation
		{
			// Token: 0x060013C5 RID: 5061 RVA: 0x00077F81 File Offset: 0x00076181
			public BoardInformation(ref BoardGameMuTorere.PawnInformation[] pawns, ref TileBaseInformation[] tiles)
			{
				this.PawnInformation = pawns;
				this.TileInformation = tiles;
			}

			// Token: 0x0400096B RID: 2411
			public readonly BoardGameMuTorere.PawnInformation[] PawnInformation;

			// Token: 0x0400096C RID: 2412
			public readonly TileBaseInformation[] TileInformation;
		}

		// Token: 0x02000210 RID: 528
		public struct PawnInformation
		{
			// Token: 0x060013C6 RID: 5062 RVA: 0x00077F93 File Offset: 0x00076193
			public PawnInformation(int x)
			{
				this.X = x;
			}

			// Token: 0x0400096D RID: 2413
			public readonly int X;
		}
	}
}
