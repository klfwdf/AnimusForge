using HarmonyLib;

namespace Bannerlord.Harmony;

internal class HarmonyRef : HarmonyLib.Harmony
{
	public HarmonyRef(string id)
		: base(id)
	{
	}
}
