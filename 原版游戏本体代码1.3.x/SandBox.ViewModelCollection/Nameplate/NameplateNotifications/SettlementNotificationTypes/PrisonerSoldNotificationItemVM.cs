using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes
{
	// Token: 0x02000026 RID: 38
	public class PrisonerSoldNotificationItemVM : SettlementNotificationItemBaseVM
	{
		// Token: 0x1700010C RID: 268
		// (get) Token: 0x0600034F RID: 847 RVA: 0x0000E3B8 File Offset: 0x0000C5B8
		// (set) Token: 0x06000350 RID: 848 RVA: 0x0000E3C0 File Offset: 0x0000C5C0
		public MobileParty Party { get; private set; }

		// Token: 0x06000351 RID: 849 RVA: 0x0000E3CC File Offset: 0x0000C5CC
		public PrisonerSoldNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, MobileParty party, TroopRoster prisoners, int createdTick)
			: base(onRemove, createdTick)
		{
			this._prisonersAmount = prisoners.TotalManCount;
			base.Text = SandBoxUIHelper.GetPrisonersSoldNotificationText(this._prisonersAmount);
			this.Party = party;
			base.CharacterName = ((party.LeaderHero != null) ? party.LeaderHero.Name.ToString() : party.Name.ToString());
			base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(PartyBaseHelper.GetVisualPartyLeader(party.Party), false));
			base.RelationType = 0;
			base.CreatedTick = createdTick;
			if (party.LeaderHero != null)
			{
				base.RelationType = (party.LeaderHero.Clan.IsAtWarWith(Hero.MainHero.Clan) ? (-1) : 1);
			}
		}

		// Token: 0x06000352 RID: 850 RVA: 0x0000E48A File Offset: 0x0000C68A
		public void AddNewPrisoners(TroopRoster newPrisoners)
		{
			this._prisonersAmount += newPrisoners.Count;
			base.Text = SandBoxUIHelper.GetPrisonersSoldNotificationText(this._prisonersAmount);
		}

		// Token: 0x040001B2 RID: 434
		private int _prisonersAmount;
	}
}
