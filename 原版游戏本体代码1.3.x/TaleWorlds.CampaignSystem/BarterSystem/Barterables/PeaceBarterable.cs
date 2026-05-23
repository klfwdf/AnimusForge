using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables
{
	// Token: 0x02000487 RID: 1159
	public class PeaceBarterable : Barterable
	{
		// Token: 0x17000E6E RID: 3694
		// (get) Token: 0x060048C2 RID: 18626 RVA: 0x0016EA52 File Offset: 0x0016CC52
		public CampaignTime Duration { get; }

		// Token: 0x17000E6F RID: 3695
		// (get) Token: 0x060048C3 RID: 18627 RVA: 0x0016EA5A File Offset: 0x0016CC5A
		public override string StringID
		{
			get
			{
				return "peace_barterable";
			}
		}

		// Token: 0x060048C4 RID: 18628 RVA: 0x0016EA61 File Offset: 0x0016CC61
		public PeaceBarterable(Hero owner, IFaction peaceOfferingFaction, IFaction offeredFaction, CampaignTime duration)
			: base(owner, null)
		{
			this.Duration = duration;
			this.PeaceOfferingFaction = peaceOfferingFaction;
			this.OfferedFaction = offeredFaction;
		}

		// Token: 0x060048C5 RID: 18629 RVA: 0x0016EA81 File Offset: 0x0016CC81
		public PeaceBarterable(IFaction peaceOfferingFaction, IFaction offeredFaction, CampaignTime duration)
			: base(peaceOfferingFaction.Leader, null)
		{
			this.Duration = duration;
			this.PeaceOfferingFaction = peaceOfferingFaction;
			this.OfferedFaction = offeredFaction;
		}

		// Token: 0x17000E70 RID: 3696
		// (get) Token: 0x060048C6 RID: 18630 RVA: 0x0016EAA5 File Offset: 0x0016CCA5
		public override TextObject Name
		{
			get
			{
				TextObject textObject = new TextObject("{=R0bJS0pn}Make peace with the {OTHER_FACTION}", null);
				textObject.SetTextVariable("OTHER_FACTION", this.OfferedFaction.InformalName);
				return textObject;
			}
		}

		// Token: 0x060048C7 RID: 18631 RVA: 0x0016EACC File Offset: 0x0016CCCC
		public override int GetUnitValueForFaction(IFaction factionToEvaluateFor)
		{
			float num = 0f;
			IFaction faction = this.OfferedFaction;
			IFaction faction2 = this.PeaceOfferingFaction;
			if (factionToEvaluateFor.MapFaction == faction)
			{
				IFaction faction3 = faction2;
				IFaction faction4 = faction;
				faction = faction3;
				faction2 = faction4;
			}
			if (faction == null || faction2 == null)
			{
				return 0;
			}
			TextObject textObject;
			num = (float)((int)Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeaceForClan(faction2, faction, factionToEvaluateFor.Leader.Clan, out textObject, false));
			if (factionToEvaluateFor.IsKingdomFaction)
			{
				float num2 = 0f;
				int num3 = 0;
				foreach (Clan clan in ((Kingdom)factionToEvaluateFor).Clans)
				{
					float num4 = ((clan.Leader != null) ? ((clan.Leader.Gold < 50000) ? (1f + 0.5f * ((50000f - (float)clan.Leader.Gold) / 50000f)) : ((clan.Leader.Gold > 200000) ? MathF.Max(0.66f, MathF.Pow(200000f / (float)clan.Leader.Gold, 0.4f)) : 1f)) : 1f);
					num2 += num4;
					num3++;
				}
				float num5 = (num2 + 1f) / ((float)num3 + 1f);
				num /= num5;
			}
			return (int)num;
		}

		// Token: 0x060048C8 RID: 18632 RVA: 0x0016EC44 File Offset: 0x0016CE44
		public override bool IsCompatible(Barterable barterable)
		{
			PeaceBarterable peaceBarterable = barterable as PeaceBarterable;
			return peaceBarterable == null || peaceBarterable.OfferedFaction != base.OriginalOwner.MapFaction;
		}

		// Token: 0x060048C9 RID: 18633 RVA: 0x0016EC73 File Offset: 0x0016CE73
		public override ImageIdentifier GetVisualIdentifier()
		{
			return null;
		}

		// Token: 0x060048CA RID: 18634 RVA: 0x0016EC76 File Offset: 0x0016CE76
		public override string GetEncyclopediaLink()
		{
			return base.OriginalOwner.MapFaction.EncyclopediaLink;
		}

		// Token: 0x060048CB RID: 18635 RVA: 0x0016EC88 File Offset: 0x0016CE88
		public override void Apply()
		{
			if (this.PeaceOfferingFaction.MapFaction.IsAtWarWith(this.OfferedFaction))
			{
				MakePeaceAction.Apply(this.PeaceOfferingFaction.MapFaction, this.OfferedFaction);
				if (PlayerEncounter.Current != null && Hero.OneToOneConversationHero == base.OriginalOwner)
				{
					PlayerEncounter.LeaveEncounter = true;
					PartyBase originalParty = base.OriginalParty;
					bool flag;
					if (originalParty == null)
					{
						flag = null != null;
					}
					else
					{
						MobileParty mobileParty = originalParty.MobileParty;
						flag = ((mobileParty != null) ? mobileParty.Ai.AiBehaviorPartyBase : null) != null;
					}
					if (flag)
					{
						LocatableSearchData<MobileParty> locatableSearchData = Campaign.Current.MobilePartyLocator.StartFindingLocatablesAroundPosition(MobileParty.MainParty.Position.ToVec2(), 5f);
						for (MobileParty mobileParty2 = Campaign.Current.MobilePartyLocator.FindNextLocatable(ref locatableSearchData); mobileParty2 != null; mobileParty2 = Campaign.Current.MobilePartyLocator.FindNextLocatable(ref locatableSearchData))
						{
							if (!mobileParty2.IsMainParty && mobileParty2.MapFaction == base.OriginalOwner.MapFaction && (mobileParty2.TargetParty == MobileParty.MainParty || mobileParty2.Ai.AiBehaviorPartyBase == PartyBase.MainParty))
							{
								mobileParty2.SetMoveModeHold();
							}
						}
						if (base.OriginalParty.MobileParty.Army != null && MobileParty.MainParty.Army != base.OriginalParty.MobileParty.Army)
						{
							base.OriginalParty.MobileParty.Army.LeaderParty.SetMoveModeHold();
						}
					}
				}
			}
		}

		// Token: 0x060048CC RID: 18636 RVA: 0x0016EDE6 File Offset: 0x0016CFE6
		internal static void AutoGeneratedStaticCollectObjectsPeaceBarterable(object o, List<object> collectedObjects)
		{
			((PeaceBarterable)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x060048CD RID: 18637 RVA: 0x0016EDF4 File Offset: 0x0016CFF4
		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x0400140F RID: 5135
		public readonly IFaction PeaceOfferingFaction;

		// Token: 0x04001410 RID: 5136
		public readonly IFaction OfferedFaction;
	}
}
