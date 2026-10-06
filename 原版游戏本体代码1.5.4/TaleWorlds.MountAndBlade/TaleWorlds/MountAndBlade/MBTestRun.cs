namespace TaleWorlds.MountAndBlade;

public class MBTestRun
{
	public static bool EnterEditMode()
	{
		return MBAPI.IMBTestRun.EnterEditMode();
	}

	public static bool NewScene()
	{
		return MBAPI.IMBTestRun.NewScene();
	}

	public static bool LeaveEditMode()
	{
		return MBAPI.IMBTestRun.LeaveEditMode();
	}

	public static bool OpenScene(string sceneName)
	{
		return MBAPI.IMBTestRun.OpenScene(sceneName);
	}

	public static bool CloseScene()
	{
		return MBAPI.IMBTestRun.CloseScene();
	}

	public static bool SaveScene()
	{
		return false;
	}

	public static bool OpenDefaultScene()
	{
		return false;
	}

	public static int GetFPS()
	{
		return MBAPI.IMBTestRun.GetFPS();
	}

	public static void StartMission()
	{
		MBAPI.IMBTestRun.StartMission();
	}
}
