namespace TaleWorlds.Core;

public abstract class PlayerGameState : GameState
{
	private VirtualPlayer _peer;

	public VirtualPlayer Peer
	{
		get
		{
			return _peer;
		}
		private set
		{
			_peer = value;
		}
	}
}
