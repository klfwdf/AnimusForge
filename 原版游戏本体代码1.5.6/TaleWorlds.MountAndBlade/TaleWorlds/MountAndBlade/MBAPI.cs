using System.Collections.Generic;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public static class MBAPI
{
	internal static IMBTestRun IMBTestRun;

	internal static IMBActionSet IMBActionSet;

	internal static IMBAgent IMBAgent;

	internal static IMBAgentVisuals IMBAgentVisuals;

	internal static IMBAnimation IMBAnimation;

	internal static IMBDelegate IMBDelegate;

	internal static IMBItem IMBItem;

	internal static IMBEditor IMBEditor;

	internal static IMBMission IMBMission;

	internal static IMBMultiplayerData IMBMultiplayerData;

	internal static IMouseManager IMouseManager;

	internal static IMBNetwork IMBNetwork;

	internal static IMBPeer IMBPeer;

	internal static IMBSkeletonExtensions IMBSkeletonExtensions;

	internal static IMBGameEntityExtensions IMBGameEntityExtensions;

	internal static IMBScreen IMBScreen;

	internal static IMBSoundEvent IMBSoundEvent;

	internal static IMBVoiceManager IMBVoiceManager;

	internal static IMBTeam IMBTeam;

	internal static IMBWorld IMBWorld;

	internal static IInput IInput;

	internal static IMBMessageManager IMBMessageManager;

	internal static IMBWindowManager IMBWindowManager;

	internal static IMBDebugExtensions IMBDebugExtensions;

	internal static IMBGame IMBGame;

	internal static IMBFaceGen IMBFaceGen;

	internal static IMBMapScene IMBMapScene;

	internal static IMBBannerlordChecker IMBBannerlordChecker;

	internal static IMBBannerlordTableauManager IMBBannerlordTableauManager;

	internal static IMBBannerlordConfig IMBBannerlordConfig;

	private static Dictionary<string, object> _objects;

	private static T GetObject<T>() where T : class
	{
		if (_objects.TryGetValue(typeof(T).FullName, out var value))
		{
			return value as T;
		}
		return null;
	}

	internal static void SetObjects(Dictionary<string, object> objects)
	{
		_objects = objects;
		IMBTestRun = GetObject<IMBTestRun>();
		IMBActionSet = GetObject<IMBActionSet>();
		IMBAgent = GetObject<IMBAgent>();
		IMBAnimation = GetObject<IMBAnimation>();
		IMBDelegate = GetObject<IMBDelegate>();
		IMBItem = GetObject<IMBItem>();
		IMBEditor = GetObject<IMBEditor>();
		IMBMission = GetObject<IMBMission>();
		IMBMultiplayerData = GetObject<IMBMultiplayerData>();
		IMouseManager = GetObject<IMouseManager>();
		IMBNetwork = GetObject<IMBNetwork>();
		IMBPeer = GetObject<IMBPeer>();
		IMBSkeletonExtensions = GetObject<IMBSkeletonExtensions>();
		IMBGameEntityExtensions = GetObject<IMBGameEntityExtensions>();
		IMBScreen = GetObject<IMBScreen>();
		IMBSoundEvent = GetObject<IMBSoundEvent>();
		IMBVoiceManager = GetObject<IMBVoiceManager>();
		IMBTeam = GetObject<IMBTeam>();
		IMBWorld = GetObject<IMBWorld>();
		IInput = GetObject<IInput>();
		IMBMessageManager = GetObject<IMBMessageManager>();
		IMBWindowManager = GetObject<IMBWindowManager>();
		IMBDebugExtensions = GetObject<IMBDebugExtensions>();
		IMBGame = GetObject<IMBGame>();
		IMBFaceGen = GetObject<IMBFaceGen>();
		IMBMapScene = GetObject<IMBMapScene>();
		IMBBannerlordChecker = GetObject<IMBBannerlordChecker>();
		IMBAgentVisuals = GetObject<IMBAgentVisuals>();
		IMBBannerlordTableauManager = GetObject<IMBBannerlordTableauManager>();
		IMBBannerlordConfig = GetObject<IMBBannerlordConfig>();
	}
}
