using System;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public struct EncyclopediaListItem(object obj, string name, string description, string id, string typeName, bool playerCanSeeValues, Action onShowTooltip = null)
{
	public readonly object Object = obj;

	public readonly string Name = name;

	public readonly string Description = description;

	public readonly string Id = id;

	public readonly string TypeName = typeName;

	public readonly bool PlayerCanSeeValues = playerCanSeeValues;

	public readonly Action OnShowTooltip = onShowTooltip;
}
