using System.Collections.Generic;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Bannerlord.ButterLib.HotKeys;

public abstract class HotKeyManager
{
	private static IHotKeyManagerStatic? _staticInstance;

	public static readonly IReadOnlyDictionary<HotKeyCategory, string> Categories = new Dictionary<HotKeyCategory, string>
	{
		{
			HotKeyCategory.Action,
			"ActionCategory"
		},
		{
			HotKeyCategory.Chat,
			"ChatCategory"
		},
		{
			HotKeyCategory.CampaignMap,
			"CampaignMapCategory"
		},
		{
			HotKeyCategory.MenuShortcut,
			"MenuShortcutCategory"
		},
		{
			HotKeyCategory.OrderMenu,
			"OrderMenuCategory"
		}
	};

	internal static IHotKeyManagerStatic? StaticInstance => _staticInstance ?? (_staticInstance = ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<IHotKeyManagerStatic>());

	public static HotKeyManager? Create(string modName)
	{
		return StaticInstance?.Create(modName);
	}

	public static HotKeyManager? CreateWithOwnCategory(string modName, string categoryName)
	{
		return StaticInstance?.CreateWithOwnCategory(modName, categoryName);
	}

	public abstract T Add<T>(T hotkey) where T : HotKeyBase;

	public abstract T Add<T>() where T : HotKeyBase, new();

	public abstract IReadOnlyList<HotKeyBase> Build();
}
