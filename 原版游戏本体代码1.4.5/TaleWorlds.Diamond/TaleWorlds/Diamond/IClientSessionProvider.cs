namespace TaleWorlds.Diamond;

public interface IClientSessionProvider<T> where T : Client<T>
{
	IClientSession CreateSession(T session);
}
