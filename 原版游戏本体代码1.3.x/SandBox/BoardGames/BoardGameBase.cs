using System;
using System.Collections.Generic;
using SandBox.BoardGames.AI;
using SandBox.BoardGames.MissionLogics;
using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.BoardGames
{
	// Token: 0x020000E7 RID: 231
	public abstract class BoardGameBase
	{
		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x06000B37 RID: 2871
		public abstract int TileCount { get; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x06000B38 RID: 2872
		protected abstract bool RotateBoard { get; }

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x06000B39 RID: 2873
		protected abstract bool PreMovementStagePresent { get; }

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x06000B3A RID: 2874
		protected abstract bool DiceRollRequired { get; }

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x06000B3B RID: 2875 RVA: 0x00053FFF File Offset: 0x000521FF
		protected virtual int UnitsToPlacePerTurnInPreMovementStage
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x06000B3C RID: 2876 RVA: 0x00054002 File Offset: 0x00052202
		// (set) Token: 0x06000B3D RID: 2877 RVA: 0x0005400A File Offset: 0x0005220A
		protected virtual PawnBase SelectedUnit
		{
			get
			{
				return this._selectedUnit;
			}
			set
			{
				this.OnBeforeSelectedUnitChanged(this._selectedUnit, value);
				this._selectedUnit = value;
				this.OnAfterSelectedUnitChanged();
			}
		}

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x06000B3E RID: 2878 RVA: 0x00054026 File Offset: 0x00052226
		public TextObject Name { get; }

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x06000B3F RID: 2879 RVA: 0x0005402E File Offset: 0x0005222E
		// (set) Token: 0x06000B40 RID: 2880 RVA: 0x00054036 File Offset: 0x00052236
		public bool InPreMovementStage { get; protected set; }

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x06000B41 RID: 2881 RVA: 0x0005403F File Offset: 0x0005223F
		// (set) Token: 0x06000B42 RID: 2882 RVA: 0x00054047 File Offset: 0x00052247
		public TileBase[] Tiles { get; protected set; }

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x06000B43 RID: 2883 RVA: 0x00054050 File Offset: 0x00052250
		// (set) Token: 0x06000B44 RID: 2884 RVA: 0x00054058 File Offset: 0x00052258
		public List<PawnBase> PlayerOneUnits { get; protected set; }

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x06000B45 RID: 2885 RVA: 0x00054061 File Offset: 0x00052261
		// (set) Token: 0x06000B46 RID: 2886 RVA: 0x00054069 File Offset: 0x00052269
		public List<PawnBase> PlayerTwoUnits { get; protected set; }

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000B47 RID: 2887 RVA: 0x00054072 File Offset: 0x00052272
		// (set) Token: 0x06000B48 RID: 2888 RVA: 0x0005407A File Offset: 0x0005227A
		public int LastDice { get; protected set; }

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x06000B49 RID: 2889 RVA: 0x00054083 File Offset: 0x00052283
		public bool IsReady
		{
			get
			{
				return this.ReadyToPlay && !this.SettingUpBoard;
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x06000B4A RID: 2890 RVA: 0x00054098 File Offset: 0x00052298
		// (set) Token: 0x06000B4B RID: 2891 RVA: 0x000540A0 File Offset: 0x000522A0
		public PlayerTurn PlayerWhoStarted { get; private set; }

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x06000B4C RID: 2892 RVA: 0x000540A9 File Offset: 0x000522A9
		// (set) Token: 0x06000B4D RID: 2893 RVA: 0x000540B1 File Offset: 0x000522B1
		public GameOverEnum GameOverInfo { get; private set; }

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x06000B4E RID: 2894 RVA: 0x000540BA File Offset: 0x000522BA
		// (set) Token: 0x06000B4F RID: 2895 RVA: 0x000540C2 File Offset: 0x000522C2
		public PlayerTurn PlayerTurn { get; protected set; }

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x06000B50 RID: 2896 RVA: 0x000540CB File Offset: 0x000522CB
		protected IInputContext InputManager
		{
			get
			{
				return this.MissionHandler.Mission.InputManager;
			}
		}

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x06000B51 RID: 2897 RVA: 0x000540DD File Offset: 0x000522DD
		protected List<PawnBase> PawnSelectFilter { get; }

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x06000B52 RID: 2898 RVA: 0x000540E5 File Offset: 0x000522E5
		protected BoardGameAIBase AIOpponent
		{
			get
			{
				return this.MissionHandler.AIOpponent;
			}
		}

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x06000B53 RID: 2899 RVA: 0x000540F2 File Offset: 0x000522F2
		private bool DiceRolled
		{
			get
			{
				return this.LastDice != -1;
			}
		}

		// Token: 0x06000B54 RID: 2900 RVA: 0x00054100 File Offset: 0x00052300
		protected BoardGameBase(MissionBoardGameLogic mission, TextObject name, PlayerTurn startingPlayer)
		{
			this.Name = name;
			this.MissionHandler = mission;
			this.SetStartingPlayer(startingPlayer);
			this.PlayerOnePool = new CapturedPawnsPool();
			this.PlayerTwoPool = new CapturedPawnsPool();
			this.PlayerOneUnits = new List<PawnBase>();
			this.PlayerTwoUnits = new List<PawnBase>();
			this.PawnSelectFilter = new List<PawnBase>();
		}

		// Token: 0x06000B55 RID: 2901
		public abstract void InitializeUnits();

		// Token: 0x06000B56 RID: 2902
		public abstract void InitializeTiles();

		// Token: 0x06000B57 RID: 2903
		public abstract void InitializeSound();

		// Token: 0x06000B58 RID: 2904
		public abstract List<Move> CalculateValidMoves(PawnBase pawn);

		// Token: 0x06000B59 RID: 2905
		protected abstract PawnBase SelectPawn(PawnBase pawn);

		// Token: 0x06000B5A RID: 2906
		protected abstract bool CheckGameEnded();

		// Token: 0x06000B5B RID: 2907
		protected abstract void OnAfterBoardSetUp();

		// Token: 0x06000B5C RID: 2908 RVA: 0x0005417F File Offset: 0x0005237F
		protected virtual void OnAfterBoardRotated()
		{
		}

		// Token: 0x06000B5D RID: 2909 RVA: 0x00054181 File Offset: 0x00052381
		protected virtual void OnBeforeEndTurn()
		{
		}

		// Token: 0x06000B5E RID: 2910 RVA: 0x00054183 File Offset: 0x00052383
		public virtual void RollDice()
		{
		}

		// Token: 0x06000B5F RID: 2911 RVA: 0x00054185 File Offset: 0x00052385
		protected virtual void UpdateAllTilesPositions()
		{
		}

		// Token: 0x06000B60 RID: 2912 RVA: 0x00054187 File Offset: 0x00052387
		public virtual void InitializeDiceBoard()
		{
		}

		// Token: 0x06000B61 RID: 2913 RVA: 0x0005418C File Offset: 0x0005238C
		public virtual void Reset()
		{
			this.PlayerOnePool.PawnCount = 0;
			this.PlayerTwoPool.PawnCount = 0;
			this.ClearValidMoves();
			this.SelectedUnit = null;
			this.PawnSelectFilter.Clear();
			this.GameOverInfo = GameOverEnum.GameStillInProgress;
			this._draggingSelectedUnit = false;
			this.JustStoppedDraggingUnit = false;
			this._draggingTimer = 0f;
			BoardGameAIBase aiopponent = this.MissionHandler.AIOpponent;
			if (aiopponent != null)
			{
				aiopponent.ResetThinking();
			}
			this.ReadyToPlay = false;
			this._firstTickAfterReady = true;
			this._rotationCompleted = !this.RotateBoard;
			this.SettingUpBoard = true;
			this.UnfocusAllPawns();
			for (int i = 0; i < this.TileCount; i++)
			{
				this.Tiles[i].Reset();
			}
			this.MovesLeftToEndTurn = (this.PreMovementStagePresent ? this.UnitsToPlacePerTurnInPreMovementStage : 1);
			this.LastDice = -1;
			this._waitingAIForfeitResponse = false;
		}

		// Token: 0x06000B62 RID: 2914 RVA: 0x0005426C File Offset: 0x0005246C
		protected virtual void OnPawnArrivesGoalPosition(PawnBase pawn, Vec3 prevPos, Vec3 currentPos)
		{
			if (this.IsReady && pawn.IsPlaced && !pawn.Captured && pawn.MovingToDifferentTile)
			{
				this.MovesLeftToEndTurn--;
			}
			pawn.MovingToDifferentTile = false;
		}

		// Token: 0x06000B63 RID: 2915 RVA: 0x000542A3 File Offset: 0x000524A3
		protected virtual void HandlePreMovementStage(float dt)
		{
			Debug.FailedAssert("HandlePreMovementStage is not implemented for " + this.MissionHandler.CurrentBoardGame, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBase.cs", "HandlePreMovementStage", 293);
		}

		// Token: 0x06000B64 RID: 2916 RVA: 0x000542D4 File Offset: 0x000524D4
		public virtual void InitializeCapturedUnitsZones()
		{
			this.PlayerOnePool.Entity = Mission.Current.Scene.FindEntityWithTag((this.PlayerWhoStarted == PlayerTurn.PlayerOne) ? "captured_pawns_pool_1" : "captured_pawns_pool_2");
			this.PlayerOnePool.PawnCount = 0;
			this.PlayerTwoPool.Entity = Mission.Current.Scene.FindEntityWithTag((this.PlayerWhoStarted == PlayerTurn.PlayerOne) ? "captured_pawns_pool_2" : "captured_pawns_pool_1");
			this.PlayerTwoPool.PawnCount = 0;
		}

		// Token: 0x06000B65 RID: 2917 RVA: 0x00054355 File Offset: 0x00052555
		protected virtual void HandlePreMovementStageAI(Move move)
		{
			Debug.FailedAssert("HandlePreMovementStageAI is not implemented for " + this.MissionHandler.CurrentBoardGame, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBase.cs", "HandlePreMovementStageAI", 311);
		}

		// Token: 0x06000B66 RID: 2918 RVA: 0x00054385 File Offset: 0x00052585
		public virtual void SetPawnCaptured(PawnBase pawn, bool fake = false)
		{
			pawn.Captured = true;
		}

		// Token: 0x06000B67 RID: 2919 RVA: 0x00054390 File Offset: 0x00052590
		public virtual List<List<Move>> CalculateAllValidMoves(BoardGameSide side)
		{
			List<List<Move>> list = new List<List<Move>>(100);
			foreach (PawnBase pawn in ((side == BoardGameSide.AI) ? this.PlayerTwoUnits : this.PlayerOneUnits))
			{
				list.Add(this.CalculateValidMoves(pawn));
			}
			return list;
		}

		// Token: 0x06000B68 RID: 2920 RVA: 0x00054400 File Offset: 0x00052600
		protected virtual void SwitchPlayerTurn()
		{
			this.MissionHandler.Handler.SwitchTurns();
		}

		// Token: 0x06000B69 RID: 2921 RVA: 0x00054412 File Offset: 0x00052612
		protected virtual void MovePawnToTile(PawnBase pawn, TileBase tile, bool instantMove = false, bool displayMessage = true)
		{
			this.MovePawnToTileDelayed(pawn, tile, instantMove, displayMessage, 0f);
		}

		// Token: 0x06000B6A RID: 2922 RVA: 0x00054424 File Offset: 0x00052624
		protected virtual void MovePawnToTileDelayed(PawnBase pawn, TileBase tile, bool instantMove, bool displayMessage, float delay)
		{
			this.ClearValidMoves();
		}

		// Token: 0x06000B6B RID: 2923 RVA: 0x0005442C File Offset: 0x0005262C
		protected virtual void OnAfterDiceRollAnimation()
		{
			if (this.LastDice != -1)
			{
				this.MissionHandler.Handler.DiceRoll(this.LastDice);
			}
		}

		// Token: 0x06000B6C RID: 2924 RVA: 0x0005444D File Offset: 0x0005264D
		public void SetUserRay(Vec3 rayBegin, Vec3 rayEnd)
		{
			this._userRayBegin = rayBegin;
			this._userRayEnd = rayEnd;
		}

		// Token: 0x06000B6D RID: 2925 RVA: 0x00054460 File Offset: 0x00052660
		public void SetStartingPlayer(PlayerTurn player)
		{
			this.HasToMovePawnsAcross = this.PlayerWhoStarted != player;
			if (player == PlayerTurn.PlayerOne)
			{
				this._rotationTarget = 0f;
			}
			else if (player == PlayerTurn.PlayerTwo)
			{
				this._rotationTarget = 3.1415927f;
			}
			else
			{
				Debug.FailedAssert("Unexpected starting player caught: " + player, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\BoardGameBase.cs", "SetStartingPlayer", 382);
			}
			this.PlayerWhoStarted = player;
			this.PlayerTurn = player;
		}

		// Token: 0x06000B6E RID: 2926 RVA: 0x000544D4 File Offset: 0x000526D4
		public void SetGameOverInfo(GameOverEnum info)
		{
			this.GameOverInfo = info;
		}

		// Token: 0x06000B6F RID: 2927 RVA: 0x000544E0 File Offset: 0x000526E0
		public bool HasMovesAvailable(ref List<List<Move>> moves)
		{
			foreach (List<Move> list in moves)
			{
				if (list != null && list.Count > 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000B70 RID: 2928 RVA: 0x0005453C File Offset: 0x0005273C
		public int GetTotalMovesAvailable(ref List<List<Move>> moves)
		{
			int num = 0;
			foreach (List<Move> list in moves)
			{
				if (list != null)
				{
					num += list.Count;
				}
			}
			return num;
		}

		// Token: 0x06000B71 RID: 2929 RVA: 0x00054594 File Offset: 0x00052794
		public void PlayDiceRollSound()
		{
			Vec3 globalPosition = this.DiceBoard.GlobalPosition;
			this.MissionHandler.Mission.MakeSound(this.DiceRollSoundCodeID, globalPosition, true, false, -1, -1);
		}

		// Token: 0x06000B72 RID: 2930 RVA: 0x000545C8 File Offset: 0x000527C8
		public int GetPlayerOneUnitsAlive()
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = this.PlayerOneUnits.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator.Current.Captured)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000B73 RID: 2931 RVA: 0x00054624 File Offset: 0x00052824
		public int GetPlayerTwoUnitsAlive()
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = this.PlayerTwoUnits.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator.Current.Captured)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000B74 RID: 2932 RVA: 0x00054680 File Offset: 0x00052880
		public int GetPlayerOneUnitsDead()
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = this.PlayerOneUnits.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Captured)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000B75 RID: 2933 RVA: 0x000546DC File Offset: 0x000528DC
		public int GetPlayerTwoUnitsDead()
		{
			int num = 0;
			using (List<PawnBase>.Enumerator enumerator = this.PlayerTwoUnits.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Captured)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000B76 RID: 2934 RVA: 0x00054738 File Offset: 0x00052938
		public void Initialize()
		{
			this.BoardEntity = Mission.Current.Scene.FindEntityWithTag("boardgame");
			this.InitializeUnits();
			this.InitializeTiles();
			this.InitializeCapturedUnitsZones();
			this.InitializeDiceBoard();
			this.InitializeSound();
			this.Reset();
		}

		// Token: 0x06000B77 RID: 2935 RVA: 0x00054778 File Offset: 0x00052978
		protected void RemovePawnFromBoard(PawnBase pawn, float speed, bool instantMove = false)
		{
			CapturedPawnsPool capturedPawnsPool = (pawn.PlayerOne ? this.PlayerOnePool : this.PlayerTwoPool);
			IEnumerable<GameEntity> children = capturedPawnsPool.Entity.GetChildren();
			GameEntity gameEntity = null;
			foreach (GameEntity gameEntity2 in children)
			{
				if (gameEntity2.HasTag("pawn_" + capturedPawnsPool.PawnCount))
				{
					gameEntity = gameEntity2;
					break;
				}
			}
			capturedPawnsPool.PawnCount++;
			Vec3 origin = gameEntity.GetGlobalFrame().origin;
			float num = pawn.Entity.GlobalPosition.z - origin.z;
			float num2 = 0.001f;
			if (num > num2)
			{
				Vec3 goal = origin;
				goal.z = pawn.Entity.GlobalPosition.z;
				pawn.AddGoalPosition(goal);
			}
			else if (num < -num2)
			{
				Vec3 globalPosition = pawn.Entity.GlobalPosition;
				globalPosition.z = origin.z;
				pawn.AddGoalPosition(globalPosition);
			}
			pawn.AddGoalPosition(origin);
			pawn.MovePawnToGoalPositions(instantMove, speed, false);
		}

		// Token: 0x06000B78 RID: 2936 RVA: 0x000548A0 File Offset: 0x00052AA0
		public bool Tick(float dt)
		{
			foreach (PawnBase pawnBase in this.PlayerOneUnits)
			{
				pawnBase.Tick(dt);
			}
			foreach (PawnBase pawnBase2 in this.PlayerTwoUnits)
			{
				pawnBase2.Tick(dt);
			}
			for (int i = 0; i < this.TileCount; i++)
			{
				this.Tiles[i].Tick(dt);
			}
			if (this.MovingPawnPresent() || !this.DoneSettingUpBoard() || !this.ReadyToPlay)
			{
				return false;
			}
			if (this._firstTickAfterReady)
			{
				this._firstTickAfterReady = false;
				this.MissionHandler.Handler.Activate();
			}
			if (this.IsReady)
			{
				if (this._draggingSelectedUnit)
				{
					Vec3 userRayBegin = this._userRayBegin;
					Vec3 userRayEnd = this._userRayEnd;
					Vec3 globalPosition = this.SelectedUnit.Entity.GlobalPosition;
					float length = (userRayEnd - userRayBegin).Length;
					float num = (globalPosition - userRayBegin).Length / length;
					Vec3 vecTo = new Vec3(userRayBegin.x + (userRayEnd.x - userRayBegin.x) * num, userRayBegin.y + (userRayEnd.y - userRayBegin.y) * num, this.SelectedUnit.PosBeforeMoving.z + 0.05f, -1f);
					Vec3 pawnAtPosition = MBMath.Lerp(globalPosition, vecTo, 1f, 0.005f);
					this.SelectedUnit.SetPawnAtPosition(pawnAtPosition);
				}
				if (this.DiceRollAnimationRunning)
				{
					if (this.DiceRollAnimationTimer < 1f)
					{
						this.DiceRollAnimationTimer += dt;
					}
					else
					{
						this.DiceRollAnimationRunning = false;
						this.OnAfterDiceRollAnimation();
					}
				}
				if (this.MovesLeftToEndTurn == 0)
				{
					this.EndTurn();
				}
				else
				{
					this.UpdateTurn(dt);
				}
				this.CheckSwitchPlayerTurn();
				return true;
			}
			return false;
		}

		// Token: 0x06000B79 RID: 2937 RVA: 0x00054AB4 File Offset: 0x00052CB4
		public void ForceDice(int value)
		{
			this.LastDice = value;
		}

		// Token: 0x06000B7A RID: 2938 RVA: 0x00054ABD File Offset: 0x00052CBD
		protected PawnBase InitializeUnit(PawnBase pawnToInit)
		{
			pawnToInit.OnArrivedIntermediateGoalPosition = new Action<PawnBase, Vec3, Vec3>(this.OnPawnArrivesGoalPosition);
			pawnToInit.OnArrivedFinalGoalPosition = new Action<PawnBase, Vec3, Vec3>(this.OnPawnArrivesGoalPosition);
			return pawnToInit;
		}

		// Token: 0x06000B7B RID: 2939 RVA: 0x00054AE8 File Offset: 0x00052CE8
		protected Move HandlePlayerInput(float dt)
		{
			Move result = new Move(null, null);
			if (this.InputManager.IsHotKeyPressed("BoardGamePawnSelect") && !this._draggingSelectedUnit)
			{
				this.JustStoppedDraggingUnit = false;
				PawnBase hoveredPawnIfAny = this.GetHoveredPawnIfAny();
				TileBase hoveredTileIfAny = this.GetHoveredTileIfAny();
				if (hoveredPawnIfAny != null)
				{
					if (this.PawnSelectFilter.Count == 0 || this.PawnSelectFilter.Contains(hoveredPawnIfAny))
					{
						PawnBase selectedUnit = this.SelectedUnit;
						PawnBase pawnBase = this.SelectPawn(hoveredPawnIfAny);
						if (pawnBase.PlayerOne == (this.PlayerTurn == PlayerTurn.PlayerOne) || !pawnBase.PlayerOne == (this.PlayerTurn == PlayerTurn.PlayerTwo))
						{
							if (this.SelectedUnit != null && this.SelectedUnit == selectedUnit)
							{
								this._deselectUnit = true;
							}
						}
						else if (hoveredTileIfAny == null)
						{
							this.SelectedUnit = null;
						}
					}
				}
				else if (hoveredTileIfAny == null)
				{
					this.SelectedUnit = null;
				}
			}
			else if (this.SelectedUnit != null && this.InputManager.IsHotKeyReleased("BoardGamePawnDeselect"))
			{
				if (this._draggingSelectedUnit)
				{
					this._draggingSelectedUnit = false;
					this.JustStoppedDraggingUnit = true;
				}
				else if (this._deselectUnit)
				{
					PawnBase hoveredPawnIfAny2 = this.GetHoveredPawnIfAny();
					if (hoveredPawnIfAny2 != null && hoveredPawnIfAny2 == this.SelectedUnit)
					{
						this.SelectedUnit = null;
						this._deselectUnit = false;
					}
				}
				if (this._validMoves != null)
				{
					this.SelectedUnit.DisableCollisionBody();
					TileBase hoveredTileIfAny2 = this.GetHoveredTileIfAny();
					if (hoveredTileIfAny2 != null && (hoveredTileIfAny2.PawnOnTile == null || hoveredTileIfAny2.PawnOnTile != this.SelectedUnit))
					{
						foreach (Move move in this._validMoves)
						{
							if (hoveredTileIfAny2.Entity == move.GoalTile.Entity)
							{
								result = move;
							}
						}
					}
					this.SelectedUnit.EnableCollisionBody();
				}
				if (!result.IsValid && this.SelectedUnit != null && this.JustStoppedDraggingUnit)
				{
					this.SelectedUnit.ClearGoalPositions();
					this.SelectedUnit.AddGoalPosition(this.SelectedUnit.PosBeforeMoving);
					this.SelectedUnit.MovePawnToGoalPositions(false, 0.8f, false);
				}
				this._draggingTimer = 0f;
			}
			if (this.SelectedUnit != null && this.InputManager.IsHotKeyDown("BoardGameDragPreview"))
			{
				this._draggingTimer += dt;
				if (this._draggingTimer >= 0.2f)
				{
					this._draggingSelectedUnit = true;
					this._deselectUnit = false;
				}
			}
			return result;
		}

		// Token: 0x06000B7C RID: 2940 RVA: 0x00054D78 File Offset: 0x00052F78
		protected PawnBase GetHoveredPawnIfAny()
		{
			PawnBase pawnBase = null;
			float num;
			WeakGameEntity weakGameEntity;
			Mission.Current.Scene.RayCastForClosestEntityOrTerrain(this._userRayBegin, this._userRayEnd, out num, out weakGameEntity, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
			if (weakGameEntity.IsValid)
			{
				foreach (PawnBase pawnBase2 in this.PlayerOneUnits)
				{
					if (pawnBase2.Entity.Name.Equals(weakGameEntity.Name))
					{
						pawnBase = pawnBase2;
						break;
					}
				}
				if (pawnBase == null)
				{
					foreach (PawnBase pawnBase3 in this.PlayerTwoUnits)
					{
						if (pawnBase3.Entity.Name.Equals(weakGameEntity.Name))
						{
							pawnBase = pawnBase3;
							break;
						}
					}
				}
			}
			return pawnBase;
		}

		// Token: 0x06000B7D RID: 2941 RVA: 0x00054E7C File Offset: 0x0005307C
		protected TileBase GetHoveredTileIfAny()
		{
			TileBase result = null;
			float num;
			WeakGameEntity weakGameEntity;
			Mission.Current.Scene.RayCastForClosestEntityOrTerrain(this._userRayBegin, this._userRayEnd, out num, out weakGameEntity, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
			if (weakGameEntity.IsValid)
			{
				for (int i = 0; i < this.TileCount; i++)
				{
					TileBase tileBase = this.Tiles[i];
					if (tileBase.Entity.Name.Equals(weakGameEntity.Name))
					{
						result = tileBase;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x06000B7E RID: 2942 RVA: 0x00054EFC File Offset: 0x000530FC
		protected void CheckSwitchPlayerTurn()
		{
			if (this.PlayerTurn == PlayerTurn.PlayerOneWaiting || this.PlayerTurn == PlayerTurn.PlayerTwoWaiting)
			{
				bool flag = false;
				using (List<PawnBase>.Enumerator enumerator = this.PlayerOneUnits.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Moving)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					using (List<PawnBase>.Enumerator enumerator = this.PlayerTwoUnits.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.Moving)
							{
								flag = true;
								break;
							}
						}
					}
				}
				if (!flag)
				{
					this.SwitchPlayerTurn();
				}
			}
		}

		// Token: 0x06000B7F RID: 2943 RVA: 0x00054FBC File Offset: 0x000531BC
		protected void OnVictory(string message = "str_boardgame_victory_message")
		{
			this.MissionHandler.PlayerOneWon(message);
		}

		// Token: 0x06000B80 RID: 2944 RVA: 0x00054FCA File Offset: 0x000531CA
		protected void OnAfterEndTurn()
		{
			this.ClearValidMoves();
			this.CheckGameEnded();
			this.MovesLeftToEndTurn = (this.InPreMovementStage ? this.UnitsToPlacePerTurnInPreMovementStage : 1);
		}

		// Token: 0x06000B81 RID: 2945 RVA: 0x00054FF0 File Offset: 0x000531F0
		protected void OnDefeat(string message = "str_boardgame_defeat_message")
		{
			this.MissionHandler.PlayerTwoWon(message);
		}

		// Token: 0x06000B82 RID: 2946 RVA: 0x00054FFE File Offset: 0x000531FE
		protected void OnDraw(string message = "str_boardgame_draw_message")
		{
			this.MissionHandler.GameWasDraw(message);
		}

		// Token: 0x06000B83 RID: 2947 RVA: 0x0005500C File Offset: 0x0005320C
		private void OnBeforeSelectedUnitChanged(PawnBase oldSelectedUnit, PawnBase newSelectedUnit)
		{
			if (oldSelectedUnit != null)
			{
				oldSelectedUnit.Entity.GetMetaMesh(0).SetFactor1Linear(this.PawnUnselectedFactor);
			}
			if (newSelectedUnit != null)
			{
				newSelectedUnit.Entity.GetMetaMesh(0).SetFactor1Linear(this.PawnSelectedFactor);
			}
			this.ClearValidMoves();
		}

		// Token: 0x06000B84 RID: 2948 RVA: 0x00055048 File Offset: 0x00053248
		protected void EndTurn()
		{
			this.OnBeforeEndTurn();
			this.SwitchToWaiting();
			this.OnAfterEndTurn();
		}

		// Token: 0x06000B85 RID: 2949 RVA: 0x0005505C File Offset: 0x0005325C
		protected void ClearValidMoves()
		{
			this.HideAllValidTiles();
			if (this._validMoves != null)
			{
				this._validMoves.Clear();
				this._validMoves = null;
			}
		}

		// Token: 0x06000B86 RID: 2950 RVA: 0x00055080 File Offset: 0x00053280
		private void OnAfterSelectedUnitChanged()
		{
			if (this.SelectedUnit != null)
			{
				List<Move> list = this.CalculateValidMoves(this.SelectedUnit);
				if (list != null && list.Count > 0)
				{
					this._validMoves = list;
				}
				if (this.SelectedUnit.PlayerOne || this.MissionHandler.AIOpponent == null)
				{
					this.SelectedUnit.PlayPawnSelectSound();
					this.ShowAllValidTiles();
				}
			}
		}

		// Token: 0x06000B87 RID: 2951 RVA: 0x000550E0 File Offset: 0x000532E0
		private void UpdateTurn(float dt)
		{
			if (this.PlayerTurn == PlayerTurn.PlayerOne || (this.PlayerTurn == PlayerTurn.PlayerTwo && this.AIOpponent == null))
			{
				if (this.InPreMovementStage)
				{
					this.HandlePreMovementStage(dt);
					return;
				}
				if (!this.DiceRollRequired || this.DiceRolled)
				{
					Move move = this.HandlePlayerInput(dt);
					if (move.IsValid)
					{
						this.MovePawnToTile(move.Unit, move.GoalTile, false, true);
						return;
					}
				}
			}
			else if (this.PlayerTurn == PlayerTurn.PlayerTwo && this.AIOpponent != null && !this._waitingAIForfeitResponse)
			{
				if (this.AIOpponent.WantsToForfeit())
				{
					this.OnAIWantsForfeit();
				}
				if (this.DiceRollRequired && !this.DiceRolled)
				{
					this.RollDice();
				}
				this.AIOpponent.UpdateThinkingAboutMove(dt);
				if (this.AIOpponent.CanMakeMove())
				{
					this.SelectedUnit = this.AIOpponent.RecentMoveCalculated.Unit;
					if (this.SelectedUnit != null)
					{
						if (this.InPreMovementStage)
						{
							this.HandlePreMovementStageAI(this.AIOpponent.RecentMoveCalculated);
						}
						else
						{
							TileBase goalTile = this.AIOpponent.RecentMoveCalculated.GoalTile;
							this.MovePawnToTile(this.SelectedUnit, goalTile, false, true);
						}
					}
					else
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_boardgame_no_available_moves_opponent", null), 0, null, null, "");
						this.EndTurn();
					}
					this.AIOpponent.ResetThinking();
				}
			}
		}

		// Token: 0x06000B88 RID: 2952 RVA: 0x00055240 File Offset: 0x00053440
		private bool DoneSettingUpBoard()
		{
			bool result = !this.SettingUpBoard;
			if (this.SettingUpBoard)
			{
				if (this._rotationApplied != this._rotationTarget && this.RotateBoard)
				{
					float value = this._rotationTarget - this._rotationApplied;
					float num = 0.05f;
					float num2 = MathF.Clamp(value, -num, num);
					MatrixFrame globalFrame = this.BoardEntity.GetGlobalFrame();
					globalFrame.rotation.RotateAboutUp(num2);
					this.BoardEntity.SetGlobalFrame(globalFrame, true);
					this._rotationApplied += num2;
					if (MathF.Abs(this._rotationTarget - this._rotationApplied) <= 1E-05f)
					{
						this._rotationApplied = this._rotationTarget;
						this.UpdateAllPawnsPositions();
						this.UpdateAllTilesPositions();
						return result;
					}
				}
				else
				{
					if (!this._rotationCompleted)
					{
						this._rotationCompleted = true;
						this.OnAfterBoardRotated();
						return result;
					}
					this.SettingUpBoard = false;
					this.OnAfterBoardSetUp();
				}
			}
			return result;
		}

		// Token: 0x06000B89 RID: 2953 RVA: 0x00055324 File Offset: 0x00053524
		protected void HideAllValidTiles()
		{
			if (this._validMoves != null && this._validMoves.Count > 0)
			{
				foreach (Move move in this._validMoves)
				{
					move.GoalTile.SetVisibility(false);
				}
			}
		}

		// Token: 0x06000B8A RID: 2954 RVA: 0x00055390 File Offset: 0x00053590
		protected void ShowAllValidTiles()
		{
			if (this._validMoves != null && this._validMoves.Count > 0)
			{
				foreach (Move move in this._validMoves)
				{
					move.GoalTile.SetVisibility(true);
				}
			}
		}

		// Token: 0x06000B8B RID: 2955 RVA: 0x000553FC File Offset: 0x000535FC
		private void UnfocusAllPawns()
		{
			if (this.PlayerOneUnits != null)
			{
				foreach (PawnBase pawnBase in this.PlayerOneUnits)
				{
					pawnBase.Entity.GetMetaMesh(0).SetFactor1Linear(this.PawnUnselectedFactor);
				}
			}
			if (this.PlayerTwoUnits != null)
			{
				foreach (PawnBase pawnBase2 in this.PlayerTwoUnits)
				{
					pawnBase2.Entity.GetMetaMesh(0).SetFactor1Linear(this.PawnUnselectedFactor);
				}
			}
		}

		// Token: 0x06000B8C RID: 2956 RVA: 0x000554C0 File Offset: 0x000536C0
		private bool MovingPawnPresent()
		{
			bool flag = false;
			foreach (PawnBase pawnBase in this.PlayerOneUnits)
			{
				if (pawnBase.Moving || pawnBase.HasAnyGoalPosition)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (PawnBase pawnBase2 in this.PlayerTwoUnits)
				{
					if (pawnBase2.Moving || pawnBase2.HasAnyGoalPosition)
					{
						flag = true;
						break;
					}
				}
			}
			return flag;
		}

		// Token: 0x06000B8D RID: 2957 RVA: 0x00055578 File Offset: 0x00053778
		private void SwitchToWaiting()
		{
			if (this.PlayerTurn == PlayerTurn.PlayerOne)
			{
				this.PlayerTurn = PlayerTurn.PlayerOneWaiting;
			}
			else if (this.PlayerTurn == PlayerTurn.PlayerTwo)
			{
				this.PlayerTurn = PlayerTurn.PlayerTwoWaiting;
			}
			this.JustStoppedDraggingUnit = false;
		}

		// Token: 0x06000B8E RID: 2958 RVA: 0x000555A4 File Offset: 0x000537A4
		protected void OnAIWantsForfeit()
		{
			if (!this._waitingAIForfeitResponse)
			{
				this._waitingAIForfeitResponse = true;
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_boardgame", null).ToString(), GameTexts.FindText("str_boardgame_forfeit_question", null).ToString(), true, true, GameTexts.FindText("str_accept", null).ToString(), GameTexts.FindText("str_reject", null).ToString(), new Action(this.OnAIForfeitAccepted), new Action(this.OnAIForfeitRejected), "", 0f, null, null, null), false, false);
			}
		}

		// Token: 0x06000B8F RID: 2959 RVA: 0x00055634 File Offset: 0x00053834
		private void UpdateAllPawnsPositions()
		{
			foreach (PawnBase pawnBase in this.PlayerOneUnits)
			{
				pawnBase.UpdatePawnPosition();
			}
			foreach (PawnBase pawnBase2 in this.PlayerTwoUnits)
			{
				pawnBase2.UpdatePawnPosition();
			}
		}

		// Token: 0x06000B90 RID: 2960 RVA: 0x000556C4 File Offset: 0x000538C4
		private void OnAIForfeitAccepted()
		{
			this.MissionHandler.AIForfeitGame();
			this._waitingAIForfeitResponse = false;
		}

		// Token: 0x06000B91 RID: 2961 RVA: 0x000556D8 File Offset: 0x000538D8
		private void OnAIForfeitRejected()
		{
			this._waitingAIForfeitResponse = false;
		}

		// Token: 0x040004E0 RID: 1248
		public const string StringBoardGame = "str_boardgame";

		// Token: 0x040004E1 RID: 1249
		public const string StringForfeitQuestion = "str_boardgame_forfeit_question";

		// Token: 0x040004E2 RID: 1250
		public const string StringMovePiecePlayer = "str_boardgame_move_piece_player";

		// Token: 0x040004E3 RID: 1251
		public const string StringMovePieceOpponent = "str_boardgame_move_piece_opponent";

		// Token: 0x040004E4 RID: 1252
		public const string StringCapturePiecePlayer = "str_boardgame_capture_piece_player";

		// Token: 0x040004E5 RID: 1253
		public const string StringCapturePieceOpponent = "str_boardgame_capture_piece_opponent";

		// Token: 0x040004E6 RID: 1254
		public const string StringVictoryMessage = "str_boardgame_victory_message";

		// Token: 0x040004E7 RID: 1255
		public const string StringDefeatMessage = "str_boardgame_defeat_message";

		// Token: 0x040004E8 RID: 1256
		public const string StringDrawMessage = "str_boardgame_draw_message";

		// Token: 0x040004E9 RID: 1257
		public const string StringNoAvailableMovesPlayer = "str_boardgame_no_available_moves_player";

		// Token: 0x040004EA RID: 1258
		public const string StringNoAvailableMovesOpponent = "str_boardgame_no_available_moves_opponent";

		// Token: 0x040004EB RID: 1259
		public const string StringSeegaBarrierByP1DrawMessage = "str_boardgame_seega_barrier_by_player_one_draw_message";

		// Token: 0x040004EC RID: 1260
		public const string StringSeegaBarrierByP2DrawMessage = "str_boardgame_seega_barrier_by_player_two_draw_message";

		// Token: 0x040004ED RID: 1261
		public const string StringSeegaBarrierByP1VictoryMessage = "str_boardgame_seega_barrier_by_player_one_victory_message";

		// Token: 0x040004EE RID: 1262
		public const string StringSeegaBarrierByP2VictoryMessage = "str_boardgame_seega_barrier_by_player_two_victory_message";

		// Token: 0x040004EF RID: 1263
		public const string StringSeegaBarrierByP1DefeatMessage = "str_boardgame_seega_barrier_by_player_one_defeat_message";

		// Token: 0x040004F0 RID: 1264
		public const string StringSeegaBarrierByP2DefeatMessage = "str_boardgame_seega_barrier_by_player_two_defeat_message";

		// Token: 0x040004F1 RID: 1265
		public const string StringRollDicePlayer = "str_boardgame_roll_dice_player";

		// Token: 0x040004F2 RID: 1266
		public const string StringRollDiceOpponent = "str_boardgame_roll_dice_opponent";

		// Token: 0x040004F3 RID: 1267
		protected const int InvalidDice = -1;

		// Token: 0x040004F4 RID: 1268
		protected const float DelayBeforeMovingAnyPawn = 0.25f;

		// Token: 0x040004F5 RID: 1269
		protected const float DelayBetweenPawnMovementsBegin = 0.15f;

		// Token: 0x040004F6 RID: 1270
		private const float DiceRollAnimationDuration = 1f;

		// Token: 0x040004F7 RID: 1271
		private const float DraggingDuration = 0.2f;

		// Token: 0x040004F8 RID: 1272
		private const int UnitsToPlacePerTurnInMovementStage = 1;

		// Token: 0x040004F9 RID: 1273
		protected uint PawnSelectedFactor = uint.MaxValue;

		// Token: 0x040004FA RID: 1274
		protected uint PawnUnselectedFactor = 4282203453U;

		// Token: 0x040004FB RID: 1275
		protected MissionBoardGameLogic MissionHandler;

		// Token: 0x040004FC RID: 1276
		protected GameEntity BoardEntity;

		// Token: 0x040004FD RID: 1277
		protected GameEntity DiceBoard;

		// Token: 0x040004FE RID: 1278
		protected bool JustStoppedDraggingUnit;

		// Token: 0x040004FF RID: 1279
		protected CapturedPawnsPool PlayerOnePool;

		// Token: 0x04000500 RID: 1280
		protected bool ReadyToPlay;

		// Token: 0x04000501 RID: 1281
		protected CapturedPawnsPool PlayerTwoPool;

		// Token: 0x04000502 RID: 1282
		protected bool SettingUpBoard = true;

		// Token: 0x04000503 RID: 1283
		protected bool HasToMovePawnsAcross;

		// Token: 0x04000504 RID: 1284
		protected float DiceRollAnimationTimer;

		// Token: 0x04000505 RID: 1285
		protected int MovesLeftToEndTurn;

		// Token: 0x04000506 RID: 1286
		protected bool DiceRollAnimationRunning;

		// Token: 0x04000507 RID: 1287
		protected int DiceRollSoundCodeID;

		// Token: 0x04000508 RID: 1288
		private List<Move> _validMoves;

		// Token: 0x04000509 RID: 1289
		private PawnBase _selectedUnit;

		// Token: 0x0400050A RID: 1290
		private Vec3 _userRayBegin;

		// Token: 0x0400050B RID: 1291
		private Vec3 _userRayEnd;

		// Token: 0x0400050C RID: 1292
		private float _draggingTimer;

		// Token: 0x0400050D RID: 1293
		private bool _draggingSelectedUnit;

		// Token: 0x0400050E RID: 1294
		private float _rotationApplied;

		// Token: 0x0400050F RID: 1295
		private float _rotationTarget;

		// Token: 0x04000510 RID: 1296
		private bool _rotationCompleted;

		// Token: 0x04000511 RID: 1297
		private bool _deselectUnit;

		// Token: 0x04000512 RID: 1298
		private bool _firstTickAfterReady = true;

		// Token: 0x04000513 RID: 1299
		private bool _waitingAIForfeitResponse;
	}
}
