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

namespace SandBox.View
{
	// Token: 0x02000009 RID: 9
	public static class SandBoxViewCreator
	{
		// Token: 0x06000025 RID: 37 RVA: 0x00003204 File Offset: 0x00001404
		static SandBoxViewCreator()
		{
			SandBoxViewCreator.CollectTypes();
		}

		// Token: 0x06000026 RID: 38 RVA: 0x0000320C File Offset: 0x0000140C
		private static void CollectTypes()
		{
			SandBoxViewCreator._actualViewTypes = new Dictionary<Type, MBList<Type>>();
			Assembly assembly = typeof(ViewCreatorModule).Assembly;
			Assembly[] referencingAssembliesSafe = assembly.GetReferencingAssembliesSafe(null);
			SandBoxViewCreator.CheckOverridenViews(assembly);
			Assembly[] array = referencingAssembliesSafe;
			for (int i = 0; i < array.Length; i++)
			{
				SandBoxViewCreator.CheckOverridenViews(array[i]);
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00003258 File Offset: 0x00001458
		private static void CheckOverridenViews(Assembly assembly)
		{
			foreach (Type type in assembly.GetTypesSafe(null))
			{
				if (typeof(MapView).IsAssignableFrom(type) || typeof(MenuView).IsAssignableFrom(type) || typeof(MissionView).IsAssignableFrom(type) || typeof(ScreenBase).IsAssignableFrom(type))
				{
					object[] customAttributesSafe = type.GetCustomAttributesSafe(typeof(OverrideView), false);
					if (customAttributesSafe != null && customAttributesSafe.Length == 1)
					{
						OverrideView overrideView = customAttributesSafe[0] as OverrideView;
						if (overrideView != null)
						{
							MBList<Type> mblist;
							if (SandBoxViewCreator._actualViewTypes.TryGetValue(overrideView.BaseType, out mblist))
							{
								mblist.Add(type);
							}
							else
							{
								SandBoxViewCreator._actualViewTypes[overrideView.BaseType] = new MBList<Type> { type };
							}
						}
					}
				}
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003354 File Offset: 0x00001554
		public static ScreenBase CreateSaveLoadScreen(bool isSaving)
		{
			return ViewCreatorManager.CreateScreenView<SaveLoadScreen>(new object[] { isSaving });
		}

		// Token: 0x06000029 RID: 41 RVA: 0x0000336A File Offset: 0x0000156A
		public static MissionView CreateMissionCraftingView()
		{
			return null;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x0000336D File Offset: 0x0000156D
		public static MissionView CreateMissionNameMarkerUIHandler(Mission mission = null)
		{
			return ViewCreatorManager.CreateMissionView<MissionNameMarkerUIHandler>(mission != null, mission, Array.Empty<object>());
		}

		// Token: 0x0600002B RID: 43 RVA: 0x0000337E File Offset: 0x0000157E
		public static MissionView CreateMissionConversationView(Mission mission)
		{
			return ViewCreatorManager.CreateMissionView<MissionConversationView>(true, mission, Array.Empty<object>());
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000338C File Offset: 0x0000158C
		public static MissionView CreateMissionBarterView()
		{
			return ViewCreatorManager.CreateMissionView<BarterView>(false, null, Array.Empty<object>());
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000339A File Offset: 0x0000159A
		public static MissionView CreateMissionAgentAlarmStateView(Mission mission = null)
		{
			return ViewCreatorManager.CreateMissionView<MissionAgentAlarmStateView>(mission != null, mission, Array.Empty<object>());
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000033AB File Offset: 0x000015AB
		public static MissionView CreateMissionMainAgentDetectionView(Mission mission = null)
		{
			return ViewCreatorManager.CreateMissionView<MissionMainAgentDetectionView>(mission != null, mission, Array.Empty<object>());
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000033BC File Offset: 0x000015BC
		public static MissionView CreateMissionStealthFailCounter(Mission mission = null)
		{
			return ViewCreatorManager.CreateMissionView<MissionStealthFailCounterView>(mission != null, mission, Array.Empty<object>());
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000033CD File Offset: 0x000015CD
		public static MissionView CreateMissionTournamentView()
		{
			return ViewCreatorManager.CreateMissionView<MissionTournamentView>(false, null, Array.Empty<object>());
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000033DB File Offset: 0x000015DB
		public static MissionView CreateMissionQuestBarView()
		{
			return ViewCreatorManager.CreateMissionView<MissionQuestBarView>(false, null, Array.Empty<object>());
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000033EC File Offset: 0x000015EC
		public static MapView CreateMapView<T>(params object[] parameters) where T : MapView
		{
			Type type = typeof(T);
			MBList<Type> mblist;
			if (SandBoxViewCreator._actualViewTypes.TryGetValue(typeof(T), out mblist))
			{
				MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
				for (int i = mblist.Count - 1; i >= 0; i--)
				{
					if (activeGameAssemblies.Contains(mblist[i].Assembly))
					{
						type = mblist[i];
						break;
					}
				}
			}
			return Activator.CreateInstance(type, parameters) as MapView;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003460 File Offset: 0x00001660
		public static MenuView CreateMenuView<T>(params object[] parameters) where T : MenuView
		{
			Type type = typeof(T);
			MBList<Type> mblist;
			if (SandBoxViewCreator._actualViewTypes.TryGetValue(typeof(T), out mblist))
			{
				MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
				for (int i = mblist.Count - 1; i >= 0; i--)
				{
					if (activeGameAssemblies.Contains(mblist[i].Assembly))
					{
						type = mblist[i];
						break;
					}
				}
			}
			return Activator.CreateInstance(type, parameters) as MenuView;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000034D3 File Offset: 0x000016D3
		public static MissionView CreateBoardGameView()
		{
			return ViewCreatorManager.CreateMissionView<BoardGameView>(false, null, Array.Empty<object>());
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000034E1 File Offset: 0x000016E1
		public static MissionView CreateMissionArenaPracticeFightView()
		{
			return ViewCreatorManager.CreateMissionView<MissionArenaPracticeFightView>(false, null, Array.Empty<object>());
		}

		// Token: 0x04000006 RID: 6
		private static Dictionary<Type, MBList<Type>> _actualViewTypes;
	}
}
