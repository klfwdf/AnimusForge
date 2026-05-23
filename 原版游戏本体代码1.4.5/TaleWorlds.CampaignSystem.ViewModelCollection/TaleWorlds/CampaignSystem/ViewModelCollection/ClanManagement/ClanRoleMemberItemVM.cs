using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanRoleMemberItemVM : ViewModel
{
	private Action _onRoleAssigned;

	private MobileParty _party;

	private readonly IEnumerable<SkillEffect> _skillEffects;

	private readonly IEnumerable<PerkObject> _perks;

	private ClanPartyMemberItemVM _member;

	private HintViewModel _hint;

	private bool _isRemoveAssigneeOption;

	public PartyRole Role { get; private set; }

	public SkillObject RelevantSkill { get; private set; }

	public int RelevantSkillValue { get; private set; }

	[DataSourceProperty]
	public ClanPartyMemberItemVM Member
	{
		get
		{
			return _member;
		}
		set
		{
			if (value != _member)
			{
				_member = value;
				OnPropertyChangedWithValue(value, "Member");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRemoveAssigneeOption
	{
		get
		{
			return _isRemoveAssigneeOption;
		}
		set
		{
			if (value != _isRemoveAssigneeOption)
			{
				_isRemoveAssigneeOption = value;
				OnPropertyChangedWithValue(value, "IsRemoveAssigneeOption");
			}
		}
	}

	public ClanRoleMemberItemVM(MobileParty party, PartyRole role, ClanPartyMemberItemVM member, Action onRoleAssigned)
	{
		Role = role;
		Member = member;
		_party = party;
		_onRoleAssigned = onRoleAssigned;
		RelevantSkill = Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(role);
		RelevantSkillValue = Member?.HeroObject?.GetSkillValue(RelevantSkill) ?? (-1);
		_skillEffects = SkillEffect.All.Where((SkillEffect x) => x.Role != PartyRole.Personal);
		_perks = PerkObject.All.Where((PerkObject x) => Member.HeroObject.GetPerkValue(x));
		IsRemoveAssigneeOption = Member == null;
		Hint = new HintViewModel(IsRemoveAssigneeOption ? new TextObject("{=bfWlTVjs}Remove assignee") : GetRoleHint(Role));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}

	public void ExecuteAssignHeroToRole()
	{
		if (Member == null)
		{
			switch (Role)
			{
			case PartyRole.Quartermaster:
				_party.SetPartyQuartermaster(null);
				break;
			case PartyRole.Scout:
				_party.SetPartyScout(null);
				break;
			case PartyRole.Surgeon:
				_party.SetPartySurgeon(null);
				break;
			case PartyRole.Engineer:
				_party.SetPartyEngineer(null);
				break;
			case PartyRole.FirstMate:
				_party.SetPartyFirstMate(null);
				break;
			case PartyRole.Navigator:
				_party.SetPartyNavigator(null);
				break;
			}
		}
		else
		{
			OnSetMemberAsRole(Role);
		}
		_onRoleAssigned?.Invoke();
	}

	private void OnSetMemberAsRole(PartyRole role)
	{
		if (role != PartyRole.None)
		{
			if (!_party.GetHeroPartyRoles(Member.HeroObject).Contains(role))
			{
				switch (role)
				{
				case PartyRole.Engineer:
					_party.SetPartyEngineer(Member.HeroObject);
					break;
				case PartyRole.Quartermaster:
					_party.SetPartyQuartermaster(Member.HeroObject);
					break;
				case PartyRole.Scout:
					_party.SetPartyScout(Member.HeroObject);
					break;
				case PartyRole.Surgeon:
					_party.SetPartySurgeon(Member.HeroObject);
					break;
				case PartyRole.FirstMate:
					_party.SetPartyFirstMate(Member.HeroObject);
					break;
				case PartyRole.Navigator:
					_party.SetPartyNavigator(Member.HeroObject);
					break;
				}
				Game.Current?.EventManager.TriggerEvent(new ClanRoleAssignedThroughClanScreenEvent(role, Member.HeroObject));
			}
		}
		else if (role == PartyRole.None)
		{
			_party.RemoveOnePartyRoleOfHero(Member.HeroObject);
		}
		_onRoleAssigned?.Invoke();
	}

	private TextObject GetRoleHint(PartyRole role)
	{
		string text = "";
		if (RelevantSkillValue <= 0)
		{
			GameTexts.SetVariable("LEFT", RelevantSkill.Name.ToString());
			GameTexts.SetVariable("RIGHT", RelevantSkillValue.ToString());
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_colon_RIGHT").ToString());
			GameTexts.SetVariable("STR1", Member.Name.ToString());
			text = GameTexts.FindText("str_string_newline_string").ToString();
		}
		else if (!Campaign.Current.Models.ClanMemberPartyRoleModel.DoesHeroHaveEnoughSkillForPartyRole(Member.HeroObject, role, _party))
		{
			GameTexts.SetVariable("SKILL_NAME", RelevantSkill.Name.ToString());
			GameTexts.SetVariable("MIN_SKILL_AMOUNT", 0);
			text = GameTexts.FindText("str_character_role_disabled_tooltip").ToString();
		}
		else if (!role.Equals(PartyRole.None))
		{
			IEnumerable<SkillEffect> enumerable = _skillEffects.Where((SkillEffect x) => x.Role == role);
			IEnumerable<PerkObject> enumerable2 = _perks.Where((PerkObject x) => x.PrimaryRole == role || x.SecondaryRole == role);
			GameTexts.SetVariable("LEFT", RelevantSkill.Name.ToString());
			GameTexts.SetVariable("RIGHT", RelevantSkillValue.ToString());
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_colon_RIGHT").ToString());
			GameTexts.SetVariable("STR1", Member.Name.ToString());
			text = GameTexts.FindText("str_string_newline_string").ToString();
			int num = 0;
			TextObject textObject = GameTexts.FindText("str_LEFT_colon_RIGHT").CopyTextObject();
			textObject.SetTextVariable("LEFT", new TextObject("{=Avy8Gua1}Perks"));
			textObject.SetTextVariable("RIGHT", new TextObject("{=Gp2vmZGZ}{PERKS}"));
			foreach (PerkObject item in enumerable2)
			{
				if (num == 0)
				{
					GameTexts.SetVariable("PERKS", item.Name.ToString());
				}
				else
				{
					GameTexts.SetVariable("RIGHT", item.Name.ToString());
					GameTexts.SetVariable("LEFT", new TextObject("{=Gp2vmZGZ}{PERKS}").ToString());
					GameTexts.SetVariable("PERKS", GameTexts.FindText("str_LEFT_comma_RIGHT").ToString());
				}
				num++;
			}
			GameTexts.SetVariable("newline", "\n \n");
			if (num > 0)
			{
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", textObject.ToString());
				text = GameTexts.FindText("str_string_newline_string").ToString();
			}
			if (enumerable.Any())
			{
				GameTexts.SetVariable("LEFT", new TextObject("{=DKJIp6xG}Effects").ToString());
				string content = GameTexts.FindText("str_LEFT_colon").ToString();
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", content);
				text = GameTexts.FindText("str_string_newline_string").ToString();
				GameTexts.SetVariable("newline", "\n");
				foreach (SkillEffect item2 in enumerable)
				{
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", SkillHelper.GetEffectDescriptionForSkillLevel(item2, RelevantSkillValue).ToString());
					text = GameTexts.FindText("str_string_newline_string").ToString();
				}
			}
		}
		else
		{
			text = null;
		}
		if (!string.IsNullOrEmpty(text))
		{
			return new TextObject("{=!}" + text);
		}
		return TextObject.GetEmpty();
	}

	public string GetEffectsList(PartyRole role)
	{
		string text = "";
		IEnumerable<SkillEffect> enumerable = _skillEffects.Where((SkillEffect x) => x.Role == role);
		int num = 0;
		if (RelevantSkillValue > 0)
		{
			foreach (SkillEffect item in enumerable)
			{
				if (num == 0)
				{
					text = SkillHelper.GetEffectDescriptionForSkillLevel(item, RelevantSkillValue).ToString();
				}
				else
				{
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", SkillHelper.GetEffectDescriptionForSkillLevel(item, RelevantSkillValue).ToString());
					text = GameTexts.FindText("str_string_newline_string").ToString();
				}
				num++;
			}
		}
		return text;
	}
}
