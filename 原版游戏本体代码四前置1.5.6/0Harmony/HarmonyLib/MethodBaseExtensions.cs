using System.Reflection;

namespace HarmonyLib;

public static class MethodBaseExtensions
{
	public static bool HasMethodBody(this MethodBase member)
	{
		return (member.GetMethodBody()?.GetILAsByteArray()?.Length).GetValueOrDefault() > 0;
	}
}
