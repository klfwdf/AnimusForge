using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000156 RID: 342
	public class CharacterCreationReviewStageVM : CharacterCreationStageBaseVM
	{
		// Token: 0x06002006 RID: 8198 RVA: 0x00074E60 File Offset: 0x00073060
		public CharacterCreationReviewStageVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText, bool isBannerAndClanNameSet)
			: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
		{
			this.ReviewList = new MBBindingList<CharacterCreationReviewStageItemVM>();
			base.Title = new TextObject("{=txjiykNa}Review", null).ToString();
			base.Description = characterCreationManager.CharacterCreationContent.ReviewPageDescription.ToString();
			this._isBannerAndClanNameSet = isBannerAndClanNameSet;
			this.CannotAdvanceReasonHint = new HintViewModel();
			this.ClanBanner = new BannerImageIdentifierVM(Clan.PlayerClan.Banner, false);
			this.GainedPropertiesController = new CharacterCreationGainedPropertiesVM(this.CharacterCreationManager);
			this.Name = characterCreationManager.CharacterCreationContent.MainCharacterName;
			this.NameTextQuestion = new TextObject("{=mHVmrwRQ}Enter your name", null).ToString();
			this.AddReviewedItems();
			this.CameraControlKeys = new MBBindingList<InputKeyItemVM>();
		}

		// Token: 0x06002007 RID: 8199 RVA: 0x00074F3C File Offset: 0x0007313C
		private void AddReviewedItems()
		{
			string text = string.Empty;
			CultureObject selectedCulture = this.CharacterCreationManager.CharacterCreationContent.SelectedCulture;
			IEnumerable<FeatObject> culturalFeats = selectedCulture.GetCulturalFeats((FeatObject x) => x.IsPositive);
			IEnumerable<FeatObject> culturalFeats2 = selectedCulture.GetCulturalFeats((FeatObject x) => !x.IsPositive);
			foreach (FeatObject featObject in culturalFeats)
			{
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", featObject.Description);
				text = GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			foreach (FeatObject featObject2 in culturalFeats2)
			{
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", featObject2.Description);
				text = GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			CharacterCreationReviewStageItemVM item = new CharacterCreationReviewStageItemVM(new TextObject("{=K6GYskvJ}Culture:", null).ToString(), this.CharacterCreationManager.CharacterCreationContent.SelectedCulture.Name.ToString(), text);
			this.ReviewList.Add(item);
			foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> keyValuePair in this.CharacterCreationManager.SelectedOptions)
			{
				NarrativeMenu key = keyValuePair.Key;
				NarrativeMenuOption value = keyValuePair.Value;
				item = new CharacterCreationReviewStageItemVM(key.Title.ToString(), value.Text.ToString(), value.PositiveEffectText.ToString());
				this.ReviewList.Add(item);
			}
			if (this._isBannerAndClanNameSet)
			{
				CharacterCreationReviewStageItemVM item2 = new CharacterCreationReviewStageItemVM(new BannerImageIdentifierVM(Clan.PlayerClan.Banner, true), GameTexts.FindText("str_clan", null).ToString(), Clan.PlayerClan.Name.ToString(), null);
				this.ReviewList.Add(item2);
			}
		}

		// Token: 0x06002008 RID: 8200 RVA: 0x00075184 File Offset: 0x00073384
		public void ExecuteRandomizeName()
		{
			this.Name = NameGenerator.Current.GenerateFirstNameForPlayer(this.CharacterCreationManager.CharacterCreationContent.SelectedCulture, Hero.MainHero.IsFemale).ToString();
		}

		// Token: 0x06002009 RID: 8201 RVA: 0x000751B8 File Offset: 0x000733B8
		private void OnRefresh()
		{
			TextObject textObject = GameTexts.FindText("str_generic_character_firstname", null);
			textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(this.Name, null));
			TextObject textObject2 = GameTexts.FindText("str_generic_character_name", null);
			textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(this.Name, null));
			textObject2.SetTextVariable("CHARACTER_GENDER", Hero.MainHero.IsFemale ? 1 : 0);
			textObject.SetTextVariable("CHARACTER_GENDER", Hero.MainHero.IsFemale ? 1 : 0);
			Hero.MainHero.SetName(textObject2, textObject);
			base.CanAdvance = this.CanAdvanceToNextStage();
		}

		// Token: 0x0600200A RID: 8202 RVA: 0x0007525D File Offset: 0x0007345D
		public override void OnNextStage()
		{
			this._affirmativeAction();
		}

		// Token: 0x0600200B RID: 8203 RVA: 0x0007526A File Offset: 0x0007346A
		public override void OnPreviousStage()
		{
			this._negativeAction();
		}

		// Token: 0x0600200C RID: 8204 RVA: 0x00075278 File Offset: 0x00073478
		public override bool CanAdvanceToNextStage()
		{
			TextObject hintText = TextObject.GetEmpty();
			bool result = true;
			if (string.IsNullOrEmpty(this.Name) || string.IsNullOrWhiteSpace(this.Name))
			{
				hintText = new TextObject("{=IRcy3pWJ}Name cannot be empty", null);
				result = false;
			}
			Tuple<bool, string> tuple = CampaignUIHelper.IsStringApplicableForHeroName(this.Name);
			if (!tuple.Item1)
			{
				if (!string.IsNullOrEmpty(tuple.Item2))
				{
					hintText = new TextObject("{=!}" + tuple.Item2, null);
				}
				result = false;
			}
			this.CannotAdvanceReasonHint.HintText = hintText;
			return result;
		}

		// Token: 0x0600200D RID: 8205 RVA: 0x000752FC File Offset: 0x000734FC
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey != null)
			{
				cancelInputKey.OnFinalize();
			}
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			foreach (InputKeyItemVM inputKeyItemVM in this.CameraControlKeys)
			{
				inputKeyItemVM.OnFinalize();
			}
		}

		// Token: 0x0600200E RID: 8206 RVA: 0x00075370 File Offset: 0x00073570
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x0600200F RID: 8207 RVA: 0x0007537F File Offset: 0x0007357F
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06002010 RID: 8208 RVA: 0x00075390 File Offset: 0x00073590
		public void AddCameraControlInputKey(HotKey hotKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x06002011 RID: 8209 RVA: 0x000753B4 File Offset: 0x000735B4
		public void AddCameraControlInputKey(GameKey gameKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x06002012 RID: 8210 RVA: 0x000753D8 File Offset: 0x000735D8
		public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x17000AE8 RID: 2792
		// (get) Token: 0x06002013 RID: 8211 RVA: 0x00075404 File Offset: 0x00073604
		// (set) Token: 0x06002014 RID: 8212 RVA: 0x0007540C File Offset: 0x0007360C
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

		// Token: 0x17000AE9 RID: 2793
		// (get) Token: 0x06002015 RID: 8213 RVA: 0x0007542A File Offset: 0x0007362A
		// (set) Token: 0x06002016 RID: 8214 RVA: 0x00075432 File Offset: 0x00073632
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

		// Token: 0x17000AEA RID: 2794
		// (get) Token: 0x06002017 RID: 8215 RVA: 0x00075450 File Offset: 0x00073650
		// (set) Token: 0x06002018 RID: 8216 RVA: 0x00075458 File Offset: 0x00073658
		[DataSourceProperty]
		public MBBindingList<InputKeyItemVM> CameraControlKeys
		{
			get
			{
				return this._cameraControlKeys;
			}
			set
			{
				if (value != this._cameraControlKeys)
				{
					this._cameraControlKeys = value;
					base.OnPropertyChangedWithValue<MBBindingList<InputKeyItemVM>>(value, "CameraControlKeys");
				}
			}
		}

		// Token: 0x17000AEB RID: 2795
		// (get) Token: 0x06002019 RID: 8217 RVA: 0x00075476 File Offset: 0x00073676
		// (set) Token: 0x0600201A RID: 8218 RVA: 0x0007547E File Offset: 0x0007367E
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
					this.CharacterCreationManager.CharacterCreationContent.SetMainCharacterName(value);
					base.OnPropertyChangedWithValue<string>(value, "Name");
					this.OnRefresh();
				}
			}
		}

		// Token: 0x17000AEC RID: 2796
		// (get) Token: 0x0600201B RID: 8219 RVA: 0x000754B8 File Offset: 0x000736B8
		// (set) Token: 0x0600201C RID: 8220 RVA: 0x000754C0 File Offset: 0x000736C0
		[DataSourceProperty]
		public string NameTextQuestion
		{
			get
			{
				return this._nameTextQuestion;
			}
			set
			{
				if (value != this._nameTextQuestion)
				{
					this._nameTextQuestion = value;
					base.OnPropertyChangedWithValue<string>(value, "NameTextQuestion");
				}
			}
		}

		// Token: 0x17000AED RID: 2797
		// (get) Token: 0x0600201D RID: 8221 RVA: 0x000754E3 File Offset: 0x000736E3
		// (set) Token: 0x0600201E RID: 8222 RVA: 0x000754EB File Offset: 0x000736EB
		[DataSourceProperty]
		public MBBindingList<CharacterCreationReviewStageItemVM> ReviewList
		{
			get
			{
				return this._reviewList;
			}
			set
			{
				if (value != this._reviewList)
				{
					this._reviewList = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationReviewStageItemVM>>(value, "ReviewList");
				}
			}
		}

		// Token: 0x17000AEE RID: 2798
		// (get) Token: 0x0600201F RID: 8223 RVA: 0x00075509 File Offset: 0x00073709
		// (set) Token: 0x06002020 RID: 8224 RVA: 0x00075511 File Offset: 0x00073711
		[DataSourceProperty]
		public CharacterCreationGainedPropertiesVM GainedPropertiesController
		{
			get
			{
				return this._gainedPropertiesController;
			}
			set
			{
				if (value != this._gainedPropertiesController)
				{
					this._gainedPropertiesController = value;
					base.OnPropertyChangedWithValue<CharacterCreationGainedPropertiesVM>(value, "GainedPropertiesController");
				}
			}
		}

		// Token: 0x17000AEF RID: 2799
		// (get) Token: 0x06002021 RID: 8225 RVA: 0x0007552F File Offset: 0x0007372F
		// (set) Token: 0x06002022 RID: 8226 RVA: 0x00075537 File Offset: 0x00073737
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner
		{
			get
			{
				return this._clanBanner;
			}
			set
			{
				if (value != this._clanBanner)
				{
					this._clanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner");
				}
			}
		}

		// Token: 0x17000AF0 RID: 2800
		// (get) Token: 0x06002023 RID: 8227 RVA: 0x00075555 File Offset: 0x00073755
		// (set) Token: 0x06002024 RID: 8228 RVA: 0x0007555D File Offset: 0x0007375D
		[DataSourceProperty]
		public HintViewModel CannotAdvanceReasonHint
		{
			get
			{
				return this._cannotAdvanceReasonHint;
			}
			set
			{
				if (value != this._cannotAdvanceReasonHint)
				{
					this._cannotAdvanceReasonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CannotAdvanceReasonHint");
				}
			}
		}

		// Token: 0x17000AF1 RID: 2801
		// (get) Token: 0x06002025 RID: 8229 RVA: 0x0007557B File Offset: 0x0007377B
		// (set) Token: 0x06002026 RID: 8230 RVA: 0x00075583 File Offset: 0x00073783
		[DataSourceProperty]
		public bool CharacterGamepadControlsEnabled
		{
			get
			{
				return this._characterGamepadControlsEnabled;
			}
			set
			{
				if (value != this._characterGamepadControlsEnabled)
				{
					this._characterGamepadControlsEnabled = value;
					base.OnPropertyChangedWithValue(value, "CharacterGamepadControlsEnabled");
				}
			}
		}

		// Token: 0x04000EE9 RID: 3817
		private bool _isBannerAndClanNameSet;

		// Token: 0x04000EEA RID: 3818
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000EEB RID: 3819
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000EEC RID: 3820
		private MBBindingList<InputKeyItemVM> _cameraControlKeys;

		// Token: 0x04000EED RID: 3821
		private string _name = "";

		// Token: 0x04000EEE RID: 3822
		private string _nameTextQuestion = "";

		// Token: 0x04000EEF RID: 3823
		private MBBindingList<CharacterCreationReviewStageItemVM> _reviewList;

		// Token: 0x04000EF0 RID: 3824
		private CharacterCreationGainedPropertiesVM _gainedPropertiesController;

		// Token: 0x04000EF1 RID: 3825
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x04000EF2 RID: 3826
		private HintViewModel _cannotAdvanceReasonHint;

		// Token: 0x04000EF3 RID: 3827
		private bool _characterGamepadControlsEnabled;
	}
}
