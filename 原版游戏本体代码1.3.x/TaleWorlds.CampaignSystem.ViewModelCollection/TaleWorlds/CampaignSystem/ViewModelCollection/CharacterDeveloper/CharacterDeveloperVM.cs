using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000143 RID: 323
	public class CharacterDeveloperVM : ViewModel
	{
		// Token: 0x06001E7E RID: 7806 RVA: 0x000705BC File Offset: 0x0006E7BC
		public CharacterDeveloperVM(Action closeCharacterDeveloper)
		{
			this._closeCharacterDeveloper = closeCharacterDeveloper;
			this.TutorialNotification = new ElementNotificationVM();
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this._heroList = new List<CharacterDeveloperHeroItemVM>();
			this.HeroList = new ReadOnlyCollection<CharacterDeveloperHeroItemVM>(this._heroList);
			foreach (Hero hero in this.GetApplicableHeroes())
			{
				if (hero == null)
				{
					Debug.FailedAssert("Trying to use null hero for character developer", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\CharacterDeveloperVM.cs", ".ctor", 40);
				}
				else if (hero.HeroDeveloper == null)
				{
					Debug.FailedAssert("Hero does not have hero developer", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\CharacterDeveloperVM.cs", ".ctor", 46);
				}
				else if (hero == Hero.MainHero)
				{
					this._heroList.Insert(0, new CharacterDeveloperHeroItemVM(hero, new Action(this.OnPerkSelection)));
				}
				else
				{
					this._heroList.Add(new CharacterDeveloperHeroItemVM(hero, new Action(this.OnPerkSelection)));
				}
			}
			this._heroIndex = 0;
			this.CharacterList = new SelectorVM<SelectorItemVM>(new List<string>(), this._heroIndex, new Action<SelectorVM<SelectorItemVM>>(this.OnCharacterSelection));
			this.RefreshCharacterSelector();
			this.IsPlayerAccompanied = this._heroList.Count > 1;
			this.SetCurrentHero(this._heroList[this._heroIndex]);
			this._viewDataTracker.ClearCharacterNotification();
			this.UnopenedPerksNumForOtherChars = this._heroList.Sum(delegate(CharacterDeveloperHeroItemVM h)
			{
				if (h != this.CurrentCharacter)
				{
					return h.GetNumberOfUnselectedPerks();
				}
				return 0;
			});
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.RefreshValues();
		}

		// Token: 0x06001E7F RID: 7807 RVA: 0x00070780 File Offset: 0x0006E980
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.ResetLbl = GameTexts.FindText("str_reset", null).ToString();
			this.CancelLbl = GameTexts.FindText("str_cancel", null).ToString();
			this.SkillsText = GameTexts.FindText("str_skills", null).ToString();
			this.AddFocusText = GameTexts.FindText("str_add_focus", null).ToString();
			this.UnspentCharacterPointsText = GameTexts.FindText("str_character_unspent_character_points", null).ToString();
			this.TraitsText = new TextObject("{=FYJC7cDD}Trait(s)", null).ToString();
			this.PartyRoleText = new TextObject("{=9FJi2SaE}Party Role", null).ToString();
			this.ResetHint = new HintViewModel(GameTexts.FindText("str_reset", null), null);
			this.SkillFocusText = GameTexts.FindText("str_character_skill_focus", null).ToString();
			this.FocusVisualHint = new HintViewModel(new TextObject("{=GwA9oUBC}Your skill focus determines the rate your skill increases with practice", null), null);
			GameTexts.SetVariable("FOCUS_PER_LEVEL", Campaign.Current.Models.CharacterDevelopmentModel.FocusPointsPerLevel);
			GameTexts.SetVariable("ATTRIBUTE_EVERY_LEVEL", Campaign.Current.Models.CharacterDevelopmentModel.LevelsPerAttributePoint);
			this.UnspentCharacterPointsHint = new HintViewModel(GameTexts.FindText("str_character_points_how_to_get", null), null);
			this.UnspentAttributePointsHint = new HintViewModel(GameTexts.FindText("str_attribute_points_how_to_get", null), null);
			this.SetPreviousCharacterHint();
			this.SetNextCharacterHint();
			this.CharacterList.RefreshValues();
			this.CurrentCharacter.RefreshValues();
		}

		// Token: 0x06001E80 RID: 7808 RVA: 0x00070913 File Offset: 0x0006EB13
		private void SetPreviousCharacterHint()
		{
			this.PreviousCharacterHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetPreviousCharacterKeyText());
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_prev_char", null));
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x06001E81 RID: 7809 RVA: 0x0007092C File Offset: 0x0006EB2C
		private void SetNextCharacterHint()
		{
			this.NextCharacterHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetNextCharacterKeyText());
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_inventory_next_char", null));
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x06001E82 RID: 7810 RVA: 0x00070948 File Offset: 0x0006EB48
		public void SelectHero(Hero hero)
		{
			for (int i = 0; i < this._heroList.Count; i++)
			{
				if (this._heroList[i].Hero == hero)
				{
					this._heroIndex = i;
					this.RefreshCharacterSelector();
					return;
				}
			}
		}

		// Token: 0x06001E83 RID: 7811 RVA: 0x00070990 File Offset: 0x0006EB90
		private void OnCharacterSelection(SelectorVM<SelectorItemVM> newIndex)
		{
			if (newIndex.SelectedIndex >= 0 && newIndex.SelectedIndex < this._heroList.Count)
			{
				this._heroIndex = newIndex.SelectedIndex;
				this.SetCurrentHero(this._heroList[this._heroIndex]);
				this.UnopenedPerksNumForOtherChars = this._heroList.Sum(delegate(CharacterDeveloperHeroItemVM h)
				{
					if (h != this.CurrentCharacter)
					{
						return h.GetNumberOfUnselectedPerks();
					}
					return 0;
				});
				this.HasUnopenedPerksForOtherCharacters = this._heroList[this._heroIndex].GetNumberOfUnselectedPerks() > 0;
			}
		}

		// Token: 0x06001E84 RID: 7812 RVA: 0x00070A18 File Offset: 0x0006EC18
		private void OnPerkSelection()
		{
			this.RefreshCharacterSelector();
		}

		// Token: 0x06001E85 RID: 7813 RVA: 0x00070A20 File Offset: 0x0006EC20
		private void RefreshCharacterSelector()
		{
			List<string> list = new List<string>();
			for (int i = 0; i < this._heroList.Count; i++)
			{
				string text = this._heroList[i].HeroNameText;
				if (this._heroList[i].GetNumberOfUnselectedPerks() > 0)
				{
					text = GameTexts.FindText("str_STR1_space_STR2", null).SetTextVariable("STR1", text).SetTextVariable("STR2", "{=!}<img src=\"CharacterDeveloper\\UnselectedPerksIcon\" extend=\"2\">")
						.ToString();
				}
				list.Add(text);
			}
			this.CharacterList.Refresh(list, this._heroIndex, new Action<SelectorVM<SelectorItemVM>>(this.OnCharacterSelection));
		}

		// Token: 0x06001E86 RID: 7814 RVA: 0x00070AC0 File Offset: 0x0006ECC0
		public void ExecuteReset()
		{
			foreach (CharacterDeveloperHeroItemVM characterDeveloperHeroItemVM in this._heroList)
			{
				characterDeveloperHeroItemVM.ResetChanges(false);
			}
			this.RefreshCharacterSelector();
		}

		// Token: 0x06001E87 RID: 7815 RVA: 0x00070B18 File Offset: 0x0006ED18
		public void ExecuteDone()
		{
			this.ApplyAllChanges();
			this._closeCharacterDeveloper();
		}

		// Token: 0x06001E88 RID: 7816 RVA: 0x00070B2C File Offset: 0x0006ED2C
		public void ExecuteCancel()
		{
			foreach (CharacterDeveloperHeroItemVM characterDeveloperHeroItemVM in this._heroList)
			{
				characterDeveloperHeroItemVM.ResetChanges(true);
			}
			this._closeCharacterDeveloper();
		}

		// Token: 0x06001E89 RID: 7817 RVA: 0x00070B88 File Offset: 0x0006ED88
		private void SetCurrentHero(CharacterDeveloperHeroItemVM currentHero)
		{
			CharacterDeveloperVM.<>c__DisplayClass18_0 CS$<>8__locals1 = new CharacterDeveloperVM.<>c__DisplayClass18_0();
			CharacterDeveloperVM.<>c__DisplayClass18_0 CS$<>8__locals2 = CS$<>8__locals1;
			CharacterDeveloperHeroItemVM currentCharacter = this.CurrentCharacter;
			SkillObject prevSkill;
			if (currentCharacter == null)
			{
				prevSkill = null;
			}
			else
			{
				SkillVM skillVM = currentCharacter.Skills.FirstOrDefault((SkillVM s) => s.IsInspected);
				prevSkill = ((skillVM != null) ? skillVM.Skill : null);
			}
			CS$<>8__locals2.prevSkill = prevSkill;
			this.CurrentCharacter = currentHero;
			if (CS$<>8__locals1.prevSkill != null)
			{
				CharacterDeveloperHeroItemVM currentCharacter2 = this.CurrentCharacter;
				if (currentCharacter2 == null)
				{
					return;
				}
				currentCharacter2.SetCurrentSkill(this.CurrentCharacter.Skills.FirstOrDefault((SkillVM s) => s.Skill == CS$<>8__locals1.prevSkill));
			}
		}

		// Token: 0x06001E8A RID: 7818 RVA: 0x00070C20 File Offset: 0x0006EE20
		public void ApplyAllChanges()
		{
			foreach (CharacterDeveloperHeroItemVM characterDeveloperHeroItemVM in this._heroList)
			{
				characterDeveloperHeroItemVM.ApplyChanges();
			}
		}

		// Token: 0x06001E8B RID: 7819 RVA: 0x00070C70 File Offset: 0x0006EE70
		public bool IsThereAnyChanges()
		{
			return this._heroList.Any((CharacterDeveloperHeroItemVM c) => c.IsThereAnyChanges());
		}

		// Token: 0x06001E8C RID: 7820 RVA: 0x00070C9C File Offset: 0x0006EE9C
		private List<Hero> GetApplicableHeroes()
		{
			List<Hero> list = new List<Hero>();
			Func<Hero, bool> func = (Hero x) => x != null && x.HeroState != Hero.CharacterStates.Disabled && x.IsAlive && !x.IsChild;
			Clan playerClan = Clan.PlayerClan;
			IEnumerable<Hero> enumerable = ((playerClan != null) ? playerClan.Heroes : null);
			foreach (Hero hero in (enumerable ?? Enumerable.Empty<Hero>()))
			{
				if (func(hero))
				{
					list.Add(hero);
				}
			}
			Clan playerClan2 = Clan.PlayerClan;
			enumerable = ((playerClan2 != null) ? playerClan2.Companions : null);
			foreach (Hero hero2 in (enumerable ?? Enumerable.Empty<Hero>()))
			{
				if (func(hero2) && !list.Contains(hero2))
				{
					list.Add(hero2);
				}
			}
			return list;
		}

		// Token: 0x06001E8D RID: 7821 RVA: 0x00070D9C File Offset: 0x0006EF9C
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = string.Empty;
					if (this._isActivePerkHighlightsApplied)
					{
						this.SetAvailablePerksHighlightState(false);
						this._isActivePerkHighlightsApplied = false;
					}
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = this._latestTutorialElementID;
					if (!this._isActivePerkHighlightsApplied && this._latestTutorialElementID == this._availablePerksHighlighId)
					{
						this.SetAvailablePerksHighlightState(true);
						this._isActivePerkHighlightsApplied = true;
						SkillVM skillVM = this.CurrentCharacter.Skills.FirstOrDefault((SkillVM s) => s.NumOfUnopenedPerks > 0);
						if (skillVM == null)
						{
							return;
						}
						skillVM.ExecuteInspect();
					}
				}
			}
		}

		// Token: 0x06001E8E RID: 7822 RVA: 0x00070E74 File Offset: 0x0006F074
		private void SetAvailablePerksHighlightState(bool state)
		{
			foreach (SkillVM skillVM in this.CurrentCharacter.Skills)
			{
				foreach (PerkVM perkVM in skillVM.Perks)
				{
					if (state && perkVM.CurrentState == PerkVM.PerkStates.EarnedButNotSelected)
					{
						perkVM.IsTutorialHighlightEnabled = true;
					}
					else if (!state)
					{
						perkVM.IsTutorialHighlightEnabled = false;
					}
				}
			}
		}

		// Token: 0x06001E8F RID: 7823 RVA: 0x00070F14 File Offset: 0x0006F114
		public override void OnFinalize()
		{
			base.OnFinalize();
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.CancelInputKey.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.PreviousCharacterInputKey.OnFinalize();
			this.NextCharacterInputKey.OnFinalize();
			this._heroList.ForEach(delegate(CharacterDeveloperHeroItemVM h)
			{
				h.OnFinalize();
			});
		}

		// Token: 0x17000A62 RID: 2658
		// (get) Token: 0x06001E90 RID: 7824 RVA: 0x00070F98 File Offset: 0x0006F198
		// (set) Token: 0x06001E91 RID: 7825 RVA: 0x00070FA0 File Offset: 0x0006F1A0
		[DataSourceProperty]
		public string CurrentCharacterNameText
		{
			get
			{
				return this._currentCharacterNameText;
			}
			set
			{
				if (value != this._currentCharacterNameText)
				{
					this._currentCharacterNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCharacterNameText");
				}
			}
		}

		// Token: 0x17000A63 RID: 2659
		// (get) Token: 0x06001E92 RID: 7826 RVA: 0x00070FC3 File Offset: 0x0006F1C3
		// (set) Token: 0x06001E93 RID: 7827 RVA: 0x00070FCC File Offset: 0x0006F1CC
		[DataSourceProperty]
		public CharacterDeveloperHeroItemVM CurrentCharacter
		{
			get
			{
				return this._currentCharacter;
			}
			set
			{
				if (value != this._currentCharacter)
				{
					if (this._currentCharacter != null)
					{
						if (this._currentCharacter.IsInspectingAnAttribute)
						{
							this._currentCharacter.ExecuteStopInspectingCurrentAttribute();
						}
						if (this._currentCharacter.PerkSelection.IsActive)
						{
							this._currentCharacter.PerkSelection.ExecuteDeactivate();
						}
					}
					this._currentCharacter = value;
					CharacterDeveloperHeroItemVM currentCharacter = this._currentCharacter;
					this.CurrentCharacterNameText = ((currentCharacter != null) ? currentCharacter.HeroNameText : null) ?? string.Empty;
					base.OnPropertyChangedWithValue<CharacterDeveloperHeroItemVM>(value, "CurrentCharacter");
				}
			}
		}

		// Token: 0x17000A64 RID: 2660
		// (get) Token: 0x06001E94 RID: 7828 RVA: 0x00071058 File Offset: 0x0006F258
		// (set) Token: 0x06001E95 RID: 7829 RVA: 0x00071060 File Offset: 0x0006F260
		[DataSourceProperty]
		public SelectorVM<SelectorItemVM> CharacterList
		{
			get
			{
				return this._characterList;
			}
			set
			{
				if (value != this._characterList)
				{
					this._characterList = value;
					base.OnPropertyChangedWithValue<SelectorVM<SelectorItemVM>>(value, "CharacterList");
				}
			}
		}

		// Token: 0x17000A65 RID: 2661
		// (get) Token: 0x06001E96 RID: 7830 RVA: 0x0007107E File Offset: 0x0006F27E
		// (set) Token: 0x06001E97 RID: 7831 RVA: 0x00071086 File Offset: 0x0006F286
		[DataSourceProperty]
		public HintViewModel FocusVisualHint
		{
			get
			{
				return this._focusVisualHint;
			}
			set
			{
				if (value != this._focusVisualHint)
				{
					this._focusVisualHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FocusVisualHint");
				}
			}
		}

		// Token: 0x17000A66 RID: 2662
		// (get) Token: 0x06001E98 RID: 7832 RVA: 0x000710A4 File Offset: 0x0006F2A4
		// (set) Token: 0x06001E99 RID: 7833 RVA: 0x000710AC File Offset: 0x0006F2AC
		[DataSourceProperty]
		public HintViewModel ResetHint
		{
			get
			{
				return this._resetHint;
			}
			set
			{
				if (value != this._resetHint)
				{
					this._resetHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResetHint");
				}
			}
		}

		// Token: 0x17000A67 RID: 2663
		// (get) Token: 0x06001E9A RID: 7834 RVA: 0x000710CA File Offset: 0x0006F2CA
		// (set) Token: 0x06001E9B RID: 7835 RVA: 0x000710D2 File Offset: 0x0006F2D2
		[DataSourceProperty]
		public ElementNotificationVM TutorialNotification
		{
			get
			{
				return this._tutorialNotification;
			}
			set
			{
				if (value != this._tutorialNotification)
				{
					this._tutorialNotification = value;
					base.OnPropertyChangedWithValue<ElementNotificationVM>(value, "TutorialNotification");
				}
			}
		}

		// Token: 0x17000A68 RID: 2664
		// (get) Token: 0x06001E9C RID: 7836 RVA: 0x000710F0 File Offset: 0x0006F2F0
		// (set) Token: 0x06001E9D RID: 7837 RVA: 0x000710F8 File Offset: 0x0006F2F8
		[DataSourceProperty]
		public bool IsPlayerAccompanied
		{
			get
			{
				return this._isPlayerAccompanied;
			}
			set
			{
				if (value != this._isPlayerAccompanied)
				{
					this._isPlayerAccompanied = value;
					base.OnPropertyChangedWithValue(value, "IsPlayerAccompanied");
				}
			}
		}

		// Token: 0x17000A69 RID: 2665
		// (get) Token: 0x06001E9E RID: 7838 RVA: 0x00071116 File Offset: 0x0006F316
		// (set) Token: 0x06001E9F RID: 7839 RVA: 0x0007111E File Offset: 0x0006F31E
		[DataSourceProperty]
		public string UnspentCharacterPointsText
		{
			get
			{
				return this._unspentFocusPointsText;
			}
			set
			{
				if (value != this._unspentFocusPointsText)
				{
					this._unspentFocusPointsText = value;
					base.OnPropertyChangedWithValue<string>(value, "UnspentCharacterPointsText");
				}
			}
		}

		// Token: 0x17000A6A RID: 2666
		// (get) Token: 0x06001EA0 RID: 7840 RVA: 0x00071141 File Offset: 0x0006F341
		// (set) Token: 0x06001EA1 RID: 7841 RVA: 0x00071149 File Offset: 0x0006F349
		[DataSourceProperty]
		public string TraitsText
		{
			get
			{
				return this._traitsText;
			}
			set
			{
				if (value != this._traitsText)
				{
					this._traitsText = value;
					base.OnPropertyChangedWithValue<string>(value, "TraitsText");
				}
			}
		}

		// Token: 0x17000A6B RID: 2667
		// (get) Token: 0x06001EA2 RID: 7842 RVA: 0x0007116C File Offset: 0x0006F36C
		// (set) Token: 0x06001EA3 RID: 7843 RVA: 0x00071174 File Offset: 0x0006F374
		[DataSourceProperty]
		public string PartyRoleText
		{
			get
			{
				return this._partyRoleText;
			}
			set
			{
				if (value != this._partyRoleText)
				{
					this._partyRoleText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyRoleText");
				}
			}
		}

		// Token: 0x17000A6C RID: 2668
		// (get) Token: 0x06001EA4 RID: 7844 RVA: 0x00071197 File Offset: 0x0006F397
		// (set) Token: 0x06001EA5 RID: 7845 RVA: 0x0007119F File Offset: 0x0006F39F
		[DataSourceProperty]
		public HintViewModel UnspentCharacterPointsHint
		{
			get
			{
				return this._unspentCharacterPointsHint;
			}
			set
			{
				if (value != this._unspentCharacterPointsHint)
				{
					this._unspentCharacterPointsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UnspentCharacterPointsHint");
				}
			}
		}

		// Token: 0x17000A6D RID: 2669
		// (get) Token: 0x06001EA6 RID: 7846 RVA: 0x000711BD File Offset: 0x0006F3BD
		// (set) Token: 0x06001EA7 RID: 7847 RVA: 0x000711C5 File Offset: 0x0006F3C5
		[DataSourceProperty]
		public HintViewModel UnspentAttributePointsHint
		{
			get
			{
				return this._unspentAttributePointsHint;
			}
			set
			{
				if (value != this._unspentAttributePointsHint)
				{
					this._unspentAttributePointsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UnspentAttributePointsHint");
				}
			}
		}

		// Token: 0x17000A6E RID: 2670
		// (get) Token: 0x06001EA8 RID: 7848 RVA: 0x000711E3 File Offset: 0x0006F3E3
		// (set) Token: 0x06001EA9 RID: 7849 RVA: 0x000711EB File Offset: 0x0006F3EB
		[DataSourceProperty]
		public BasicTooltipViewModel PreviousCharacterHint
		{
			get
			{
				return this._previousCharacterHint;
			}
			set
			{
				if (value != this._previousCharacterHint)
				{
					this._previousCharacterHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "PreviousCharacterHint");
				}
			}
		}

		// Token: 0x17000A6F RID: 2671
		// (get) Token: 0x06001EAA RID: 7850 RVA: 0x00071209 File Offset: 0x0006F409
		// (set) Token: 0x06001EAB RID: 7851 RVA: 0x00071211 File Offset: 0x0006F411
		[DataSourceProperty]
		public BasicTooltipViewModel NextCharacterHint
		{
			get
			{
				return this._nextCharacterHint;
			}
			set
			{
				if (value != this._nextCharacterHint)
				{
					this._nextCharacterHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "NextCharacterHint");
				}
			}
		}

		// Token: 0x17000A70 RID: 2672
		// (get) Token: 0x06001EAC RID: 7852 RVA: 0x0007122F File Offset: 0x0006F42F
		// (set) Token: 0x06001EAD RID: 7853 RVA: 0x00071237 File Offset: 0x0006F437
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x17000A71 RID: 2673
		// (get) Token: 0x06001EAE RID: 7854 RVA: 0x0007125A File Offset: 0x0006F45A
		// (set) Token: 0x06001EAF RID: 7855 RVA: 0x00071262 File Offset: 0x0006F462
		[DataSourceProperty]
		public string ResetLbl
		{
			get
			{
				return this._resetLbl;
			}
			set
			{
				if (value != this._resetLbl)
				{
					this._resetLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ResetLbl");
				}
			}
		}

		// Token: 0x17000A72 RID: 2674
		// (get) Token: 0x06001EB0 RID: 7856 RVA: 0x00071285 File Offset: 0x0006F485
		// (set) Token: 0x06001EB1 RID: 7857 RVA: 0x0007128D File Offset: 0x0006F48D
		[DataSourceProperty]
		public string CancelLbl
		{
			get
			{
				return this._cancelLbl;
			}
			set
			{
				if (value != this._cancelLbl)
				{
					this._cancelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelLbl");
				}
			}
		}

		// Token: 0x17000A73 RID: 2675
		// (get) Token: 0x06001EB2 RID: 7858 RVA: 0x000712B0 File Offset: 0x0006F4B0
		// (set) Token: 0x06001EB3 RID: 7859 RVA: 0x000712B8 File Offset: 0x0006F4B8
		[DataSourceProperty]
		public string SkillFocusText
		{
			get
			{
				return this._skillFocusText;
			}
			set
			{
				if (value != this._skillFocusText)
				{
					this._skillFocusText = value;
					base.OnPropertyChangedWithValue<string>(value, "SkillFocusText");
				}
			}
		}

		// Token: 0x17000A74 RID: 2676
		// (get) Token: 0x06001EB4 RID: 7860 RVA: 0x000712DB File Offset: 0x0006F4DB
		// (set) Token: 0x06001EB5 RID: 7861 RVA: 0x000712E3 File Offset: 0x0006F4E3
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

		// Token: 0x17000A75 RID: 2677
		// (get) Token: 0x06001EB6 RID: 7862 RVA: 0x00071306 File Offset: 0x0006F506
		// (set) Token: 0x06001EB7 RID: 7863 RVA: 0x0007130E File Offset: 0x0006F50E
		[DataSourceProperty]
		public string SkillsText
		{
			get
			{
				return this._skillsText;
			}
			set
			{
				if (value != this._skillsText)
				{
					this._skillsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SkillsText");
				}
			}
		}

		// Token: 0x17000A76 RID: 2678
		// (get) Token: 0x06001EB8 RID: 7864 RVA: 0x00071331 File Offset: 0x0006F531
		// (set) Token: 0x06001EB9 RID: 7865 RVA: 0x00071339 File Offset: 0x0006F539
		[DataSourceProperty]
		public int UnopenedPerksNumForOtherChars
		{
			get
			{
				return this._unopenedPerksNumForOtherChars;
			}
			set
			{
				if (value != this._unopenedPerksNumForOtherChars)
				{
					this._unopenedPerksNumForOtherChars = value;
					base.OnPropertyChangedWithValue(value, "UnopenedPerksNumForOtherChars");
				}
			}
		}

		// Token: 0x17000A77 RID: 2679
		// (get) Token: 0x06001EBA RID: 7866 RVA: 0x00071357 File Offset: 0x0006F557
		// (set) Token: 0x06001EBB RID: 7867 RVA: 0x0007135F File Offset: 0x0006F55F
		[DataSourceProperty]
		public bool HasUnopenedPerksForOtherCharacters
		{
			get
			{
				return this._hasUnopenedPerksForCurrentCharacter;
			}
			set
			{
				if (value != this._hasUnopenedPerksForCurrentCharacter)
				{
					this._hasUnopenedPerksForCurrentCharacter = value;
					base.OnPropertyChangedWithValue(value, "HasUnopenedPerksForOtherCharacters");
				}
			}
		}

		// Token: 0x06001EBC RID: 7868 RVA: 0x0007137D File Offset: 0x0006F57D
		private TextObject GetPreviousCharacterKeyText()
		{
			if (this.PreviousCharacterInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.PreviousCharacterInputKey.KeyID);
		}

		// Token: 0x06001EBD RID: 7869 RVA: 0x000713AB File Offset: 0x0006F5AB
		private TextObject GetNextCharacterKeyText()
		{
			if (this.NextCharacterInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.NextCharacterInputKey.KeyID);
		}

		// Token: 0x06001EBE RID: 7870 RVA: 0x000713D9 File Offset: 0x0006F5D9
		public void SetCancelInputKey(HotKey gameKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(gameKey, true);
		}

		// Token: 0x06001EBF RID: 7871 RVA: 0x000713E8 File Offset: 0x0006F5E8
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001EC0 RID: 7872 RVA: 0x000713F7 File Offset: 0x0006F5F7
		public void SetResetInputKey(HotKey hotKey)
		{
			this.ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001EC1 RID: 7873 RVA: 0x00071406 File Offset: 0x0006F606
		public void SetPreviousCharacterInputKey(HotKey hotKey)
		{
			this.PreviousCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetPreviousCharacterHint();
		}

		// Token: 0x06001EC2 RID: 7874 RVA: 0x0007141B File Offset: 0x0006F61B
		public void SetNextCharacterInputKey(HotKey hotKey)
		{
			this.NextCharacterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetNextCharacterHint();
		}

		// Token: 0x06001EC3 RID: 7875 RVA: 0x00071430 File Offset: 0x0006F630
		public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
		{
			this._getKeyTextFromKeyId = getKeyTextFromKeyId;
		}

		// Token: 0x17000A78 RID: 2680
		// (get) Token: 0x06001EC4 RID: 7876 RVA: 0x00071439 File Offset: 0x0006F639
		// (set) Token: 0x06001EC5 RID: 7877 RVA: 0x00071441 File Offset: 0x0006F641
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x17000A79 RID: 2681
		// (get) Token: 0x06001EC6 RID: 7878 RVA: 0x0007145F File Offset: 0x0006F65F
		// (set) Token: 0x06001EC7 RID: 7879 RVA: 0x00071467 File Offset: 0x0006F667
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000A7A RID: 2682
		// (get) Token: 0x06001EC8 RID: 7880 RVA: 0x00071485 File Offset: 0x0006F685
		// (set) Token: 0x06001EC9 RID: 7881 RVA: 0x0007148D File Offset: 0x0006F68D
		[DataSourceProperty]
		public InputKeyItemVM ResetInputKey
		{
			get
			{
				return this._resetInputKey;
			}
			set
			{
				if (value != this._resetInputKey)
				{
					this._resetInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ResetInputKey");
				}
			}
		}

		// Token: 0x17000A7B RID: 2683
		// (get) Token: 0x06001ECA RID: 7882 RVA: 0x000714AB File Offset: 0x0006F6AB
		// (set) Token: 0x06001ECB RID: 7883 RVA: 0x000714B3 File Offset: 0x0006F6B3
		[DataSourceProperty]
		public InputKeyItemVM PreviousCharacterInputKey
		{
			get
			{
				return this._previousCharacterInputKey;
			}
			set
			{
				if (value != this._previousCharacterInputKey)
				{
					this._previousCharacterInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PreviousCharacterInputKey");
				}
			}
		}

		// Token: 0x17000A7C RID: 2684
		// (get) Token: 0x06001ECC RID: 7884 RVA: 0x000714D1 File Offset: 0x0006F6D1
		// (set) Token: 0x06001ECD RID: 7885 RVA: 0x000714D9 File Offset: 0x0006F6D9
		[DataSourceProperty]
		public InputKeyItemVM NextCharacterInputKey
		{
			get
			{
				return this._nextCharacterInputKey;
			}
			set
			{
				if (value != this._nextCharacterInputKey)
				{
					this._nextCharacterInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "NextCharacterInputKey");
				}
			}
		}

		// Token: 0x04000E3C RID: 3644
		private readonly Action _closeCharacterDeveloper;

		// Token: 0x04000E3D RID: 3645
		private readonly List<CharacterDeveloperHeroItemVM> _heroList;

		// Token: 0x04000E3E RID: 3646
		private readonly IViewDataTracker _viewDataTracker;

		// Token: 0x04000E3F RID: 3647
		public readonly ReadOnlyCollection<CharacterDeveloperHeroItemVM> HeroList;

		// Token: 0x04000E40 RID: 3648
		private int _heroIndex;

		// Token: 0x04000E41 RID: 3649
		private string _latestTutorialElementID;

		// Token: 0x04000E42 RID: 3650
		private Func<string, TextObject> _getKeyTextFromKeyId;

		// Token: 0x04000E43 RID: 3651
		private bool _isActivePerkHighlightsApplied;

		// Token: 0x04000E44 RID: 3652
		private readonly string _availablePerksHighlighId = "AvailablePerks";

		// Token: 0x04000E45 RID: 3653
		private string _skillsText;

		// Token: 0x04000E46 RID: 3654
		private string _doneLbl;

		// Token: 0x04000E47 RID: 3655
		private string _resetLbl;

		// Token: 0x04000E48 RID: 3656
		private string _cancelLbl;

		// Token: 0x04000E49 RID: 3657
		private string _unspentFocusPointsText;

		// Token: 0x04000E4A RID: 3658
		private string _traitsText;

		// Token: 0x04000E4B RID: 3659
		private string _partyRoleText;

		// Token: 0x04000E4C RID: 3660
		private HintViewModel _unspentCharacterPointsHint;

		// Token: 0x04000E4D RID: 3661
		private HintViewModel _unspentAttributePointsHint;

		// Token: 0x04000E4E RID: 3662
		private BasicTooltipViewModel _previousCharacterHint;

		// Token: 0x04000E4F RID: 3663
		private BasicTooltipViewModel _nextCharacterHint;

		// Token: 0x04000E50 RID: 3664
		private string _addFocusText;

		// Token: 0x04000E51 RID: 3665
		private bool _isPlayerAccompanied;

		// Token: 0x04000E52 RID: 3666
		private string _skillFocusText;

		// Token: 0x04000E53 RID: 3667
		private ElementNotificationVM _tutorialNotification;

		// Token: 0x04000E54 RID: 3668
		private HintViewModel _resetHint;

		// Token: 0x04000E55 RID: 3669
		private HintViewModel _focusVisualHint;

		// Token: 0x04000E56 RID: 3670
		private CharacterDeveloperHeroItemVM _currentCharacter;

		// Token: 0x04000E57 RID: 3671
		private string _currentCharacterNameText;

		// Token: 0x04000E58 RID: 3672
		private SelectorVM<SelectorItemVM> _characterList;

		// Token: 0x04000E59 RID: 3673
		private int _unopenedPerksNumForOtherChars;

		// Token: 0x04000E5A RID: 3674
		private bool _hasUnopenedPerksForCurrentCharacter;

		// Token: 0x04000E5B RID: 3675
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000E5C RID: 3676
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000E5D RID: 3677
		private InputKeyItemVM _resetInputKey;

		// Token: 0x04000E5E RID: 3678
		private InputKeyItemVM _previousCharacterInputKey;

		// Token: 0x04000E5F RID: 3679
		private InputKeyItemVM _nextCharacterInputKey;
	}
}
