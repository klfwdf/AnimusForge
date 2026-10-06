namespace TaleWorlds.Library;

public enum SaveResult
{
	Success,
	NoSpace,
	Corrupted,
	GeneralFailure,
	FileDriverFailure,
	PlatformFileHelperFailure,
	ConfigFileFailure,
	SaveLimitReached
}
