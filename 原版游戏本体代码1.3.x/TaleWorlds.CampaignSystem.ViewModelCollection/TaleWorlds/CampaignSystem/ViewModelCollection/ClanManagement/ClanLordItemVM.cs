using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000126 RID: 294
	public class ClanLordItemVM : ViewModel
	{
		// Token: 0x06001A8C RID: 6796 RVA: 0x00063738 File Offset: 0x00061938
		public ClanLordItemVM(Hero hero, ITeleportationCampaignBehavior teleportationBehavior, Action<Hero> showHeroOnMap, Action<ClanLordItemVM> onCharacterSelect, Action onRecall, Action onTalk)
		{
			this._hero = hero;
			this._onCharacterSelect = onCharacterSelect;
			this._onRecall = onRecall;
			this._onTalk = onTalk;
			this._showHeroOnMap = showHeroOnMap;
			this._teleportationBehavior = teleportationBehavior;
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false);
			this.Visual = new CharacterImageIdentifierVM(characterCode);
			this.Skills = new MBBindingList<EncyclopediaSkillVM>();
			this.Traits = new MBBindingList<EncyclopediaTraitItemVM>();
			this.IsFamilyMember = Hero.MainHero.Clan.AliveLords.Contains(this._hero);
			this.Banner_9 = new BannerImageIdentifierVM(hero.ClanBanner, true);
			this.RefreshValues();
		}

		// Token: 0x06001A8D RID: 6797 RVA: 0x00063824 File Offset: 0x00061A24
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._hero.Name.ToString();
			StringHelpers.SetCharacterProperties("NPC", this._hero.CharacterObject, null, false);
			this.CurrentActionText = ((this._hero != Hero.MainHero) ? CampaignUIHelper.GetHeroBehaviorText(this._hero, this._teleportationBehavior) : "");
			this.LocationText = this.CurrentActionText;
			this.PregnantHint = new HintViewModel(GameTexts.FindText("str_pregnant", null), null);
			this.UpdateProperties();
		}

		// Token: 0x06001A8E RID: 6798 RVA: 0x000638B9 File Offset: 0x00061AB9
		public void ExecuteLocationLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x06001A8F RID: 6799 RVA: 0x000638CC File Offset: 0x00061ACC
		public void UpdateProperties()
		{
			this.RelationToMainHeroText = "";
			this.GovernorOfText = "";
			this.Skills.Clear();
			this.Traits.Clear();
			this.IsMainHero = this._hero == Hero.MainHero;
			this.IsPregnant = this._hero.IsPregnant;
			List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList<SkillObject>();
			list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			foreach (SkillObject skill in list)
			{
				this.Skills.Add(new EncyclopediaSkillVM(skill, this._hero.GetSkillValue(skill)));
			}
			foreach (TraitObject traitObject in CampaignUIHelper.GetHeroTraits())
			{
				if (this._hero.GetTraitLevel(traitObject) != 0)
				{
					this.Traits.Add(new EncyclopediaTraitItemVM(traitObject, this._hero));
				}
			}
			this.IsChild = FaceGen.GetMaturityTypeWithAge(this._hero.Age) <= BodyMeshMaturityType.Child;
			if (this._hero != Hero.MainHero)
			{
				this.RelationToMainHeroText = CampaignUIHelper.GetHeroRelationToHeroText(this._hero, Hero.MainHero, true).ToString();
			}
			if (this._hero.GovernorOf != null)
			{
				GameTexts.SetVariable("SETTLEMENT_NAME", this._hero.GovernorOf.Owner.Settlement.EncyclopediaLinkWithName);
				this.GovernorOfText = GameTexts.FindText("str_governor_of_label", null).ToString();
			}
			this.HeroModel = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.HeroModel.FillFrom(this._hero, -1, false, false);
			this.Banner_9 = new BannerImageIdentifierVM(this._hero.ClanBanner, true);
			bool flag = MobileParty.MainParty.CurrentSettlement == null || MobileParty.MainParty.CurrentSettlement == this._hero.CurrentSettlement;
			this.CanShowLocationOfHero = this._hero.GetCampaignPosition().IsValid() && this._hero.PartyBelongedTo != MobileParty.MainParty && flag;
			this.ShowOnMapHint = new HintViewModel(this.CanShowLocationOfHero ? this._showLocationOfHeroOnMap : TextObject.GetEmpty(), null);
			TextObject empty = TextObject.GetEmpty();
			bool flag2 = this._hero.PartyBelongedTo == MobileParty.MainParty;
			this.IsTalkVisible = flag2 && !this.IsMainHero;
			this.IsTalkEnabled = this.IsTalkVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out empty);
			bool flag3;
			bool flag4;
			IMapPoint mapPoint;
			this.IsTeleporting = this._teleportationBehavior.GetTargetOfTeleportingHero(this._hero, out flag3, out flag4, out mapPoint);
			TextObject empty2 = TextObject.GetEmpty();
			this.IsRecallVisible = !this.IsMainHero && !flag2 && !this.IsTeleporting;
			this.IsRecallEnabled = this.IsRecallVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out empty2) && FactionHelper.IsMainClanMemberAvailableForRecall(this._hero, MobileParty.MainParty, out empty2);
			this.RecallHint = new HintViewModel(this.IsRecallEnabled ? this._recallHeroToMainPartyHintText : empty2, null);
			this.TalkHint = new HintViewModel(this.IsTalkEnabled ? this._talkToHeroHintText : empty, null);
		}

		// Token: 0x06001A90 RID: 6800 RVA: 0x00063C28 File Offset: 0x00061E28
		public void ExecuteLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this._hero.EncyclopediaLink);
		}

		// Token: 0x06001A91 RID: 6801 RVA: 0x00063C44 File Offset: 0x00061E44
		public void OnCharacterSelect()
		{
			this._onCharacterSelect(this);
		}

		// Token: 0x06001A92 RID: 6802 RVA: 0x00063C52 File Offset: 0x00061E52
		public virtual void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(Hero), new object[] { this._hero, true });
		}

		// Token: 0x06001A93 RID: 6803 RVA: 0x00063C7B File Offset: 0x00061E7B
		public virtual void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001A94 RID: 6804 RVA: 0x00063C82 File Offset: 0x00061E82
		public Hero GetHero()
		{
			return this._hero;
		}

		// Token: 0x06001A95 RID: 6805 RVA: 0x00063C8C File Offset: 0x00061E8C
		public void ExecuteRename()
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=2lFwF07j}Change Name", null).ToString(), string.Empty, true, true, GameTexts.FindText("str_done", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action<string>(this.OnNamingHeroOver), null, false, new Func<string, Tuple<bool, string>>(CampaignUIHelper.IsStringApplicableForHeroName), "", ""), false, false);
		}

		// Token: 0x06001A96 RID: 6806 RVA: 0x00063D00 File Offset: 0x00061F00
		private void OnNamingHeroOver(string suggestedName)
		{
			if (CampaignUIHelper.IsStringApplicableForHeroName(suggestedName).Item1)
			{
				TextObject textObject = GameTexts.FindText("str_generic_character_firstname", null);
				textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(suggestedName, null));
				TextObject textObject2 = GameTexts.FindText("str_generic_character_name", null);
				textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(suggestedName, null));
				textObject2.SetTextVariable("CHARACTER_GENDER", this._hero.IsFemale ? 1 : 0);
				textObject.SetTextVariable("CHARACTER_GENDER", this._hero.IsFemale ? 1 : 0);
				this._hero.SetName(textObject2, textObject);
				this.Name = suggestedName;
				MobileParty partyBelongedTo = this._hero.PartyBelongedTo;
				if (((partyBelongedTo != null) ? partyBelongedTo.Army : null) != null && this._hero.PartyBelongedTo.Army.LeaderParty.Owner == this._hero)
				{
					this._hero.PartyBelongedTo.Army.UpdateName();
					return;
				}
			}
			else
			{
				Debug.FailedAssert("Suggested name is not acceptable. This shouldn't happen", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanLordItemVM.cs", "OnNamingHeroOver", 203);
			}
		}

		// Token: 0x06001A97 RID: 6807 RVA: 0x00063E13 File Offset: 0x00062013
		public void ExecuteShowOnMap()
		{
			if (this._hero != null && this.CanShowLocationOfHero)
			{
				this._showHeroOnMap(this._hero);
			}
		}

		// Token: 0x06001A98 RID: 6808 RVA: 0x00063E36 File Offset: 0x00062036
		public void ExecuteRecall()
		{
			Action onRecall = this._onRecall;
			if (onRecall == null)
			{
				return;
			}
			onRecall();
		}

		// Token: 0x06001A99 RID: 6809 RVA: 0x00063E48 File Offset: 0x00062048
		public void ExecuteTalk()
		{
			Action onTalk = this._onTalk;
			if (onTalk == null)
			{
				return;
			}
			onTalk();
		}

		// Token: 0x06001A9A RID: 6810 RVA: 0x00063E5A File Offset: 0x0006205A
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.HeroModel.OnFinalize();
		}

		// Token: 0x170008F2 RID: 2290
		// (get) Token: 0x06001A9B RID: 6811 RVA: 0x00063E6D File Offset: 0x0006206D
		// (set) Token: 0x06001A9C RID: 6812 RVA: 0x00063E75 File Offset: 0x00062075
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSkillVM> Skills
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
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSkillVM>>(value, "Skills");
				}
			}
		}

		// Token: 0x170008F3 RID: 2291
		// (get) Token: 0x06001A9D RID: 6813 RVA: 0x00063E93 File Offset: 0x00062093
		// (set) Token: 0x06001A9E RID: 6814 RVA: 0x00063E9B File Offset: 0x0006209B
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

		// Token: 0x170008F4 RID: 2292
		// (get) Token: 0x06001A9F RID: 6815 RVA: 0x00063EB9 File Offset: 0x000620B9
		// (set) Token: 0x06001AA0 RID: 6816 RVA: 0x00063EC1 File Offset: 0x000620C1
		[DataSourceProperty]
		public HeroViewModel HeroModel
		{
			get
			{
				return this._heroModel;
			}
			set
			{
				if (value != this._heroModel)
				{
					this._heroModel = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "HeroModel");
				}
			}
		}

		// Token: 0x170008F5 RID: 2293
		// (get) Token: 0x06001AA1 RID: 6817 RVA: 0x00063EDF File Offset: 0x000620DF
		// (set) Token: 0x06001AA2 RID: 6818 RVA: 0x00063EE7 File Offset: 0x000620E7
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170008F6 RID: 2294
		// (get) Token: 0x06001AA3 RID: 6819 RVA: 0x00063F05 File Offset: 0x00062105
		// (set) Token: 0x06001AA4 RID: 6820 RVA: 0x00063F0D File Offset: 0x0006210D
		[DataSourceProperty]
		public bool IsChild
		{
			get
			{
				return this._isChild;
			}
			set
			{
				if (value != this._isChild)
				{
					this._isChild = value;
					base.OnPropertyChangedWithValue(value, "IsChild");
				}
			}
		}

		// Token: 0x170008F7 RID: 2295
		// (get) Token: 0x06001AA5 RID: 6821 RVA: 0x00063F2B File Offset: 0x0006212B
		// (set) Token: 0x06001AA6 RID: 6822 RVA: 0x00063F33 File Offset: 0x00062133
		[DataSourceProperty]
		public bool IsTeleporting
		{
			get
			{
				return this._isTeleporting;
			}
			set
			{
				if (value != this._isTeleporting)
				{
					this._isTeleporting = value;
					base.OnPropertyChangedWithValue(value, "IsTeleporting");
				}
			}
		}

		// Token: 0x170008F8 RID: 2296
		// (get) Token: 0x06001AA7 RID: 6823 RVA: 0x00063F51 File Offset: 0x00062151
		// (set) Token: 0x06001AA8 RID: 6824 RVA: 0x00063F59 File Offset: 0x00062159
		[DataSourceProperty]
		public bool IsRecallVisible
		{
			get
			{
				return this._isRecallVisible;
			}
			set
			{
				if (value != this._isRecallVisible)
				{
					this._isRecallVisible = value;
					base.OnPropertyChangedWithValue(value, "IsRecallVisible");
				}
			}
		}

		// Token: 0x170008F9 RID: 2297
		// (get) Token: 0x06001AA9 RID: 6825 RVA: 0x00063F77 File Offset: 0x00062177
		// (set) Token: 0x06001AAA RID: 6826 RVA: 0x00063F7F File Offset: 0x0006217F
		[DataSourceProperty]
		public bool IsRecallEnabled
		{
			get
			{
				return this._isRecallEnabled;
			}
			set
			{
				if (value != this._isRecallEnabled)
				{
					this._isRecallEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsRecallEnabled");
				}
			}
		}

		// Token: 0x170008FA RID: 2298
		// (get) Token: 0x06001AAB RID: 6827 RVA: 0x00063F9D File Offset: 0x0006219D
		// (set) Token: 0x06001AAC RID: 6828 RVA: 0x00063FA5 File Offset: 0x000621A5
		[DataSourceProperty]
		public bool IsTalkVisible
		{
			get
			{
				return this._isTalkVisible;
			}
			set
			{
				if (value != this._isTalkVisible)
				{
					this._isTalkVisible = value;
					base.OnPropertyChangedWithValue(value, "IsTalkVisible");
				}
			}
		}

		// Token: 0x170008FB RID: 2299
		// (get) Token: 0x06001AAD RID: 6829 RVA: 0x00063FC3 File Offset: 0x000621C3
		// (set) Token: 0x06001AAE RID: 6830 RVA: 0x00063FCB File Offset: 0x000621CB
		[DataSourceProperty]
		public bool IsTalkEnabled
		{
			get
			{
				return this._isTalkEnabled;
			}
			set
			{
				if (value != this._isTalkEnabled)
				{
					this._isTalkEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsTalkEnabled");
				}
			}
		}

		// Token: 0x170008FC RID: 2300
		// (get) Token: 0x06001AAF RID: 6831 RVA: 0x00063FE9 File Offset: 0x000621E9
		// (set) Token: 0x06001AB0 RID: 6832 RVA: 0x00063FF1 File Offset: 0x000621F1
		[DataSourceProperty]
		public bool CanShowLocationOfHero
		{
			get
			{
				return this._canShowLocationOfHero;
			}
			set
			{
				if (value != this._canShowLocationOfHero)
				{
					this._canShowLocationOfHero = value;
					base.OnPropertyChangedWithValue(value, "CanShowLocationOfHero");
				}
			}
		}

		// Token: 0x170008FD RID: 2301
		// (get) Token: 0x06001AB1 RID: 6833 RVA: 0x0006400F File Offset: 0x0006220F
		// (set) Token: 0x06001AB2 RID: 6834 RVA: 0x00064017 File Offset: 0x00062217
		[DataSourceProperty]
		public bool IsMainHero
		{
			get
			{
				return this._isMainHero;
			}
			set
			{
				if (value != this._isMainHero)
				{
					this._isMainHero = value;
					base.OnPropertyChangedWithValue(value, "IsMainHero");
				}
			}
		}

		// Token: 0x170008FE RID: 2302
		// (get) Token: 0x06001AB3 RID: 6835 RVA: 0x00064035 File Offset: 0x00062235
		// (set) Token: 0x06001AB4 RID: 6836 RVA: 0x0006403D File Offset: 0x0006223D
		[DataSourceProperty]
		public bool IsFamilyMember
		{
			get
			{
				return this._isFamilyMember;
			}
			set
			{
				if (value != this._isFamilyMember)
				{
					this._isFamilyMember = value;
					base.OnPropertyChangedWithValue(value, "IsFamilyMember");
				}
			}
		}

		// Token: 0x170008FF RID: 2303
		// (get) Token: 0x06001AB5 RID: 6837 RVA: 0x0006405B File Offset: 0x0006225B
		// (set) Token: 0x06001AB6 RID: 6838 RVA: 0x00064063 File Offset: 0x00062263
		[DataSourceProperty]
		public bool IsPregnant
		{
			get
			{
				return this._isPregnant;
			}
			set
			{
				if (value != this._isPregnant)
				{
					this._isPregnant = value;
					base.OnPropertyChangedWithValue(value, "IsPregnant");
				}
			}
		}

		// Token: 0x17000900 RID: 2304
		// (get) Token: 0x06001AB7 RID: 6839 RVA: 0x00064081 File Offset: 0x00062281
		// (set) Token: 0x06001AB8 RID: 6840 RVA: 0x00064089 File Offset: 0x00062289
		[DataSourceProperty]
		public CharacterImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x17000901 RID: 2305
		// (get) Token: 0x06001AB9 RID: 6841 RVA: 0x000640A7 File Offset: 0x000622A7
		// (set) Token: 0x06001ABA RID: 6842 RVA: 0x000640AF File Offset: 0x000622AF
		[DataSourceProperty]
		public BannerImageIdentifierVM Banner_9
		{
			get
			{
				return this._banner_9;
			}
			set
			{
				if (value != this._banner_9)
				{
					this._banner_9 = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner_9");
				}
			}
		}

		// Token: 0x17000902 RID: 2306
		// (get) Token: 0x06001ABB RID: 6843 RVA: 0x000640CD File Offset: 0x000622CD
		// (set) Token: 0x06001ABC RID: 6844 RVA: 0x000640D5 File Offset: 0x000622D5
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._locationText;
			}
			set
			{
				if (value != this._locationText)
				{
					this._locationText = value;
					base.OnPropertyChangedWithValue<string>(value, "LocationText");
				}
			}
		}

		// Token: 0x17000903 RID: 2307
		// (get) Token: 0x06001ABD RID: 6845 RVA: 0x000640F8 File Offset: 0x000622F8
		// (set) Token: 0x06001ABE RID: 6846 RVA: 0x00064100 File Offset: 0x00062300
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

		// Token: 0x17000904 RID: 2308
		// (get) Token: 0x06001ABF RID: 6847 RVA: 0x00064123 File Offset: 0x00062323
		// (set) Token: 0x06001AC0 RID: 6848 RVA: 0x0006412B File Offset: 0x0006232B
		[DataSourceProperty]
		public string RelationToMainHeroText
		{
			get
			{
				return this._relationToMainHeroText;
			}
			set
			{
				if (value != this._relationToMainHeroText)
				{
					this._relationToMainHeroText = value;
					base.OnPropertyChangedWithValue<string>(value, "RelationToMainHeroText");
				}
			}
		}

		// Token: 0x17000905 RID: 2309
		// (get) Token: 0x06001AC1 RID: 6849 RVA: 0x0006414E File Offset: 0x0006234E
		// (set) Token: 0x06001AC2 RID: 6850 RVA: 0x00064156 File Offset: 0x00062356
		[DataSourceProperty]
		public string GovernorOfText
		{
			get
			{
				return this._governorOfText;
			}
			set
			{
				if (value != this._governorOfText)
				{
					this._governorOfText = value;
					base.OnPropertyChangedWithValue<string>(value, "GovernorOfText");
				}
			}
		}

		// Token: 0x17000906 RID: 2310
		// (get) Token: 0x06001AC3 RID: 6851 RVA: 0x00064179 File Offset: 0x00062379
		// (set) Token: 0x06001AC4 RID: 6852 RVA: 0x00064181 File Offset: 0x00062381
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

		// Token: 0x17000907 RID: 2311
		// (get) Token: 0x06001AC5 RID: 6853 RVA: 0x000641A4 File Offset: 0x000623A4
		// (set) Token: 0x06001AC6 RID: 6854 RVA: 0x000641AC File Offset: 0x000623AC
		[DataSourceProperty]
		public HintViewModel PregnantHint
		{
			get
			{
				return this._pregnantHint;
			}
			set
			{
				if (value != this._pregnantHint)
				{
					this._pregnantHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PregnantHint");
				}
			}
		}

		// Token: 0x17000908 RID: 2312
		// (get) Token: 0x06001AC7 RID: 6855 RVA: 0x000641CA File Offset: 0x000623CA
		// (set) Token: 0x06001AC8 RID: 6856 RVA: 0x000641D2 File Offset: 0x000623D2
		[DataSourceProperty]
		public HintViewModel ShowOnMapHint
		{
			get
			{
				return this._showOnMapHint;
			}
			set
			{
				if (value != this._showOnMapHint)
				{
					this._showOnMapHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ShowOnMapHint");
				}
			}
		}

		// Token: 0x17000909 RID: 2313
		// (get) Token: 0x06001AC9 RID: 6857 RVA: 0x000641F0 File Offset: 0x000623F0
		// (set) Token: 0x06001ACA RID: 6858 RVA: 0x000641F8 File Offset: 0x000623F8
		[DataSourceProperty]
		public HintViewModel RecallHint
		{
			get
			{
				return this._recallHint;
			}
			set
			{
				if (value != this._recallHint)
				{
					this._recallHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RecallHint");
				}
			}
		}

		// Token: 0x1700090A RID: 2314
		// (get) Token: 0x06001ACB RID: 6859 RVA: 0x00064216 File Offset: 0x00062416
		// (set) Token: 0x06001ACC RID: 6860 RVA: 0x0006421E File Offset: 0x0006241E
		[DataSourceProperty]
		public HintViewModel TalkHint
		{
			get
			{
				return this._talkHint;
			}
			set
			{
				if (value != this._talkHint)
				{
					this._talkHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "TalkHint");
				}
			}
		}

		// Token: 0x04000C5F RID: 3167
		private readonly Action<ClanLordItemVM> _onCharacterSelect;

		// Token: 0x04000C60 RID: 3168
		private readonly Action _onRecall;

		// Token: 0x04000C61 RID: 3169
		private readonly Action _onTalk;

		// Token: 0x04000C62 RID: 3170
		private readonly Hero _hero;

		// Token: 0x04000C63 RID: 3171
		private readonly Action<Hero> _showHeroOnMap;

		// Token: 0x04000C64 RID: 3172
		private readonly ITeleportationCampaignBehavior _teleportationBehavior;

		// Token: 0x04000C65 RID: 3173
		private readonly TextObject _prisonerOfText = new TextObject("{=a8nRxITn}Prisoner of {PARTY_NAME}", null);

		// Token: 0x04000C66 RID: 3174
		private readonly TextObject _showLocationOfHeroOnMap = new TextObject("{=aGJYQOef}Show hero's location on map.", null);

		// Token: 0x04000C67 RID: 3175
		private readonly TextObject _recallHeroToMainPartyHintText = new TextObject("{=ANV8UV5f}Recall this member to your party.", null);

		// Token: 0x04000C68 RID: 3176
		private readonly TextObject _talkToHeroHintText = new TextObject("{=j4BdjLYp}Start a conversation with this clan member.", null);

		// Token: 0x04000C69 RID: 3177
		private CharacterImageIdentifierVM _visual;

		// Token: 0x04000C6A RID: 3178
		private BannerImageIdentifierVM _banner_9;

		// Token: 0x04000C6B RID: 3179
		private bool _isSelected;

		// Token: 0x04000C6C RID: 3180
		private bool _isChild;

		// Token: 0x04000C6D RID: 3181
		private bool _isMainHero;

		// Token: 0x04000C6E RID: 3182
		private bool _isFamilyMember;

		// Token: 0x04000C6F RID: 3183
		private bool _isPregnant;

		// Token: 0x04000C70 RID: 3184
		private bool _isTeleporting;

		// Token: 0x04000C71 RID: 3185
		private bool _isRecallVisible;

		// Token: 0x04000C72 RID: 3186
		private bool _isRecallEnabled;

		// Token: 0x04000C73 RID: 3187
		private bool _isTalkVisible;

		// Token: 0x04000C74 RID: 3188
		private bool _isTalkEnabled;

		// Token: 0x04000C75 RID: 3189
		private bool _canShowLocationOfHero;

		// Token: 0x04000C76 RID: 3190
		private string _name;

		// Token: 0x04000C77 RID: 3191
		private string _locationText;

		// Token: 0x04000C78 RID: 3192
		private string _relationToMainHeroText;

		// Token: 0x04000C79 RID: 3193
		private string _governorOfText;

		// Token: 0x04000C7A RID: 3194
		private string _currentActionText;

		// Token: 0x04000C7B RID: 3195
		private HeroViewModel _heroModel;

		// Token: 0x04000C7C RID: 3196
		private MBBindingList<EncyclopediaSkillVM> _skills;

		// Token: 0x04000C7D RID: 3197
		private MBBindingList<EncyclopediaTraitItemVM> _traits;

		// Token: 0x04000C7E RID: 3198
		private HintViewModel _pregnantHint;

		// Token: 0x04000C7F RID: 3199
		private HintViewModel _showOnMapHint;

		// Token: 0x04000C80 RID: 3200
		private HintViewModel _recallHint;

		// Token: 0x04000C81 RID: 3201
		private HintViewModel _talkHint;
	}
}
