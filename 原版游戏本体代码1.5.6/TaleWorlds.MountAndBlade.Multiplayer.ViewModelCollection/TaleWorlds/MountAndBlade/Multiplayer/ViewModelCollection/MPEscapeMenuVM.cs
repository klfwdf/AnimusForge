using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MPEscapeMenuVM : EscapeMenuVM
{
	public MPEscapeMenuVM(IEnumerable<EscapeMenuItemVM> items, TextObject title = null)
		: base(items, title)
	{
	}
}
