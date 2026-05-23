using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies
{
	// Token: 0x0200006B RID: 107
	public class KingdomPolicyItemVM : KingdomItemVM
	{
		// Token: 0x0600089D RID: 2205 RVA: 0x00026C2C File Offset: 0x00024E2C
		public KingdomPolicyItemVM(PolicyObject policy, Action<KingdomPolicyItemVM> onSelect, Func<PolicyObject, bool> getIsPolicyActive)
		{
			this._onSelect = onSelect;
			this._policy = policy;
			this._getIsPolicyActive = getIsPolicyActive;
			this.Name = policy.Name.ToString();
			this.Explanation = policy.Description.ToString();
			this.LikelihoodHint = new HintViewModel();
			this.PolicyEffectList = new MBBindingList<StringItemWithHintVM>();
			foreach (string text in policy.SecondaryEffects.ToString().Split(new char[] { '\n' }))
			{
				this.PolicyEffectList.Add(new StringItemWithHintVM(text, TextObject.GetEmpty()));
			}
			this.RefreshValues();
		}

		// Token: 0x0600089E RID: 2206 RVA: 0x00026CD8 File Offset: 0x00024ED8
		public override void RefreshValues()
		{
			base.RefreshValues();
			Func<PolicyObject, bool> getIsPolicyActive = this._getIsPolicyActive;
			this.PolicyAcceptanceText = ((getIsPolicyActive != null && getIsPolicyActive(this.Policy)) ? GameTexts.FindText("str_policy_support_for_abolishing", null).ToString() : GameTexts.FindText("str_policy_support_for_enacting", null).ToString());
		}

		// Token: 0x0600089F RID: 2207 RVA: 0x00026D30 File Offset: 0x00024F30
		private void DeterminePolicyLikelihood()
		{
			float likelihoodForSponsor = new KingdomElection(new KingdomPolicyDecision(Clan.PlayerClan, this._policy, false)).GetLikelihoodForSponsor(Clan.PlayerClan);
			this.PolicyLikelihood = MathF.Round(likelihoodForSponsor * 100f);
			GameTexts.SetVariable("NUMBER", this.PolicyLikelihood);
			this.PolicyLikelihoodText = GameTexts.FindText("str_NUMBER_percent", null).ToString();
		}

		// Token: 0x060008A0 RID: 2208 RVA: 0x00026D96 File Offset: 0x00024F96
		protected override void OnSelect()
		{
			base.OnSelect();
			this._onSelect(this);
		}

		// Token: 0x1700027E RID: 638
		// (get) Token: 0x060008A1 RID: 2209 RVA: 0x00026DAA File Offset: 0x00024FAA
		// (set) Token: 0x060008A2 RID: 2210 RVA: 0x00026DB2 File Offset: 0x00024FB2
		[DataSourceProperty]
		public string PolicyAcceptanceText
		{
			get
			{
				return this._policyAcceptanceText;
			}
			set
			{
				if (value != this._policyAcceptanceText)
				{
					this._policyAcceptanceText = value;
					base.OnPropertyChangedWithValue<string>(value, "PolicyAcceptanceText");
				}
			}
		}

		// Token: 0x1700027F RID: 639
		// (get) Token: 0x060008A3 RID: 2211 RVA: 0x00026DD5 File Offset: 0x00024FD5
		// (set) Token: 0x060008A4 RID: 2212 RVA: 0x00026DDD File Offset: 0x00024FDD
		[DataSourceProperty]
		public MBBindingList<StringItemWithHintVM> PolicyEffectList
		{
			get
			{
				return this._policyEffectList;
			}
			set
			{
				if (value != this._policyEffectList)
				{
					this._policyEffectList = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringItemWithHintVM>>(value, "PolicyEffectList");
				}
			}
		}

		// Token: 0x17000280 RID: 640
		// (get) Token: 0x060008A5 RID: 2213 RVA: 0x00026DFB File Offset: 0x00024FFB
		// (set) Token: 0x060008A6 RID: 2214 RVA: 0x00026E03 File Offset: 0x00025003
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

		// Token: 0x17000281 RID: 641
		// (get) Token: 0x060008A7 RID: 2215 RVA: 0x00026E26 File Offset: 0x00025026
		// (set) Token: 0x060008A8 RID: 2216 RVA: 0x00026E2E File Offset: 0x0002502E
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

		// Token: 0x17000282 RID: 642
		// (get) Token: 0x060008A9 RID: 2217 RVA: 0x00026E4C File Offset: 0x0002504C
		// (set) Token: 0x060008AA RID: 2218 RVA: 0x00026E54 File Offset: 0x00025054
		[DataSourceProperty]
		public PolicyObject Policy
		{
			get
			{
				return this._policy;
			}
			set
			{
				if (value != this._policy)
				{
					this._policy = value;
					base.OnPropertyChangedWithValue<PolicyObject>(value, "Policy");
				}
			}
		}

		// Token: 0x17000283 RID: 643
		// (get) Token: 0x060008AB RID: 2219 RVA: 0x00026E72 File Offset: 0x00025072
		// (set) Token: 0x060008AC RID: 2220 RVA: 0x00026E7A File Offset: 0x0002507A
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

		// Token: 0x17000284 RID: 644
		// (get) Token: 0x060008AD RID: 2221 RVA: 0x00026E98 File Offset: 0x00025098
		// (set) Token: 0x060008AE RID: 2222 RVA: 0x00026EA0 File Offset: 0x000250A0
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000285 RID: 645
		// (get) Token: 0x060008AF RID: 2223 RVA: 0x00026EC3 File Offset: 0x000250C3
		// (set) Token: 0x060008B0 RID: 2224 RVA: 0x00026ECB File Offset: 0x000250CB
		[DataSourceProperty]
		public string Explanation
		{
			get
			{
				return this._explanation;
			}
			set
			{
				if (value != this._explanation)
				{
					this._explanation = value;
					base.OnPropertyChangedWithValue<string>(value, "Explanation");
				}
			}
		}

		// Token: 0x040003BC RID: 956
		private readonly Action<KingdomPolicyItemVM> _onSelect;

		// Token: 0x040003BD RID: 957
		private readonly Func<PolicyObject, bool> _getIsPolicyActive;

		// Token: 0x040003BE RID: 958
		private string _name;

		// Token: 0x040003BF RID: 959
		private string _explanation;

		// Token: 0x040003C0 RID: 960
		private string _policyAcceptanceText;

		// Token: 0x040003C1 RID: 961
		private PolicyObject _policy;

		// Token: 0x040003C2 RID: 962
		private int _policyLikelihood;

		// Token: 0x040003C3 RID: 963
		private string _policyLikelihoodText;

		// Token: 0x040003C4 RID: 964
		private HintViewModel _likelihoodHint;

		// Token: 0x040003C5 RID: 965
		private MBBindingList<StringItemWithHintVM> _policyEffectList;
	}
}
