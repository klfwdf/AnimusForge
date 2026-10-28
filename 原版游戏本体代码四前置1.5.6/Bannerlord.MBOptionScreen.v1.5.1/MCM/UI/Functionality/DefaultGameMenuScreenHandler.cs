using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection.Logger;
using Bannerlord.BUTR.Shared.Extensions;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;
using TaleWorlds.ScreenSystem;

namespace MCM.UI.Functionality;

internal sealed class DefaultGameMenuScreenHandler : BaseGameMenuScreenHandler
{
	private static readonly AccessTools.FieldRef<InitialMenuOptionVM, InitialStateOption>? InitialStateOption = HarmonyLib.BUTR.Extensions.AccessTools2.FieldRefAccess<InitialMenuOptionVM, InitialStateOption>("InitialStateOption");

	private static readonly WeakReference<InitialMenuVM> _instance = new WeakReference<InitialMenuVM>(null);

	private readonly IBUTRLogger _logger;

	private static Dictionary<string, (int, Func<ScreenBase?>, TextObject)> ScreensCache { get; } = new Dictionary<string, (int, Func<ScreenBase>, TextObject)>();

	public DefaultGameMenuScreenHandler(IBUTRLogger<DefaultGameMenuScreenHandler> logger)
	{
		_logger = logger;
		Harmony harmony = new Harmony("bannerlord.mcm.mainmenuscreeninjection_v4");
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(InitialMenuVM), "RefreshMenuOptions"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(DefaultGameMenuScreenHandler), "RefreshMenuOptionsPostfix"), 300));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RefreshMenuOptionsPostfix(InitialMenuVM __instance, ref MBBindingList<InitialMenuOptionVM>? ____menuOptions)
	{
		if (____menuOptions == null || InitialStateOption == null)
		{
			return;
		}
		_instance.SetTarget(__instance);
		foreach (KeyValuePair<string, (int, Func<ScreenBase>, TextObject)> item2 in ScreensCache)
		{
			Bannerlord.BUTR.Shared.Extensions.DictionaryExtensions.Deconstruct(item2, out var key, out var value);
			string id = key;
			(int, Func<ScreenBase>, TextObject) tuple = value;
			value = tuple;
			int index = value.Item1;
			Func<ScreenBase> screenFactory = value.Item2;
			TextObject item = value.Item3;
			InitialStateOption initialStateOption = new InitialStateOption(id, item, 9000, delegate
			{
				ScreenBase screenBase = screenFactory();
				if (screenBase != null)
				{
					ScreenManager.PushScreen(screenBase);
				}
			}, () => ((bool, TextObject))(false, null));
			int index2 = ____menuOptions.FindIndex((InitialMenuOptionVM i) => InitialStateOption(i).OrderIndex > index);
			____menuOptions.Insert(index2, new InitialMenuOptionVM(initialStateOption));
		}
	}

	public override void AddScreen(string internalName, int index, Func<ScreenBase?> screenFactory, TextObject? text)
	{
		if ((object)text == null)
		{
			return;
		}
		if (_instance.TryGetTarget(out InitialMenuVM target) && InitialStateOption != null)
		{
			InitialStateOption initialStateOption = new InitialStateOption(internalName, text, index, delegate
			{
				ScreenBase screenBase = screenFactory();
				if (screenBase != null)
				{
					ScreenManager.PushScreen(screenBase);
				}
			}, () => ((bool, TextObject))(false, null));
			MBBindingList<InitialMenuOptionVM> menuOptions = target.MenuOptions;
			int index2 = menuOptions.FindIndex((InitialMenuOptionVM i) => InitialStateOption(i).OrderIndex > index);
			menuOptions?.Insert(index2, new InitialMenuOptionVM(initialStateOption));
		}
		ScreensCache[internalName] = (index, screenFactory, text);
	}

	public override void RemoveScreen(string internalName)
	{
		if (_instance.TryGetTarget(out InitialMenuVM target) && InitialStateOption != null)
		{
			MBBindingList<InitialMenuOptionVM> menuOptions = target.MenuOptions;
			InitialMenuOptionVM initialMenuOptionVM = menuOptions?.FirstOrDefault((InitialMenuOptionVM i) => InitialStateOption(i).Id == internalName);
			if (initialMenuOptionVM != null)
			{
				menuOptions?.Remove(initialMenuOptionVM);
			}
		}
		if (ScreensCache.ContainsKey(internalName))
		{
			ScreensCache.Remove(internalName);
		}
	}
}
