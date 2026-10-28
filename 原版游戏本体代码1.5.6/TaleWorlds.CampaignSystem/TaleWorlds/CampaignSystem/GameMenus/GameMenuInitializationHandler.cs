using System;

namespace TaleWorlds.CampaignSystem.GameMenus;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class GameMenuInitializationHandler : Attribute
{
	public string MenuId { get; private set; }

	public GameMenuInitializationHandler(string menuId)
	{
		MenuId = menuId;
	}
}
