namespace TaleWorlds.Diamond;

public interface IClientSessionFactory
{
	IClientSession CreateSession(int aliveCheckInterval);
}
