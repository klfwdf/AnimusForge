using TaleWorlds.MountAndBlade;

namespace SandBox;

internal class SandBoxEditorMissionTester : IEditorMissionTester
{
	void IEditorMissionTester.StartMissionForEditor(string missionName, string sceneName, string levels)
	{
		MBGameManager.StartNewGame(new EditorSceneMissionManager(missionName, sceneName, levels, forReplay: false, "", isRecord: false, 0f, 0f));
	}

	void IEditorMissionTester.StartMissionForReplayEditor(string missionName, string sceneName, string levels, string fileName, bool record, float startTime, float endTime)
	{
		MBGameManager.StartNewGame(new EditorSceneMissionManager(missionName, sceneName, levels, forReplay: true, fileName, record, startTime, endTime));
	}
}
