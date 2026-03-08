using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000145 RID: 325
	public class SkillVM : ViewModel
	{
		// Token: 0x06001EEA RID: 7914 RVA: 0x00071968 File Offset: 0x0006FB68
		public SkillVM(SkillObject skill, CharacterDeveloperHeroItemVM heroItem, Action<PerkVM> onStartPerkSelection)
		{
			SkillVM <>4__this = this;
			this._heroItem = heroItem;
			this.Skill = skill;
			this.MaxLevel = 300;
			this.SkillId = skill.StringId;
			this._onStartPerkSelection = onStartPerkSelection;
			this.IsInspected = false;
			this.SkillEffects = new MBBindingList<BindingListStringItem>();
			this.Perks = new MBBindingList<PerkVM>();
			this.AddFocusHint = new HintViewModel();
			this.LearningRateTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetLearningRateTooltip(<>4__this._heroItem.CharacterAttributes, <>4__this.CurrentFocusLevel, heroItem.Hero.GetSkillValue(skill), <>4__this.Skill));
			this.LearningLimitTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetLearningLimitTooltip(<>4__this._heroItem.CharacterAttributes, <>4__this.CurrentFocusLevel, <>4__this.Skill));
			this.InitializeValues();
			this._focusConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_skill_focus");
			this._skillConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_skills");
			this.RefreshValues();
		}

		// Token: 0x06001EEB RID: 7915 RVA: 0x00071A98 File Offset: 0x0006FC98
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.AddFocusText = GameTexts.FindText("str_add_focus", null).ToString();
			this.HowToLearnText = this.Skill.HowToLearnSkillText.ToString();
			this.HowToLearnTitle = GameTexts.FindText("str_how_to_learn", null).ToString();
			this.DescriptionText = this.Skill.Description.ToString();
			this.NameText = this.Skill.Name.ToString();
			this.InitializeValues();
			this.RefreshWithCurrentValues();
			this.SkillEffects.ApplyActionOnAllItems(delegate(BindingListStringItem x)
			{
				x.RefreshValues();
			});
			this.Perks.ApplyActionOnAllItems(delegate(PerkVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06001EEC RID: 7916 RVA: 0x00071B7C File Offset: 0x0006FD7C
		public void InitializeValues()
		{
			if (this._heroItem.HeroDeveloper == null)
			{
				this.Level = 0;
			}
			else
			{
				this.Level = this._heroItem.HeroDeveloper.Hero.GetSkillValue(this.Skill);
				this.NextLevel = this.Level + 1;
				this.CurrentSkillXP = this._heroItem.HeroDeveloper.GetSkillXpProgress(this.Skill);
				this.XpRequiredForNextLevel = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(this.Level + 1) - Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(this.Level);
				this.ProgressPercentage = 100.0 * (double)this._currentSkillXP / (double)this.XpRequiredForNextLevel;
				this.ProgressHint = new BasicTooltipViewModel(delegate()
				{
					GameTexts.SetVariable("CURRENT_XP", this.CurrentSkillXP.ToString());
					GameTexts.SetVariable("LEVEL_MAX_XP", this.XpRequiredForNextLevel.ToString());
					return GameTexts.FindText("str_current_xp_over_max", null).ToString();
				});
				GameTexts.SetVariable("CURRENT_XP", this.CurrentSkillXP.ToString());
				GameTexts.SetVariable("LEVEL_MAX_XP", this.XpRequiredForNextLevel.ToString());
				this.ProgressText = GameTexts.FindText("str_current_xp_over_max", null).ToString();
				this.SkillXPHint = new BasicTooltipViewModel(delegate()
				{
					GameTexts.SetVariable("REQUIRED_XP_FOR_NEXT_LEVEL", this.XpRequiredForNextLevel - this.CurrentSkillXP);
					return GameTexts.FindText("str_skill_xp_hint", null).ToString();
				});
			}
			this._orgFocusAmount = this._heroItem.HeroDeveloper.GetFocus(this.Skill);
			this.CurrentFocusLevel = this._orgFocusAmount;
			this.CreateLists();
		}

		// Token: 0x06001EED RID: 7917 RVA: 0x00071CEC File Offset: 0x0006FEEC
		public void RefreshWithCurrentValues()
		{
			float resultNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(this._heroItem.CharacterAttributes, this.CurrentFocusLevel, this._heroItem.Hero.GetSkillValue(this.Skill), this.Skill, false).ResultNumber;
			GameTexts.SetVariable("COUNT", resultNumber.ToString("0.00"));
			this.CurrentLearningRateText = GameTexts.FindText("str_learning_rate_COUNT", null).ToString();
			this.CanLearnSkill = Math.Round((double)resultNumber, 2) > 0.0;
			this.LearningRate = resultNumber;
			this.FullLearningRateLevel = MathF.Round(Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(this._heroItem.CharacterAttributes, this.CurrentFocusLevel, this.Skill, false).ResultNumber);
			int requiredFocusPointsToAddFocusWithCurrentFocus = this._heroItem.GetRequiredFocusPointsToAddFocusWithCurrentFocus(this.Skill);
			GameTexts.SetVariable("COSTAMOUNT", requiredFocusPointsToAddFocusWithCurrentFocus);
			this.FocusCostText = requiredFocusPointsToAddFocusWithCurrentFocus.ToString();
			GameTexts.SetVariable("COUNT", requiredFocusPointsToAddFocusWithCurrentFocus);
			GameTexts.SetVariable("RIGHT", "");
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_cost_COUNT", null));
			MBTextManager.SetTextVariable("FOCUS_ICON", "{=!}<img src=\"CharacterDeveloper\\cp_icon\">", false);
			this.NextLevelCostText = GameTexts.FindText("str_sf_text_with_focus_icon", null).ToString();
			this.RefreshCanAddFocus();
		}

		// Token: 0x06001EEE RID: 7918 RVA: 0x00071E54 File Offset: 0x00070054
		public void CreateLists()
		{
			this.SkillEffects.Clear();
			this.Perks.Clear();
			int skillValue = this._heroItem.HeroDeveloper.Hero.GetSkillValue(this.Skill);
			foreach (SkillEffect effect in from x in SkillEffect.All
				where x.EffectedSkill == this.Skill
				select x)
			{
				this.SkillEffects.Add(new BindingListStringItem(CampaignUIHelper.GetSkillEffectText(effect, skillValue)));
			}
			foreach (PerkObject perkObject in from p in PerkObject.All
				where p.Skill == this.Skill
				orderby p.RequiredSkillValue
				select p)
			{
				PerkVM.PerkAlternativeType alternativeType = ((perkObject.AlternativePerk == null) ? PerkVM.PerkAlternativeType.NoAlternative : ((perkObject.StringId.CompareTo(perkObject.AlternativePerk.StringId) < 0) ? PerkVM.PerkAlternativeType.FirstAlternative : PerkVM.PerkAlternativeType.SecondAlternative));
				PerkVM item = new PerkVM(perkObject, this.IsPerkAvailable(perkObject), alternativeType, new Action<PerkVM>(this.OnStartPerkSelection), new Action<PerkVM>(this.OnPerkSelectionOver), new Func<PerkObject, bool>(this.IsPerkSelected), new Func<PerkObject, bool>(this.IsPreviousPerkSelected));
				this.Perks.Add(item);
			}
			this.RefreshNumOfUnopenedPerks();
		}

		// Token: 0x06001EEF RID: 7919 RVA: 0x00071FE8 File Offset: 0x000701E8
		public void RefreshLists(SkillObject skill = null)
		{
			if (skill != null && skill != this.Skill)
			{
				return;
			}
			foreach (PerkVM perkVM in this.Perks)
			{
				perkVM.RefreshState();
			}
			this.RefreshNumOfUnopenedPerks();
		}

		// Token: 0x06001EF0 RID: 7920 RVA: 0x00072048 File Offset: 0x00070248
		private void RefreshNumOfUnopenedPerks()
		{
			int num = 0;
			foreach (PerkVM perkVM in this.Perks)
			{
				if ((perkVM.CurrentState == PerkVM.PerkStates.EarnedButNotSelected || perkVM.CurrentState == PerkVM.PerkStates.EarnedPreviousPerkNotSelected) && (perkVM.AlternativeType == 1 || perkVM.AlternativeType == 0))
				{
					num++;
				}
			}
			this.NumOfUnopenedPerks = num;
		}

		// Token: 0x06001EF1 RID: 7921 RVA: 0x000720C0 File Offset: 0x000702C0
		private bool IsPerkSelected(PerkObject perk)
		{
			return this._heroItem.HeroDeveloper.GetPerkValue(perk) || this._heroItem.PerkSelection.IsPerkSelected(perk);
		}

		// Token: 0x06001EF2 RID: 7922 RVA: 0x000720E8 File Offset: 0x000702E8
		private bool IsPreviousPerkSelected(PerkObject perk)
		{
			IEnumerable<PerkObject> source = from p in PerkObject.All
				where p.Skill == perk.Skill && p.RequiredSkillValue < perk.RequiredSkillValue
				select p;
			if (!source.Any<PerkObject>())
			{
				return true;
			}
			PerkObject perkObject = source.MaxBy((PerkObject p) => p.RequiredSkillValue - perk.RequiredSkillValue);
			return this.IsPerkSelected(perkObject) || (perkObject.AlternativePerk != null && this.IsPerkSelected(perkObject.AlternativePerk));
		}

		// Token: 0x06001EF3 RID: 7923 RVA: 0x00072157 File Offset: 0x00070357
		private bool IsPerkAvailable(PerkObject perk)
		{
			return perk.RequiredSkillValue <= (float)this.Level;
		}

		// Token: 0x06001EF4 RID: 7924 RVA: 0x0007216C File Offset: 0x0007036C
		public void RefreshCanAddFocus()
		{
			bool playerHasEnoughPoints = this._heroItem.UnspentCharacterPoints >= this._heroItem.GetRequiredFocusPointsToAddFocusWithCurrentFocus(this.Skill);
			bool isMaxedSkill = this._currentFocusLevel >= Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill;
			string addFocusHintString = CampaignUIHelper.GetAddFocusHintString(playerHasEnoughPoints, isMaxedSkill, this.CurrentFocusLevel);
			this.AddFocusHint.HintText = (string.IsNullOrEmpty(addFocusHintString) ? TextObject.GetEmpty() : new TextObject("{=!}" + addFocusHintString, null));
			this.CanAddFocus = this._heroItem.CanAddFocusToSkillWithFocusAmount(this._currentFocusLevel);
		}

		// Token: 0x06001EF5 RID: 7925 RVA: 0x0007220C File Offset: 0x0007040C
		public void ExecuteAddFocus()
		{
			if (this.CanAddFocus)
			{
				this._heroItem.UnspentCharacterPoints -= this._heroItem.GetRequiredFocusPointsToAddFocusWithCurrentFocus(this.Skill);
				int currentFocusLevel = this.CurrentFocusLevel;
				this.CurrentFocusLevel = currentFocusLevel + 1;
				this._heroItem.RefreshCharacterValues();
				this.RefreshWithCurrentValues();
				MBInformationManager.HideInformations();
				Game.Current.EventManager.TriggerEvent<FocusAddedByPlayerEvent>(new FocusAddedByPlayerEvent(this._heroItem.Hero, this.Skill));
			}
		}

		// Token: 0x06001EF6 RID: 7926 RVA: 0x0007228F File Offset: 0x0007048F
		public void ExecuteShowFocusConcept()
		{
			if (this._focusConceptObj != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._focusConceptObj.EncyclopediaLink);
				return;
			}
			Debug.FailedAssert("Couldn't find Focus encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\SkillVM.cs", "ExecuteShowFocusConcept", 252);
		}

		// Token: 0x06001EF7 RID: 7927 RVA: 0x000722CD File Offset: 0x000704CD
		public void ExecuteShowSkillConcept()
		{
			if (this._focusConceptObj != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._skillConceptObj.EncyclopediaLink);
				return;
			}
			Debug.FailedAssert("Couldn't find Focus encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\SkillVM.cs", "ExecuteShowSkillConcept", 264);
		}

		// Token: 0x06001EF8 RID: 7928 RVA: 0x0007230B File Offset: 0x0007050B
		public void ExecuteInspect()
		{
			this._heroItem.SetCurrentSkill(this);
			this.RefreshCanAddFocus();
		}

		// Token: 0x06001EF9 RID: 7929 RVA: 0x0007231F File Offset: 0x0007051F
		public void ResetChanges()
		{
			this.CurrentFocusLevel = this._orgFocusAmount;
			this.Perks.ApplyActionOnAllItems(delegate(PerkVM p)
			{
				p.RefreshState();
			});
			this.RefreshNumOfUnopenedPerks();
		}

		// Token: 0x06001EFA RID: 7930 RVA: 0x0007235D File Offset: 0x0007055D
		public bool IsThereAnyChanges()
		{
			return this.CurrentFocusLevel != this._orgFocusAmount;
		}

		// Token: 0x06001EFB RID: 7931 RVA: 0x00072370 File Offset: 0x00070570
		public void ApplyChanges()
		{
			for (int i = 0; i < this.CurrentFocusLevel - this._orgFocusAmount; i++)
			{
				this._heroItem.HeroDeveloper.AddFocus(this.Skill, 1, true);
			}
			this._orgFocusAmount = this.CurrentFocusLevel;
		}

		// Token: 0x06001EFC RID: 7932 RVA: 0x000723BC File Offset: 0x000705BC
		private void OnStartPerkSelection(PerkVM perk)
		{
			this._onStartPerkSelection(perk);
			if (perk.AlternativeType != 0)
			{
				this.Perks.SingleOrDefault((PerkVM p) => p.Perk == perk.Perk.AlternativePerk).IsInSelection = true;
			}
		}

		// Token: 0x06001EFD RID: 7933 RVA: 0x00072414 File Offset: 0x00070614
		private void OnPerkSelectionOver(PerkVM perk)
		{
			if (perk.AlternativeType != 0)
			{
				this.Perks.SingleOrDefault((PerkVM p) => p.Perk == perk.Perk.AlternativePerk).IsInSelection = false;
			}
		}

		// Token: 0x17000A88 RID: 2696
		// (get) Token: 0x06001EFE RID: 7934 RVA: 0x00072458 File Offset: 0x00070658
		// (set) Token: 0x06001EFF RID: 7935 RVA: 0x00072460 File Offset: 0x00070660
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x17000A89 RID: 2697
		// (get) Token: 0x06001F00 RID: 7936 RVA: 0x00072483 File Offset: 0x00070683
		// (set) Token: 0x06001F01 RID: 7937 RVA: 0x0007248B File Offset: 0x0007068B
		[DataSourceProperty]
		public string HowToLearnText
		{
			get
			{
				return this._howToLearnText;
			}
			set
			{
				if (value != this._howToLearnText)
				{
					this._howToLearnText = value;
					base.OnPropertyChangedWithValue<string>(value, "HowToLearnText");
				}
			}
		}

		// Token: 0x17000A8A RID: 2698
		// (get) Token: 0x06001F02 RID: 7938 RVA: 0x000724AE File Offset: 0x000706AE
		// (set) Token: 0x06001F03 RID: 7939 RVA: 0x000724B6 File Offset: 0x000706B6
		[DataSourceProperty]
		public string HowToLearnTitle
		{
			get
			{
				return this._howToLearnTitle;
			}
			set
			{
				if (value != this._howToLearnTitle)
				{
					this._howToLearnTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "HowToLearnTitle");
				}
			}
		}

		// Token: 0x17000A8B RID: 2699
		// (get) Token: 0x06001F04 RID: 7940 RVA: 0x000724D9 File Offset: 0x000706D9
		// (set) Token: 0x06001F05 RID: 7941 RVA: 0x000724E1 File Offset: 0x000706E1
		[DataSourceProperty]
		public bool CanAddFocus
		{
			get
			{
				return this._canAddFocus;
			}
			set
			{
				if (value != this._canAddFocus)
				{
					this._canAddFocus = value;
					base.OnPropertyChangedWithValue(value, "CanAddFocus");
				}
			}
		}

		// Token: 0x17000A8C RID: 2700
		// (get) Token: 0x06001F06 RID: 7942 RVA: 0x000724FF File Offset: 0x000706FF
		// (set) Token: 0x06001F07 RID: 7943 RVA: 0x00072507 File Offset: 0x00070707
		[DataSourceProperty]
		public bool CanLearnSkill
		{
			get
			{
				return this._canLearnSkill;
			}
			set
			{
				if (value != this._canLearnSkill)
				{
					this._canLearnSkill = value;
					base.OnPropertyChangedWithValue(value, "CanLearnSkill");
				}
			}
		}

		// Token: 0x17000A8D RID: 2701
		// (get) Token: 0x06001F08 RID: 7944 RVA: 0x00072525 File Offset: 0x00070725
		// (set) Token: 0x06001F09 RID: 7945 RVA: 0x0007252D File Offset: 0x0007072D
		[DataSourceProperty]
		public string NextLevelLearningRateText
		{
			get
			{
				return this._nextLevelLearningRateText;
			}
			set
			{
				if (value != this._nextLevelLearningRateText)
				{
					this._nextLevelLearningRateText = value;
					base.OnPropertyChangedWithValue<string>(value, "NextLevelLearningRateText");
				}
			}
		}

		// Token: 0x17000A8E RID: 2702
		// (get) Token: 0x06001F0A RID: 7946 RVA: 0x00072550 File Offset: 0x00070750
		// (set) Token: 0x06001F0B RID: 7947 RVA: 0x00072558 File Offset: 0x00070758
		[DataSourceProperty]
		public string NextLevelCostText
		{
			get
			{
				return this._nextLevelCostText;
			}
			set
			{
				if (value != this._nextLevelCostText)
				{
					this._nextLevelCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "NextLevelCostText");
				}
			}
		}

		// Token: 0x17000A8F RID: 2703
		// (get) Token: 0x06001F0C RID: 7948 RVA: 0x0007257B File Offset: 0x0007077B
		// (set) Token: 0x06001F0D RID: 7949 RVA: 0x00072583 File Offset: 0x00070783
		[DataSourceProperty]
		public BasicTooltipViewModel ProgressHint
		{
			get
			{
				return this._progressHint;
			}
			set
			{
				if (value != this._progressHint)
				{
					this._progressHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ProgressHint");
				}
			}
		}

		// Token: 0x17000A90 RID: 2704
		// (get) Token: 0x06001F0E RID: 7950 RVA: 0x000725A1 File Offset: 0x000707A1
		// (set) Token: 0x06001F0F RID: 7951 RVA: 0x000725A9 File Offset: 0x000707A9
		[DataSourceProperty]
		public BasicTooltipViewModel SkillXPHint
		{
			get
			{
				return this._skillXPHint;
			}
			set
			{
				if (value != this._skillXPHint)
				{
					this._skillXPHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SkillXPHint");
				}
			}
		}

		// Token: 0x17000A91 RID: 2705
		// (get) Token: 0x06001F10 RID: 7952 RVA: 0x000725C7 File Offset: 0x000707C7
		// (set) Token: 0x06001F11 RID: 7953 RVA: 0x000725CF File Offset: 0x000707CF
		[DataSourceProperty]
		public HintViewModel AddFocusHint
		{
			get
			{
				return this._addFocusHint;
			}
			set
			{
				if (value != this._addFocusHint)
				{
					this._addFocusHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AddFocusHint");
				}
			}
		}

		// Token: 0x17000A92 RID: 2706
		// (get) Token: 0x06001F12 RID: 7954 RVA: 0x000725ED File Offset: 0x000707ED
		// (set) Token: 0x06001F13 RID: 7955 RVA: 0x000725F5 File Offset: 0x000707F5
		[DataSourceProperty]
		public BasicTooltipViewModel LearningLimitTooltip
		{
			get
			{
				return this._learningLimitTooltip;
			}
			set
			{
				if (value != this._learningLimitTooltip)
				{
					this._learningLimitTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "LearningLimitTooltip");
				}
			}
		}

		// Token: 0x17000A93 RID: 2707
		// (get) Token: 0x06001F14 RID: 7956 RVA: 0x00072613 File Offset: 0x00070813
		// (set) Token: 0x06001F15 RID: 7957 RVA: 0x0007261B File Offset: 0x0007081B
		[DataSourceProperty]
		public BasicTooltipViewModel LearningRateTooltip
		{
			get
			{
				return this._learningRateTooltip;
			}
			set
			{
				if (value != this._learningRateTooltip)
				{
					this._learningRateTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "LearningRateTooltip");
				}
			}
		}

		// Token: 0x17000A94 RID: 2708
		// (get) Token: 0x06001F16 RID: 7958 RVA: 0x00072639 File Offset: 0x00070839
		// (set) Token: 0x06001F17 RID: 7959 RVA: 0x00072641 File Offset: 0x00070841
		[DataSourceProperty]
		public double ProgressPercentage
		{
			get
			{
				return this._progressPercentage;
			}
			set
			{
				if (value != this._progressPercentage)
				{
					this._progressPercentage = value;
					base.OnPropertyChangedWithValue(value, "ProgressPercentage");
				}
			}
		}

		// Token: 0x17000A95 RID: 2709
		// (get) Token: 0x06001F18 RID: 7960 RVA: 0x0007265F File Offset: 0x0007085F
		// (set) Token: 0x06001F19 RID: 7961 RVA: 0x00072667 File Offset: 0x00070867
		[DataSourceProperty]
		public float LearningRate
		{
			get
			{
				return this._learningRate;
			}
			set
			{
				if (value != this._learningRate)
				{
					this._learningRate = value;
					base.OnPropertyChangedWithValue(value, "LearningRate");
				}
			}
		}

		// Token: 0x17000A96 RID: 2710
		// (get) Token: 0x06001F1A RID: 7962 RVA: 0x00072685 File Offset: 0x00070885
		// (set) Token: 0x06001F1B RID: 7963 RVA: 0x0007268D File Offset: 0x0007088D
		[DataSourceProperty]
		public int CurrentSkillXP
		{
			get
			{
				return this._currentSkillXP;
			}
			set
			{
				if (value != this._currentSkillXP)
				{
					this._currentSkillXP = value;
					base.OnPropertyChangedWithValue(value, "CurrentSkillXP");
				}
			}
		}

		// Token: 0x17000A97 RID: 2711
		// (get) Token: 0x06001F1C RID: 7964 RVA: 0x000726AB File Offset: 0x000708AB
		// (set) Token: 0x06001F1D RID: 7965 RVA: 0x000726B3 File Offset: 0x000708B3
		[DataSourceProperty]
		public int NextLevel
		{
			get
			{
				return this._nextLevel;
			}
			set
			{
				if (value != this._nextLevel)
				{
					this._nextLevel = value;
					base.OnPropertyChangedWithValue(value, "NextLevel");
				}
			}
		}

		// Token: 0x17000A98 RID: 2712
		// (get) Token: 0x06001F1E RID: 7966 RVA: 0x000726D1 File Offset: 0x000708D1
		// (set) Token: 0x06001F1F RID: 7967 RVA: 0x000726D9 File Offset: 0x000708D9
		[DataSourceProperty]
		public int FullLearningRateLevel
		{
			get
			{
				return this._fullLearningRateLevel;
			}
			set
			{
				if (value != this._fullLearningRateLevel)
				{
					this._fullLearningRateLevel = value;
					base.OnPropertyChangedWithValue(value, "FullLearningRateLevel");
				}
			}
		}

		// Token: 0x17000A99 RID: 2713
		// (get) Token: 0x06001F20 RID: 7968 RVA: 0x000726F7 File Offset: 0x000708F7
		// (set) Token: 0x06001F21 RID: 7969 RVA: 0x000726FF File Offset: 0x000708FF
		[DataSourceProperty]
		public int XpRequiredForNextLevel
		{
			get
			{
				return this._xpRequiredForNextLevel;
			}
			set
			{
				if (value != this._xpRequiredForNextLevel)
				{
					this._xpRequiredForNextLevel = value;
					base.OnPropertyChangedWithValue(value, "XpRequiredForNextLevel");
				}
			}
		}

		// Token: 0x17000A9A RID: 2714
		// (get) Token: 0x06001F22 RID: 7970 RVA: 0x0007271D File Offset: 0x0007091D
		// (set) Token: 0x06001F23 RID: 7971 RVA: 0x00072725 File Offset: 0x00070925
		[DataSourceProperty]
		public int NumOfUnopenedPerks
		{
			get
			{
				return this._numOfUnopenedPerks;
			}
			set
			{
				if (value != this._numOfUnopenedPerks)
				{
					this._numOfUnopenedPerks = value;
					base.OnPropertyChangedWithValue(value, "NumOfUnopenedPerks");
				}
			}
		}

		// Token: 0x17000A9B RID: 2715
		// (get) Token: 0x06001F24 RID: 7972 RVA: 0x00072743 File Offset: 0x00070943
		// (set) Token: 0x06001F25 RID: 7973 RVA: 0x0007274B File Offset: 0x0007094B
		[DataSourceProperty]
		public string ProgressText
		{
			get
			{
				return this._progressText;
			}
			set
			{
				if (value != this._progressText)
				{
					this._progressText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProgressText");
				}
			}
		}

		// Token: 0x17000A9C RID: 2716
		// (get) Token: 0x06001F26 RID: 7974 RVA: 0x0007276E File Offset: 0x0007096E
		// (set) Token: 0x06001F27 RID: 7975 RVA: 0x00072776 File Offset: 0x00070976
		[DataSourceProperty]
		public string FocusCostText
		{
			get
			{
				return this._focusCostText;
			}
			set
			{
				if (value != this._focusCostText)
				{
					this._focusCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "FocusCostText");
				}
			}
		}

		// Token: 0x17000A9D RID: 2717
		// (get) Token: 0x06001F28 RID: 7976 RVA: 0x00072799 File Offset: 0x00070999
		// (set) Token: 0x06001F29 RID: 7977 RVA: 0x000727A1 File Offset: 0x000709A1
		[DataSourceProperty]
		public MBBindingList<PerkVM> Perks
		{
			get
			{
				return this._perks;
			}
			set
			{
				if (value != this._perks)
				{
					this._perks = value;
					base.OnPropertyChangedWithValue<MBBindingList<PerkVM>>(value, "Perks");
				}
			}
		}

		// Token: 0x17000A9E RID: 2718
		// (get) Token: 0x06001F2A RID: 7978 RVA: 0x000727BF File Offset: 0x000709BF
		// (set) Token: 0x06001F2B RID: 7979 RVA: 0x000727C7 File Offset: 0x000709C7
		[DataSourceProperty]
		public MBBindingList<BindingListStringItem> SkillEffects
		{
			get
			{
				return this._skillEffects;
			}
			set
			{
				if (value != this._skillEffects)
				{
					this._skillEffects = value;
					base.OnPropertyChangedWithValue<MBBindingList<BindingListStringItem>>(value, "SkillEffects");
				}
			}
		}

		// Token: 0x17000A9F RID: 2719
		// (get) Token: 0x06001F2C RID: 7980 RVA: 0x000727E5 File Offset: 0x000709E5
		// (set) Token: 0x06001F2D RID: 7981 RVA: 0x000727ED File Offset: 0x000709ED
		[DataSourceProperty]
		public int MaxLevel
		{
			get
			{
				return this._maxLevel;
			}
			set
			{
				if (value != this._maxLevel)
				{
					this._maxLevel = value;
					base.OnPropertyChangedWithValue(value, "MaxLevel");
				}
			}
		}

		// Token: 0x17000AA0 RID: 2720
		// (get) Token: 0x06001F2E RID: 7982 RVA: 0x0007280B File Offset: 0x00070A0B
		// (set) Token: 0x06001F2F RID: 7983 RVA: 0x00072813 File Offset: 0x00070A13
		[DataSourceProperty]
		public string CurrentLearningRateText
		{
			get
			{
				return this._currentLearningRateText;
			}
			set
			{
				if (value != this._currentLearningRateText)
				{
					this._currentLearningRateText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentLearningRateText");
				}
			}
		}

		// Token: 0x17000AA1 RID: 2721
		// (get) Token: 0x06001F30 RID: 7984 RVA: 0x00072836 File Offset: 0x00070A36
		// (set) Token: 0x06001F31 RID: 7985 RVA: 0x0007283E File Offset: 0x00070A3E
		[DataSourceProperty]
		public int CurrentFocusLevel
		{
			get
			{
				return this._currentFocusLevel;
			}
			set
			{
				if (value != this._currentFocusLevel)
				{
					this._currentFocusLevel = value;
					base.OnPropertyChangedWithValue(value, "CurrentFocusLevel");
				}
			}
		}

		// Token: 0x17000AA2 RID: 2722
		// (get) Token: 0x06001F32 RID: 7986 RVA: 0x0007285C File Offset: 0x00070A5C
		// (set) Token: 0x06001F33 RID: 7987 RVA: 0x00072864 File Offset: 0x00070A64
		[DataSourceProperty]
		public string AddFocusText
		{
			get
			{
				return this._addFocusText;
			}
			set
			{
				if (value != this._addFocusText)
				{
					this._addFocusText = value;
					base.OnPropertyChangedWithValue<string>(value, "AddFocusText");
				}
			}
		}

		// Token: 0x17000AA3 RID: 2723
		// (get) Token: 0x06001F34 RID: 7988 RVA: 0x00072887 File Offset: 0x00070A87
		// (set) Token: 0x06001F35 RID: 7989 RVA: 0x0007288F File Offset: 0x00070A8F
		[DataSourceProperty]
		public string SkillId
		{
			get
			{
				return this._skillId;
			}
			set
			{
				if (value != this._skillId)
				{
					this._skillId = value;
					base.OnPropertyChangedWithValue<string>(value, "SkillId");
				}
			}
		}

		// Token: 0x17000AA4 RID: 2724
		// (get) Token: 0x06001F36 RID: 7990 RVA: 0x000728B2 File Offset: 0x00070AB2
		// (set) Token: 0x06001F37 RID: 7991 RVA: 0x000728BA File Offset: 0x00070ABA
		[DataSourceProperty]
		public bool IsInspected
		{
			get
			{
				return this._isInspected;
			}
			set
			{
				if (value != this._isInspected)
				{
					this._isInspected = value;
					base.OnPropertyChangedWithValue(value, "IsInspected");
				}
			}
		}

		// Token: 0x17000AA5 RID: 2725
		// (get) Token: 0x06001F38 RID: 7992 RVA: 0x000728D8 File Offset: 0x00070AD8
		// (set) Token: 0x06001F39 RID: 7993 RVA: 0x000728E0 File Offset: 0x00070AE0
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

		// Token: 0x17000AA6 RID: 2726
		// (get) Token: 0x06001F3A RID: 7994 RVA: 0x00072903 File Offset: 0x00070B03
		// (set) Token: 0x06001F3B RID: 7995 RVA: 0x0007290B File Offset: 0x00070B0B
		[DataSourceProperty]
		public int Level
		{
			get
			{
				return this._level;
			}
			set
			{
				if (value != this._level)
				{
					this._level = value;
					base.OnPropertyChangedWithValue(value, "Level");
				}
			}
		}

		// Token: 0x04000E71 RID: 3697
		public const int MAX_SKILL_LEVEL = 300;

		// Token: 0x04000E72 RID: 3698
		public readonly SkillObject Skill;

		// Token: 0x04000E73 RID: 3699
		private readonly CharacterDeveloperHeroItemVM _heroItem;

		// Token: 0x04000E74 RID: 3700
		private readonly Concept _focusConceptObj;

		// Token: 0x04000E75 RID: 3701
		private readonly Concept _skillConceptObj;

		// Token: 0x04000E76 RID: 3702
		private readonly Action<PerkVM> _onStartPerkSelection;

		// Token: 0x04000E77 RID: 3703
		private int _orgFocusAmount;

		// Token: 0x04000E78 RID: 3704
		private MBBindingList<BindingListStringItem> _skillEffects;

		// Token: 0x04000E79 RID: 3705
		private MBBindingList<PerkVM> _perks;

		// Token: 0x04000E7A RID: 3706
		private BasicTooltipViewModel _progressHint;

		// Token: 0x04000E7B RID: 3707
		private HintViewModel _addFocusHint;

		// Token: 0x04000E7C RID: 3708
		private BasicTooltipViewModel _skillXPHint;

		// Token: 0x04000E7D RID: 3709
		private BasicTooltipViewModel _learningLimitTooltip;

		// Token: 0x04000E7E RID: 3710
		private BasicTooltipViewModel _learningRateTooltip;

		// Token: 0x04000E7F RID: 3711
		private string _nameText;

		// Token: 0x04000E80 RID: 3712
		private string _skillId;

		// Token: 0x04000E81 RID: 3713
		private string _addFocusText;

		// Token: 0x04000E82 RID: 3714
		private string _focusCostText;

		// Token: 0x04000E83 RID: 3715
		private string _currentLearningRateText;

		// Token: 0x04000E84 RID: 3716
		private string _nextLevelLearningRateText;

		// Token: 0x04000E85 RID: 3717
		private string _nextLevelCostText;

		// Token: 0x04000E86 RID: 3718
		private string _howToLearnText;

		// Token: 0x04000E87 RID: 3719
		private string _howToLearnTitle;

		// Token: 0x04000E88 RID: 3720
		private string _progressText;

		// Token: 0x04000E89 RID: 3721
		private string _descriptionText;

		// Token: 0x04000E8A RID: 3722
		private int _level = -1;

		// Token: 0x04000E8B RID: 3723
		private int _maxLevel;

		// Token: 0x04000E8C RID: 3724
		private int _currentFocusLevel;

		// Token: 0x04000E8D RID: 3725
		private int _currentSkillXP;

		// Token: 0x04000E8E RID: 3726
		private int _xpRequiredForNextLevel;

		// Token: 0x04000E8F RID: 3727
		private int _nextLevel;

		// Token: 0x04000E90 RID: 3728
		private int _fullLearningRateLevel;

		// Token: 0x04000E91 RID: 3729
		private int _numOfUnopenedPerks;

		// Token: 0x04000E92 RID: 3730
		private bool _isInspected;

		// Token: 0x04000E93 RID: 3731
		private bool _canAddFocus;

		// Token: 0x04000E94 RID: 3732
		private bool _canLearnSkill;

		// Token: 0x04000E95 RID: 3733
		private float _learningRate;

		// Token: 0x04000E96 RID: 3734
		private double _progressPercentage;

		// Token: 0x020002D0 RID: 720
		private enum SkillType
		{
			// Token: 0x04001395 RID: 5013
			Default,
			// Token: 0x04001396 RID: 5014
			Party,
			// Token: 0x04001397 RID: 5015
			Leader
		}
	}
}
