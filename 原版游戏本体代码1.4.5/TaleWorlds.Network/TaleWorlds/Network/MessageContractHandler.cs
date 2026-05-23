namespace TaleWorlds.Network;

internal abstract class MessageContractHandler
{
	public abstract void Invoke(MessageContract messageContract);
}
internal class MessageContractHandler<T> : MessageContractHandler where T : MessageContract
{
	private MessageContractHandlerDelegate<T> _method;

	public MessageContractHandler(MessageContractHandlerDelegate<T> method)
	{
		_method = method;
	}

	public override void Invoke(MessageContract messageContract)
	{
		_method(messageContract as T);
	}
}
