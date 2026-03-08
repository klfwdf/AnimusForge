using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x0200014D RID: 333
	public class CharacterCreationCultureStageVM : CharacterCreationStageBaseVM
	{
		// Token: 0x06001F89 RID: 8073 RVA: 0x00073380 File Offset: 0x00071580
		public CharacterCreationCultureStageVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText, Action<CultureObject> onCultureSelected)
			: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
		{
			this._onCultureSelected = onCultureSelected;
			CharacterCreationContent currentContent = (GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent;
			this.Cultures = new MBBindingList<CharacterCreationCultureVM>();
			base.Title = GameTexts.FindText("str_culture", null).ToString();
			base.Description = new TextObject("{=fz2kQjFS}Choose your character's culture:", null).ToString();
			base.SelectionText = new TextObject("{=MaHMOzL2}Character Culture", null).ToString();
			foreach (CultureObject culture in currentContent.GetCultures())
			{
				CharacterCreationCultureVM item = new CharacterCreationCultureVM(culture, new Action<CharacterCreationCultureVM>(this.OnCultureSelection));
				this.Cultures.Add(item);
			}
			this.SortCultureList(this.Cultures);
			if (currentContent.SelectedCulture != null)
			{
				CharacterCreationCultureVM characterCreationCultureVM = this.Cultures.FirstOrDefault((CharacterCreationCultureVM c) => c.Culture == currentContent.SelectedCulture);
				if (characterCreationCultureVM != null)
				{
					this.OnCultureSelection(characterCreationCultureVM);
				}
			}
		}

		// Token: 0x06001F8A RID: 8074 RVA: 0x000734AC File Offset: 0x000716AC
		private void SortCultureList(MBBindingList<CharacterCreationCultureVM> listToWorkOn)
		{
			int swapFromIndex = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("vlan")));
			this.Swap(listToWorkOn, swapFromIndex, 0);
			int swapFromIndex2 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("stur")));
			this.Swap(listToWorkOn, swapFromIndex2, 1);
			int swapFromIndex3 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("empi")));
			this.Swap(listToWorkOn, swapFromIndex3, 2);
			int swapFromIndex4 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("aser")));
			this.Swap(listToWorkOn, swapFromIndex4, 3);
			int swapFromIndex5 = listToWorkOn.IndexOf(listToWorkOn.Single((CharacterCreationCultureVM i) => i.CultureID.Contains("khuz")));
			this.Swap(listToWorkOn, swapFromIndex5, 4);
		}

		// Token: 0x06001F8B RID: 8075 RVA: 0x000735C4 File Offset: 0x000717C4
		public void OnCultureSelection(CharacterCreationCultureVM selectedCulture)
		{
			this.InitializePlayersFaceKeyAccordingToCultureSelection(selectedCulture);
			foreach (CharacterCreationCultureVM characterCreationCultureVM in from c in this.Cultures
				where c.IsSelected
				select c)
			{
				characterCreationCultureVM.IsSelected = false;
			}
			selectedCulture.IsSelected = true;
			this.CurrentSelectedCulture = selectedCulture;
			base.AnyItemSelected = true;
			base.CanAdvance = this.CanAdvanceToNextStage();
			Action<CultureObject> onCultureSelected = this._onCultureSelected;
			if (onCultureSelected == null)
			{
				return;
			}
			onCultureSelected(selectedCulture.Culture);
		}

		// Token: 0x06001F8C RID: 8076 RVA: 0x00073674 File Offset: 0x00071874
		private void InitializePlayersFaceKeyAccordingToCultureSelection(CharacterCreationCultureVM selectedCulture)
		{
			if (selectedCulture.Culture.DefaultCharacterCreationBodyProperty != null)
			{
				CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(selectedCulture.Culture.DefaultCharacterCreationBodyProperty.BodyPropertyMax, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
				Hero.MainHero.Culture = selectedCulture.Culture;
			}
		}

		// Token: 0x06001F8D RID: 8077 RVA: 0x000736CC File Offset: 0x000718CC
		private void Swap(MBBindingList<CharacterCreationCultureVM> listToWorkOn, int swapFromIndex, int swapToIndex)
		{
			if (swapFromIndex != swapToIndex)
			{
				CharacterCreationCultureVM value = listToWorkOn[swapToIndex];
				listToWorkOn[swapToIndex] = listToWorkOn[swapFromIndex];
				listToWorkOn[swapFromIndex] = value;
			}
		}

		// Token: 0x06001F8E RID: 8078 RVA: 0x000736FC File Offset: 0x000718FC
		public override void OnNextStage()
		{
			if (this.CurrentSelectedCulture == null)
			{
				Debug.FailedAssert("Selected culture can't be null at this stage", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterCreation\\CharacterCreationCultureStageVM.cs", "OnNextStage", 111);
				return;
			}
			(GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent.SetSelectedCulture(this.CurrentSelectedCulture.Culture, this.CharacterCreationManager);
			this._affirmativeAction();
		}

		// Token: 0x06001F8F RID: 8079 RVA: 0x00073762 File Offset: 0x00071962
		public override void OnPreviousStage()
		{
			this._negativeAction();
		}

		// Token: 0x06001F90 RID: 8080 RVA: 0x0007376F File Offset: 0x0007196F
		public override bool CanAdvanceToNextStage()
		{
			return this.Cultures.Any((CharacterCreationCultureVM s) => s.IsSelected);
		}

		// Token: 0x06001F91 RID: 8081 RVA: 0x0007379B File Offset: 0x0007199B
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey != null)
			{
				cancelInputKey.OnFinalize();
			}
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey == null)
			{
				return;
			}
			doneInputKey.OnFinalize();
		}

		// Token: 0x06001F92 RID: 8082 RVA: 0x000737C4 File Offset: 0x000719C4
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001F93 RID: 8083 RVA: 0x000737D3 File Offset: 0x000719D3
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000ABE RID: 2750
		// (get) Token: 0x06001F94 RID: 8084 RVA: 0x000737E2 File Offset: 0x000719E2
		// (set) Token: 0x06001F95 RID: 8085 RVA: 0x000737EA File Offset: 0x000719EA
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

		// Token: 0x17000ABF RID: 2751
		// (get) Token: 0x06001F96 RID: 8086 RVA: 0x00073808 File Offset: 0x00071A08
		// (set) Token: 0x06001F97 RID: 8087 RVA: 0x00073810 File Offset: 0x00071A10
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

		// Token: 0x17000AC0 RID: 2752
		// (get) Token: 0x06001F98 RID: 8088 RVA: 0x0007382E File Offset: 0x00071A2E
		// (set) Token: 0x06001F99 RID: 8089 RVA: 0x00073836 File Offset: 0x00071A36
		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value != this._isActive)
				{
					this._isActive = value;
					base.OnPropertyChangedWithValue(value, "IsActive");
				}
			}
		}

		// Token: 0x17000AC1 RID: 2753
		// (get) Token: 0x06001F9A RID: 8090 RVA: 0x00073854 File Offset: 0x00071A54
		// (set) Token: 0x06001F9B RID: 8091 RVA: 0x0007385C File Offset: 0x00071A5C
		[DataSourceProperty]
		public MBBindingList<CharacterCreationCultureVM> Cultures
		{
			get
			{
				return this._cultures;
			}
			set
			{
				if (value != this._cultures)
				{
					this._cultures = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationCultureVM>>(value, "Cultures");
				}
			}
		}

		// Token: 0x17000AC2 RID: 2754
		// (get) Token: 0x06001F9C RID: 8092 RVA: 0x0007387A File Offset: 0x00071A7A
		// (set) Token: 0x06001F9D RID: 8093 RVA: 0x00073882 File Offset: 0x00071A82
		[DataSourceProperty]
		public CharacterCreationCultureVM CurrentSelectedCulture
		{
			get
			{
				return this._currentSelectedCulture;
			}
			set
			{
				if (value != this._currentSelectedCulture)
				{
					this._currentSelectedCulture = value;
					base.OnPropertyChangedWithValue<CharacterCreationCultureVM>(value, "CurrentSelectedCulture");
				}
			}
		}

		// Token: 0x04000EB5 RID: 3765
		private Action<CultureObject> _onCultureSelected;

		// Token: 0x04000EB6 RID: 3766
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000EB7 RID: 3767
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000EB8 RID: 3768
		private bool _isActive;

		// Token: 0x04000EB9 RID: 3769
		private MBBindingList<CharacterCreationCultureVM> _cultures;

		// Token: 0x04000EBA RID: 3770
		private CharacterCreationCultureVM _currentSelectedCulture;
	}
}
