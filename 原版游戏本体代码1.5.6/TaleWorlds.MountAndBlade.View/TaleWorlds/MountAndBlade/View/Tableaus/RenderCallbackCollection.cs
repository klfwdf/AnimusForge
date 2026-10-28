using System;
using System.Collections.Generic;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public struct RenderCallbackCollection
{
	public List<Action<Texture>> SetActions { get; private set; }

	public List<Action> CancelActions { get; private set; }

	public static RenderCallbackCollection CreateEmpty()
	{
		return new RenderCallbackCollection
		{
			SetActions = new List<Action<Texture>>(),
			CancelActions = new List<Action>()
		};
	}
}
