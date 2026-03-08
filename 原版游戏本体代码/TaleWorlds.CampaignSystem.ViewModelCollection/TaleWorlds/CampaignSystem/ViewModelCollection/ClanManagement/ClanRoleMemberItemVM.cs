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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200012D RID: 301
	public class ClanRoleMemberItemVM : ViewModel
	{
		// Token: 0x17000990 RID: 2448
		// (get) Token: 0x06001C16 RID: 7190 RVA: 0x000676EA File Offset: 0x000658EA
		// (set) Token: 0x06001C17 RID: 7191 RVA: 0x000676F2 File Offset: 0x000658F2
		public PartyRole Role { get; private set; }

		// Token: 0x17000991 RID: 2449
		// (get) Token: 0x06001C18 RID: 7192 RVA: 0x000676FB File Offset: 0x000658FB
		// (set) Token: 0x06001C19 RID: 7193 RVA: 0x00067703 File Offset: 0x00065903
		public SkillObject RelevantSkill { get; private set; }

		// Token: 0x17000992 RID: 2450
		// (get) Token: 0x06001C1A RID: 7194 RVA: 0x0006770C File Offset: 0x0006590C
		// (set) Token: 0x06001C1B RID: 7195 RVA: 0x00067714 File Offset: 0x00065914
		public int RelevantSkillValue { get; private set; }

		// Token: 0x06001C1C RID: 7196 RVA: 0x00067720 File Offset: 0x00065920
		public ClanRoleMemberItemVM(MobileParty party, PartyRole role, ClanPartyMemberItemVM member, Action onRoleAssigned)
		{
			this.Role = role;
			this.Member = member;
			this._party = party;
			this._onRoleAssigned = onRoleAssigned;
			this.RelevantSkill = ClanRoleMemberItemVM.GetRelevantSkillForRole(role);
			ClanPartyMemberItemVM member2 = this.Member;
			int? num;
			if (member2 == null)
			{
				num = null;
			}
			else
			{
				Hero heroObject = member2.HeroObject;
				num = ((heroObject != null) ? new int?(heroObject.GetSkillValue(this.RelevantSkill)) : null);
			}
			this.RelevantSkillValue = num ?? (-1);
			this._skillEffects = from x in SkillEffect.All
				where x.Role != PartyRole.Personal
				select x;
			this._perks = from x in PerkObject.All
				where this.Member.HeroObject.GetPerkValue(x)
				select x;
			this.IsRemoveAssigneeOption = this.Member == null;
			this.Hint = new HintViewModel(this.IsRemoveAssigneeOption ? new TextObject("{=bfWlTVjs}Remove assignee", null) : this.GetRoleHint(this.Role), null);
		}

		// Token: 0x06001C1D RID: 7197 RVA: 0x00067836 File Offset: 0x00065A36
		public override void RefreshValues()
		{
			base.RefreshValues();
		}

		// Token: 0x06001C1E RID: 7198 RVA: 0x0006783E File Offset: 0x00065A3E
		public override void OnFinalize()
		{
			base.OnFinalize();
		}

		// Token: 0x06001C1F RID: 7199 RVA: 0x00067848 File Offset: 0x00065A48
		public void ExecuteAssignHeroToRole()
		{
			if (this.Member == null)
			{
				switch (this.Role)
				{
				case PartyRole.Surgeon:
					this._party.SetPartySurgeon(null);
					break;
				case PartyRole.Engineer:
					this._party.SetPartyEngineer(null);
					break;
				case PartyRole.Scout:
					this._party.SetPartyScout(null);
					break;
				case PartyRole.Quartermaster:
					this._party.SetPartyQuartermaster(null);
					break;
				}
			}
			else
			{
				this.OnSetMemberAsRole(this.Role);
			}
			Action onRoleAssigned = this._onRoleAssigned;
			if (onRoleAssigned == null)
			{
				return;
			}
			onRoleAssigned();
		}

		// Token: 0x06001C20 RID: 7200 RVA: 0x000678D4 File Offset: 0x00065AD4
		private void OnSetMemberAsRole(PartyRole role)
		{
			if (role != PartyRole.None)
			{
				if (this._party.GetHeroPartyRole(this.Member.HeroObject) != role)
				{
					this._party.RemoveHeroPartyRole(this.Member.HeroObject);
					if (role == PartyRole.Engineer)
					{
						this._party.SetPartyEngineer(this.Member.HeroObject);
					}
					else if (role == PartyRole.Quartermaster)
					{
						this._party.SetPartyQuartermaster(this.Member.HeroObject);
					}
					else if (role == PartyRole.Scout)
					{
						this._party.SetPartyScout(this.Member.HeroObject);
					}
					else if (role == PartyRole.Surgeon)
					{
						this._party.SetPartySurgeon(this.Member.HeroObject);
					}
					Game game = Game.Current;
					if (game != null)
					{
						game.EventManager.TriggerEvent<ClanRoleAssignedThroughClanScreenEvent>(new ClanRoleAssignedThroughClanScreenEvent(role, this.Member.HeroObject));
					}
				}
			}
			else if (role == PartyRole.None)
			{
				this._party.RemoveHeroPartyRole(this.Member.HeroObject);
			}
			Action onRoleAssigned = this._onRoleAssigned;
			if (onRoleAssigned == null)
			{
				return;
			}
			onRoleAssigned();
		}

		// Token: 0x06001C21 RID: 7201 RVA: 0x000679DC File Offset: 0x00065BDC
		private TextObject GetRoleHint(PartyRole role)
		{
			string text = "";
			if (this.RelevantSkillValue <= 0)
			{
				GameTexts.SetVariable("LEFT", this.RelevantSkill.Name.ToString());
				GameTexts.SetVariable("RIGHT", this.RelevantSkillValue.ToString());
				GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString());
				GameTexts.SetVariable("STR1", this.Member.Name.ToString());
				text = GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			else if (!ClanRoleMemberItemVM.DoesHeroHaveEnoughSkillForRole(this.Member.HeroObject, role, this._party))
			{
				GameTexts.SetVariable("SKILL_NAME", this.RelevantSkill.Name.ToString());
				GameTexts.SetVariable("MIN_SKILL_AMOUNT", 0);
				text = GameTexts.FindText("str_character_role_disabled_tooltip", null).ToString();
			}
			else
			{
				if (!role.Equals(PartyRole.None))
				{
					IEnumerable<SkillEffect> enumerable = from x in this._skillEffects
						where x.Role == role
						select x;
					IEnumerable<PerkObject> enumerable2 = from x in this._perks
						where x.PrimaryRole == role || x.SecondaryRole == role
						select x;
					GameTexts.SetVariable("LEFT", this.RelevantSkill.Name.ToString());
					GameTexts.SetVariable("RIGHT", this.RelevantSkillValue.ToString());
					GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString());
					GameTexts.SetVariable("STR1", this.Member.Name.ToString());
					text = GameTexts.FindText("str_string_newline_string", null).ToString();
					int num = 0;
					TextObject textObject = GameTexts.FindText("str_LEFT_colon_RIGHT", null).CopyTextObject();
					textObject.SetTextVariable("LEFT", new TextObject("{=Avy8Gua1}Perks", null));
					textObject.SetTextVariable("RIGHT", new TextObject("{=Gp2vmZGZ}{PERKS}", null));
					foreach (PerkObject perkObject in enumerable2)
					{
						if (num == 0)
						{
							GameTexts.SetVariable("PERKS", perkObject.Name.ToString());
						}
						else
						{
							GameTexts.SetVariable("RIGHT", perkObject.Name.ToString());
							GameTexts.SetVariable("LEFT", new TextObject("{=Gp2vmZGZ}{PERKS}", null).ToString());
							GameTexts.SetVariable("PERKS", GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString());
						}
						num++;
					}
					GameTexts.SetVariable("newline", "\n \n");
					if (num > 0)
					{
						GameTexts.SetVariable("STR1", text);
						GameTexts.SetVariable("STR2", textObject.ToString());
						text = GameTexts.FindText("str_string_newline_string", null).ToString();
					}
					GameTexts.SetVariable("LEFT", new TextObject("{=DKJIp6xG}Effects", null).ToString());
					string content = GameTexts.FindText("str_LEFT_colon", null).ToString();
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", content);
					text = GameTexts.FindText("str_string_newline_string", null).ToString();
					GameTexts.SetVariable("newline", "\n");
					using (IEnumerator<SkillEffect> enumerator2 = enumerable.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							SkillEffect effect = enumerator2.Current;
							GameTexts.SetVariable("STR1", text);
							GameTexts.SetVariable("STR2", SkillHelper.GetEffectDescriptionForSkillLevel(effect, this.RelevantSkillValue).ToString());
							text = GameTexts.FindText("str_string_newline_string", null).ToString();
						}
						goto IL_395;
					}
				}
				text = null;
			}
			IL_395:
			if (!string.IsNullOrEmpty(text))
			{
				return new TextObject("{=!}" + text, null);
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x06001C22 RID: 7202 RVA: 0x00067DBC File Offset: 0x00065FBC
		public string GetEffectsList(PartyRole role)
		{
			string text = "";
			IEnumerable<SkillEffect> enumerable = from x in this._skillEffects
				where x.Role == role
				select x;
			int num = 0;
			if (this.RelevantSkillValue > 0)
			{
				foreach (SkillEffect effect in enumerable)
				{
					if (num == 0)
					{
						text = SkillHelper.GetEffectDescriptionForSkillLevel(effect, this.RelevantSkillValue).ToString();
					}
					else
					{
						GameTexts.SetVariable("STR1", text);
						GameTexts.SetVariable("STR2", SkillHelper.GetEffectDescriptionForSkillLevel(effect, this.RelevantSkillValue).ToString());
						text = GameTexts.FindText("str_string_newline_string", null).ToString();
					}
					num++;
				}
			}
			return text;
		}

		// Token: 0x06001C23 RID: 7203 RVA: 0x00067E90 File Offset: 0x00066090
		private static SkillObject GetRelevantSkillForRole(PartyRole role)
		{
			if (role == PartyRole.Engineer)
			{
				return DefaultSkills.Engineering;
			}
			if (role == PartyRole.Quartermaster)
			{
				return DefaultSkills.Steward;
			}
			if (role == PartyRole.Scout)
			{
				return DefaultSkills.Scouting;
			}
			if (role == PartyRole.Surgeon)
			{
				return DefaultSkills.Medicine;
			}
			Debug.FailedAssert(string.Format("Undefined clan role relevant skill {0}", role), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanRoleMemberItemVM.cs", "GetRelevantSkillForRole", 246);
			return null;
		}

		// Token: 0x06001C24 RID: 7204 RVA: 0x00067EEC File Offset: 0x000660EC
		public static bool IsHeroAssignableForRole(Hero hero, PartyRole role, MobileParty party)
		{
			return ClanRoleMemberItemVM.DoesHeroHaveEnoughSkillForRole(hero, role, party) && hero.CanBeGovernorOrHavePartyRole();
		}

		// Token: 0x06001C25 RID: 7205 RVA: 0x00067F00 File Offset: 0x00066100
		private static bool DoesHeroHaveEnoughSkillForRole(Hero hero, PartyRole role, MobileParty party)
		{
			if (party.GetHeroPartyRole(hero) == role)
			{
				return true;
			}
			if (role == PartyRole.Engineer)
			{
				return MobilePartyHelper.IsHeroAssignableForEngineerInParty(hero, party);
			}
			if (role == PartyRole.Quartermaster)
			{
				return MobilePartyHelper.IsHeroAssignableForQuartermasterInParty(hero, party);
			}
			if (role == PartyRole.Scout)
			{
				return MobilePartyHelper.IsHeroAssignableForScoutInParty(hero, party);
			}
			if (role == PartyRole.Surgeon)
			{
				return MobilePartyHelper.IsHeroAssignableForSurgeonInParty(hero, party);
			}
			if (role == PartyRole.None)
			{
				return true;
			}
			Debug.FailedAssert(string.Format("Undefined clan role is asked if assignable {0}", role), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanRoleMemberItemVM.cs", "DoesHeroHaveEnoughSkillForRole", 284);
			return false;
		}

		// Token: 0x17000993 RID: 2451
		// (get) Token: 0x06001C26 RID: 7206 RVA: 0x00067F75 File Offset: 0x00066175
		// (set) Token: 0x06001C27 RID: 7207 RVA: 0x00067F7D File Offset: 0x0006617D
		[DataSourceProperty]
		public ClanPartyMemberItemVM Member
		{
			get
			{
				return this._member;
			}
			set
			{
				if (value != this._member)
				{
					this._member = value;
					base.OnPropertyChangedWithValue<ClanPartyMemberItemVM>(value, "Member");
				}
			}
		}

		// Token: 0x17000994 RID: 2452
		// (get) Token: 0x06001C28 RID: 7208 RVA: 0x00067F9B File Offset: 0x0006619B
		// (set) Token: 0x06001C29 RID: 7209 RVA: 0x00067FA3 File Offset: 0x000661A3
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x17000995 RID: 2453
		// (get) Token: 0x06001C2A RID: 7210 RVA: 0x00067FC1 File Offset: 0x000661C1
		// (set) Token: 0x06001C2B RID: 7211 RVA: 0x00067FC9 File Offset: 0x000661C9
		[DataSourceProperty]
		public bool IsRemoveAssigneeOption
		{
			get
			{
				return this._isRemoveAssigneeOption;
			}
			set
			{
				if (value != this._isRemoveAssigneeOption)
				{
					this._isRemoveAssigneeOption = value;
					base.OnPropertyChangedWithValue(value, "IsRemoveAssigneeOption");
				}
			}
		}

		// Token: 0x04000D1F RID: 3359
		private Action _onRoleAssigned;

		// Token: 0x04000D20 RID: 3360
		private MobileParty _party;

		// Token: 0x04000D21 RID: 3361
		private readonly IEnumerable<SkillEffect> _skillEffects;

		// Token: 0x04000D22 RID: 3362
		private readonly IEnumerable<PerkObject> _perks;

		// Token: 0x04000D23 RID: 3363
		private ClanPartyMemberItemVM _member;

		// Token: 0x04000D24 RID: 3364
		private HintViewModel _hint;

		// Token: 0x04000D25 RID: 3365
		private bool _isRemoveAssigneeOption;
	}
}
