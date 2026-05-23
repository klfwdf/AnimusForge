using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables
{
	// Token: 0x0200047E RID: 1150
	public class DeclareWarBarterable : Barterable
	{
		// Token: 0x17000E55 RID: 3669
		// (get) Token: 0x0600485D RID: 18525 RVA: 0x0016D3FA File Offset: 0x0016B5FA
		public override string StringID
		{
			get
			{
				return "declare_war_barterable";
			}
		}

		// Token: 0x17000E56 RID: 3670
		// (get) Token: 0x0600485E RID: 18526 RVA: 0x0016D401 File Offset: 0x0016B601
		// (set) Token: 0x0600485F RID: 18527 RVA: 0x0016D409 File Offset: 0x0016B609
		public IFaction DeclaringFaction { get; private set; }

		// Token: 0x17000E57 RID: 3671
		// (get) Token: 0x06004860 RID: 18528 RVA: 0x0016D412 File Offset: 0x0016B612
		// (set) Token: 0x06004861 RID: 18529 RVA: 0x0016D41A File Offset: 0x0016B61A
		public IFaction OtherFaction { get; private set; }

		// Token: 0x17000E58 RID: 3672
		// (get) Token: 0x06004862 RID: 18530 RVA: 0x0016D423 File Offset: 0x0016B623
		public override TextObject Name
		{
			get
			{
				TextObject textObject = new TextObject("{=GZwNgIon}Declare war against {OTHER_FACTION}", null);
				textObject.SetTextVariable("OTHER_FACTION", this.OtherFaction.Name);
				return textObject;
			}
		}

		// Token: 0x06004863 RID: 18531 RVA: 0x0016D447 File Offset: 0x0016B647
		public DeclareWarBarterable(IFaction declaringFaction, IFaction otherFaction)
			: base(declaringFaction.Leader, null)
		{
			this.DeclaringFaction = declaringFaction;
			this.OtherFaction = otherFaction;
		}

		// Token: 0x06004864 RID: 18532 RVA: 0x0016D464 File Offset: 0x0016B664
		public override void Apply()
		{
			DeclareWarAction.ApplyByDefault(base.OriginalOwner.MapFaction, this.OtherFaction.MapFaction);
		}

		// Token: 0x06004865 RID: 18533 RVA: 0x0016D484 File Offset: 0x0016B684
		public override int GetUnitValueForFaction(IFaction faction)
		{
			int result = 0;
			Clan evaluatingClan = ((faction is Clan) ? ((Clan)faction) : ((Kingdom)faction).RulingClan);
			if (faction.MapFaction == base.OriginalOwner.MapFaction)
			{
				TextObject textObject;
				result = (int)Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(base.OriginalOwner.MapFaction, this.OtherFaction.MapFaction, evaluatingClan, out textObject, false);
			}
			else if (faction.MapFaction == this.OtherFaction.MapFaction)
			{
				TextObject textObject;
				result = (int)Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(this.OtherFaction.MapFaction, base.OriginalOwner.MapFaction, evaluatingClan, out textObject, false);
			}
			return result;
		}

		// Token: 0x06004866 RID: 18534 RVA: 0x0016D538 File Offset: 0x0016B738
		public override ImageIdentifier GetVisualIdentifier()
		{
			return null;
		}
	}
}
