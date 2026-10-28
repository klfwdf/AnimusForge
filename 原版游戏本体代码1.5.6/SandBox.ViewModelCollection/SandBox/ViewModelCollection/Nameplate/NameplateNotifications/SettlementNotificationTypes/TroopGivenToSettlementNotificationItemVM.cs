using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class TroopGivenToSettlementNotificationItemVM : SettlementNotificationItemBaseVM
{
	public Hero GiverHero { get; private set; }

	public TroopRoster Troops { get; private set; }

	public TroopGivenToSettlementNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Hero giverHero, TroopRoster troops, int createdTick)
		: base(onRemove, createdTick)
	{
		GiverHero = giverHero;
		Troops = troops;
		base.Text = SandBoxUIHelper.GetTroopGivenToSettlementNotificationText(Troops.TotalManCount);
		base.CharacterName = ((GiverHero != null) ? GiverHero.Name.ToString() : "null hero");
		base.CharacterVisual = ((GiverHero != null) ? new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(GiverHero.CharacterObject)) : new CharacterImageIdentifierVM(null));
		base.RelationType = 0;
		base.CreatedTick = createdTick;
		if (GiverHero != null)
		{
			base.RelationType = ((!GiverHero.Clan.IsAtWarWith(Hero.MainHero.Clan)) ? 1 : (-1));
		}
	}

	public void AddNewAction(TroopRoster newTroops)
	{
		Troops.Add(newTroops);
		base.Text = SandBoxUIHelper.GetTroopGivenToSettlementNotificationText(Troops.TotalManCount);
	}
}
