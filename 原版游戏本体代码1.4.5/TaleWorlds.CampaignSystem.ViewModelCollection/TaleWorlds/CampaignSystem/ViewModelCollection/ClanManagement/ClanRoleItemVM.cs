using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanRoleItemVM : ViewModel
{
	private class ClanRoleMemberComparer : IComparer<ClanRoleMemberItemVM>
	{
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

	private Action<ClanRoleItemVM> _onRoleSelectionToggled;

	private Action _onRoleAssigned;

	private MBBindingList<ClanPartyMemberItemVM> _heroMembers;

	private MobileParty _party;

	private ClanRoleMemberComparer _comparer;

	private bool _isEnabled;

	private ClanRoleMemberItemVM _clanLeader;

	private MBBindingList<ClanRoleMemberItemVM> _members;

	private ClanRoleMemberItemVM _effectiveOwner;

	private HintViewModel _notAssignedHint;

	private HintViewModel _disabledHint;

	private bool _isNotAssigned;

	private bool _hasEffects;

	private string _roleId;

	private string _name;

	private string _assignedMemberEffects;

	private string _noEffectText;

	public PartyRole Role { get; private set; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public ClanRoleMemberItemVM ClanLeader
	{
		get
		{
			return _clanLeader;
		}
		set
		{
			if (value != _clanLeader)
			{
				_clanLeader = value;
				OnPropertyChangedWithValue(value, "ClanLeader");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanRoleMemberItemVM> Members
	{
		get
		{
			return _members;
		}
		set
		{
			if (value != _members)
			{
				_members = value;
				OnPropertyChangedWithValue(value, "Members");
			}
		}
	}

	[DataSourceProperty]
	public ClanRoleMemberItemVM EffectiveOwner
	{
		get
		{
			return _effectiveOwner;
		}
		set
		{
			if (value != _effectiveOwner)
			{
				_effectiveOwner = value;
				OnPropertyChangedWithValue(value, "EffectiveOwner");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NotAssignedHint
	{
		get
		{
			return _notAssignedHint;
		}
		set
		{
			if (value != _notAssignedHint)
			{
				_notAssignedHint = value;
				OnPropertyChangedWithValue(value, "NotAssignedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNotAssigned
	{
		get
		{
			return _isNotAssigned;
		}
		set
		{
			if (value != _isNotAssigned)
			{
				_isNotAssigned = value;
				OnPropertyChangedWithValue(value, "IsNotAssigned");
			}
		}
	}

	[DataSourceProperty]
	public bool HasEffects
	{
		get
		{
			return _hasEffects;
		}
		set
		{
			if (value != _hasEffects)
			{
				_hasEffects = value;
				OnPropertyChangedWithValue(value, "HasEffects");
			}
		}
	}

	[DataSourceProperty]
	public string RoleId
	{
		get
		{
			return _roleId;
		}
		set
		{
			if (value != _roleId)
			{
				_roleId = value;
				OnPropertyChangedWithValue(value, "RoleId");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string AssignedMemberEffects
	{
		get
		{
			return _assignedMemberEffects;
		}
		set
		{
			if (value != _assignedMemberEffects)
			{
				_assignedMemberEffects = value;
				OnPropertyChangedWithValue(value, "AssignedMemberEffects");
			}
		}
	}

	[DataSourceProperty]
	public string NoEffectText
	{
		get
		{
			return _noEffectText;
		}
		set
		{
			if (value != _noEffectText)
			{
				_noEffectText = value;
				OnPropertyChangedWithValue(value, "NoEffectText");
			}
		}
	}

	public ClanRoleItemVM(MobileParty party, PartyRole role, MBBindingList<ClanPartyMemberItemVM> heroMembers, Action<ClanRoleItemVM> onRoleSelectionToggled, Action onRoleAssigned)
	{
		Role = role;
		_comparer = new ClanRoleMemberComparer();
		_party = party;
		_onRoleSelectionToggled = onRoleSelectionToggled;
		_onRoleAssigned = onRoleAssigned;
		_heroMembers = heroMembers;
		Members = new MBBindingList<ClanRoleMemberItemVM>();
		NotAssignedHint = new HintViewModel(new TextObject("{=S1iS3OYj}Party leader is default for unassigned roles"));
		DisabledHint = new HintViewModel();
		IsEnabled = true;
		RoleId = GetRoleIdentifier(role);
		Refresh();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = GameTexts.FindText("role", Role.ToString()).ToString();
		NoEffectText = GameTexts.FindText("str_clan_role_no_effect").ToString();
		AssignedMemberEffects = EffectiveOwner?.GetEffectsList(Role) ?? "";
		HasEffects = !string.IsNullOrEmpty(AssignedMemberEffects);
		Members.ApplyActionOnAllItems(delegate(ClanRoleMemberItemVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Members.ApplyActionOnAllItems(delegate(ClanRoleMemberItemVM x)
		{
			x.OnFinalize();
		});
	}

	private static string GetRoleIdentifier(PartyRole role)
	{
		return role switch
		{
			PartyRole.Ruler => "rule", 
			PartyRole.ClanLeader => "clan_leader", 
			PartyRole.Governor => "governor", 
			PartyRole.ArmyCommander => "commander", 
			PartyRole.PartyLeader => "party_leader", 
			PartyRole.PartyOwner => "party_owner", 
			PartyRole.Surgeon => "surgeon", 
			PartyRole.Engineer => "engineer", 
			PartyRole.Scout => "scout", 
			PartyRole.Quartermaster => "quartermaser", 
			PartyRole.PartyMember => "member", 
			PartyRole.Personal => "personal", 
			PartyRole.Captain => "captain", 
			PartyRole.FirstMate => "first_mate", 
			PartyRole.Navigator => "navigator", 
			_ => string.Empty, 
		};
	}

	public void Refresh()
	{
		Members.ApplyActionOnAllItems(delegate(ClanRoleMemberItemVM x)
		{
			x.OnFinalize();
		});
		Members.Clear();
		foreach (ClanPartyMemberItemVM heroMember in _heroMembers)
		{
			if (heroMember.IsLeader)
			{
				ClanLeader = new ClanRoleMemberItemVM(_party, Role, heroMember, null);
			}
			else if (Campaign.Current.Models.ClanMemberPartyRoleModel.IsHeroAssignableForPartyRole(heroMember.HeroObject, Role, _party))
			{
				Members.Add(new ClanRoleMemberItemVM(_party, Role, heroMember, OnRoleAssigned));
			}
		}
		Members.Add(new ClanRoleMemberItemVM(_party, Role, null, OnRoleAssigned));
		Members.Sort(_comparer);
		GetMemberAssignedToRole(_party, Role, out var roleOwner, out var effectiveRoleOwner);
		if (effectiveRoleOwner == ClanLeader?.Member.HeroObject)
		{
			EffectiveOwner = ClanLeader;
		}
		else
		{
			for (int num = 0; num < Members.Count; num++)
			{
				ClanRoleMemberItemVM clanRoleMemberItemVM = Members[num];
				if (clanRoleMemberItemVM.Member.HeroObject == effectiveRoleOwner)
				{
					EffectiveOwner = clanRoleMemberItemVM;
					break;
				}
			}
		}
		IsNotAssigned = roleOwner == null;
	}

	public void ExecuteToggleRoleSelection()
	{
		_onRoleSelectionToggled?.Invoke(this);
	}

	private void GetMemberAssignedToRole(MobileParty party, PartyRole role, out Hero roleOwner, out Hero effectiveRoleOwner)
	{
		roleOwner = party.GetRoleHolder(role);
		switch (role)
		{
		case PartyRole.Quartermaster:
			effectiveRoleOwner = party.EffectiveQuartermaster;
			return;
		case PartyRole.Scout:
			effectiveRoleOwner = party.EffectiveScout;
			return;
		case PartyRole.Surgeon:
			effectiveRoleOwner = party.EffectiveSurgeon;
			return;
		case PartyRole.Engineer:
			effectiveRoleOwner = party.EffectiveEngineer;
			return;
		case PartyRole.FirstMate:
			effectiveRoleOwner = party.EffectiveFirstMate;
			return;
		case PartyRole.Navigator:
			effectiveRoleOwner = party.EffectiveNavigator;
			return;
		}
		effectiveRoleOwner = party.LeaderHero;
		roleOwner = party.LeaderHero;
		Debug.FailedAssert("Given party role is not valid.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanRoleItemVM.cs", "GetMemberAssignedToRole", 175);
	}

	private void OnRoleAssigned()
	{
		MBInformationManager.HideInformations();
		_onRoleAssigned?.Invoke();
	}

	public void SetEnabled(bool enabled, TextObject disabledHint)
	{
		IsEnabled = enabled;
		DisabledHint.HintText = disabledHint;
	}
}
