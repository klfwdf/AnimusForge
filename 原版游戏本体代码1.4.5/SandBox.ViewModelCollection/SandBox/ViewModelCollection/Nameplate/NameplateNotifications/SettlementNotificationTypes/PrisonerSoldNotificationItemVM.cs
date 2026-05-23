using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class PrisonerSoldNotificationItemVM : SettlementNotificationItemBaseVM
{
	private int _prisonersAmount;

	public MobileParty Party { get; private set; }

	public PrisonerSoldNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, MobileParty party, TroopRoster prisoners, int createdTick)
		: base(onRemove, createdTick)
	{
		_prisonersAmount = prisoners.TotalManCount;
		base.Text = SandBoxUIHelper.GetPrisonersSoldNotificationText(_prisonersAmount);
		Party = party;
		base.CharacterName = ((party.LeaderHero != null) ? party.LeaderHero.Name.ToString() : party.Name.ToString());
		base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(PartyBaseHelper.GetVisualPartyLeader(party.Party)));
		base.RelationType = 0;
		base.CreatedTick = createdTick;
		if (party.LeaderHero != null)
		{
			base.RelationType = ((!party.LeaderHero.Clan.IsAtWarWith(Hero.MainHero.Clan)) ? 1 : (-1));
		}
	}

	public void AddNewPrisoners(TroopRoster newPrisoners)
	{
		_prisonersAmount += newPrisoners.Count;
		base.Text = SandBoxUIHelper.GetPrisonersSoldNotificationText(_prisonersAmount);
	}
}
