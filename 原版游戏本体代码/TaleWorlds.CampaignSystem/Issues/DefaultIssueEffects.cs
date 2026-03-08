using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x0200037B RID: 891
	public class DefaultIssueEffects
	{
		// Token: 0x17000C42 RID: 3138
		// (get) Token: 0x060033CE RID: 13262 RVA: 0x000D504C File Offset: 0x000D324C
		private static DefaultIssueEffects Instance
		{
			get
			{
				return Campaign.Current.DefaultIssueEffects;
			}
		}

		// Token: 0x17000C43 RID: 3139
		// (get) Token: 0x060033CF RID: 13263 RVA: 0x000D5058 File Offset: 0x000D3258
		public static IssueEffect SettlementLoyalty
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementLoyalty;
			}
		}

		// Token: 0x17000C44 RID: 3140
		// (get) Token: 0x060033D0 RID: 13264 RVA: 0x000D5064 File Offset: 0x000D3264
		public static IssueEffect SettlementSecurity
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementSecurity;
			}
		}

		// Token: 0x17000C45 RID: 3141
		// (get) Token: 0x060033D1 RID: 13265 RVA: 0x000D5070 File Offset: 0x000D3270
		public static IssueEffect SettlementMilitia
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementMilitia;
			}
		}

		// Token: 0x17000C46 RID: 3142
		// (get) Token: 0x060033D2 RID: 13266 RVA: 0x000D507C File Offset: 0x000D327C
		public static IssueEffect SettlementProsperity
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementProsperity;
			}
		}

		// Token: 0x17000C47 RID: 3143
		// (get) Token: 0x060033D3 RID: 13267 RVA: 0x000D5088 File Offset: 0x000D3288
		public static IssueEffect VillageHearth
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectVillageHearth;
			}
		}

		// Token: 0x17000C48 RID: 3144
		// (get) Token: 0x060033D4 RID: 13268 RVA: 0x000D5094 File Offset: 0x000D3294
		public static IssueEffect SettlementFood
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementFood;
			}
		}

		// Token: 0x17000C49 RID: 3145
		// (get) Token: 0x060033D5 RID: 13269 RVA: 0x000D50A0 File Offset: 0x000D32A0
		public static IssueEffect SettlementTax
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementTax;
			}
		}

		// Token: 0x17000C4A RID: 3146
		// (get) Token: 0x060033D6 RID: 13270 RVA: 0x000D50AC File Offset: 0x000D32AC
		public static IssueEffect SettlementGarrison
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectSettlementGarrison;
			}
		}

		// Token: 0x17000C4B RID: 3147
		// (get) Token: 0x060033D7 RID: 13271 RVA: 0x000D50B8 File Offset: 0x000D32B8
		public static IssueEffect HalfVillageProduction
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectHalfVillageProduction;
			}
		}

		// Token: 0x17000C4C RID: 3148
		// (get) Token: 0x060033D8 RID: 13272 RVA: 0x000D50C4 File Offset: 0x000D32C4
		public static IssueEffect IssueOwnerPower
		{
			get
			{
				return DefaultIssueEffects.Instance._issueEffectIssueOwnerPower;
			}
		}

		// Token: 0x17000C4D RID: 3149
		// (get) Token: 0x060033D9 RID: 13273 RVA: 0x000D50D0 File Offset: 0x000D32D0
		public static IssueEffect ClanInfluence
		{
			get
			{
				return DefaultIssueEffects.Instance._clanInfluence;
			}
		}

		// Token: 0x060033DA RID: 13274 RVA: 0x000D50DC File Offset: 0x000D32DC
		public DefaultIssueEffects()
		{
			this.RegisterAll();
		}

		// Token: 0x060033DB RID: 13275 RVA: 0x000D50EC File Offset: 0x000D32EC
		private void RegisterAll()
		{
			this._issueEffectSettlementLoyalty = this.Create("issue_effect_settlement_loyalty");
			this._issueEffectSettlementSecurity = this.Create("issue_effect_settlement_security");
			this._issueEffectSettlementMilitia = this.Create("issue_effect_settlement_militia");
			this._issueEffectSettlementProsperity = this.Create("issue_effect_settlement_prosperity");
			this._issueEffectVillageHearth = this.Create("issue_effect_village_hearth");
			this._issueEffectSettlementFood = this.Create("issue_effect_settlement_food");
			this._issueEffectSettlementTax = this.Create("issue_effect_settlement_tax");
			this._issueEffectSettlementGarrison = this.Create("issue_effect_settlement_garrison");
			this._issueEffectHalfVillageProduction = this.Create("issue_effect_half_village_production");
			this._issueEffectIssueOwnerPower = this.Create("issue_effect_issue_owner_power");
			this._clanInfluence = this.Create("issue_effect_clan_influence");
			this.InitializeAll();
		}

		// Token: 0x060033DC RID: 13276 RVA: 0x000D51BA File Offset: 0x000D33BA
		private IssueEffect Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<IssueEffect>(new IssueEffect(stringId));
		}

		// Token: 0x060033DD RID: 13277 RVA: 0x000D51D4 File Offset: 0x000D33D4
		private void InitializeAll()
		{
			this._issueEffectSettlementLoyalty.Initialize(new TextObject("{=YO0x7ZAo}Loyalty", null), new TextObject("{=xAWvm25T}Effects settlement's loyalty.", null));
			this._issueEffectSettlementSecurity.Initialize(new TextObject("{=MqCH7R4A}Security", null), new TextObject("{=h117Qj3E}Effects settlement's security.", null));
			this._issueEffectSettlementMilitia.Initialize(new TextObject("{=gsVtO9A7}Militia", null), new TextObject("{=dTmPV82D}Effects settlement's militia.", null));
			this._issueEffectSettlementProsperity.Initialize(new TextObject("{=IagYTD5O}Prosperity", null), new TextObject("{=ETye0JMY}Effects settlement's prosperity.", null));
			this._issueEffectVillageHearth.Initialize(new TextObject("{=f5X5uU0m}Village Hearth", null), new TextObject("{=7TbVhbT9}Effects village's hearth.", null));
			this._issueEffectSettlementFood.Initialize(new TextObject("{=qSi4DlT4}Food", null), new TextObject("{=onDsUkUl}Effects settlement's food.", null));
			this._issueEffectSettlementTax.Initialize(new TextObject("{=2awf1tei}Tax", null), new TextObject("{=q2Ovtr1s}Effects settlement's tax.", null));
			this._issueEffectSettlementGarrison.Initialize(new TextObject("{=jlgjLDo7}Garrison", null), new TextObject("{=WJ7SnBgN}Effects settlement's garrison.", null));
			this._issueEffectHalfVillageProduction.Initialize(new TextObject("{=bGyrPe8c}Production", null), new TextObject("{=arbaXvQf}Effects village's production.", null));
			this._issueEffectIssueOwnerPower.Initialize(new TextObject("{=gGXelWQX}Issue owner power", null), new TextObject("{=tjudHtDB}Effects the power of issue owner in the settlement.", null));
			this._clanInfluence.Initialize(new TextObject("{=KN6khbSl}Clan Influence", null), new TextObject("{=y2aLOwOs}Effects the influence of clan.", null));
		}

		// Token: 0x04000EB9 RID: 3769
		private IssueEffect _issueEffectSettlementGarrison;

		// Token: 0x04000EBA RID: 3770
		private IssueEffect _issueEffectSettlementLoyalty;

		// Token: 0x04000EBB RID: 3771
		private IssueEffect _issueEffectSettlementSecurity;

		// Token: 0x04000EBC RID: 3772
		private IssueEffect _issueEffectSettlementMilitia;

		// Token: 0x04000EBD RID: 3773
		private IssueEffect _issueEffectSettlementProsperity;

		// Token: 0x04000EBE RID: 3774
		private IssueEffect _issueEffectVillageHearth;

		// Token: 0x04000EBF RID: 3775
		private IssueEffect _issueEffectSettlementFood;

		// Token: 0x04000EC0 RID: 3776
		private IssueEffect _issueEffectSettlementTax;

		// Token: 0x04000EC1 RID: 3777
		private IssueEffect _issueEffectHalfVillageProduction;

		// Token: 0x04000EC2 RID: 3778
		private IssueEffect _issueEffectIssueOwnerPower;

		// Token: 0x04000EC3 RID: 3779
		private IssueEffect _clanInfluence;
	}
}
