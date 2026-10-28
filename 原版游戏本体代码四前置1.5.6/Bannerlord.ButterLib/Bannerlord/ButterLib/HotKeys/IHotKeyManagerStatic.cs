using System.Collections.Generic;

namespace Bannerlord.ButterLib.HotKeys;

internal interface IHotKeyManagerStatic
{
	IList<HotKeyBase> HotKeys { get; }

	HotKeyManager Create(string modName);

	HotKeyManager CreateWithOwnCategory(string modName, string categoryName);
}
