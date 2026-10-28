using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.BUTR.Shared.Helpers;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.DelayedSubModule;

public static class DelayedSubModuleManager
{
	private record Key(Type Type, int Priority, string[]? Before, string[]? After);

	private static ConcurrentDictionary<Key, object?> RegisteredTypes { get; }

	private static event EventHandler<SubscriptionGlobalEventArgs>? OnMethod;

	static DelayedSubModuleManager()
	{
		RegisteredTypes = new ConcurrentDictionary<Key, object>();
		Harmony harmony = new Harmony("butterlib.delayedsubmoduleloader.static");
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnSubModuleLoad"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => BaseSubModuleLoadPostfix(x))));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnSubModuleUnloaded"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => BaseOnSubModuleUnloadedPostfix(x))));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnBeforeInitialModuleScreenSetAsRoot"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => BaseOnBeforeInitialModuleScreenSetAsRootPostfix(x))));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnGameStart"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => BaseOnGameStartPostfix(x))));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnGameEnd"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => BaseOnGameEndPostfix(x))));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void BaseSubModuleLoadPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: true, SubscriptionType.AfterMethod, "OnSubModuleLoad"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void BaseOnSubModuleUnloadedPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: true, SubscriptionType.AfterMethod, "OnSubModuleUnloaded"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void BaseOnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: true, SubscriptionType.AfterMethod, "OnBeforeInitialModuleScreenSetAsRoot"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void BaseOnGameStartPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: true, SubscriptionType.AfterMethod, "OnGameStart"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void BaseOnGameEndPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: true, SubscriptionType.AfterMethod, "OnGameEnd"));
	}

	public static void Register<TSubModule>() where TSubModule : MBSubModuleBase
	{
		Register<TSubModule>(-1, null, null);
	}

	public static void Register(Type subModule)
	{
		Register(subModule, -1, null, null);
	}

	public static void Register<TSubModule>(int priority, string[]? before, string[]? after) where TSubModule : MBSubModuleBase
	{
		Register(typeof(TSubModule), priority, before, after);
	}

	public static void Register(Type subModule, int priority, string[]? before, string[]? after)
	{
		Key key = new Key(subModule, priority, before, after);
		if (!RegisteredTypes.TryAdd(key, null))
		{
			return;
		}
		Harmony harmony = new Harmony("butterlib.delayedsubmoduleloader." + subModule.Name.ToLowerInvariant());
		MethodInfo methodInfo = HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnSubModuleLoad");
		if ((object)methodInfo != null)
		{
			harmony.Patch(methodInfo, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => SubModuleLoadPrefix(x)), priority, before, after), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => SubModuleLoadPostfix(x)), priority, before, after));
		}
		if ((object)HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnSubModuleUnloaded") != null)
		{
			harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnSubModuleUnloaded"), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnSubModuleUnloadedPrefix(x)), priority, before, after), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnSubModuleUnloadedPostfix(x)), priority, before, after));
		}
		if ((object)HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnBeforeInitialModuleScreenSetAsRoot") != null)
		{
			harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnBeforeInitialModuleScreenSetAsRoot"), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnBeforeInitialModuleScreenSetAsRootPrefix(x)), priority, before, after), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnBeforeInitialModuleScreenSetAsRootPostfix(x)), priority, before, after));
		}
		if ((object)HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnGameStart") != null)
		{
			harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnGameStart"), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnGameStartPrefix(x)), priority, before, after), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnGameStartPostfix(x)), priority, before, after));
		}
		if ((object)HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnGameEnd") != null)
		{
			harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.DeclaredMethod(subModule, "OnGameEnd"), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnGameEndPrefix(x)), priority, before, after), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((MBSubModuleBase x) => OnGameEndPostfix(x)), priority, before, after));
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void SubModuleLoadPrefix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.BeforeMethod, "OnSubModuleLoad"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void SubModuleLoadPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.AfterMethod, "OnSubModuleLoad"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnSubModuleUnloadedPrefix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.BeforeMethod, "OnSubModuleUnloaded"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnSubModuleUnloadedPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.AfterMethod, "OnSubModuleUnloaded"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnBeforeInitialModuleScreenSetAsRootPrefix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.BeforeMethod, "OnBeforeInitialModuleScreenSetAsRoot"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.AfterMethod, "OnBeforeInitialModuleScreenSetAsRoot"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnGameStartPrefix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.BeforeMethod, "OnGameStart"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnGameStartPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.AfterMethod, "OnGameStart"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnGameEndPrefix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.BeforeMethod, "OnGameEnd"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnGameEndPostfix(MBSubModuleBase __instance)
	{
		OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), isBase: false, SubscriptionType.AfterMethod, "OnGameEnd"));
	}

	public static void Subscribe(Type mbSubModuleType, MBSubModuleBase caller, string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate)
	{
		if (mbSubModuleType.IsSubclassOf(typeof(MBSubModuleBase)))
		{
			Subscribe(mbSubModuleType, caller.GetType(), method, subscriptionType, @delegate);
		}
	}

	public static void Subscribe<T1, T2>(string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate) where T1 : MBSubModuleBase where T2 : MBSubModuleBase
	{
		Subscribe(typeof(T1), typeof(T2), method, subscriptionType, @delegate);
	}

	public static void Subscribe<T>(MBSubModuleBase caller, string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate) where T : MBSubModuleBase
	{
		Subscribe(typeof(T), caller.GetType(), method, subscriptionType, @delegate);
	}

	private static void Subscribe(Type mbSubModuleType, Type caller, string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate)
	{
		List<ModuleInfoExtendedHelper> list = ModuleInfoHelper.GetLoadedModules().ToList();
		ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(caller);
		ModuleInfoExtendedHelper moduleByType2 = ModuleInfoHelper.GetModuleByType(mbSubModuleType);
		int num = list.IndexOf(moduleByType);
		int num2 = list.IndexOf(moduleByType2);
		if (num < num2)
		{
			OnMethod += delegate(object s, SubscriptionGlobalEventArgs e)
			{
				if (e.IsValid(mbSubModuleType, method, subscriptionType))
				{
					@delegate(s, new SubscriptionEventArgs(e.IsBase));
				}
			};
		}
		if (num == num2)
		{
			if (subscriptionType == SubscriptionType.BeforeMethod)
			{
				@delegate(null, new SubscriptionEventArgs(isBase: false));
			}
			if (subscriptionType == SubscriptionType.AfterMethod)
			{
				OnMethod += delegate(object s, SubscriptionGlobalEventArgs e)
				{
					if (e.IsValid(mbSubModuleType, method, subscriptionType))
					{
						@delegate(s, new SubscriptionEventArgs(e.IsBase));
					}
				};
			}
		}
		if (num > num2)
		{
			@delegate(null, new SubscriptionEventArgs(isBase: false));
		}
	}
}
