using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.HotKeys;
using TaleWorlds.InputSystem;

namespace Bannerlord.ButterLib.Implementation.HotKeys;

internal sealed class HotKeyManagerStaticImplementation : IHotKeyManagerStatic
{
	public IList<HotKeyBase> HotKeys => HotKeyManagerImplementation.GlobalHotKeyStorage;

	public Bannerlord.ButterLib.HotKeys.HotKeyManager Create(string modName)
	{
		if (TaleWorlds.InputSystem.HotKeyManager.GetAllCategories().Any((GameKeyContext x) => string.Equals(x.GameKeyCategoryId, modName, StringComparison.OrdinalIgnoreCase)))
		{
			throw new ArgumentException("Hotkeys For Mod With This Name Already Exists.", "modName");
		}
		return new HotKeyManagerImplementation(modName);
	}

	public Bannerlord.ButterLib.HotKeys.HotKeyManager CreateWithOwnCategory(string modName, string categoryName)
	{
		if (TaleWorlds.InputSystem.HotKeyManager.GetAllCategories().Any((GameKeyContext x) => string.Equals(x.GameKeyCategoryId, modName, StringComparison.OrdinalIgnoreCase)))
		{
			throw new ArgumentException("Hotkeys For Mod With This Name Already Exists.", "modName");
		}
		return new HotKeyManagerImplementation(modName, categoryName);
	}
}
