using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.ExceptionHandler.Extensions;

internal static class PlatformFileHelperPCExtended
{
	private delegate string GetDirectoryFullPathDelegate(object instance, PlatformDirectoryPath directoryPath);

	private delegate string GetFileFullPathDelegate(object instance, PlatformFilePath filePath);

	private delegate object GetPlatformFileHelperDelegate();

	private static readonly GetDirectoryFullPathDelegate? GetDirectoryFullPathMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetDirectoryFullPathDelegate>("TaleWorlds.Library.PlatformFileHelperPC:GetDirectoryFullPath");

	private static readonly GetFileFullPathDelegate? GetFileFullPathMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetFileFullPathDelegate>("TaleWorlds.Library.PlatformFileHelperPC:GetFileFullPath");

	private static readonly GetPlatformFileHelperDelegate? GetPlatformFileHelper = HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertyGetterDelegate<GetPlatformFileHelperDelegate>("TaleWorlds.Library.Common:PlatformFileHelper");

	public static string? GetFileFullPath(PlatformFilePath filePath)
	{
		if (GetPlatformFileHelper != null && GetFileFullPathMethod != null)
		{
			object obj = GetPlatformFileHelper();
			if (obj != null)
			{
				return GetFileFullPathMethod(obj, filePath);
			}
		}
		return null;
	}

	public static string? GetDirectoryFullPath(PlatformDirectoryPath directoryPath)
	{
		if (GetPlatformFileHelper != null && GetDirectoryFullPathMethod != null)
		{
			object obj = GetPlatformFileHelper();
			if (obj != null)
			{
				return GetDirectoryFullPathMethod(obj, directoryPath);
			}
		}
		return null;
	}
}
