using Helpers;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class NPCEquipmentsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
	{
		Hero leader = kingdom.Leader;
		Hero hero = oldRulingClan?.Leader;
		(Equipment, Equipment) equipmentsForChangingRuler = Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentsForChangingRuler(leader, hero, Equipment.EquipmentType.Civilian);
		(Equipment, Equipment) equipmentsForChangingRuler2 = Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentsForChangingRuler(leader, hero, Equipment.EquipmentType.Battle);
		if (leader != Hero.MainHero)
		{
			EquipmentHelper.AssignHeroEquipmentFromEquipment(leader, equipmentsForChangingRuler2.Item1);
			EquipmentHelper.AssignHeroEquipmentFromEquipment(leader, equipmentsForChangingRuler.Item1);
		}
		if (hero != null && hero.IsActive && hero != Hero.MainHero)
		{
			EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipmentsForChangingRuler2.Item2);
			EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipmentsForChangingRuler.Item2);
		}
	}
}
