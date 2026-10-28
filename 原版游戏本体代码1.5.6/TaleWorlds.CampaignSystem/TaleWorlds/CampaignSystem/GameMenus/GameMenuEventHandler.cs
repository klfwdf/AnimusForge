using System;

namespace TaleWorlds.CampaignSystem.GameMenus;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class GameMenuEventHandler : Attribute
{
	public enum EventType
	{
		OnCondition,
		OnConsequence
	}

	public string MenuId { get; private set; }

	public string MenuOptionId { get; private set; }

	public EventType Type { get; private set; }

	public GameMenuEventHandler(string menuId, string menuOptionId, EventType type)
	{
		MenuId = menuId;
		MenuOptionId = menuOptionId;
		Type = type;
	}
}
