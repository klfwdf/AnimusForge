namespace TaleWorlds.Engine;

public class SceneProblemChecker
{
	[EngineCallback(null, false)]
	internal static bool OnCheckForSceneProblems(Scene scene)
	{
		return false;
	}
}
