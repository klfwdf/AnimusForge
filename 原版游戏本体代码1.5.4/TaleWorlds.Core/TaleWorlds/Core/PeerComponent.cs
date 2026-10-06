namespace TaleWorlds.Core;

public abstract class PeerComponent : IEntityComponent
{
	private VirtualPlayer _peer;

	public VirtualPlayer Peer
	{
		get
		{
			return _peer;
		}
		set
		{
			_peer = value;
		}
	}

	public string Name => Peer.UserName;

	public bool IsMine => Peer.IsMine;

	public uint TypeId { get; set; }

	public virtual void Initialize()
	{
	}

	public T GetComponent<T>() where T : PeerComponent
	{
		return Peer.GetComponent<T>();
	}

	public virtual void OnInitialize()
	{
	}

	public virtual void OnFinalize()
	{
	}
}
