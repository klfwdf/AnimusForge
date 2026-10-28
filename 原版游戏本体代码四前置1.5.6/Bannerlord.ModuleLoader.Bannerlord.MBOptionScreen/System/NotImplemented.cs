namespace System;

internal static class NotImplemented
{
	internal static Exception ByDesign => new NotImplementedException();

	internal static Exception ByDesignWithMessage(string message)
	{
		return new NotImplementedException(message);
	}

	internal static Exception ActiveIssue(string issue)
	{
		return new NotImplementedException();
	}
}
