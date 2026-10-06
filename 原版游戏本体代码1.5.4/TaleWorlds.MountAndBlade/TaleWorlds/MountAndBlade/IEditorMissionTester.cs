namespace TaleWorlds.MountAndBlade;

public interface IEditorMissionTester
{
	void StartMissionForEditor(string missionName, string sceneName, string levels);

	void StartMissionForReplayEditor(string missionName, string sceneName, string levels, string fileName, bool record, float startTime, float endTime);
}
