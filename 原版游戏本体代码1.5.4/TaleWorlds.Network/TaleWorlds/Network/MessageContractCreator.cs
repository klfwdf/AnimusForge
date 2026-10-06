namespace TaleWorlds.Network;

internal abstract class MessageContractCreator
{
	public abstract MessageContract Invoke();
}
internal class MessageContractCreator<T> : MessageContractCreator where T : MessageContract, new()
{
	public override MessageContract Invoke()
	{
		return new T();
	}
}
