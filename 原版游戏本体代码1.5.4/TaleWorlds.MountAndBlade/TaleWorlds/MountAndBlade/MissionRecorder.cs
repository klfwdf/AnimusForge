using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MissionRecorder
{
	private readonly Mission _mission;

	public MissionRecorder(Mission mission)
	{
		_mission = mission;
	}

	public void RestartRecord()
	{
		MBAPI.IMBMission.RestartRecord(_mission.Pointer);
	}

	public void ProcessRecordUntilTime(float time)
	{
		MBAPI.IMBMission.ProcessRecordUntilTime(_mission.Pointer, time);
	}

	public bool IsEndOfRecord()
	{
		return MBAPI.IMBMission.EndOfRecord(_mission.Pointer);
	}

	public void StartRecording()
	{
		MBAPI.IMBMission.StartRecording();
	}

	public void RecordCurrentState()
	{
		MBAPI.IMBMission.RecordCurrentState(_mission.Pointer);
	}

	public void BackupRecordToFile(string fileName, string gameType, string sceneLevels)
	{
		MBAPI.IMBMission.BackupRecordToFile(_mission.Pointer, fileName, gameType, sceneLevels);
	}

	public void RestoreRecordFromFile(string fileName)
	{
		MBAPI.IMBMission.RestoreRecordFromFile(_mission.Pointer, fileName);
	}

	public void ClearRecordBuffers()
	{
		MBAPI.IMBMission.ClearRecordBuffers(_mission.Pointer);
	}

	public static string GetSceneNameForReplay(PlatformFilePath fileName)
	{
		return MBAPI.IMBMission.GetSceneNameForReplay(fileName);
	}

	public static string GetGameTypeForReplay(PlatformFilePath fileName)
	{
		return MBAPI.IMBMission.GetGameTypeForReplay(fileName);
	}

	public static string GetSceneLevelsForReplay(PlatformFilePath fileName)
	{
		return MBAPI.IMBMission.GetSceneLevelsForReplay(fileName);
	}

	public static string GetAtmosphereNameForReplay(PlatformFilePath fileName)
	{
		return MBAPI.IMBMission.GetAtmosphereNameForReplay(fileName);
	}

	public static int GetAtmosphereSeasonForReplay(PlatformFilePath fileName)
	{
		return MBAPI.IMBMission.GetAtmosphereSeasonForReplay(fileName);
	}
}
