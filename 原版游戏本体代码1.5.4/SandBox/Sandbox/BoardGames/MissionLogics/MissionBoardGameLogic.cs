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

namespace SandBox.BoardGames.MissionLogics;

public class MissionBoardGameLogic : MissionLogic
{
	private const string BoardGameEntityTag = "boardgame";

	private const string SpecialTargetGamblerNpcTag = "gambler_npc";

	public IBoardGameHandler Handler;

	private PlayerTurn _startingPlayer = PlayerTurn.PlayerTwo;

	private Chair _playerChair;

	private Chair _opposingChair;

	private string _specialTagCacheOfOpposingHero;

	private bool _isTavernGame;

	private bool _startingBoardGame;

	private BoardGameHelper.BoardGameState _boardGameState;

	public BoardGameBase Board { get; private set; }

	public BoardGameAIBase AIOpponent { get; private set; }

	public bool IsOpposingAgentMovingToPlayingChair => BoardGameAgentBehavior.IsAgentMovingToChair(OpposingAgent);

	public bool IsGameInProgress { get; private set; }

	public BoardGameHelper.BoardGameState BoardGameFinalState => _boardGameState;

	public CultureObject.BoardGameType CurrentBoardGame { get; private set; }

	public BoardGameHelper.AIDifficulty Difficulty { get; private set; }

	public int BetAmount { get; private set; }

	public Agent OpposingAgent { get; private set; }

	public event Action GameStarted;

	public event Action GameEnded;

	public override void AfterStart()
	{
		base.AfterStart();
		_opposingChair = base.Mission.Scene.FindEntityWithTag("gambler_npc").CollectScriptComponentsIncludingChildrenRecursive<Chair>().FirstOrDefault();
		_playerChair = base.Mission.Scene.FindEntityWithTag("gambler_player").CollectScriptComponentsIncludingChildrenRecursive<Chair>().FirstOrDefault();
		foreach (StandingPoint standingPoint in _opposingChair.StandingPoints)
		{
			standingPoint.IsDisabledForPlayers = true;
		}
	}

	public void SetStartingPlayer(bool playerOneStarts)
	{
		_startingPlayer = ((!playerOneStarts) ? PlayerTurn.PlayerTwo : PlayerTurn.PlayerOne);
	}

	public void StartBoardGame()
	{
		_startingBoardGame = true;
	}

	private void BoardGameInit(CultureObject.BoardGameType game)
	{
		if (Board == null)
		{
			switch (game)
			{
			case CultureObject.BoardGameType.Seega:
				Board = new BoardGameSeega(this, _startingPlayer);
				AIOpponent = new BoardGameAISeega(Difficulty, this);
				break;
			case CultureObject.BoardGameType.Puluc:
				Board = new BoardGamePuluc(this, _startingPlayer);
				AIOpponent = new BoardGameAIPuluc(Difficulty, this);
				break;
			case CultureObject.BoardGameType.MuTorere:
				Board = new BoardGameMuTorere(this, _startingPlayer);
				AIOpponent = new BoardGameAIMuTorere(Difficulty, this);
				break;
			case CultureObject.BoardGameType.Konane:
				Board = new BoardGameKonane(this, _startingPlayer);
				AIOpponent = new BoardGameAIKonane(Difficulty, this);
				break;
			case CultureObject.BoardGameType.BaghChal:
				Board = new BoardGameBaghChal(this, _startingPlayer);
				AIOpponent = new BoardGameAIBaghChal(Difficulty, this);
				break;
			case CultureObject.BoardGameType.Tablut:
				Board = new BoardGameTablut(this, _startingPlayer);
				AIOpponent = new BoardGameAITablut(Difficulty, this);
				break;
			default:
				Debug.FailedAssert("[DEBUG]No board with this name was found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\BoardGames\\MissionLogics\\MissionBoardGameLogic.cs", "BoardGameInit", 119);
				break;
			}
			Board.Initialize();
			if (AIOpponent != null)
			{
				AIOpponent.Initialize();
			}
		}
		else
		{
			Board.SetStartingPlayer(_startingPlayer);
			Board.InitializeUnits();
			Board.InitializeCapturedUnitsZones();
			Board.Reset();
			if (AIOpponent != null)
			{
				AIOpponent.SetDifficulty(Difficulty);
				AIOpponent.Initialize();
			}
		}
		if (Handler != null)
		{
			Handler.Install();
		}
		_boardGameState = BoardGameHelper.BoardGameState.None;
		IsGameInProgress = true;
		_isTavernGame = CampaignMission.Current.Location == Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern");
	}

	public override void OnMissionTick(float dt)
	{
		if (!base.Mission.IsInPhotoMode)
		{
			if (_startingBoardGame)
			{
				_startingBoardGame = false;
				BoardGameInit(CurrentBoardGame);
				this.GameStarted?.Invoke();
			}
			else if (IsGameInProgress)
			{
				Board.Tick(dt);
			}
			else if (OpposingAgent != null && OpposingAgent.IsHero && Hero.OneToOneConversationHero == null && CheckIfBothSidesAreSitting())
			{
				StartBoardGame();
			}
		}
	}

	public void DetectOpposingAgent()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent == ConversationMission.OneToOneConversationAgent)
			{
				OpposingAgent = agent;
				if (agent.IsHero)
				{
					BoardGameAgentBehavior.AddTargetChair(OpposingAgent, _opposingChair);
				}
				AgentNavigator agentNavigator = OpposingAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
				_specialTagCacheOfOpposingHero = agentNavigator.SpecialTargetTag;
				agentNavigator.SpecialTargetTag = "gambler_npc";
				break;
			}
		}
	}

	public bool CheckIfBothSidesAreSitting()
	{
		if (Agent.Main != null && OpposingAgent != null && _playerChair.IsAgentFullySitting(Agent.Main))
		{
			return _opposingChair.IsAgentFullySitting(OpposingAgent);
		}
		return false;
	}

	public void PlayerOneWon(string message = "str_boardgame_victory_message")
	{
		Agent opposingAgent = OpposingAgent;
		SetGameOver(GameOverEnum.PlayerOneWon);
		ShowInquiry(message, opposingAgent);
	}

	public void PlayerTwoWon(string message = "str_boardgame_defeat_message")
	{
		Agent opposingAgent = OpposingAgent;
		SetGameOver(GameOverEnum.PlayerTwoWon);
		ShowInquiry(message, opposingAgent);
	}

	public void GameWasDraw(string message = "str_boardgame_draw_message")
	{
		Agent opposingAgent = OpposingAgent;
		SetGameOver(GameOverEnum.Draw);
		ShowInquiry(message, opposingAgent);
	}

	private void ShowInquiry(string message, Agent conversationAgent)
	{
		InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_boardgame").ToString(), GameTexts.FindText(message).ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", delegate
		{
			StartConversationWithOpponentAfterGameEnd(conversationAgent);
		}, null));
	}

	private void StartConversationWithOpponentAfterGameEnd(Agent conversationAgent)
	{
		MissionConversationLogic.Current.StartConversation(conversationAgent, setActionsInstantly: false);
		_boardGameState = BoardGameHelper.BoardGameState.None;
	}

	public void SetGameOver(GameOverEnum gameOverInfo)
	{
		base.Mission.MainAgent.ClearTargetFrame();
		if (Handler != null && gameOverInfo != GameOverEnum.PlayerCanceledTheGame)
		{
			Handler.Uninstall();
		}
		Hero opposingHero = (OpposingAgent.IsHero ? ((CharacterObject)OpposingAgent.Character).HeroObject : null);
		switch (gameOverInfo)
		{
		case GameOverEnum.PlayerOneWon:
			_boardGameState = BoardGameHelper.BoardGameState.Win;
			break;
		case GameOverEnum.PlayerTwoWon:
			_boardGameState = BoardGameHelper.BoardGameState.Loss;
			break;
		case GameOverEnum.Draw:
			_boardGameState = BoardGameHelper.BoardGameState.Draw;
			break;
		case GameOverEnum.PlayerCanceledTheGame:
			_boardGameState = BoardGameHelper.BoardGameState.None;
			break;
		}
		if (gameOverInfo != GameOverEnum.PlayerCanceledTheGame)
		{
			CampaignEventDispatcher.Instance.OnPlayerBoardGameOver(opposingHero, _boardGameState);
		}
		this.GameEnded?.Invoke();
		BoardGameAgentBehavior.RemoveBoardGameBehaviorOfAgent(OpposingAgent);
		OpposingAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag = _specialTagCacheOfOpposingHero;
		OpposingAgent = null;
		IsGameInProgress = false;
		AIOpponent?.OnSetGameOver();
	}

	public void ForfeitGame()
	{
		Board.SetGameOverInfo(GameOverEnum.PlayerTwoWon);
		Agent opposingAgent = OpposingAgent;
		SetGameOver(Board.GameOverInfo);
		StartConversationWithOpponentAfterGameEnd(opposingAgent);
	}

	public void AIForfeitGame()
	{
		Board.SetGameOverInfo(GameOverEnum.PlayerOneWon);
		SetGameOver(Board.GameOverInfo);
	}

	public void RollDice()
	{
		Board.RollDice();
	}

	public bool RequiresDiceRolling()
	{
		return CurrentBoardGame switch
		{
			CultureObject.BoardGameType.Seega => false, 
			CultureObject.BoardGameType.Puluc => true, 
			CultureObject.BoardGameType.Konane => false, 
			CultureObject.BoardGameType.MuTorere => false, 
			CultureObject.BoardGameType.Tablut => false, 
			CultureObject.BoardGameType.BaghChal => false, 
			_ => false, 
		};
	}

	public void SetBetAmount(int bet)
	{
		BetAmount = bet;
	}

	public void SetCurrentDifficulty(BoardGameHelper.AIDifficulty difficulty)
	{
		Difficulty = difficulty;
	}

	public void SetBoardGame(CultureObject.BoardGameType game)
	{
		CurrentBoardGame = game;
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		if (IsGameInProgress)
		{
			SetGameOver(GameOverEnum.PlayerCanceledTheGame);
		}
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		canLeave = true;
		return null;
	}

	public static bool IsBoardGameAvailable()
	{
		MissionBoardGameLogic missionBoardGameLogic = Mission.Current?.GetMissionBehavior<MissionBoardGameLogic>();
		if (Mission.Current?.Scene != null && missionBoardGameLogic != null && Mission.Current.Scene.FindEntityWithTag("boardgame") != null)
		{
			return missionBoardGameLogic.OpposingAgent == null;
		}
		return false;
	}

	public static bool IsThereActiveBoardGameWithHero(Hero hero)
	{
		MissionBoardGameLogic missionBoardGameLogic = Mission.Current?.GetMissionBehavior<MissionBoardGameLogic>();
		if (Mission.Current?.Scene != null && Mission.Current.Scene.FindEntityWithTag("boardgame") != null && missionBoardGameLogic != null)
		{
			return missionBoardGameLogic.OpposingAgent?.Character == hero.CharacterObject;
		}
		return false;
	}

	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign && !Campaign.Current.ConversationManager.IsConversationInProgress && IsThereAgentAction(userAgent, agent))
		{
			Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(agent, setActionsInstantly: false);
		}
	}

	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (userAgent.IsMainAgent && _playerChair.IsAgentFullySitting(Agent.Main))
		{
			return _opposingChair.IsAgentFullySitting(otherAgent);
		}
		return false;
	}
}
