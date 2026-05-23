using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bannerlord.BUTR.Shared.Extensions;
using BUTR.DependencyInjection.Logger;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;
using TaleWorlds.ScreenSystem;

namespace MCM.UI.Functionality
{
	// Token: 0x02000025 RID: 37
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class DefaultGameMenuScreenHandler : BaseGameMenuScreenHandler
	{
		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000178 RID: 376 RVA: 0x00006B7B File Offset: 0x00004D7B
		[Nullable(new byte[] { 1, 1, 0, 1, 2, 1 })]
		private static Dictionary<string, ValueTuple<int, Func<ScreenBase>, TextObject>> ScreensCache
		{
			[return: Nullable(new byte[] { 1, 1, 0, 1, 2, 1 })]
			get;
		} = new Dictionary<string, ValueTuple<int, Func<ScreenBase>, TextObject>>();

		// Token: 0x06000179 RID: 377 RVA: 0x00006B84 File Offset: 0x00004D84
		public DefaultGameMenuScreenHandler(IBUTRLogger<DefaultGameMenuScreenHandler> logger)
		{
			this._logger = logger;
			Harmony harmony = new Harmony("bannerlord.mcm.mainmenuscreeninjection_v4");
			harmony.Patch(AccessTools2.Method(typeof(InitialMenuVM), "RefreshMenuOptions", null, null, true), null, new HarmonyMethod(AccessTools2.Method(typeof(DefaultGameMenuScreenHandler), "RefreshMenuOptionsPostfix", null, null, true), 300, null, null, null), null, null);
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00006BF8 File Offset: 0x00004DF8
		[MethodImpl(MethodImplOptions.NoInlining)]
		private unsafe static void RefreshMenuOptionsPostfix(InitialMenuVM __instance, [Nullable(new byte[] { 2, 1 })] ref MBBindingList<InitialMenuOptionVM> ____menuOptions)
		{
			if (____menuOptions == null || DefaultGameMenuScreenHandler.InitialStateOption == null)
			{
				return;
			}
			DefaultGameMenuScreenHandler._instance.SetTarget(__instance);
			foreach (KeyValuePair<string, ValueTuple<int, Func<ScreenBase>, TextObject>> tuple in DefaultGameMenuScreenHandler.ScreensCache)
			{
				string text2;
				ValueTuple<int, Func<ScreenBase>, TextObject> valueTuple;
				tuple.Deconstruct(out text2, out valueTuple);
				string key = text2;
				ValueTuple<int, Func<ScreenBase>, TextObject> value = valueTuple;
				valueTuple = value;
				int index = valueTuple.Item1;
				Func<ScreenBase> screenFactory = valueTuple.Item2;
				TextObject text = valueTuple.Item3;
				InitialStateOption initialState = new InitialStateOption(key, text, 9000, delegate()
				{
					ScreenBase screen = screenFactory();
					if (screen != null)
					{
						ScreenManager.PushScreen(screen);
					}
				}, () => new ValueTuple<bool, TextObject>(false, null), null, null);
				int insertIndex = ____menuOptions.FindIndex((InitialMenuOptionVM i) => DefaultGameMenuScreenHandler.InitialStateOption(i)->OrderIndex > index);
				____menuOptions.Insert(insertIndex, new InitialMenuOptionVM(initialState));
			}
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00006D00 File Offset: 0x00004F00
		public unsafe override void AddScreen(string internalName, int index, [Nullable(new byte[] { 1, 2 })] Func<ScreenBase> screenFactory, [Nullable(2)] TextObject text)
		{
			if (text == null)
			{
				return;
			}
			InitialMenuVM instance;
			if (DefaultGameMenuScreenHandler._instance.TryGetTarget(out instance) && DefaultGameMenuScreenHandler.InitialStateOption != null)
			{
				InitialStateOption initialState = new InitialStateOption(internalName, text, index, delegate()
				{
					ScreenBase screen = screenFactory();
					if (screen != null)
					{
						ScreenManager.PushScreen(screen);
					}
				}, () => new ValueTuple<bool, TextObject>(false, null), null, null);
				MBBindingList<InitialMenuOptionVM> menuOptions = instance.MenuOptions;
				int insertIndex = menuOptions.FindIndex((InitialMenuOptionVM i) => DefaultGameMenuScreenHandler.InitialStateOption(i)->OrderIndex > index);
				if (menuOptions != null)
				{
					menuOptions.Insert(insertIndex, new InitialMenuOptionVM(initialState));
				}
			}
			DefaultGameMenuScreenHandler.ScreensCache[internalName] = new ValueTuple<int, Func<ScreenBase>, TextObject>(index, screenFactory, text);
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00006DC4 File Offset: 0x00004FC4
		public unsafe override void RemoveScreen(string internalName)
		{
			InitialMenuVM instance;
			if (DefaultGameMenuScreenHandler._instance.TryGetTarget(out instance) && DefaultGameMenuScreenHandler.InitialStateOption != null)
			{
				MBBindingList<InitialMenuOptionVM> menuOptions = instance.MenuOptions;
				InitialMenuOptionVM found = ((menuOptions != null) ? menuOptions.FirstOrDefault((InitialMenuOptionVM i) => DefaultGameMenuScreenHandler.InitialStateOption(i)->Id == internalName) : null);
				if (found != null && menuOptions != null)
				{
					menuOptions.Remove(found);
				}
			}
			if (DefaultGameMenuScreenHandler.ScreensCache.ContainsKey(internalName))
			{
				DefaultGameMenuScreenHandler.ScreensCache.Remove(internalName);
			}
		}

		// Token: 0x04000060 RID: 96
		[Nullable(new byte[] { 2, 1, 1 })]
		private static readonly AccessTools.FieldRef<InitialMenuOptionVM, InitialStateOption> InitialStateOption = AccessTools2.FieldRefAccess<InitialMenuOptionVM, InitialStateOption>("InitialStateOption", true);

		// Token: 0x04000061 RID: 97
		private static readonly WeakReference<InitialMenuVM> _instance = new WeakReference<InitialMenuVM>(null);

		// Token: 0x04000063 RID: 99
		private readonly IBUTRLogger _logger;
	}
}
