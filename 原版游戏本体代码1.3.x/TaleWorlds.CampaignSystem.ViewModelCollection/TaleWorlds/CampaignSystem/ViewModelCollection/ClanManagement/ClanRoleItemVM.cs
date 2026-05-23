using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200012C RID: 300
	public class ClanRoleItemVM : ViewModel
	{
		// Token: 0x17000985 RID: 2437
		// (get) Token: 0x06001BF8 RID: 7160 RVA: 0x000671DD File Offset: 0x000653DD
		// (set) Token: 0x06001BF9 RID: 7161 RVA: 0x000671E5 File Offset: 0x000653E5
		public PartyRole Role { get; private set; }

		// Token: 0x06001BFA RID: 7162 RVA: 0x000671F0 File Offset: 0x000653F0
		public ClanRoleItemVM(MobileParty party, PartyRole role, MBBindingList<ClanPartyMemberItemVM> heroMembers, Action<ClanRoleItemVM> onRoleSelectionToggled, Action onRoleAssigned)
		{
			this.Role = role;
			this._comparer = new ClanRoleItemVM.ClanRoleMemberComparer();
			this._party = party;
			this._onRoleSelectionToggled = onRoleSelectionToggled;
			this._onRoleAssigned = onRoleAssigned;
			this._heroMembers = heroMembers;
			this.Members = new MBBindingList<ClanRoleMemberItemVM>();
			this.NotAssignedHint = new HintViewModel(new TextObject("{=S1iS3OYj}Party leader is default for unassigned roles", null), null);
			this.DisabledHint = new HintViewModel();
			this.IsEnabled = true;
			this.Refresh();
			this.RefreshValues();
		}

		// Token: 0x06001BFB RID: 7163 RVA: 0x00067274 File Offset: 0x00065474
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = GameTexts.FindText("role", this.Role.ToString()).ToString();
			this.NoEffectText = GameTexts.FindText("str_clan_role_no_effect", null).ToString();
			ClanRoleMemberItemVM effectiveOwner = this.EffectiveOwner;
			this.AssignedMemberEffects = ((effectiveOwner != null) ? effectiveOwner.GetEffectsList(this.Role) : null) ?? "";
			this.HasEffects = !string.IsNullOrEmpty(this.AssignedMemberEffects);
			this.Members.ApplyActionOnAllItems(delegate(ClanRoleMemberItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06001BFC RID: 7164 RVA: 0x0006732B File Offset: 0x0006552B
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.Members.ApplyActionOnAllItems(delegate(ClanRoleMemberItemVM x)
			{
				x.OnFinalize();
			});
		}

		// Token: 0x06001BFD RID: 7165 RVA: 0x00067360 File Offset: 0x00065560
		public void Refresh()
		{
			this.Members.ApplyActionOnAllItems(delegate(ClanRoleMemberItemVM x)
			{
				x.OnFinalize();
			});
			this.Members.Clear();
			foreach (ClanPartyMemberItemVM clanPartyMemberItemVM in this._heroMembers)
			{
				if (ClanRoleMemberItemVM.IsHeroAssignableForRole(clanPartyMemberItemVM.HeroObject, this.Role, this._party))
				{
					this.Members.Add(new ClanRoleMemberItemVM(this._party, this.Role, clanPartyMemberItemVM, new Action(this.OnRoleAssigned)));
				}
			}
			this.Members.Add(new ClanRoleMemberItemVM(this._party, this.Role, null, new Action(this.OnRoleAssigned)));
			this.Members.Sort(this._comparer);
			Hero effectiveRoleOwner;
			Hero hero;
			this.GetMemberAssignedToRole(this._party, this.Role, out hero, out effectiveRoleOwner);
			this.EffectiveOwner = this.Members.FirstOrDefault(delegate(ClanRoleMemberItemVM x)
			{
				ClanPartyMemberItemVM member = x.Member;
				return ((member != null) ? member.HeroObject : null) == effectiveRoleOwner;
			});
			this.IsNotAssigned = hero == null;
		}

		// Token: 0x06001BFE RID: 7166 RVA: 0x000674A0 File Offset: 0x000656A0
		public void ExecuteToggleRoleSelection()
		{
			Action<ClanRoleItemVM> onRoleSelectionToggled = this._onRoleSelectionToggled;
			if (onRoleSelectionToggled == null)
			{
				return;
			}
			onRoleSelectionToggled(this);
		}

		// Token: 0x06001BFF RID: 7167 RVA: 0x000674B4 File Offset: 0x000656B4
		private void GetMemberAssignedToRole(MobileParty party, PartyRole role, out Hero roleOwner, out Hero effectiveRoleOwner)
		{
			roleOwner = party.GetRoleHolder(role);
			switch (role)
			{
			case PartyRole.Surgeon:
				effectiveRoleOwner = party.EffectiveSurgeon;
				return;
			case PartyRole.Engineer:
				effectiveRoleOwner = party.EffectiveEngineer;
				return;
			case PartyRole.Scout:
				effectiveRoleOwner = party.EffectiveScout;
				return;
			case PartyRole.Quartermaster:
				effectiveRoleOwner = party.EffectiveQuartermaster;
				return;
			default:
				effectiveRoleOwner = party.LeaderHero;
				roleOwner = party.LeaderHero;
				Debug.FailedAssert("Given party role is not valid.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanRoleItemVM.cs", "GetMemberAssignedToRole", 107);
				return;
			}
		}

		// Token: 0x06001C00 RID: 7168 RVA: 0x00067533 File Offset: 0x00065733
		private void OnRoleAssigned()
		{
			MBInformationManager.HideInformations();
			Action onRoleAssigned = this._onRoleAssigned;
			if (onRoleAssigned == null)
			{
				return;
			}
			onRoleAssigned();
		}

		// Token: 0x06001C01 RID: 7169 RVA: 0x0006754A File Offset: 0x0006574A
		public void SetEnabled(bool enabled, TextObject disabledHint)
		{
			this.IsEnabled = enabled;
			this.DisabledHint.HintText = disabledHint;
		}

		// Token: 0x17000986 RID: 2438
		// (get) Token: 0x06001C02 RID: 7170 RVA: 0x0006755F File Offset: 0x0006575F
		// (set) Token: 0x06001C03 RID: 7171 RVA: 0x00067567 File Offset: 0x00065767
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000987 RID: 2439
		// (get) Token: 0x06001C04 RID: 7172 RVA: 0x00067585 File Offset: 0x00065785
		// (set) Token: 0x06001C05 RID: 7173 RVA: 0x0006758D File Offset: 0x0006578D
		[DataSourceProperty]
		public MBBindingList<ClanRoleMemberItemVM> Members
		{
			get
			{
				return this._members;
			}
			set
			{
				if (value != this._members)
				{
					this._members = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanRoleMemberItemVM>>(value, "Members");
				}
			}
		}

		// Token: 0x17000988 RID: 2440
		// (get) Token: 0x06001C06 RID: 7174 RVA: 0x000675AB File Offset: 0x000657AB
		// (set) Token: 0x06001C07 RID: 7175 RVA: 0x000675B3 File Offset: 0x000657B3
		[DataSourceProperty]
		public ClanRoleMemberItemVM EffectiveOwner
		{
			get
			{
				return this._effectiveOwner;
			}
			set
			{
				if (value != this._effectiveOwner)
				{
					this._effectiveOwner = value;
					base.OnPropertyChangedWithValue<ClanRoleMemberItemVM>(value, "EffectiveOwner");
				}
			}
		}

		// Token: 0x17000989 RID: 2441
		// (get) Token: 0x06001C08 RID: 7176 RVA: 0x000675D1 File Offset: 0x000657D1
		// (set) Token: 0x06001C09 RID: 7177 RVA: 0x000675D9 File Offset: 0x000657D9
		[DataSourceProperty]
		public HintViewModel NotAssignedHint
		{
			get
			{
				return this._notAssignedHint;
			}
			set
			{
				if (value != this._notAssignedHint)
				{
					this._notAssignedHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "NotAssignedHint");
				}
			}
		}

		// Token: 0x1700098A RID: 2442
		// (get) Token: 0x06001C0A RID: 7178 RVA: 0x000675F7 File Offset: 0x000657F7
		// (set) Token: 0x06001C0B RID: 7179 RVA: 0x000675FF File Offset: 0x000657FF
		[DataSourceProperty]
		public HintViewModel DisabledHint
		{
			get
			{
				return this._disabledHint;
			}
			set
			{
				if (value != this._disabledHint)
				{
					this._disabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisabledHint");
				}
			}
		}

		// Token: 0x1700098B RID: 2443
		// (get) Token: 0x06001C0C RID: 7180 RVA: 0x0006761D File Offset: 0x0006581D
		// (set) Token: 0x06001C0D RID: 7181 RVA: 0x00067625 File Offset: 0x00065825
		[DataSourceProperty]
		public bool IsNotAssigned
		{
			get
			{
				return this._isNotAssigned;
			}
			set
			{
				if (value != this._isNotAssigned)
				{
					this._isNotAssigned = value;
					base.OnPropertyChangedWithValue(value, "IsNotAssigned");
				}
			}
		}

		// Token: 0x1700098C RID: 2444
		// (get) Token: 0x06001C0E RID: 7182 RVA: 0x00067643 File Offset: 0x00065843
		// (set) Token: 0x06001C0F RID: 7183 RVA: 0x0006764B File Offset: 0x0006584B
		[DataSourceProperty]
		public bool HasEffects
		{
			get
			{
				return this._hasEffects;
			}
			set
			{
				if (value != this._hasEffects)
				{
					this._hasEffects = value;
					base.OnPropertyChangedWithValue(value, "HasEffects");
				}
			}
		}

		// Token: 0x1700098D RID: 2445
		// (get) Token: 0x06001C10 RID: 7184 RVA: 0x00067669 File Offset: 0x00065869
		// (set) Token: 0x06001C11 RID: 7185 RVA: 0x00067671 File Offset: 0x00065871
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

		// Token: 0x1700098E RID: 2446
		// (get) Token: 0x06001C12 RID: 7186 RVA: 0x00067694 File Offset: 0x00065894
		// (set) Token: 0x06001C13 RID: 7187 RVA: 0x0006769C File Offset: 0x0006589C
		[DataSourceProperty]
		public string AssignedMemberEffects
		{
			get
			{
				return this._assignedMemberEffects;
			}
			set
			{
				if (value != this._assignedMemberEffects)
				{
					this._assignedMemberEffects = value;
					base.OnPropertyChangedWithValue<string>(value, "AssignedMemberEffects");
				}
			}
		}

		// Token: 0x1700098F RID: 2447
		// (get) Token: 0x06001C14 RID: 7188 RVA: 0x000676BF File Offset: 0x000658BF
		// (set) Token: 0x06001C15 RID: 7189 RVA: 0x000676C7 File Offset: 0x000658C7
		[DataSourceProperty]
		public string NoEffectText
		{
			get
			{
				return this._noEffectText;
			}
			set
			{
				if (value != this._noEffectText)
				{
					this._noEffectText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoEffectText");
				}
			}
		}

		// Token: 0x04000D0D RID: 3341
		private Action<ClanRoleItemVM> _onRoleSelectionToggled;

		// Token: 0x04000D0E RID: 3342
		private Action _onRoleAssigned;

		// Token: 0x04000D0F RID: 3343
		private MBBindingList<ClanPartyMemberItemVM> _heroMembers;

		// Token: 0x04000D10 RID: 3344
		private MobileParty _party;

		// Token: 0x04000D11 RID: 3345
		private ClanRoleItemVM.ClanRoleMemberComparer _comparer;

		// Token: 0x04000D12 RID: 3346
		private bool _isEnabled;

		// Token: 0x04000D13 RID: 3347
		private MBBindingList<ClanRoleMemberItemVM> _members;

		// Token: 0x04000D14 RID: 3348
		private ClanRoleMemberItemVM _effectiveOwner;

		// Token: 0x04000D15 RID: 3349
		private HintViewModel _notAssignedHint;

		// Token: 0x04000D16 RID: 3350
		private HintViewModel _disabledHint;

		// Token: 0x04000D17 RID: 3351
		private bool _isNotAssigned;

		// Token: 0x04000D18 RID: 3352
		private bool _hasEffects;

		// Token: 0x04000D19 RID: 3353
		private string _name;

		// Token: 0x04000D1A RID: 3354
		private string _assignedMemberEffects;

		// Token: 0x04000D1B RID: 3355
		private string _noEffectText;

		// Token: 0x02000286 RID: 646
		private class ClanRoleMemberComparer : IComparer<ClanRoleMemberItemVM>
		{
			// Token: 0x0600259C RID: 9628 RVA: 0x00080DA8 File Offset: 0x0007EFA8
			public int Compare(ClanRoleMemberItemVM x, ClanRoleMemberItemVM y)
			{
				int num = y.RelevantSkillValue.CompareTo(x.RelevantSkillValue);
				if (num == 0)
				{
					return x.Member.HeroObject.Name.ToString().CompareTo(y.Member.HeroObject.Name.ToString());
				}
				return num;
			}
		}
	}
}
