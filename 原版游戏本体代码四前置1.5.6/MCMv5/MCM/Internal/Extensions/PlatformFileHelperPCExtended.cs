using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace MCM.Internal.Extensions;

internal static class PlatformFileHelperPCExtended
{
	private delegate string GetDirectoryFullPathDelegate(object instance, PlatformDirectoryPath directoryPath);

	private delegate object GetPlatformFileHelperDelegate();

	private static GetDirectoryFullPathDelegate? GetDirectoryFullPathMethod = AccessTools2.GetDelegate<GetDirectoryFullPathDelegate>("TaleWorlds.Library.PlatformFileHelperPC:GetDirectoryFullPath");

	private static GetPlatformFileHelperDelegate? GetPlatformFileHelper = AccessTools2.GetPropertyGetterDelegate<GetPlatformFileHelperDelegate>("TaleWorlds.Library.Common:PlatformFileHelper");

	public static string? GetDirectoryFullPath(PlatformDirectoryPath directoryPath)
	{
		object result;
		if (GetPlatformFileHelper != null && GetDirectoryFullPathMethod != null)
		{
			object obj = GetPlatformFileHelper();
			if (obj != null)
			{
				result = GetDirectoryFullPathMethod(obj, directoryPath);
				goto IL_002b;
			}
		}
		result = null;
		goto IL_002b;
		IL_002b:
		return (string?)result;
	}
}
