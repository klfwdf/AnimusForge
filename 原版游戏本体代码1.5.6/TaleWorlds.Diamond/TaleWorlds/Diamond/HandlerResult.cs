namespace TaleWorlds.Diamond;

public class HandlerResult
{
	public bool IsSuccessful { get; }

	public string Error { get; }

	public Message NextMessage { get; }

	protected HandlerResult(bool isSuccessful, string error = null, Message followUp = null)
	{
		IsSuccessful = isSuccessful;
		Error = error;
		NextMessage = followUp;
	}

	public static HandlerResult CreateSuccessful()
	{
		return new HandlerResult(isSuccessful: true);
	}

	public static HandlerResult CreateSuccessful(Message nextMessage)
	{
		return new HandlerResult(isSuccessful: true, null, nextMessage);
	}

	public static HandlerResult CreateFailed(string error)
	{
		return new HandlerResult(isSuccessful: false, error);
	}
}
