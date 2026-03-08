using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies
{
	// Token: 0x0200006A RID: 106
	public class KingdomPoliciesVM : KingdomCategoryVM
	{
		// Token: 0x06000864 RID: 2148 RVA: 0x00026134 File Offset: 0x00024334
		public KingdomPoliciesVM(Action<KingdomDecision> forceDecide)
		{
			this._forceDecide = forceDecide;
			this.ActivePolicies = new MBBindingList<KingdomPolicyItemVM>();
			this.OtherPolicies = new MBBindingList<KingdomPolicyItemVM>();
			this.DoneHint = new HintViewModel();
			this._playerKingdom = Hero.MainHero.MapFaction as Kingdom;
			this.ProposalAndDisavowalCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(Clan.PlayerClan);
			base.IsAcceptableItemSelected = false;
			this.RefreshValues();
			this.ExecuteSwitchMode();
		}

		// Token: 0x06000865 RID: 2149 RVA: 0x000261C0 File Offset: 0x000243C0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PoliciesText = GameTexts.FindText("str_policies", null).ToString();
			this.ActivePoliciesText = GameTexts.FindText("str_active_policies", null).ToString();
			this.OtherPoliciesText = GameTexts.FindText("str_other_policies", null).ToString();
			this.ProposeNewPolicyText = GameTexts.FindText("str_propose_new_policy", null).ToString();
			this.DisavowPolicyText = GameTexts.FindText("str_disavow_a_policy", null).ToString();
			base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_policy_selected", null).ToString();
			base.CategoryNameText = new TextObject("{=Sls0KQVn}Elections", null).ToString();
			this.RefreshPolicyList();
		}

		// Token: 0x06000866 RID: 2150 RVA: 0x00026274 File Offset: 0x00024474
		public void SelectPolicy(PolicyObject policy)
		{
			bool flag = false;
			foreach (KingdomPolicyItemVM kingdomPolicyItemVM in this.ActivePolicies)
			{
				if (kingdomPolicyItemVM.Policy == policy)
				{
					this.OnPolicySelect(kingdomPolicyItemVM);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (KingdomPolicyItemVM kingdomPolicyItemVM2 in this.OtherPolicies)
				{
					if (kingdomPolicyItemVM2.Policy == policy)
					{
						this.OnPolicySelect(kingdomPolicyItemVM2);
						flag = true;
						break;
					}
				}
			}
		}

		// Token: 0x06000867 RID: 2151 RVA: 0x0002631C File Offset: 0x0002451C
		private void OnPolicySelect(KingdomPolicyItemVM policy)
		{
			if (this.CurrentSelectedPolicy != policy)
			{
				if (this.CurrentSelectedPolicy != null)
				{
					this.CurrentSelectedPolicy.IsSelected = false;
				}
				this.CurrentSelectedPolicy = policy;
				if (this.CurrentSelectedPolicy != null)
				{
					this.CurrentSelectedPolicy.IsSelected = true;
					this._currentSelectedPolicyObject = policy.Policy;
					this._currentItemsUnresolvedDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision d)
					{
						KingdomPolicyDecision kingdomPolicyDecision;
						return (kingdomPolicyDecision = d as KingdomPolicyDecision) != null && kingdomPolicyDecision.Policy == this._currentSelectedPolicyObject && !d.ShouldBeCancelled();
					});
					if (this._currentItemsUnresolvedDecision != null)
					{
						TextObject hintText;
						this.CanProposeOrDisavowPolicy = this.GetCanProposeOrDisavowPolicyWithReason(true, out hintText);
						this.DoneHint.HintText = hintText;
						this.ProposeOrDisavowText = GameTexts.FindText("str_resolve", null).ToString();
						this.ProposeActionExplanationText = GameTexts.FindText("str_resolve_explanation", null).ToString();
						this.PolicyLikelihood = KingdomPoliciesVM.CalculateLikelihood(policy.Policy);
					}
					else
					{
						float influence = Clan.PlayerClan.Influence;
						int proposalAndDisavowalCost = this.ProposalAndDisavowalCost;
						bool isUnderMercenaryService = Clan.PlayerClan.IsUnderMercenaryService;
						TextObject hintText2;
						this.CanProposeOrDisavowPolicy = this.GetCanProposeOrDisavowPolicyWithReason(false, out hintText2);
						this.DoneHint.HintText = hintText2;
						if (this.IsPolicyActive(policy.Policy))
						{
							this.ProposeActionExplanationText = GameTexts.FindText("str_policy_propose_again_action_explanation", null).SetTextVariable("SUPPORT", KingdomPoliciesVM.CalculateLikelihood(policy.Policy)).ToString();
						}
						else
						{
							this.ProposeActionExplanationText = GameTexts.FindText("str_policy_propose_action_explanation", null).SetTextVariable("SUPPORT", KingdomPoliciesVM.CalculateLikelihood(policy.Policy)).ToString();
						}
						this.ProposeOrDisavowText = ((this._playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose", null).ToString() : GameTexts.FindText("str_policy_enact", null).ToString());
						base.NotificationCount = Clan.PlayerClan.Kingdom.UnresolvedDecisions.Count((KingdomDecision d) => !d.ShouldBeCancelled());
						this.PolicyLikelihood = KingdomPoliciesVM.CalculateLikelihood(policy.Policy);
					}
					GameTexts.SetVariable("NUMBER", this.PolicyLikelihood);
					this.PolicyLikelihoodText = GameTexts.FindText("str_NUMBER_percent", null).ToString();
				}
				base.IsAcceptableItemSelected = this.CurrentSelectedPolicy != null;
			}
		}

		// Token: 0x06000868 RID: 2152 RVA: 0x00026558 File Offset: 0x00024758
		private bool GetCanProposeOrDisavowPolicyWithReason(bool hasUnresolvedDecision, out TextObject disabledReason)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				disabledReason = GameTexts.FindText("str_mercenaries_cannot_propose_policies", null);
				return false;
			}
			if (!hasUnresolvedDecision && Clan.PlayerClan.Influence < (float)this.ProposalAndDisavowalCost)
			{
				disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence", null);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000869 RID: 2153 RVA: 0x000265BC File Offset: 0x000247BC
		public void RefreshPolicyList()
		{
			this.ActivePolicies.Clear();
			this.OtherPolicies.Clear();
			if (this._playerKingdom != null)
			{
				foreach (PolicyObject policy in this._playerKingdom.ActivePolicies)
				{
					this.ActivePolicies.Add(new KingdomPolicyItemVM(policy, new Action<KingdomPolicyItemVM>(this.OnPolicySelect), new Func<PolicyObject, bool>(this.IsPolicyActive)));
				}
				foreach (PolicyObject policy2 in from p in PolicyObject.All
					where !this.IsPolicyActive(p)
					select p)
				{
					this.OtherPolicies.Add(new KingdomPolicyItemVM(policy2, new Action<KingdomPolicyItemVM>(this.OnPolicySelect), new Func<PolicyObject, bool>(this.IsPolicyActive)));
				}
			}
			GameTexts.SetVariable("STR", this.ActivePolicies.Count);
			this.NumOfActivePoliciesText = GameTexts.FindText("str_STR_in_parentheses", null).ToString();
			GameTexts.SetVariable("STR", this.OtherPolicies.Count);
			this.NumOfOtherPoliciesText = GameTexts.FindText("str_STR_in_parentheses", null).ToString();
			this.SetDefaultSelectedPolicy();
		}

		// Token: 0x0600086A RID: 2154 RVA: 0x0002671C File Offset: 0x0002491C
		private bool IsPolicyActive(PolicyObject policy)
		{
			return this._playerKingdom.ActivePolicies.Contains(policy);
		}

		// Token: 0x0600086B RID: 2155 RVA: 0x00026730 File Offset: 0x00024930
		private void SetDefaultSelectedPolicy()
		{
			KingdomPolicyItemVM policy = (this.IsInProposeMode ? this.OtherPolicies.FirstOrDefault<KingdomPolicyItemVM>() : this.ActivePolicies.FirstOrDefault<KingdomPolicyItemVM>());
			this.OnPolicySelect(policy);
		}

		// Token: 0x0600086C RID: 2156 RVA: 0x00026768 File Offset: 0x00024968
		private void ExecuteSwitchMode()
		{
			this.IsInProposeMode = !this.IsInProposeMode;
			this.CurrentActiveModeText = (this.IsInProposeMode ? this.OtherPoliciesText : this.ActivePoliciesText);
			this.CurrentActionText = (this.IsInProposeMode ? this.DisavowPolicyText : this.ProposeNewPolicyText);
			this.SetDefaultSelectedPolicy();
		}

		// Token: 0x0600086D RID: 2157 RVA: 0x000267C4 File Offset: 0x000249C4
		private void ExecuteProposeOrDisavow()
		{
			if (this._currentItemsUnresolvedDecision != null)
			{
				this._forceDecide(this._currentItemsUnresolvedDecision);
				return;
			}
			if (this.CanProposeOrDisavowPolicy)
			{
				KingdomDecision kingdomDecision = new KingdomPolicyDecision(Clan.PlayerClan, this._currentSelectedPolicyObject, this.IsPolicyActive(this._currentSelectedPolicyObject));
				Clan.PlayerClan.Kingdom.AddDecision(kingdomDecision, false);
				this._forceDecide(kingdomDecision);
			}
		}

		// Token: 0x17000268 RID: 616
		// (get) Token: 0x0600086E RID: 2158 RVA: 0x0002682D File Offset: 0x00024A2D
		// (set) Token: 0x0600086F RID: 2159 RVA: 0x00026835 File Offset: 0x00024A35
		[DataSourceProperty]
		public HintViewModel DoneHint
		{
			get
			{
				return this._doneHint;
			}
			set
			{
				if (value != this._doneHint)
				{
					this._doneHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DoneHint");
				}
			}
		}

		// Token: 0x17000269 RID: 617
		// (get) Token: 0x06000870 RID: 2160 RVA: 0x00026853 File Offset: 0x00024A53
		// (set) Token: 0x06000871 RID: 2161 RVA: 0x0002685B File Offset: 0x00024A5B
		[DataSourceProperty]
		public MBBindingList<KingdomPolicyItemVM> ActivePolicies
		{
			get
			{
				return this._activePolicies;
			}
			set
			{
				if (value != this._activePolicies)
				{
					this._activePolicies = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomPolicyItemVM>>(value, "ActivePolicies");
				}
			}
		}

		// Token: 0x1700026A RID: 618
		// (get) Token: 0x06000872 RID: 2162 RVA: 0x00026879 File Offset: 0x00024A79
		// (set) Token: 0x06000873 RID: 2163 RVA: 0x00026881 File Offset: 0x00024A81
		[DataSourceProperty]
		public MBBindingList<KingdomPolicyItemVM> OtherPolicies
		{
			get
			{
				return this._otherPolicies;
			}
			set
			{
				if (value != this._otherPolicies)
				{
					this._otherPolicies = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomPolicyItemVM>>(value, "OtherPolicies");
				}
			}
		}

		// Token: 0x1700026B RID: 619
		// (get) Token: 0x06000874 RID: 2164 RVA: 0x0002689F File Offset: 0x00024A9F
		// (set) Token: 0x06000875 RID: 2165 RVA: 0x000268A7 File Offset: 0x00024AA7
		[DataSourceProperty]
		public KingdomPolicyItemVM CurrentSelectedPolicy
		{
			get
			{
				return this._currentSelectedPolicy;
			}
			set
			{
				if (value != this._currentSelectedPolicy)
				{
					this._currentSelectedPolicy = value;
					base.OnPropertyChangedWithValue<KingdomPolicyItemVM>(value, "CurrentSelectedPolicy");
				}
			}
		}

		// Token: 0x1700026C RID: 620
		// (get) Token: 0x06000876 RID: 2166 RVA: 0x000268C5 File Offset: 0x00024AC5
		// (set) Token: 0x06000877 RID: 2167 RVA: 0x000268CD File Offset: 0x00024ACD
		[DataSourceProperty]
		public bool CanProposeOrDisavowPolicy
		{
			get
			{
				return this._canProposeOrDisavowPolicy;
			}
			set
			{
				if (value != this._canProposeOrDisavowPolicy)
				{
					this._canProposeOrDisavowPolicy = value;
					base.OnPropertyChangedWithValue(value, "CanProposeOrDisavowPolicy");
				}
			}
		}

		// Token: 0x1700026D RID: 621
		// (get) Token: 0x06000878 RID: 2168 RVA: 0x000268EB File Offset: 0x00024AEB
		// (set) Token: 0x06000879 RID: 2169 RVA: 0x000268F3 File Offset: 0x00024AF3
		[DataSourceProperty]
		public int ProposalAndDisavowalCost
		{
			get
			{
				return this._proposalAndDisavowalCost;
			}
			set
			{
				if (value != this._proposalAndDisavowalCost)
				{
					this._proposalAndDisavowalCost = value;
					base.OnPropertyChangedWithValue(value, "ProposalAndDisavowalCost");
				}
			}
		}

		// Token: 0x1700026E RID: 622
		// (get) Token: 0x0600087A RID: 2170 RVA: 0x00026911 File Offset: 0x00024B11
		// (set) Token: 0x0600087B RID: 2171 RVA: 0x00026919 File Offset: 0x00024B19
		[DataSourceProperty]
		public string NumOfActivePoliciesText
		{
			get
			{
				return this._numOfActivePoliciesText;
			}
			set
			{
				if (value != this._numOfActivePoliciesText)
				{
					this._numOfActivePoliciesText = value;
					base.OnPropertyChangedWithValue<string>(value, "NumOfActivePoliciesText");
				}
			}
		}

		// Token: 0x1700026F RID: 623
		// (get) Token: 0x0600087C RID: 2172 RVA: 0x0002693C File Offset: 0x00024B3C
		// (set) Token: 0x0600087D RID: 2173 RVA: 0x00026944 File Offset: 0x00024B44
		[DataSourceProperty]
		public string NumOfOtherPoliciesText
		{
			get
			{
				return this._numOfOtherPoliciesText;
			}
			set
			{
				if (value != this._numOfOtherPoliciesText)
				{
					this._numOfOtherPoliciesText = value;
					base.OnPropertyChangedWithValue<string>(value, "NumOfOtherPoliciesText");
				}
			}
		}

		// Token: 0x17000270 RID: 624
		// (get) Token: 0x0600087E RID: 2174 RVA: 0x00026967 File Offset: 0x00024B67
		// (set) Token: 0x0600087F RID: 2175 RVA: 0x0002696F File Offset: 0x00024B6F
		[DataSourceProperty]
		public bool IsInProposeMode
		{
			get
			{
				return this._isInProposeMode;
			}
			set
			{
				if (value != this._isInProposeMode)
				{
					this._isInProposeMode = value;
					base.OnPropertyChangedWithValue(value, "IsInProposeMode");
				}
			}
		}

		// Token: 0x17000271 RID: 625
		// (get) Token: 0x06000880 RID: 2176 RVA: 0x0002698D File Offset: 0x00024B8D
		// (set) Token: 0x06000881 RID: 2177 RVA: 0x00026995 File Offset: 0x00024B95
		[DataSourceProperty]
		public string DisavowPolicyText
		{
			get
			{
				return this._disavowPolicyText;
			}
			set
			{
				if (value != this._disavowPolicyText)
				{
					this._disavowPolicyText = value;
					base.OnPropertyChangedWithValue<string>(value, "DisavowPolicyText");
				}
			}
		}

		// Token: 0x17000272 RID: 626
		// (get) Token: 0x06000882 RID: 2178 RVA: 0x000269B8 File Offset: 0x00024BB8
		// (set) Token: 0x06000883 RID: 2179 RVA: 0x000269C0 File Offset: 0x00024BC0
		[DataSourceProperty]
		public string CurrentActiveModeText
		{
			get
			{
				return this._currentActiveModeText;
			}
			set
			{
				if (value != this._currentActiveModeText)
				{
					this._currentActiveModeText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentActiveModeText");
				}
			}
		}

		// Token: 0x17000273 RID: 627
		// (get) Token: 0x06000884 RID: 2180 RVA: 0x000269E3 File Offset: 0x00024BE3
		// (set) Token: 0x06000885 RID: 2181 RVA: 0x000269EB File Offset: 0x00024BEB
		[DataSourceProperty]
		public string CurrentActionText
		{
			get
			{
				return this._currentActionText;
			}
			set
			{
				if (value != this._currentActionText)
				{
					this._currentActionText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentActionText");
				}
			}
		}

		// Token: 0x17000274 RID: 628
		// (get) Token: 0x06000886 RID: 2182 RVA: 0x00026A0E File Offset: 0x00024C0E
		// (set) Token: 0x06000887 RID: 2183 RVA: 0x00026A16 File Offset: 0x00024C16
		[DataSourceProperty]
		public string ProposeNewPolicyText
		{
			get
			{
				return this._proposeNewPolicyText;
			}
			set
			{
				if (value != this._proposeNewPolicyText)
				{
					this._proposeNewPolicyText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProposeNewPolicyText");
				}
			}
		}

		// Token: 0x17000275 RID: 629
		// (get) Token: 0x06000888 RID: 2184 RVA: 0x00026A39 File Offset: 0x00024C39
		// (set) Token: 0x06000889 RID: 2185 RVA: 0x00026A41 File Offset: 0x00024C41
		[DataSourceProperty]
		public string BackText
		{
			get
			{
				return this._backText;
			}
			set
			{
				if (value != this._backText)
				{
					this._backText = value;
					base.OnPropertyChangedWithValue<string>(value, "BackText");
				}
			}
		}

		// Token: 0x17000276 RID: 630
		// (get) Token: 0x0600088A RID: 2186 RVA: 0x00026A64 File Offset: 0x00024C64
		// (set) Token: 0x0600088B RID: 2187 RVA: 0x00026A6C File Offset: 0x00024C6C
		[DataSourceProperty]
		public string PoliciesText
		{
			get
			{
				return this._policiesText;
			}
			set
			{
				if (value != this._policiesText)
				{
					this._policiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "PoliciesText");
				}
			}
		}

		// Token: 0x17000277 RID: 631
		// (get) Token: 0x0600088C RID: 2188 RVA: 0x00026A8F File Offset: 0x00024C8F
		// (set) Token: 0x0600088D RID: 2189 RVA: 0x00026A97 File Offset: 0x00024C97
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

		// Token: 0x17000278 RID: 632
		// (get) Token: 0x0600088E RID: 2190 RVA: 0x00026ABA File Offset: 0x00024CBA
		// (set) Token: 0x0600088F RID: 2191 RVA: 0x00026AC2 File Offset: 0x00024CC2
		[DataSourceProperty]
		public string PolicyLikelihoodText
		{
			get
			{
				return this._policyLikelihoodText;
			}
			set
			{
				if (value != this._policyLikelihoodText)
				{
					this._policyLikelihoodText = value;
					base.OnPropertyChangedWithValue<string>(value, "PolicyLikelihoodText");
				}
			}
		}

		// Token: 0x17000279 RID: 633
		// (get) Token: 0x06000890 RID: 2192 RVA: 0x00026AE5 File Offset: 0x00024CE5
		// (set) Token: 0x06000891 RID: 2193 RVA: 0x00026AED File Offset: 0x00024CED
		[DataSourceProperty]
		public HintViewModel LikelihoodHint
		{
			get
			{
				return this._likelihoodHint;
			}
			set
			{
				if (value != this._likelihoodHint)
				{
					this._likelihoodHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LikelihoodHint");
				}
			}
		}

		// Token: 0x1700027A RID: 634
		// (get) Token: 0x06000892 RID: 2194 RVA: 0x00026B0B File Offset: 0x00024D0B
		// (set) Token: 0x06000893 RID: 2195 RVA: 0x00026B13 File Offset: 0x00024D13
		[DataSourceProperty]
		public int PolicyLikelihood
		{
			get
			{
				return this._policyLikelihood;
			}
			set
			{
				if (value != this._policyLikelihood)
				{
					this._policyLikelihood = value;
					base.OnPropertyChangedWithValue(value, "PolicyLikelihood");
				}
			}
		}

		// Token: 0x1700027B RID: 635
		// (get) Token: 0x06000894 RID: 2196 RVA: 0x00026B31 File Offset: 0x00024D31
		// (set) Token: 0x06000895 RID: 2197 RVA: 0x00026B39 File Offset: 0x00024D39
		[DataSourceProperty]
		public string OtherPoliciesText
		{
			get
			{
				return this._otherPoliciesText;
			}
			set
			{
				if (value != this._otherPoliciesText)
				{
					this._otherPoliciesText = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherPoliciesText");
				}
			}
		}

		// Token: 0x1700027C RID: 636
		// (get) Token: 0x06000896 RID: 2198 RVA: 0x00026B5C File Offset: 0x00024D5C
		// (set) Token: 0x06000897 RID: 2199 RVA: 0x00026B64 File Offset: 0x00024D64
		[DataSourceProperty]
		public string ProposeOrDisavowText
		{
			get
			{
				return this._proposeOrDisavowText;
			}
			set
			{
				if (value != this._proposeOrDisavowText)
				{
					this._proposeOrDisavowText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProposeOrDisavowText");
				}
			}
		}

		// Token: 0x1700027D RID: 637
		// (get) Token: 0x06000898 RID: 2200 RVA: 0x00026B87 File Offset: 0x00024D87
		// (set) Token: 0x06000899 RID: 2201 RVA: 0x00026B8F File Offset: 0x00024D8F
		[DataSourceProperty]
		public string ProposeActionExplanationText
		{
			get
			{
				return this._proposeActionExplanationText;
			}
			set
			{
				if (value != this._proposeActionExplanationText)
				{
					this._proposeActionExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProposeActionExplanationText");
				}
			}
		}

		// Token: 0x0600089A RID: 2202 RVA: 0x00026BB2 File Offset: 0x00024DB2
		private static int CalculateLikelihood(PolicyObject policy)
		{
			return MathF.Round(new KingdomElection(new KingdomPolicyDecision(Clan.PlayerClan, policy, Clan.PlayerClan.Kingdom.ActivePolicies.Contains(policy))).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
		}

		// Token: 0x040003A2 RID: 930
		private readonly Action<KingdomDecision> _forceDecide;

		// Token: 0x040003A3 RID: 931
		private readonly Kingdom _playerKingdom;

		// Token: 0x040003A4 RID: 932
		private PolicyObject _currentSelectedPolicyObject;

		// Token: 0x040003A5 RID: 933
		private KingdomDecision _currentItemsUnresolvedDecision;

		// Token: 0x040003A6 RID: 934
		private MBBindingList<KingdomPolicyItemVM> _activePolicies;

		// Token: 0x040003A7 RID: 935
		private MBBindingList<KingdomPolicyItemVM> _otherPolicies;

		// Token: 0x040003A8 RID: 936
		private KingdomPolicyItemVM _currentSelectedPolicy;

		// Token: 0x040003A9 RID: 937
		private bool _canProposeOrDisavowPolicy;

		// Token: 0x040003AA RID: 938
		private bool _isInProposeMode = true;

		// Token: 0x040003AB RID: 939
		private string _proposeOrDisavowText;

		// Token: 0x040003AC RID: 940
		private string _proposeActionExplanationText;

		// Token: 0x040003AD RID: 941
		private string _activePoliciesText;

		// Token: 0x040003AE RID: 942
		private string _otherPoliciesText;

		// Token: 0x040003AF RID: 943
		private string _currentActiveModeText;

		// Token: 0x040003B0 RID: 944
		private string _currentActionText;

		// Token: 0x040003B1 RID: 945
		private string _proposeNewPolicyText;

		// Token: 0x040003B2 RID: 946
		private string _disavowPolicyText;

		// Token: 0x040003B3 RID: 947
		private string _policiesText;

		// Token: 0x040003B4 RID: 948
		private string _backText;

		// Token: 0x040003B5 RID: 949
		private int _proposalAndDisavowalCost;

		// Token: 0x040003B6 RID: 950
		private string _numOfActivePoliciesText;

		// Token: 0x040003B7 RID: 951
		private string _numOfOtherPoliciesText;

		// Token: 0x040003B8 RID: 952
		private HintViewModel _doneHint;

		// Token: 0x040003B9 RID: 953
		private string _policyLikelihoodText;

		// Token: 0x040003BA RID: 954
		private HintViewModel _likelihoodHint;

		// Token: 0x040003BB RID: 955
		private int _policyLikelihood;
	}
}
