using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.HotKeys;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Implementation.HotKeys;

internal sealed class HotKeyManagerImplementation : Bannerlord.ButterLib.HotKeys.HotKeyManager
{
	internal static readonly List<HotKeyCategoryContainer> GlobalContainerStorage = new List<HotKeyCategoryContainer>();

	internal static readonly List<HotKeyBase> GlobalHotKeyStorage = new List<HotKeyBase>();

	private int _currentId;

	private readonly string _modName;

	private readonly string _categoryName;

	private readonly List<HotKeyBase> _instanceHotKeys = new List<HotKeyBase>();

	internal HotKeyManagerImplementation(string modName, string categoryName = "")
	{
		_modName = modName;
		_categoryName = categoryName;
	}

	public override T Add<T>(T hotkey)
	{
		if (_instanceHotKeys.Any((HotKeyBase x) => string.Equals(x.Uid, hotkey.Uid, StringComparison.OrdinalIgnoreCase)))
		{
			throw new ArgumentException("A hotkey called '" + hotkey.Uid + "' was already registered by this HotKeyManager!", "hotkey");
		}
		_instanceHotKeys.Add(hotkey);
		hotkey.Id = _currentId;
		_currentId++;
		return hotkey;
	}

	public override T Add<T>()
	{
		return Add(new T());
	}

	public override IReadOnlyList<HotKeyBase> Build()
	{
		if (GlobalContainerStorage.Any((HotKeyCategoryContainer x) => string.Equals(x.GameKeyCategoryId, _modName, StringComparison.OrdinalIgnoreCase)))
		{
			throw new ArgumentException("A HotKeyManager with a modname '" + _modName + "' was already built!", "_modName");
		}
		HotKeyCategoryContainer hotKeyCategoryContainer = new HotKeyCategoryContainer(_modName, _categoryName, _instanceHotKeys);
		TaleWorlds.InputSystem.HotKeyManager.RegisterInitialContexts(TaleWorlds.InputSystem.HotKeyManager.GetAllCategories().ToList().Concat(new global::_003C_003Ez__ReadOnlySingleElementList<GameKeyContext>(hotKeyCategoryContainer)));
		GlobalContainerStorage.Add(hotKeyCategoryContainer);
		MBReadOnlyList<GameKey> registeredGameKeys = hotKeyCategoryContainer.RegisteredGameKeys;
		foreach (HotKeyBase hotKey in _instanceHotKeys)
		{
			foreach (GameKey item in registeredGameKeys.Where((GameKey gameKey) => gameKey != null && string.Equals(gameKey.StringId, hotKey.Uid, StringComparison.OrdinalIgnoreCase)))
			{
				hotKey.GameKey = item;
			}
		}
		GlobalHotKeyStorage.AddRange(_instanceHotKeys);
		return _instanceHotKeys;
	}
}
