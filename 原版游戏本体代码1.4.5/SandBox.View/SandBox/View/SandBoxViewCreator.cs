using System;
using System.Collections.Generic;
using System.Reflection;
using SandBox.View.Map;
using SandBox.View.Menu;
using SandBox.View.Missions;
using SandBox.View.Missions.NameMarkers;
using SandBox.View.Missions.Tournaments;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.ScreenSystem;

namespace SandBox.View;

public static class SandBoxViewCreator
{
	private static Dictionary<Type, MBList<Type>> _actualViewTypes;

	static SandBoxViewCreator()
	{
		CollectTypes();
	}

	private static void CollectTypes()
	{
		_actualViewTypes = new Dictionary<Type, MBList<Type>>();
		Assembly assembly = typeof(ViewCreatorModule).Assembly;
		Assembly[] referencingAssembliesSafe = assembly.GetReferencingAssembliesSafe();
		CheckOverridenViews(assembly);
		Assembly[] array = referencingAssembliesSafe;
		for (int i = 0; i < array.Length; i++)
		{
			CheckOverridenViews(array[i]);
		}
	}

	private static void CheckOverridenViews(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			if (!typeof(MapView).IsAssignableFrom(item) && !typeof(MenuView).IsAssignableFrom(item) && !typeof(MissionView).IsAssignableFrom(item) && !typeof(ScreenBase).IsAssignableFrom(item))
			{
				continue;
			}
			object[] customAttributesSafe = item.GetCustomAttributesSafe(typeof(OverrideView), inherit: false);
			if (customAttributesSafe != null && customAttributesSafe.Length == 1 && customAttributesSafe[0] is OverrideView overrideView)
			{
				if (_actualViewTypes.TryGetValue(overrideView.BaseType, out var value))
				{
					value.Add(item);
					continue;
				}
				_actualViewTypes[overrideView.BaseType] = new MBList<Type> { item };
			}
		}
	}

	public static ScreenBase CreateSaveLoadScreen(bool isSaving)
	{
		return ViewCreatorManager.CreateScreenView<SaveLoadScreen>(new object[1] { isSaving });
	}

	public static MissionView CreateMissionCraftingView()
	{
		return null;
	}

	public static MissionView CreateMissionNameMarkerUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionNameMarkerUIHandler>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionConversationView(Mission mission)
	{
		return ViewCreatorManager.CreateMissionView<MissionConversationView>(isNetwork: true, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionBarterView()
	{
		return ViewCreatorManager.CreateMissionView<BarterView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionAgentAlarmStateView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionAgentAlarmStateView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionMainAgentDetectionView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionMainAgentDetectionView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionStealthFailCounter(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionStealthFailCounterView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionTournamentView()
	{
		return ViewCreatorManager.CreateMissionView<MissionTournamentView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionQuestBarView()
	{
		return ViewCreatorManager.CreateMissionView<MissionQuestBarView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MapView CreateMapView<T>(params object[] parameters) where T : MapView
	{
		Type type = typeof(T);
		if (_actualViewTypes.TryGetValue(typeof(T), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
		}
		return Activator.CreateInstance(type, parameters) as MapView;
	}

	public static MenuView CreateMenuView<T>(params object[] parameters) where T : MenuView
	{
		Type type = typeof(T);
		if (_actualViewTypes.TryGetValue(typeof(T), out var value))
		{
			MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (activeGameAssemblies.Contains(value[num].Assembly))
				{
					type = value[num];
					break;
				}
			}
		}
		return Activator.CreateInstance(type, parameters) as MenuView;
	}

	public static MissionView CreateBoardGameView()
	{
		return ViewCreatorManager.CreateMissionView<BoardGameView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionArenaPracticeFightView()
	{
		return ViewCreatorManager.CreateMissionView<MissionArenaPracticeFightView>(isNetwork: false, null, Array.Empty<object>());
	}
}
