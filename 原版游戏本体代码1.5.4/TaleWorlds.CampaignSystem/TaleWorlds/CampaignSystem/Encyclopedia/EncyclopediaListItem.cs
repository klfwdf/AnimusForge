using System;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public struct EncyclopediaListItem
{
	public readonly object Object;

	public readonly string Name;

	public readonly string Description;

	public readonly string Id;

	public readonly string TypeName;

	public readonly bool PlayerCanSeeValues;

	public readonly Action OnShowTooltip;

	public EncyclopediaListItem(object obj, string name, string description, string id, string typeName, bool playerCanSeeValues, Action onShowTooltip = null)
	{
		Object = obj;
		Name = name;
		Description = description;
		Id = id;
		TypeName = typeName;
		PlayerCanSeeValues = playerCanSeeValues;
		OnShowTooltip = onShowTooltip;
	}
}
