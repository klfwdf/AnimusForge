using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x02000080 RID: 128
	public class PolicyDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x17000341 RID: 833
		// (get) Token: 0x06000A93 RID: 2707 RVA: 0x0002D9AC File Offset: 0x0002BBAC
		public KingdomPolicyDecision PolicyDecision
		{
			get
			{
				KingdomPolicyDecision result;
				if ((result = this._policyDecision) == null)
				{
					result = (this._policyDecision = this._decision as KingdomPolicyDecision);
				}
				return result;
			}
		}

		// Token: 0x17000342 RID: 834
		// (get) Token: 0x06000A94 RID: 2708 RVA: 0x0002D9D7 File Offset: 0x0002BBD7
		public PolicyObject Policy
		{
			get
			{
				return this.PolicyDecision.Policy;
			}
		}

		// Token: 0x06000A95 RID: 2709 RVA: 0x0002D9E4 File Offset: 0x0002BBE4
		public PolicyDecisionItemVM(KingdomPolicyDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			base.DecisionType = 3;
		}

		// Token: 0x06000A96 RID: 2710 RVA: 0x0002D9F8 File Offset: 0x0002BBF8
		protected override void InitValues()
		{
			base.InitValues();
			base.DecisionType = 3;
			this.NameText = this.Policy.Name.ToString();
			this.PolicyDescriptionText = this.Policy.Description.ToString();
			this.PolicyEffectList = new MBBindingList<StringItemWithHintVM>();
			foreach (string text in this.Policy.SecondaryEffects.ToString().Split(new char[] { '\n' }))
			{
				this.PolicyEffectList.Add(new StringItemWithHintVM(text, TextObject.GetEmpty()));
			}
		}

		// Token: 0x17000343 RID: 835
		// (get) Token: 0x06000A97 RID: 2711 RVA: 0x0002DA92 File Offset: 0x0002BC92
		// (set) Token: 0x06000A98 RID: 2712 RVA: 0x0002DA9A File Offset: 0x0002BC9A
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

		// Token: 0x17000344 RID: 836
		// (get) Token: 0x06000A99 RID: 2713 RVA: 0x0002DABD File Offset: 0x0002BCBD
		// (set) Token: 0x06000A9A RID: 2714 RVA: 0x0002DAC5 File Offset: 0x0002BCC5
		[DataSourceProperty]
		public string PolicyDescriptionText
		{
			get
			{
				return this._policyDescriptionText;
			}
			set
			{
				if (value != this._policyDescriptionText)
				{
					this._policyDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "PolicyDescriptionText");
				}
			}
		}

		// Token: 0x17000345 RID: 837
		// (get) Token: 0x06000A9B RID: 2715 RVA: 0x0002DAE8 File Offset: 0x0002BCE8
		// (set) Token: 0x06000A9C RID: 2716 RVA: 0x0002DAF0 File Offset: 0x0002BCF0
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

		// Token: 0x040004AD RID: 1197
		private KingdomPolicyDecision _policyDecision;

		// Token: 0x040004AE RID: 1198
		private MBBindingList<StringItemWithHintVM> _policyEffectList;

		// Token: 0x040004AF RID: 1199
		private string _nameText;

		// Token: 0x040004B0 RID: 1200
		private string _policyDescriptionText;
	}
}
