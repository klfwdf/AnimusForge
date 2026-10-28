using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class GameOverState : TaleWorlds.Core.GameState
{
	public enum GameOverReason
	{
		Retirement,
		ClanDestroyed,
		Victory
	}

	private IGameOverStateHandler _handler;

	public override bool IsMenuState => true;

	public IGameOverStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public GameOverReason Reason { get; private set; }

	public GameOverState()
	{
	}

	public GameOverState(GameOverReason reason)
	{
		Reason = reason;
	}

	public static GameOverState CreateForVictory()
	{
		return Game.Current?.GameStateManager.CreateState<GameOverState>(new object[1] { GameOverReason.Victory });
	}

	public static GameOverState CreateForRetirement()
	{
		return Game.Current?.GameStateManager.CreateState<GameOverState>(new object[1] { GameOverReason.Retirement });
	}

	public static GameOverState CreateForClanDestroyed()
	{
		return Game.Current?.GameStateManager.CreateState<GameOverState>(new object[1] { GameOverReason.ClanDestroyed });
	}
}
