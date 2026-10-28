namespace TaleWorlds.Library;

public struct SaveResultWithMessage
{
	public readonly SaveResult SaveResult;

	public readonly string Message;

	public static SaveResultWithMessage Default => new SaveResultWithMessage(SaveResult.Success, string.Empty);

	public SaveResultWithMessage(SaveResult result, string message)
	{
		SaveResult = result;
		Message = message;
	}
}
