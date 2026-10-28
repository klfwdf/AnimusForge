namespace HarmonyLib;

internal enum InjectionType
{
	Unknown,
	Instance,
	OriginalMethod,
	ArgsArray,
	Result,
	ResultRef,
	State,
	Exception,
	RunOriginal
}
