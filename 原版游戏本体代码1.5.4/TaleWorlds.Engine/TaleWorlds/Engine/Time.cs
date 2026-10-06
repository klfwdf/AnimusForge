namespace TaleWorlds.Engine;

public static class Time
{
	public static float ApplicationTime => EngineApplicationInterface.ITime.GetApplicationTime();
}
