using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables
{
	// Token: 0x02000486 RID: 1158
	public class NoAttackBarterable : Barterable
	{
		// Token: 0x17000E6C RID: 3692
		// (get) Token: 0x060048BC RID: 18620 RVA: 0x0016E8CF File Offset: 0x0016CACF
		public override string StringID
		{
			get
			{
				return "no_attack_barterable";
			}
		}

		// Token: 0x060048BD RID: 18621 RVA: 0x0016E8D6 File Offset: 0x0016CAD6
		public NoAttackBarterable(Hero originalOwner, Hero otherHero, PartyBase ownerParty, PartyBase otherParty, CampaignTime duration)
			: base(originalOwner, ownerParty)
		{
			this._otherFaction = otherParty.MapFaction;
			this._duration = duration;
			this._otherHero = otherHero;
			this._otherParty = otherParty;
		}

		// Token: 0x17000E6D RID: 3693
		// (get) Token: 0x060048BE RID: 18622 RVA: 0x0016E904 File Offset: 0x0016CB04
		public override TextObject Name
		{
			get
			{
				TextObject textObject = new TextObject("{=Y3lGJT8H}{PARTY} won't attack {FACTION} for {DURATION} {?DURATION>1}days{?}day{\\?}.", null);
				textObject.SetTextVariable("PARTY", base.OriginalParty.Name);
				textObject.SetTextVariable("FACTION", this._otherFaction.Name);
				textObject.SetTextVariable("DURATION", this._duration.ToDays.ToString());
				return textObject;
			}
		}

		// Token: 0x060048BF RID: 18623 RVA: 0x0016E96C File Offset: 0x0016CB6C
		public override void Apply()
		{
			if (base.OriginalParty == MobileParty.MainParty.Party)
			{
				if (this._otherFaction.NotAttackableByPlayerUntilTime.IsPast)
				{
					this._otherFaction.NotAttackableByPlayerUntilTime = CampaignTime.Now;
				}
				this._otherFaction.NotAttackableByPlayerUntilTime += this._duration;
			}
		}

		// Token: 0x060048C0 RID: 18624 RVA: 0x0016E9CC File Offset: 0x0016CBCC
		public override int GetUnitValueForFaction(IFaction faction)
		{
			int result = 0;
			float militaryValueOfParty = Campaign.Current.Models.ValuationModel.GetMilitaryValueOfParty(base.OriginalParty.MobileParty);
			if (faction.MapFaction == this._otherFaction.MapFaction && faction.MapFaction.IsAtWarWith(base.OriginalParty.MapFaction))
			{
				result = (int)(militaryValueOfParty * 0.1f);
			}
			else if (faction.MapFaction == base.OriginalParty.MapFaction)
			{
				result = -(int)(militaryValueOfParty * 0.1f);
			}
			return result;
		}

		// Token: 0x060048C1 RID: 18625 RVA: 0x0016EA4F File Offset: 0x0016CC4F
		public override ImageIdentifier GetVisualIdentifier()
		{
			return null;
		}

		// Token: 0x0400140A RID: 5130
		private readonly IFaction _otherFaction;

		// Token: 0x0400140B RID: 5131
		private readonly CampaignTime _duration;

		// Token: 0x0400140C RID: 5132
		private readonly Hero _otherHero;

		// Token: 0x0400140D RID: 5133
		private readonly PartyBase _otherParty;
	}
}
