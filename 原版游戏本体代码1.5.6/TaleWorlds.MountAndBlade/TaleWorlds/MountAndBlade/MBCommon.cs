using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

public class MBCommon
{
	public enum GameType
	{
		Single,
		MultiClient,
		MultiServer,
		MultiClientServer,
		SingleReplay,
		SingleRecord
	}

	[EngineStruct("rglTimer_type", false, null)]
	public enum TimeType
	{
		[CustomEngineStructMemberData("Real_timer")]
		Application,
		[CustomEngineStructMemberData("Tactical_timer")]
		Mission
	}

	private static GameType _currentGameType;

	public static GameType CurrentGameType
	{
		get
		{
			return _currentGameType;
		}
		set
		{
			_currentGameType = value;
			MBAPI.IMBWorld.SetGameType((int)value);
		}
	}

	public static bool IsDebugMode => false;

	public static bool IsPaused { get; private set; }

	public static void PauseGameEngine()
	{
		IsPaused = true;
		MBAPI.IMBWorld.PauseGame();
	}

	public static void UnPauseGameEngine()
	{
		IsPaused = false;
		MBAPI.IMBWorld.UnpauseGame();
	}

	public static float GetApplicationTime()
	{
		return MBAPI.IMBWorld.GetGlobalTime(TimeType.Application);
	}

	public static float GetTotalMissionTime()
	{
		return MBAPI.IMBWorld.GetGlobalTime(TimeType.Mission);
	}

	public static void FixSkeletons()
	{
		MBAPI.IMBWorld.FixSkeletons();
	}

	public static void CheckResourceModifications()
	{
		MBAPI.IMBWorld.CheckResourceModifications();
	}

	public static int Hash(int i, object o)
	{
		return ((i * 397) ^ o.GetHashCode()).ToString().GetHashCode();
	}
}
