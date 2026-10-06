namespace TaleWorlds.Library;

public static class UiStringHelper
{
	public static bool IsStringNoneOrEmptyForUi(string str)
	{
		if (!string.IsNullOrEmpty(str))
		{
			return str == "none";
		}
		return true;
	}
}
