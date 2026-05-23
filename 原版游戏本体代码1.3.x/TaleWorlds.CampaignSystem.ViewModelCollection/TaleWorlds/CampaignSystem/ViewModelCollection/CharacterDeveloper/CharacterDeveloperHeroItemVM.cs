using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000142 RID: 322
	public class CharacterDeveloperHeroItemVM : ViewModel
	{
		// Token: 0x17000A47 RID: 2631
		// (get) Token: 0x06001E37 RID: 7735 RVA: 0x0006F6EF File Offset: 0x0006D8EF
		public HeroDeveloper HeroDeveloper
		{
			get
			{
				return this.Hero.HeroDeveloper;
			}
		}

		// Token: 0x17000A48 RID: 2632
		// (get) Token: 0x06001E38 RID: 7736 RVA: 0x0006F6FC File Offset: 0x0006D8FC
		// (set) Token: 0x06001E39 RID: 7737 RVA: 0x0006F704 File Offset: 0x0006D904
		public Hero Hero { get; private set; }

		// Token: 0x17000A49 RID: 2633
		// (get) Token: 0x06001E3A RID: 7738 RVA: 0x0006F70D File Offset: 0x0006D90D
		// (set) Token: 0x06001E3B RID: 7739 RVA: 0x0006F715 File Offset: 0x0006D915
		public int OrgUnspentFocusPoints { get; private set; }

		// Token: 0x17000A4A RID: 2634
		// (get) Token: 0x06001E3C RID: 7740 RVA: 0x0006F71E File Offset: 0x0006D91E
		// (set) Token: 0x06001E3D RID: 7741 RVA: 0x0006F726 File Offset: 0x0006D926
		public int OrgUnspentAttributePoints { get; private set; }

		// Token: 0x17000A4B RID: 2635
		// (get) Token: 0x06001E3E RID: 7742 RVA: 0x0006F72F File Offset: 0x0006D92F
		public IReadOnlyPropertyOwner<CharacterAttribute> CharacterAttributes
		{
			get
			{
				return this._characterAttributes;
			}
		}

		// Token: 0x06001E3F RID: 7743 RVA: 0x0006F738 File Offset: 0x0006D938
		public CharacterDeveloperHeroItemVM(Hero hero, Action onPerkSelection)
		{
			this.LevelHint = new HintViewModel();
			this.Hero = hero;
			this.OrgUnspentFocusPoints = this.HeroDeveloper.UnspentFocusPoints;
			this.UnspentCharacterPoints = this.OrgUnspentFocusPoints;
			this.OrgUnspentAttributePoints = this.HeroDeveloper.UnspentAttributePoints;
			this.UnspentAttributePoints = this.OrgUnspentAttributePoints;
			this.Attributes = new MBBindingList<CharacterAttributeItemVM>();
			this._characterAttributes = new PropertyOwner<CharacterAttribute>();
			this.PerkSelection = new PerkSelectionVM(this.HeroDeveloper, new Action<SkillObject>(this.RefreshPerksOfSkill), onPerkSelection);
			this.InitializeCharacter();
			this.RefreshValues();
		}

		// Token: 0x06001E40 RID: 7744 RVA: 0x0006F7D8 File Offset: 0x0006D9D8
		public override void RefreshValues()
		{
			base.RefreshValues();
			StringHelpers.SetCharacterProperties("HERO", this.Hero.CharacterObject, null, false);
			this.HeroNameText = this.Hero.CharacterObject.Name.ToString();
			MBTextManager.SetTextVariable("LEVEL", this.Hero.CharacterObject.Level);
			this.HeroLevelText = GameTexts.FindText("str_level_with_value", null).ToString();
			this.HeroInfoText = GameTexts.FindText("str_hero_name_level", null).ToString();
			this.FocusPointsText = GameTexts.FindText("str_focus_points", null).ToString();
			this.InitializeCharacter();
			this.Skills.ApplyActionOnAllItems(delegate(SkillVM x)
			{
				x.RefreshValues();
			});
			this.CurrentSkill.RefreshValues();
		}

		// Token: 0x06001E41 RID: 7745 RVA: 0x0006F8B8 File Offset: 0x0006DAB8
		private void InitializeCharacter()
		{
			this.HeroCharacter = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.Skills = new MBBindingList<SkillVM>();
			this.Traits = new MBBindingList<EncyclopediaTraitItemVM>();
			this.Attributes.Clear();
			this.HeroCharacter.FillFrom(this.Hero, -1, false, false);
			this.HeroCharacter.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
			this.HeroCharacter.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
			this.HeroCharacter.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
			List<CharacterAttribute> list = TaleWorlds.CampaignSystem.Extensions.Attributes.All.ToList<CharacterAttribute>();
			list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
			foreach (CharacterAttribute characterAttribute in list)
			{
				this._characterAttributes.SetPropertyValue(characterAttribute, this.Hero.GetAttributeValue(characterAttribute));
				this.Attributes.Add(new CharacterAttributeItemVM(this.Hero, characterAttribute, this, new Action<CharacterAttributeItemVM>(this.OnInspectAttribute), new Action<CharacterAttributeItemVM>(this.OnAddAttributePoint)));
			}
			List<SkillObject> list2 = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList<SkillObject>();
			list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			foreach (SkillObject skill in list2)
			{
				this.Skills.Add(new SkillVM(skill, this, new Action<PerkVM>(this.OnStartPerkSelection)));
			}
			this.HasExtraSkills = this.Skills.Count > 18;
			foreach (SkillVM skillVM in this.Skills)
			{
				skillVM.RefreshWithCurrentValues();
			}
			foreach (CharacterAttributeItemVM characterAttributeItemVM in this.Attributes)
			{
				characterAttributeItemVM.RefreshWithCurrentValues();
			}
			this.SetCurrentSkill(this.Skills[0]);
			this.RefreshCharacterValues();
			this.CharacterStats = new MBBindingList<StringPairItemVM>();
			if (this.Hero.GovernorOf != null)
			{
				GameTexts.SetVariable("SETTLEMENT_NAME", this.Hero.GovernorOf.Name.ToString());
				this.CharacterStats.Add(new StringPairItemVM(GameTexts.FindText("str_governor_of_label", null).ToString(), "", null));
			}
			if (MobileParty.MainParty.GetHeroPartyRole(this.Hero) != PartyRole.None)
			{
				this.CharacterStats.Add(new StringPairItemVM(CampaignUIHelper.GetHeroClanRoleText(this.Hero, Clan.PlayerClan), "", null));
			}
			foreach (TraitObject traitObject in CampaignUIHelper.GetHeroTraits())
			{
				if (this.Hero.GetTraitLevel(traitObject) != 0)
				{
					this.Traits.Add(new EncyclopediaTraitItemVM(traitObject, this.Hero));
				}
			}
		}

		// Token: 0x06001E42 RID: 7746 RVA: 0x0006FBE8 File Offset: 0x0006DDE8
		private void OnInspectAttribute(CharacterAttributeItemVM att)
		{
			this.CurrentInspectedAttribute = att;
			this.IsInspectingAnAttribute = true;
		}

		// Token: 0x06001E43 RID: 7747 RVA: 0x0006FBF8 File Offset: 0x0006DDF8
		private void OnAddAttributePoint(CharacterAttributeItemVM att)
		{
			int unspentAttributePoints = this.UnspentAttributePoints;
			this.UnspentAttributePoints = unspentAttributePoints - 1;
			this._characterAttributes.SetPropertyValue(att.AttributeType, this._characterAttributes.GetPropertyValue(att.AttributeType) + 1);
			this.RefreshCharacterValues();
		}

		// Token: 0x06001E44 RID: 7748 RVA: 0x0006FC3F File Offset: 0x0006DE3F
		public void ExecuteStopInspectingCurrentAttribute()
		{
			this.IsInspectingAnAttribute = false;
			this.CurrentInspectedAttribute = null;
		}

		// Token: 0x06001E45 RID: 7749 RVA: 0x0006FC50 File Offset: 0x0006DE50
		public void RefreshCharacterValues()
		{
			this.CurrentTotalSkill = this.HeroDeveloper.TotalXp - this.HeroDeveloper.GetXpRequiredForLevel(this.Hero.CharacterObject.Level);
			this.SkillPointsRequiredForNextLevel = this.HeroDeveloper.GetXpRequiredForLevel(this.Hero.CharacterObject.Level + 1) - this.HeroDeveloper.GetXpRequiredForLevel(this.Hero.CharacterObject.Level);
			GameTexts.SetVariable("CURRENTAMOUNT", this.CurrentTotalSkill);
			GameTexts.SetVariable("TARGETAMOUNT", this.SkillPointsRequiredForNextLevel);
			this.LevelProgressText = GameTexts.FindText("str_character_skillpoint_progress", null).ToString();
			GameTexts.SetVariable("newline", "\n");
			GameTexts.SetVariable("CURRENT_SKILL_POINTS", this.CurrentTotalSkill);
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_total_skill_points", null));
			GameTexts.SetVariable("NEXT_SKILL_POINTS", this.SkillPointsRequiredForNextLevel);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_next_level_at", null));
			string content = GameTexts.FindText("str_string_newline_string", null).ToString();
			GameTexts.SetVariable("SKILL_LEVEL_FOR_LEVEL_UP", this.SkillPointsRequiredForNextLevel - this.CurrentTotalSkill);
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_how_to_level_up_character", null));
			string str = GameTexts.FindText("str_string_newline_string", null).ToString();
			this.LevelHint.HintText = new TextObject("{=!}" + str, null);
			foreach (SkillVM skillVM in this.Skills)
			{
				skillVM.RefreshWithCurrentValues();
			}
			foreach (CharacterAttributeItemVM characterAttributeItemVM in this.Attributes)
			{
				characterAttributeItemVM.RefreshWithCurrentValues();
			}
		}

		// Token: 0x06001E46 RID: 7750 RVA: 0x0006FE44 File Offset: 0x0006E044
		public void RefreshPerksOfSkill(SkillObject skill)
		{
			SkillVM skillVM = this.Skills.SingleOrDefault((SkillVM s) => s.Skill == skill);
			if (skillVM == null)
			{
				return;
			}
			skillVM.RefreshLists(null);
		}

		// Token: 0x06001E47 RID: 7751 RVA: 0x0006FE80 File Offset: 0x0006E080
		public void ResetChanges(bool isCancel)
		{
			this.PerkSelection.ResetSelectedPerks();
			foreach (CharacterAttribute characterAttribute in TaleWorlds.CampaignSystem.Extensions.Attributes.All)
			{
				this._characterAttributes.SetPropertyValue(characterAttribute, this.Hero.GetAttributeValue(characterAttribute));
			}
			if (!isCancel)
			{
				this.UnspentCharacterPoints = this.OrgUnspentFocusPoints;
				this.UnspentAttributePoints = this.OrgUnspentAttributePoints;
			}
			foreach (CharacterAttributeItemVM characterAttributeItemVM in this.Attributes)
			{
				characterAttributeItemVM.Reset();
			}
			if (!isCancel)
			{
				foreach (CharacterAttributeItemVM characterAttributeItemVM2 in this.Attributes)
				{
					characterAttributeItemVM2.RefreshWithCurrentValues();
				}
			}
			foreach (SkillVM skillVM in this.Skills)
			{
				skillVM.ResetChanges();
			}
			if (!isCancel)
			{
				foreach (SkillVM skillVM2 in this.Skills)
				{
					skillVM2.RefreshWithCurrentValues();
				}
			}
		}

		// Token: 0x06001E48 RID: 7752 RVA: 0x0006FFF4 File Offset: 0x0006E1F4
		public void ApplyChanges()
		{
			this.PerkSelection.ApplySelectedPerks();
			foreach (CharacterAttributeItemVM characterAttributeItemVM in this.Attributes)
			{
				characterAttributeItemVM.Commit();
			}
			foreach (SkillVM skillVM in this.Skills)
			{
				skillVM.ApplyChanges();
			}
		}

		// Token: 0x06001E49 RID: 7753 RVA: 0x00070084 File Offset: 0x0006E284
		public void SetCurrentSkill(SkillVM skill)
		{
			if (this.CurrentSkill != null)
			{
				this.CurrentSkill.IsInspected = false;
			}
			this.CurrentSkill = skill;
			this.CurrentSkill.IsInspected = true;
		}

		// Token: 0x06001E4A RID: 7754 RVA: 0x000700B0 File Offset: 0x0006E2B0
		public bool IsThereAnyChanges()
		{
			bool flag = this.Skills.Any((SkillVM s) => s.IsThereAnyChanges());
			return this.UnspentCharacterPoints != this.OrgUnspentFocusPoints || this.UnspentAttributePoints != this.OrgUnspentAttributePoints || this.PerkSelection.IsAnyPerkSelected() || flag;
		}

		// Token: 0x06001E4B RID: 7755 RVA: 0x00070114 File Offset: 0x0006E314
		public int GetRequiredFocusPointsToAddFocusWithCurrentFocus(SkillObject skill)
		{
			return this.Hero.HeroDeveloper.GetRequiredFocusPointsToAddFocus(skill);
		}

		// Token: 0x06001E4C RID: 7756 RVA: 0x00070127 File Offset: 0x0006E327
		public bool CanAddFocusToSkillWithFocusAmount(int currentFocusAmount)
		{
			return currentFocusAmount < Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill && this.UnspentCharacterPoints > 0;
		}

		// Token: 0x06001E4D RID: 7757 RVA: 0x0007014C File Offset: 0x0006E34C
		public bool IsSkillMaxAmongOtherSkills(SkillVM skill)
		{
			if (this.Skills.Count > 0)
			{
				int currentFocusLevel = skill.CurrentFocusLevel;
				return this.Skills.Max((SkillVM s) => s.CurrentFocusLevel) <= currentFocusLevel;
			}
			return false;
		}

		// Token: 0x06001E4E RID: 7758 RVA: 0x000701A0 File Offset: 0x0006E3A0
		public string GetNameWithNumOfUnopenedPerks()
		{
			if (this.Skills.Sum((SkillVM s) => s.NumOfUnopenedPerks) == 0)
			{
				return this.HeroNameText;
			}
			GameTexts.SetVariable("STR1", this.HeroNameText);
			GameTexts.SetVariable("STR2", "{=!}<img src=\"CharacterDeveloper\\UnselectedPerksIcon\" extend=\"2\">");
			return GameTexts.FindText("str_STR1_space_STR2", null).ToString();
		}

		// Token: 0x06001E4F RID: 7759 RVA: 0x0007020F File Offset: 0x0006E40F
		private void OnStartPerkSelection(PerkVM perk)
		{
			this.PerkSelection.SetCurrentSelectionPerk(perk);
		}

		// Token: 0x06001E50 RID: 7760 RVA: 0x0007021D File Offset: 0x0006E41D
		public int GetNumberOfUnselectedPerks()
		{
			return this.Skills.Sum((SkillVM s) => s.NumOfUnopenedPerks);
		}

		// Token: 0x06001E51 RID: 7761 RVA: 0x00070249 File Offset: 0x0006E449
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.HeroCharacter.OnFinalize();
		}

		// Token: 0x17000A4C RID: 2636
		// (get) Token: 0x06001E52 RID: 7762 RVA: 0x0007025C File Offset: 0x0006E45C
		// (set) Token: 0x06001E53 RID: 7763 RVA: 0x00070264 File Offset: 0x0006E464
		[DataSourceProperty]
		public MBBindingList<SkillVM> Skills
		{
			get
			{
				return this._skills;
			}
			set
			{
				if (value != this._skills)
				{
					this._skills = value;
					base.OnPropertyChangedWithValue<MBBindingList<SkillVM>>(value, "Skills");
				}
			}
		}

		// Token: 0x17000A4D RID: 2637
		// (get) Token: 0x06001E54 RID: 7764 RVA: 0x00070282 File Offset: 0x0006E482
		// (set) Token: 0x06001E55 RID: 7765 RVA: 0x0007028A File Offset: 0x0006E48A
		[DataSourceProperty]
		public MBBindingList<StringPairItemVM> CharacterStats
		{
			get
			{
				return this._characterStats;
			}
			set
			{
				if (value != this._characterStats)
				{
					this._characterStats = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringPairItemVM>>(value, "CharacterStats");
				}
			}
		}

		// Token: 0x17000A4E RID: 2638
		// (get) Token: 0x06001E56 RID: 7766 RVA: 0x000702A8 File Offset: 0x0006E4A8
		// (set) Token: 0x06001E57 RID: 7767 RVA: 0x000702B0 File Offset: 0x0006E4B0
		[DataSourceProperty]
		public MBBindingList<CharacterAttributeItemVM> Attributes
		{
			get
			{
				return this._attributes;
			}
			set
			{
				if (value != this._attributes)
				{
					this._attributes = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterAttributeItemVM>>(value, "Attributes");
				}
			}
		}

		// Token: 0x17000A4F RID: 2639
		// (get) Token: 0x06001E58 RID: 7768 RVA: 0x000702CE File Offset: 0x0006E4CE
		// (set) Token: 0x06001E59 RID: 7769 RVA: 0x000702D6 File Offset: 0x0006E4D6
		[DataSourceProperty]
		public MBBindingList<EncyclopediaTraitItemVM> Traits
		{
			get
			{
				return this._traits;
			}
			set
			{
				if (value != this._traits)
				{
					this._traits = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaTraitItemVM>>(value, "Traits");
				}
			}
		}

		// Token: 0x17000A50 RID: 2640
		// (get) Token: 0x06001E5A RID: 7770 RVA: 0x000702F4 File Offset: 0x0006E4F4
		// (set) Token: 0x06001E5B RID: 7771 RVA: 0x000702FC File Offset: 0x0006E4FC
		[DataSourceProperty]
		public PerkSelectionVM PerkSelection
		{
			get
			{
				return this._perkSelection;
			}
			set
			{
				if (value != this._perkSelection)
				{
					this._perkSelection = value;
					base.OnPropertyChangedWithValue<PerkSelectionVM>(value, "PerkSelection");
				}
			}
		}

		// Token: 0x17000A51 RID: 2641
		// (get) Token: 0x06001E5C RID: 7772 RVA: 0x0007031A File Offset: 0x0006E51A
		// (set) Token: 0x06001E5D RID: 7773 RVA: 0x00070322 File Offset: 0x0006E522
		[DataSourceProperty]
		public SkillVM CurrentSkill
		{
			get
			{
				return this._currentSkill;
			}
			set
			{
				if (value != this._currentSkill)
				{
					this._currentSkill = value;
					base.OnPropertyChangedWithValue<SkillVM>(value, "CurrentSkill");
				}
			}
		}

		// Token: 0x17000A52 RID: 2642
		// (get) Token: 0x06001E5E RID: 7774 RVA: 0x00070340 File Offset: 0x0006E540
		// (set) Token: 0x06001E5F RID: 7775 RVA: 0x00070348 File Offset: 0x0006E548
		[DataSourceProperty]
		public CharacterAttributeItemVM CurrentInspectedAttribute
		{
			get
			{
				return this._currentInspectedAttribute;
			}
			set
			{
				if (value != this._currentInspectedAttribute)
				{
					this._currentInspectedAttribute = value;
					base.OnPropertyChangedWithValue<CharacterAttributeItemVM>(value, "CurrentInspectedAttribute");
				}
			}
		}

		// Token: 0x17000A53 RID: 2643
		// (get) Token: 0x06001E60 RID: 7776 RVA: 0x00070366 File Offset: 0x0006E566
		// (set) Token: 0x06001E61 RID: 7777 RVA: 0x0007036E File Offset: 0x0006E56E
		[DataSourceProperty]
		public string FocusPointsText
		{
			get
			{
				return this._focusPointsText;
			}
			set
			{
				if (value != this._focusPointsText)
				{
					this._focusPointsText = value;
					base.OnPropertyChangedWithValue<string>(value, "FocusPointsText");
				}
			}
		}

		// Token: 0x17000A54 RID: 2644
		// (get) Token: 0x06001E62 RID: 7778 RVA: 0x00070391 File Offset: 0x0006E591
		// (set) Token: 0x06001E63 RID: 7779 RVA: 0x00070399 File Offset: 0x0006E599
		[DataSourceProperty]
		public string LevelProgressText
		{
			get
			{
				return this._levelProgressText;
			}
			set
			{
				if (value != this._levelProgressText)
				{
					this._levelProgressText = value;
					base.OnPropertyChangedWithValue<string>(value, "LevelProgressText");
				}
			}
		}

		// Token: 0x17000A55 RID: 2645
		// (get) Token: 0x06001E64 RID: 7780 RVA: 0x000703BC File Offset: 0x0006E5BC
		// (set) Token: 0x06001E65 RID: 7781 RVA: 0x000703C4 File Offset: 0x0006E5C4
		[DataSourceProperty]
		public HeroViewModel HeroCharacter
		{
			get
			{
				return this._heroCharacter;
			}
			set
			{
				if (value != this._heroCharacter)
				{
					this._heroCharacter = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "HeroCharacter");
				}
			}
		}

		// Token: 0x17000A56 RID: 2646
		// (get) Token: 0x06001E66 RID: 7782 RVA: 0x000703E2 File Offset: 0x0006E5E2
		// (set) Token: 0x06001E67 RID: 7783 RVA: 0x000703EA File Offset: 0x0006E5EA
		[DataSourceProperty]
		public bool IsInspectingAnAttribute
		{
			get
			{
				return this._isInspectingAnAttribute;
			}
			set
			{
				if (value != this._isInspectingAnAttribute)
				{
					this._isInspectingAnAttribute = value;
					base.OnPropertyChangedWithValue(value, "IsInspectingAnAttribute");
				}
			}
		}

		// Token: 0x17000A57 RID: 2647
		// (get) Token: 0x06001E68 RID: 7784 RVA: 0x00070408 File Offset: 0x0006E608
		// (set) Token: 0x06001E69 RID: 7785 RVA: 0x00070410 File Offset: 0x0006E610
		[DataSourceProperty]
		public int LevelProgressPercentage
		{
			get
			{
				return this._levelProgressPercentage;
			}
			set
			{
				if (value != this._levelProgressPercentage)
				{
					this._levelProgressPercentage = value;
					base.OnPropertyChangedWithValue(value, "LevelProgressPercentage");
				}
			}
		}

		// Token: 0x17000A58 RID: 2648
		// (get) Token: 0x06001E6A RID: 7786 RVA: 0x0007042E File Offset: 0x0006E62E
		// (set) Token: 0x06001E6B RID: 7787 RVA: 0x00070436 File Offset: 0x0006E636
		[DataSourceProperty]
		public int CurrentTotalSkill
		{
			get
			{
				return this._currentTotalSkill;
			}
			set
			{
				if (value != this._currentTotalSkill)
				{
					this._currentTotalSkill = value;
					base.OnPropertyChangedWithValue(value, "CurrentTotalSkill");
				}
			}
		}

		// Token: 0x17000A59 RID: 2649
		// (get) Token: 0x06001E6C RID: 7788 RVA: 0x00070454 File Offset: 0x0006E654
		// (set) Token: 0x06001E6D RID: 7789 RVA: 0x0007045C File Offset: 0x0006E65C
		[DataSourceProperty]
		public int SkillPointsRequiredForCurrentLevel
		{
			get
			{
				return this._skillPointsRequiredForCurrentLevel;
			}
			set
			{
				if (value != this._skillPointsRequiredForCurrentLevel)
				{
					this._skillPointsRequiredForCurrentLevel = value;
					base.OnPropertyChangedWithValue(value, "SkillPointsRequiredForCurrentLevel");
				}
			}
		}

		// Token: 0x17000A5A RID: 2650
		// (get) Token: 0x06001E6E RID: 7790 RVA: 0x0007047A File Offset: 0x0006E67A
		// (set) Token: 0x06001E6F RID: 7791 RVA: 0x00070482 File Offset: 0x0006E682
		[DataSourceProperty]
		public int SkillPointsRequiredForNextLevel
		{
			get
			{
				return this._skillPointsRequiredForNextLevel;
			}
			set
			{
				if (value != this._skillPointsRequiredForNextLevel)
				{
					this._skillPointsRequiredForNextLevel = value;
					base.OnPropertyChangedWithValue(value, "SkillPointsRequiredForNextLevel");
				}
			}
		}

		// Token: 0x17000A5B RID: 2651
		// (get) Token: 0x06001E70 RID: 7792 RVA: 0x000704A0 File Offset: 0x0006E6A0
		// (set) Token: 0x06001E71 RID: 7793 RVA: 0x000704A8 File Offset: 0x0006E6A8
		[DataSourceProperty]
		public int UnspentCharacterPoints
		{
			get
			{
				return this._unspentCharacterPoints;
			}
			set
			{
				if (value != this._unspentCharacterPoints)
				{
					this._unspentCharacterPoints = value;
					base.OnPropertyChangedWithValue(value, "UnspentCharacterPoints");
				}
			}
		}

		// Token: 0x17000A5C RID: 2652
		// (get) Token: 0x06001E72 RID: 7794 RVA: 0x000704C6 File Offset: 0x0006E6C6
		// (set) Token: 0x06001E73 RID: 7795 RVA: 0x000704CE File Offset: 0x0006E6CE
		[DataSourceProperty]
		public int UnspentAttributePoints
		{
			get
			{
				return this._unspentAttributePoints;
			}
			set
			{
				if (value != this._unspentAttributePoints)
				{
					this._unspentAttributePoints = value;
					base.OnPropertyChangedWithValue(value, "UnspentAttributePoints");
				}
			}
		}

		// Token: 0x17000A5D RID: 2653
		// (get) Token: 0x06001E74 RID: 7796 RVA: 0x000704EC File Offset: 0x0006E6EC
		// (set) Token: 0x06001E75 RID: 7797 RVA: 0x000704F4 File Offset: 0x0006E6F4
		[DataSourceProperty]
		public HintViewModel LevelHint
		{
			get
			{
				return this._levelHint;
			}
			set
			{
				if (value != this._levelHint)
				{
					this._levelHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LevelHint");
				}
			}
		}

		// Token: 0x17000A5E RID: 2654
		// (get) Token: 0x06001E76 RID: 7798 RVA: 0x00070512 File Offset: 0x0006E712
		// (set) Token: 0x06001E77 RID: 7799 RVA: 0x0007051A File Offset: 0x0006E71A
		[DataSourceProperty]
		public string HeroNameText
		{
			get
			{
				return this._heroNameText;
			}
			set
			{
				if (value != this._heroNameText)
				{
					this._heroNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "HeroNameText");
				}
			}
		}

		// Token: 0x17000A5F RID: 2655
		// (get) Token: 0x06001E78 RID: 7800 RVA: 0x0007053D File Offset: 0x0006E73D
		// (set) Token: 0x06001E79 RID: 7801 RVA: 0x00070545 File Offset: 0x0006E745
		[DataSourceProperty]
		public string HeroInfoText
		{
			get
			{
				return this._heroInfoText;
			}
			set
			{
				if (value != this._heroInfoText)
				{
					this._heroInfoText = value;
					base.OnPropertyChangedWithValue<string>(value, "HeroInfoText");
				}
			}
		}

		// Token: 0x17000A60 RID: 2656
		// (get) Token: 0x06001E7A RID: 7802 RVA: 0x00070568 File Offset: 0x0006E768
		// (set) Token: 0x06001E7B RID: 7803 RVA: 0x00070570 File Offset: 0x0006E770
		[DataSourceProperty]
		public string HeroLevelText
		{
			get
			{
				return this._heroLevelText;
			}
			set
			{
				if (value != this._heroLevelText)
				{
					this._heroLevelText = value;
					base.OnPropertyChangedWithValue<string>(value, "HeroLevelText");
				}
			}
		}

		// Token: 0x17000A61 RID: 2657
		// (get) Token: 0x06001E7C RID: 7804 RVA: 0x00070593 File Offset: 0x0006E793
		// (set) Token: 0x06001E7D RID: 7805 RVA: 0x0007059B File Offset: 0x0006E79B
		[DataSourceProperty]
		public bool HasExtraSkills
		{
			get
			{
				return this._hasExtraSkills;
			}
			set
			{
				if (value != this._hasExtraSkills)
				{
					this._hasExtraSkills = value;
					base.OnPropertyChangedWithValue(value, "HasExtraSkills");
				}
			}
		}

		// Token: 0x04000E25 RID: 3621
		private readonly PropertyOwner<CharacterAttribute> _characterAttributes;

		// Token: 0x04000E26 RID: 3622
		private MBBindingList<SkillVM> _skills;

		// Token: 0x04000E27 RID: 3623
		private PerkSelectionVM _perkSelection;

		// Token: 0x04000E28 RID: 3624
		private HeroViewModel _heroCharacter;

		// Token: 0x04000E29 RID: 3625
		private int _skillPointsRequiredForCurrentLevel;

		// Token: 0x04000E2A RID: 3626
		private int _skillPointsRequiredForNextLevel;

		// Token: 0x04000E2B RID: 3627
		private int _currentTotalSkill;

		// Token: 0x04000E2C RID: 3628
		private int _levelProgressPercentage;

		// Token: 0x04000E2D RID: 3629
		private int _unspentCharacterPoints;

		// Token: 0x04000E2E RID: 3630
		private int _unspentAttributePoints;

		// Token: 0x04000E2F RID: 3631
		private string _levelProgressText;

		// Token: 0x04000E30 RID: 3632
		private string _heroNameText;

		// Token: 0x04000E31 RID: 3633
		private string _heroInfoText;

		// Token: 0x04000E32 RID: 3634
		private bool _isInspectingAnAttribute;

		// Token: 0x04000E33 RID: 3635
		private HintViewModel _levelHint;

		// Token: 0x04000E34 RID: 3636
		private SkillVM _currentSkill;

		// Token: 0x04000E35 RID: 3637
		private CharacterAttributeItemVM _currentInspectedAttribute;

		// Token: 0x04000E36 RID: 3638
		private string _heroLevelText;

		// Token: 0x04000E37 RID: 3639
		private string _focusPointsText;

		// Token: 0x04000E38 RID: 3640
		private MBBindingList<StringPairItemVM> _characterStats;

		// Token: 0x04000E39 RID: 3641
		private MBBindingList<CharacterAttributeItemVM> _attributes;

		// Token: 0x04000E3A RID: 3642
		private MBBindingList<EncyclopediaTraitItemVM> _traits;

		// Token: 0x04000E3B RID: 3643
		private bool _hasExtraSkills;
	}
}
