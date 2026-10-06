using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core;

public class DummyCommunicator : ICommunicator
{
	public VirtualPlayer VirtualPlayer { get; }

	public bool IsNetworkActive => false;

	public bool IsConnectionActive => false;

	public bool IsServerPeer => false;

	public bool IsSynchronized
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	public void OnSynchronizeComponentTo(VirtualPlayer peer, PeerComponent component)
	{
	}

	public void OnAddComponent(PeerComponent component)
	{
	}

	public void OnRemoveComponent(PeerComponent component)
	{
	}

	private DummyCommunicator(int index, string name)
	{
		VirtualPlayer = new VirtualPlayer(index, name, PlayerId.Empty, this);
	}

	public static DummyCommunicator CreateAsServer(int index, string name)
	{
		return new DummyCommunicator(index, name);
	}

	public static DummyCommunicator CreateAsClient(string name, int index)
	{
		return new DummyCommunicator(index, name);
	}
}
