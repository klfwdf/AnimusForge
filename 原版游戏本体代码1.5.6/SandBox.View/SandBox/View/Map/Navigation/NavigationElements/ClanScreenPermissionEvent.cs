using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class ClanScreenPermissionEvent : EventBase
{
	public Action<bool, TextObject> IsClanScreenAvailable { get; private set; }

	public ClanScreenPermissionEvent(Action<bool, TextObject> isClanScreenAvailable)
	{
		IsClanScreenAvailable = isClanScreenAvailable;
	}
}
