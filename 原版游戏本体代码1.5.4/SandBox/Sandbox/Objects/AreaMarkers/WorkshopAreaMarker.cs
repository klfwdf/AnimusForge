using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers;

public class WorkshopAreaMarker : AreaMarker
{
	public override string Tag => GetWorkshop()?.Tag;

	public Workshop GetWorkshop()
	{
		Workshop result = null;
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (settlement != null && settlement.IsTown && settlement.Town.Workshops.Length > AreaIndex && AreaIndex > 0)
		{
			result = settlement.Town.Workshops[AreaIndex];
		}
		return result;
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (!MBEditor.HelpersEnabled() || !CheckToggle)
		{
			return;
		}
		float distanceSquared = AreaRadius * AreaRadius;
		List<GameEntity> entities = new List<GameEntity>();
		base.Scene.GetEntities(ref entities);
		foreach (GameEntity item in entities)
		{
			item.HasTag("shop_prop");
		}
		foreach (UsableMachine item2 in (from x in entities.FindAllWithType<UsableMachine>()
			where x.GameEntity.GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition) <= distanceSquared
			select x).ToList())
		{
			_ = item2;
		}
	}

	public WorkshopType GetWorkshopType()
	{
		return GetWorkshop()?.WorkshopType;
	}

	public override TextObject GetName()
	{
		return GetWorkshop()?.Name;
	}
}
