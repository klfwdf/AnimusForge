using System;
using System.Linq;
using Helpers;
using SandBox.BoardGames.AI;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Objects.Usables;
using SandBox.Source.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace SandBox.BoardGames.MissionLogics
{
	// Token: 0x020000FD RID: 253
	public class MissionBoardGameLogic : MissionLogic
	{
		// Token: 0x14000012 RID: 18
		// (add) Token: 0x06000CA4 RID: 3236 RVA: 0x0005D4F0 File Offset: 0x0005B6F0
		// (remove) Token: 0x06000CA5 RID: 3237 RVA: 0x0005D528 File Offset: 0x0005B728
		public event Action GameStarted;

		// Token: 0x14000013 RID: 19
		// (add) Token: 0x06000CA6 RID: 3238 RVA: 0x0005D560 File Offset: 0x0005B760
		// (remove) Token: 0x06000CA7 RID: 3239 RVA: 0x0005D598 File Offset: 0x0005B798
		public event Action GameEnded;

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x06000CA8 RID: 3240 RVA: 0x0005D5CD File Offset: 0x0005B7CD
		// (set) Token: 0x06000CA9 RID: 3241 RVA: 0x0005D5D5 File Offset: 0x0005B7D5
		public BoardGameBase Board { get; private set; }

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000CAA RID: 3242 RVA: 0x0005D5DE File Offset: 0x0005B7DE
		// (set) Token: 0x06000CAB RID: 3243 RVA: 0x0005D5E6 File Offset: 0x0005B7E6
		public BoardGameAIBase AIOpponent { get; private set; }

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000CAC RID: 3244 RVA: 0x0005D5EF File Offset: 0x0005B7EF
		public bool IsOpposingAgentMovingToPlayingChair
		{
			get
			{
				return BoardGameAgentBehavior.IsAgentMovingToChair(this.OpposingAgent);
			}
		}

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x06000CAD RID: 3245 RVA: 0x0005D5FC File Offset: 0x0005B7FC
		// (set) Token: 0x06000CAE RID: 3246 RVA: 0x0005D604 File Offset: 0x0005B804
		public bool IsGameInProgress { get; private set; }

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x06000CAF RID: 3247 RVA: 0x0005D60D File Offset: 0x0005B80D
		public BoardGameHelper.BoardGameState BoardGameFinalState
		{
			get
			{
				return this._boardGameState;
			}
		}

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x06000CB0 RID: 3248 RVA: 0x0005D615 File Offset: 0x0005B815
		// (set) Token: 0x06000CB1 RID: 3249 RVA: 0x0005D61D File Offset: 0x0005B81D
		public CultureObject.BoardGameType CurrentBoardGame { get; private set; }

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x06000CB2 RID: 3250 RVA: 0x0005D626 File Offset: 0x0005B826
		// (set) Token: 0x06000CB3 RID: 3251 RVA: 0x0005D62E File Offset: 0x0005B82E
		public BoardGameHelper.AIDifficulty Difficulty { get; private set; }

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x06000CB4 RID: 3252 RVA: 0x0005D637 File Offset: 0x0005B837
		// (set) Token: 0x06000CB5 RID: 3253 RVA: 0x0005D63F File Offset: 0x0005B83F
		public int BetAmount { get; private set; }

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x06000CB6 RID: 3254 RVA: 0x0005D648 File Offset: 0x0005B848
		// (set) Token: 0x06000CB7 RID: 3255 RVA: 0x0005D650 File Offset: 0x0005B850
		public Agent OpposingAgent { get; private set; }

		// Token: 0x06000CB8 RID: 3256 RVA: 0x0005D65C File Offset: 0x0005B85C
		public override void AfterStart()
		{
			base.AfterStart();
			this._opposingChair = base.Mission.Scene.FindEntityWithTag("gambler_npc").CollectScriptComponentsIncludingChildrenRecursive<Chair>().FirstOrDefault<Chair>();
			this._playerChair = base.Mission.Scene.FindEntityWithTag("gambler_player").CollectScriptComponentsIncludingChildrenRecursive<Chair>().FirstOrDefault<Chair>();
			foreach (StandingPoint standingPoint in this._opposingChair.StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
		}

		// Token: 0x06000CB9 RID: 3257 RVA: 0x0005D704 File Offset: 0x0005B904
		public void SetStartingPlayer(bool playerOneStarts)
		{
			this._startingPlayer = (playerOneStarts ? PlayerTurn.PlayerOne : PlayerTurn.PlayerTwo);
		}

		// Token: 0x06000CBA RID: 3258 RVA: 0x0005D713 File Offset: 0x0005B913
		public void StartBoardGame()
		{
			this._startingBoardGame = true;
		}

		// Token: 0x06000CBB RID: 3259 RVA: 0x0005D71C File Offset: 0x0005B91C
		private void BoardGameInit(CultureObject.BoardGameType game)
		{
			if (this.Board == null)
			{
				switch (game)
				{
				case CultureObject.BoardGameType.Seega:
					this.Board = new BoardGameSeega(this, this._startingPlayer);
					this.AIOpponent = new BoardGameAISeega(this.Difficulty, this);
					break;
				case CultureObject.BoardGameType.Puluc:
					this.Board = new BoardGamePuluc(this, this._startingPlayer);
					this.AIOpponent = new BoardGameAIPuluc(this.Difficulty, this);
					break;
				case CultureObject.BoardGameType.Konane:
					this.Board = new BoardGameKonane(this, this._startingPlayer);
					this.AIOpponent = new BoardGameAIKonane(this.Difficulty, this);
					break;
				case CultureObject.BoardGameType.MuTorere:
					this.Board = new BoardGameMuTorere(this, this._startingPlayer);
					this.AIOpponent = new BoardGameAIMuTorere(this.Difficulty, this);
					break;
				case CultureObject.BoardGameType.Tablut:
					this.Board = new BoardGameTablut(this, this._startingPlayer);
					this.AIOpponent = new BoardGameAITablut(this.Difficulty, this);
					break;
				case CultureObject.BoardGameType.BaghChal:
					this.Board = new BoardGameBaghChal(this, this._startingPlayer);
					this.AIOpponent = new BoardGameAIBaghChal(this.Difficulty, this);
					break;
				default:
					Debug.FailedAssert("[DEBUG]No board with this name was found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\MissionLogics\\MissionBoardGameLogic.cs", "BoardGameInit", 119);
					break;
				}
				this.Board.Initialize();
				if (this.AIOpponent != null)
				{
					this.AIOpponent.Initialize();
				}
			}
			else
			{
				this.Board.SetStartingPlayer(this._startingPlayer);
				this.Board.InitializeUnits();
				this.Board.InitializeCapturedUnitsZones();
				this.Board.Reset();
				if (this.AIOpponent != null)
				{
					this.AIOpponent.SetDifficulty(this.Difficulty);
					this.AIOpponent.Initialize();
				}
			}
			if (this.Handler != null)
			{
				this.Handler.Install();
			}
			this._boardGameState = BoardGameHelper.BoardGameState.None;
			this.IsGameInProgress = true;
			this._isTavernGame = CampaignMission.Current.Location == Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern");
		}

		// Token: 0x06000CBC RID: 3260 RVA: 0x0005D918 File Offset: 0x0005BB18
		public override void OnMissionTick(float dt)
		{
			if (base.Mission.IsInPhotoMode)
			{
				return;
			}
			if (this._startingBoardGame)
			{
				this._startingBoardGame = false;
				this.BoardGameInit(this.CurrentBoardGame);
				Action gameStarted = this.GameStarted;
				if (gameStarted == null)
				{
					return;
				}
				gameStarted();
				return;
			}
			else
			{
				if (this.IsGameInProgress)
				{
					this.Board.Tick(dt);
					return;
				}
				if (this.OpposingAgent != null && this.OpposingAgent.IsHero && Hero.OneToOneConversationHero == null && this.CheckIfBothSidesAreSitting())
				{
					this.StartBoardGame();
				}
				return;
			}
		}

		// Token: 0x06000CBD RID: 3261 RVA: 0x0005D9A0 File Offset: 0x0005BBA0
		public void DetectOpposingAgent()
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == ConversationMission.OneToOneConversationAgent)
				{
					this.OpposingAgent = agent;
					if (agent.IsHero)
					{
						BoardGameAgentBehavior.AddTargetChair(this.OpposingAgent, this._opposingChair);
					}
					AgentNavigator agentNavigator = this.OpposingAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
					this._specialTagCacheOfOpposingHero = agentNavigator.SpecialTargetTag;
					agentNavigator.SpecialTargetTag = "gambler_npc";
					break;
				}
			}
		}

		// Token: 0x06000CBE RID: 3262 RVA: 0x0005DA44 File Offset: 0x0005BC44
		public bool CheckIfBothSidesAreSitting()
		{
			return Agent.Main != null && this.OpposingAgent != null && this._playerChair.IsAgentFullySitting(Agent.Main) && this._opposingChair.IsAgentFullySitting(this.OpposingAgent);
		}

		// Token: 0x06000CBF RID: 3263 RVA: 0x0005DA7C File Offset: 0x0005BC7C
		public void PlayerOneWon(string message = "str_boardgame_victory_message")
		{
			Agent opposingAgent = this.OpposingAgent;
			this.SetGameOver(GameOverEnum.PlayerOneWon);
			this.ShowInquiry(message, opposingAgent);
		}

		// Token: 0x06000CC0 RID: 3264 RVA: 0x0005DAA0 File Offset: 0x0005BCA0
		public void PlayerTwoWon(string message = "str_boardgame_defeat_message")
		{
			Agent opposingAgent = this.OpposingAgent;
			this.SetGameOver(GameOverEnum.PlayerTwoWon);
			this.ShowInquiry(message, opposingAgent);
		}

		// Token: 0x06000CC1 RID: 3265 RVA: 0x0005DAC4 File Offset: 0x0005BCC4
		public void GameWasDraw(string message = "str_boardgame_draw_message")
		{
			Agent opposingAgent = this.OpposingAgent;
			this.SetGameOver(GameOverEnum.Draw);
			this.ShowInquiry(message, opposingAgent);
		}

		// Token: 0x06000CC2 RID: 3266 RVA: 0x0005DAE8 File Offset: 0x0005BCE8
		private void ShowInquiry(string message, Agent conversationAgent)
		{
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_boardgame", null).ToString(), GameTexts.FindText(message, null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", delegate()
			{
				this.StartConversationWithOpponentAfterGameEnd(conversationAgent);
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x06000CC3 RID: 3267 RVA: 0x0005DB62 File Offset: 0x0005BD62
		private void StartConversationWithOpponentAfterGameEnd(Agent conversationAgent)
		{
			MissionConversationLogic.Current.StartConversation(conversationAgent, false, false);
			this._boardGameState = BoardGameHelper.BoardGameState.None;
		}

		// Token: 0x06000CC4 RID: 3268 RVA: 0x0005DB78 File Offset: 0x0005BD78
		public void SetGameOver(GameOverEnum gameOverInfo)
		{
			if (this.IsGameInProgress)
			{
				base.Mission.MainAgent.ClearTargetFrame();
				if (this.Handler != null && gameOverInfo != GameOverEnum.PlayerCanceledTheGame)
				{
					this.Handler.Uninstall();
				}
				Hero opposingHero = (this.OpposingAgent.IsHero ? ((CharacterObject)this.OpposingAgent.Character).HeroObject : null);
				switch (gameOverInfo)
				{
				case GameOverEnum.PlayerOneWon:
					this._boardGameState = BoardGameHelper.BoardGameState.Win;
					break;
				case GameOverEnum.PlayerTwoWon:
					this._boardGameState = BoardGameHelper.BoardGameState.Loss;
					break;
				case GameOverEnum.Draw:
					this._boardGameState = BoardGameHelper.BoardGameState.Draw;
					break;
				case GameOverEnum.PlayerCanceledTheGame:
					this._boardGameState = BoardGameHelper.BoardGameState.None;
					break;
				}
				if (gameOverInfo != GameOverEnum.PlayerCanceledTheGame)
				{
					CampaignEventDispatcher.Instance.OnPlayerBoardGameOver(opposingHero, this._boardGameState);
				}
				Action gameEnded = this.GameEnded;
				if (gameEnded != null)
				{
					gameEnded();
				}
				BoardGameAgentBehavior.RemoveBoardGameBehaviorOfAgent(this.OpposingAgent);
				this.OpposingAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag = this._specialTagCacheOfOpposingHero;
				this.OpposingAgent = null;
				this.IsGameInProgress = false;
				BoardGameAIBase aiopponent = this.AIOpponent;
				if (aiopponent == null)
				{
					return;
				}
				aiopponent.OnSetGameOver();
			}
		}

		// Token: 0x06000CC5 RID: 3269 RVA: 0x0005DC88 File Offset: 0x0005BE88
		public void ForfeitGame()
		{
			this.Board.SetGameOverInfo(GameOverEnum.PlayerTwoWon);
			Agent opposingAgent = this.OpposingAgent;
			this.SetGameOver(this.Board.GameOverInfo);
			this.StartConversationWithOpponentAfterGameEnd(opposingAgent);
		}

		// Token: 0x06000CC6 RID: 3270 RVA: 0x0005DCC0 File Offset: 0x0005BEC0
		public void AIForfeitGame()
		{
			this.Board.SetGameOverInfo(GameOverEnum.PlayerOneWon);
			this.SetGameOver(this.Board.GameOverInfo);
		}

		// Token: 0x06000CC7 RID: 3271 RVA: 0x0005DCDF File Offset: 0x0005BEDF
		public void RollDice()
		{
			this.Board.RollDice();
		}

		// Token: 0x06000CC8 RID: 3272 RVA: 0x0005DCEC File Offset: 0x0005BEEC
		public bool RequiresDiceRolling()
		{
			switch (this.CurrentBoardGame)
			{
			case CultureObject.BoardGameType.Seega:
				return false;
			case CultureObject.BoardGameType.Puluc:
				return true;
			case CultureObject.BoardGameType.Konane:
				return false;
			case CultureObject.BoardGameType.MuTorere:
				return false;
			case CultureObject.BoardGameType.Tablut:
				return false;
			case CultureObject.BoardGameType.BaghChal:
				return false;
			default:
				return false;
			}
		}

		// Token: 0x06000CC9 RID: 3273 RVA: 0x0005DD2D File Offset: 0x0005BF2D
		public void SetBetAmount(int bet)
		{
			this.BetAmount = bet;
		}

		// Token: 0x06000CCA RID: 3274 RVA: 0x0005DD36 File Offset: 0x0005BF36
		public void SetCurrentDifficulty(BoardGameHelper.AIDifficulty difficulty)
		{
			this.Difficulty = difficulty;
		}

		// Token: 0x06000CCB RID: 3275 RVA: 0x0005DD3F File Offset: 0x0005BF3F
		public void SetBoardGame(CultureObject.BoardGameType game)
		{
			this.CurrentBoardGame = game;
		}

		// Token: 0x06000CCC RID: 3276 RVA: 0x0005DD48 File Offset: 0x0005BF48
		protected override void OnEndMission()
		{
			base.OnEndMission();
			this.SetGameOver(GameOverEnum.PlayerCanceledTheGame);
		}

		// Token: 0x06000CCD RID: 3277 RVA: 0x0005DD57 File Offset: 0x0005BF57
		public override InquiryData OnEndMissionRequest(out bool canLeave)
		{
			canLeave = true;
			return null;
		}

		// Token: 0x06000CCE RID: 3278 RVA: 0x0005DD60 File Offset: 0x0005BF60
		public static bool IsBoardGameAvailable()
		{
			Mission mission = Mission.Current;
			MissionBoardGameLogic missionBoardGameLogic = ((mission != null) ? mission.GetMissionBehavior<MissionBoardGameLogic>() : null);
			Mission mission2 = Mission.Current;
			return ((mission2 != null) ? mission2.Scene : null) != null && missionBoardGameLogic != null && Mission.Current.Scene.FindEntityWithTag("boardgame") != null && missionBoardGameLogic.OpposingAgent == null;
		}

		// Token: 0x06000CCF RID: 3279 RVA: 0x0005DDC4 File Offset: 0x0005BFC4
		public static bool IsThereActiveBoardGameWithHero(Hero hero)
		{
			Mission mission = Mission.Current;
			MissionBoardGameLogic missionBoardGameLogic = ((mission != null) ? mission.GetMissionBehavior<MissionBoardGameLogic>() : null);
			Mission mission2 = Mission.Current;
			if (((mission2 != null) ? mission2.Scene : null) != null && Mission.Current.Scene.FindEntityWithTag("boardgame") != null && missionBoardGameLogic != null)
			{
				Agent opposingAgent = missionBoardGameLogic.OpposingAgent;
				return ((opposingAgent != null) ? opposingAgent.Character : null) == hero.CharacterObject;
			}
			return false;
		}

		// Token: 0x06000CD0 RID: 3280 RVA: 0x0005DE37 File Offset: 0x0005C037
		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign && !Campaign.Current.ConversationManager.IsConversationInProgress && this.IsThereAgentAction(userAgent, agent))
			{
				Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(agent, false, false);
			}
		}

		// Token: 0x06000CD1 RID: 3281 RVA: 0x0005DE73 File Offset: 0x0005C073
		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			return userAgent.IsMainAgent && this._playerChair.IsAgentFullySitting(Agent.Main) && this._opposingChair.IsAgentFullySitting(otherAgent);
		}

		// Token: 0x0400057B RID: 1403
		private const string BoardGameEntityTag = "boardgame";

		// Token: 0x0400057C RID: 1404
		private const string SpecialTargetGamblerNpcTag = "gambler_npc";

		// Token: 0x0400057F RID: 1407
		public IBoardGameHandler Handler;

		// Token: 0x04000580 RID: 1408
		private PlayerTurn _startingPlayer = PlayerTurn.PlayerTwo;

		// Token: 0x04000581 RID: 1409
		private Chair _playerChair;

		// Token: 0x04000582 RID: 1410
		private Chair _opposingChair;

		// Token: 0x04000583 RID: 1411
		private string _specialTagCacheOfOpposingHero;

		// Token: 0x04000584 RID: 1412
		private bool _isTavernGame;

		// Token: 0x04000585 RID: 1413
		private bool _startingBoardGame;

		// Token: 0x04000586 RID: 1414
		private BoardGameHelper.BoardGameState _boardGameState;
	}
}
