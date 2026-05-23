using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000123 RID: 291
	public class DefaultKingdomDecisionPermissionModel : KingdomDecisionPermissionModel
	{
		// Token: 0x06001831 RID: 6193 RVA: 0x0007416B File Offset: 0x0007236B
		public override bool IsPolicyDecisionAllowed(PolicyObject policy)
		{
			return true;
		}

		// Token: 0x06001832 RID: 6194 RVA: 0x0007416E File Offset: 0x0007236E
		public override bool IsWarDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
		{
			reason = null;
			return true;
		}

		// Token: 0x06001833 RID: 6195 RVA: 0x00074174 File Offset: 0x00072374
		public override bool IsPeaceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
		{
			reason = null;
			if (!Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(kingdom1, kingdom2))
			{
				IAllianceCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
				if (campaignBehavior == null || !campaignBehavior.IsAtWarByCallToWarAgreement(kingdom1, kingdom2))
				{
					if (!Campaign.Current.Models.DiplomacyModel.IsPeaceSuitable(kingdom1, kingdom2))
					{
						reason = new TextObject("{=JkQ7fmcX}The enemy is not open to negotiations.", null);
						return false;
					}
					return true;
				}
			}
			reason = new TextObject("{=eNPupZOp}These kingdoms can not declare peace at this time.", null);
			return false;
		}

		// Token: 0x06001834 RID: 6196 RVA: 0x000741ED File Offset: 0x000723ED
		public override bool IsAnnexationDecisionAllowed(Settlement annexedSettlement)
		{
			return true;
		}

		// Token: 0x06001835 RID: 6197 RVA: 0x000741F0 File Offset: 0x000723F0
		public override bool IsExpulsionDecisionAllowed(Clan expelledClan)
		{
			return true;
		}

		// Token: 0x06001836 RID: 6198 RVA: 0x000741F3 File Offset: 0x000723F3
		public override bool IsKingSelectionDecisionAllowed(Kingdom kingdom)
		{
			return true;
		}

		// Token: 0x06001837 RID: 6199 RVA: 0x000741F6 File Offset: 0x000723F6
		public override bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
		{
			reason = null;
			return true;
		}
	}
}
