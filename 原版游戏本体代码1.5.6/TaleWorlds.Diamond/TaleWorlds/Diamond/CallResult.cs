namespace TaleWorlds.Diamond;

public sealed class CallResult
{
	public bool Success { get; }

	public FunctionResult Result { get; }

	public string SuccessfulReason { get; }

	public CallResult(bool success, FunctionResult result, string successfulReason = null)
	{
		Success = success;
		Result = result;
		SuccessfulReason = successfulReason;
	}
}
