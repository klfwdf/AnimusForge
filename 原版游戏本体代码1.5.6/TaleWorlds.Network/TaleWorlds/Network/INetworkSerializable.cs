namespace TaleWorlds.Network;

public interface INetworkSerializable
{
	void SerializeToNetworkMessage(INetworkMessageWriter networkMessage);

	void DeserializeFromNetworkMessage(INetworkMessageReader networkMessage);
}
