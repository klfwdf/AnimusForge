using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x0200007E RID: 126
	public class KingSelectionDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x17000329 RID: 809
		// (get) Token: 0x06000A61 RID: 2657 RVA: 0x0002CE29 File Offset: 0x0002B029
		public IFaction TargetFaction
		{
			get
			{
				return (this._decision as KingSelectionKingdomDecision).Kingdom;
			}
		}

		// Token: 0x06000A62 RID: 2658 RVA: 0x0002CE3B File Offset: 0x0002B03B
		public KingSelectionDecisionItemVM(KingSelectionKingdomDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			this._kingSelectionDecision = decision;
			base.DecisionType = 6;
		}

		// Token: 0x06000A63 RID: 2659 RVA: 0x0002CE54 File Offset: 0x0002B054
		protected override void InitValues()
		{
			base.InitValues();
			TextObject textObject = GameTexts.FindText("str_kingdom_decision_king_selection", null);
			textObject.SetTextVariable("FACTION", this.TargetFaction.Name);
			this.NameText = textObject.ToString();
			this.FactionBanner = new BannerImageIdentifierVM(this.TargetFaction.Banner, true);
			this.FactionName = this.TargetFaction.Culture.Name.ToString();
			bool flag = true;
			bool flag2 = true;
			int num = 0;
			int num2 = 0;
			foreach (Settlement settlement in this.TargetFaction.Settlements)
			{
				if (settlement.IsTown)
				{
					if (flag)
					{
						this.SettlementsListText = settlement.EncyclopediaLinkWithName.ToString();
						flag = false;
					}
					else
					{
						GameTexts.SetVariable("LEFT", this.SettlementsListText);
						GameTexts.SetVariable("RIGHT", settlement.EncyclopediaLinkWithName);
						this.SettlementsListText = GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
					}
					num++;
				}
				else if (settlement.IsCastle)
				{
					if (flag2)
					{
						this.CastlesListText = settlement.EncyclopediaLinkWithName.ToString();
						flag2 = false;
					}
					else
					{
						GameTexts.SetVariable("LEFT", this.CastlesListText);
						GameTexts.SetVariable("RIGHT", settlement.EncyclopediaLinkWithName);
						this.CastlesListText = GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
					}
					num2++;
				}
			}
			TextObject variable = GameTexts.FindText("str_settlements", null);
			TextObject textObject2 = GameTexts.FindText("str_STR_in_parentheses", null);
			textObject2.SetTextVariable("STR", num);
			TextObject textObject3 = GameTexts.FindText("str_LEFT_RIGHT", null);
			textObject3.SetTextVariable("LEFT", variable);
			textObject3.SetTextVariable("RIGHT", textObject2);
			this.SettlementsText = textObject3.ToString();
			TextObject variable2 = GameTexts.FindText("str_castles", null);
			TextObject textObject4 = GameTexts.FindText("str_STR_in_parentheses", null);
			textObject4.SetTextVariable("STR", num2);
			TextObject textObject5 = GameTexts.FindText("str_LEFT_RIGHT", null);
			textObject5.SetTextVariable("LEFT", variable2);
			textObject5.SetTextVariable("RIGHT", textObject4);
			this.CastlesText = textObject5.ToString();
			this.TotalStrengthText = GameTexts.FindText("str_total_strength", null).ToString();
			this.TotalStrength = (int)this.TargetFaction.CurrentTotalStrength;
			this.ActivePoliciesText = GameTexts.FindText("str_active_policies", null).ToString();
			Kingdom kingdom = this.TargetFaction as Kingdom;
			foreach (PolicyObject policyObject in kingdom.ActivePolicies)
			{
				if (policyObject == kingdom.ActivePolicies[0])
				{
					this.ActivePoliciesListText = policyObject.Name.ToString();
				}
				else
				{
					GameTexts.SetVariable("LEFT", this.ActivePoliciesListText);
					GameTexts.SetVariable("RIGHT", policyObject.Name.ToString());
					this.ActivePoliciesListText = GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
				}
			}
		}

		// Token: 0x06000A64 RID: 2660 RVA: 0x0002D184 File Offset: 0x0002B384
		private void ExecuteLocationLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x1700032A RID: 810
		// (get) Token: 0x06000A65 RID: 2661 RVA: 0x0002D196 File Offset: 0x0002B396
		// (set) Token: 0x06000A66 RID: 2662 RVA: 0x0002D19E File Offset: 0x0002B39E
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x1700032B RID: 811
		// (get) Token: 0x06000A67 RID: 2663 RVA: 0x0002D1C1 File Offset: 0x0002B3C1
		// (set) Token: 0x06000A68 RID: 2664 RVA: 0x0002D1C9 File Offset: 0x0002B3C9
		[DataSourceProperty]
		public string FactionName
		{
			get
			{
				return this._factionName;
			}
			set
			{
				if (value != this._factionName)
				{
					this._factionName = value;
					base.OnPropertyChangedWithValue<string>(value, "FactionName");
				}
			}
		}

		// Token: 0x1700032C RID: 812
		// (get) Token: 0x06000A69 RID: 2665 RVA: 0x0002D1EC File Offset: 0x0002B3EC
		// (set) Token: 0x06000A6A RID: 2666 RVA: 0x0002D1F4 File Offset: 0x0002B3F4
		[DataSourceProperty]
		public BannerImageIdentifierVM FactionBanner
		{
			get
			{
				return this._factionBanner;
			}
			set
			{
				if (value != this._factionBanner)
				{
					this._factionBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "FactionBanner");
				}
			}
		}

		// Token: 0x1700032D RID: 813
		// (get) Token: 0x06000A6B RID: 2667 RVA: 0x0002D212 File Offset: 0x0002B412
		// (set) Token: 0x06000A6C RID: 2668 RVA: 0x0002D21A File Offset: 0x0002B41A
		[DataSourceProperty]
		public string SettlementsText
		{
			get
			{
				return this._settlementsText;
			}
			set
			{
				if (value != this._settlementsText)
				{
					this._settlementsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementsText");
				}
			}
		}

		// Token: 0x1700032E RID: 814
		// (get) Token: 0x06000A6D RID: 2669 RVA: 0x0002D23D File Offset: 0x0002B43D
		// (set) Token: 0x06000A6E RID: 2670 RVA: 0x0002D245 File Offset: 0x0002B445
		[DataSourceProperty]
		public string SettlementsListText
		{
			get
			{
				return this._settlementsListText;
			}
			set
			{
				if (value != this._settlementsListText)
				{
					this._settlementsListText = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementsListText");
				}
			}
		}

		// Token: 0x1700032F RID: 815
		// (get) Token: 0x06000A6F RID: 2671 RVA: 0x0002D268 File Offset: 0x0002B468
		// (set) Token: 0x06000A70 RID: 2672 RVA: 0x0002D270 File Offset: 0x0002B470
		[DataSourceProperty]
		public string CastlesText
		{
			get
			{
				return this._castlesText;
			}
			set
			{
				if (value != this._castlesText)
				{
					this._castlesText = value;
					base.OnPropertyChangedWithValue<string>(value, "CastlesText");
				}
			}
		}

		// Token: 0x17000330 RID: 816
		// (get) Token: 0x06000A71 RID: 2673 RVA: 0x0002D293 File Offset: 0x0002B493
		// (set) Token: 0x06000A72 RID: 2674 RVA: 0x0002D29B File Offset: 0x0002B49B
		[DataSourceProperty]
		public string CastlesListText
		{
			get
			{
				return this._castlesListText;
			}
			set
			{
				if (value != this._castlesListText)
				{
					this._castlesListText = value;
					base.OnPropertyChangedWithValue<string>(value, "CastlesListText");
				}
			}
		}

		// Token: 0x17000331 RID: 817
		// (get) Token: 0x06000A73 RID: 2675 RVA: 0x0002D2BE File Offset: 0x0002B4BE
		// (set) Token: 0x06000A74 RID: 2676 RVA: 0x0002D2C6 File Offset: 0x0002B4C6
		[DataSourceProperty]
		public string TotalStrengthText
		{
			get
			{
				return this._totalStrengthText;
			}
			set
			{
				if (value != this._totalStrengthText)
				{
					this._totalStrengthText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalStrengthText");
				}
			}
		}

		// Token: 0x17000332 RID: 818
		// (get) Token: 0x06000A75 RID: 2677 RVA: 0x0002D2E9 File Offset: 0x0002B4E9
		// (set) Token: 0x06000A76 RID: 2678 RVA: 0x0002D2F1 File Offset: 0x0002B4F1
		[DataSourceProperty]
		public int TotalStrength
		{
			get
			{
				return this._totalStrength;
			}
			set
			{
				if (value != this._totalStrength)
				{
					this._totalStrength = value;
					base.OnPropertyChangedWithValue(value, "TotalStrength");
				}
			}
		}

		// Token: 0x17000333 RID: 819
		// (get) Token: 0x06000A77 RID: 2679 RVA: 0x0002D30F File Offset: 0x0002B50F
		// (set) Token: 0x06000A78 RID: 2680 RVA: 0x0002D317 File Offset: 0x0002B517
		[DataSourceProperty]
		public string ActivePoliciesText
		{
			get
			{
				return this._activePoliciesText;
			}
			set
			{
				if (value != this._activePoliciesText)
				{
					this._activePoliciesText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActivePoliciesText");
				}
			}
		}

		// Token: 0x17000334 RID: 820
		// (get) Token: 0x06000A79 RID: 2681 RVA: 0x0002D33A File Offset: 0x0002B53A
		// (set) Token: 0x06000A7A RID: 2682 RVA: 0x0002D342 File Offset: 0x0002B542
		[DataSourceProperty]
		public string ActivePoliciesListText
		{
			get
			{
				return this._activePoliciesListText;
			}
			set
			{
				if (value != this._activePoliciesListText)
				{
					this._activePoliciesListText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActivePoliciesListText");
				}
			}
		}

		// Token: 0x04000496 RID: 1174
		private readonly KingSelectionKingdomDecision _kingSelectionDecision;

		// Token: 0x04000497 RID: 1175
		private string _nameText;

		// Token: 0x04000498 RID: 1176
		private string _factionName;

		// Token: 0x04000499 RID: 1177
		private BannerImageIdentifierVM _factionBanner;

		// Token: 0x0400049A RID: 1178
		private string _settlementsText;

		// Token: 0x0400049B RID: 1179
		private string _settlementsListText;

		// Token: 0x0400049C RID: 1180
		private string _castlesText;

		// Token: 0x0400049D RID: 1181
		private string _castlesListText;

		// Token: 0x0400049E RID: 1182
		private int _totalStrength;

		// Token: 0x0400049F RID: 1183
		private string _totalStrengthText;

		// Token: 0x040004A0 RID: 1184
		private string _activePoliciesText;

		// Token: 0x040004A1 RID: 1185
		private string _activePoliciesListText;
	}
}
