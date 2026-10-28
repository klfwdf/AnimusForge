using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

internal struct UpdateAction
{
	public Widget Target;

	public Action<float> Action;

	public int Order;
}
