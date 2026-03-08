using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000153 RID: 339
	public class CharacterCreationNarrativeStageVM : CharacterCreationStageBaseVM
	{
		// Token: 0x06001FDA RID: 8154 RVA: 0x0007484C File Offset: 0x00072A4C
		public CharacterCreationNarrativeStageVM(CharacterCreationManager characterCreationManagerMenu, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText, Action onMenuChanged)
			: base(characterCreationManagerMenu, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
		{
			this._onMenuChanged = onMenuChanged;
			this.SelectionList = new MBBindingList<CharacterCreationOptionVM>();
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, null, false);
			this.GainedPropertiesController = new CharacterCreationGainedPropertiesVM(this.CharacterCreationManager);
		}

		// Token: 0x06001FDB RID: 8155 RVA: 0x0007489C File Offset: 0x00072A9C
		public void RefreshMenu()
		{
			this.SelectionList.Clear();
			foreach (NarrativeMenuOption option in this.CharacterCreationManager.GetSuitableNarrativeMenuOptions())
			{
				CharacterCreationOptionVM item = new CharacterCreationOptionVM(new Action<CharacterCreationOptionVM>(this.OnOptionSelected), option);
				this.SelectionList.Add(item);
			}
			NarrativeMenuOption narrativeMenuOption;
			if (this.CharacterCreationManager.SelectedOptions.TryGetValue(this.CharacterCreationManager.CurrentMenu, out narrativeMenuOption))
			{
				for (int i = 0; i < this.SelectionList.Count; i++)
				{
					if (this.SelectionList[i].Option == narrativeMenuOption)
					{
						this.SelectionList[i].ExecuteSelect();
					}
				}
			}
			base.Title = this.CharacterCreationManager.CurrentMenu.Title.ToString();
			base.Description = this.CharacterCreationManager.CurrentMenu.Description.ToString();
			GameTexts.SetVariable("SELECTION", base.Title);
			base.SelectionText = GameTexts.FindText("str_char_creation_generic_selection", null).ToString();
			base.CanAdvance = this.CanAdvanceToNextStage();
			Action onMenuChanged = this._onMenuChanged;
			if (onMenuChanged == null)
			{
				return;
			}
			onMenuChanged();
		}

		// Token: 0x06001FDC RID: 8156 RVA: 0x000749EC File Offset: 0x00072BEC
		public void OnOptionSelected(CharacterCreationOptionVM option)
		{
			if (this.SelectedOption != null)
			{
				this.SelectedOption.IsSelected = false;
			}
			this.SelectedOption = option;
			if (this.SelectedOption != null)
			{
				this.SelectedOption.IsSelected = true;
				this.CharacterCreationManager.OnNarrativeMenuOptionSelected(this._selectedOption.Option);
				this.SelectedOption.RefreshValues();
			}
			Action onOptionSelection = this.OnOptionSelection;
			if (onOptionSelection != null)
			{
				onOptionSelection();
			}
			base.CanAdvance = this.CanAdvanceToNextStage();
			this.GainedPropertiesController.UpdateValues();
		}

		// Token: 0x06001FDD RID: 8157 RVA: 0x00074A71 File Offset: 0x00072C71
		public override void OnNextStage()
		{
			if (this.CharacterCreationManager.TrySwitchToNextMenu())
			{
				this.RefreshMenu();
				return;
			}
			this._affirmativeAction();
		}

		// Token: 0x06001FDE RID: 8158 RVA: 0x00074A92 File Offset: 0x00072C92
		public override void OnPreviousStage()
		{
			if (this.CharacterCreationManager.TrySwitchToPreviousMenu())
			{
				this.RefreshMenu();
				return;
			}
			this._negativeAction();
		}

		// Token: 0x06001FDF RID: 8159 RVA: 0x00074AB3 File Offset: 0x00072CB3
		public override bool CanAdvanceToNextStage()
		{
			if (this.SelectionList.Count != 0)
			{
				return this.SelectionList.Any((CharacterCreationOptionVM s) => s.IsSelected);
			}
			return true;
		}

		// Token: 0x06001FE0 RID: 8160 RVA: 0x00074AEE File Offset: 0x00072CEE
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

		// Token: 0x06001FE1 RID: 8161 RVA: 0x00074B17 File Offset: 0x00072D17
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001FE2 RID: 8162 RVA: 0x00074B26 File Offset: 0x00072D26
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000AD9 RID: 2777
		// (get) Token: 0x06001FE3 RID: 8163 RVA: 0x00074B35 File Offset: 0x00072D35
		// (set) Token: 0x06001FE4 RID: 8164 RVA: 0x00074B3D File Offset: 0x00072D3D
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

		// Token: 0x17000ADA RID: 2778
		// (get) Token: 0x06001FE5 RID: 8165 RVA: 0x00074B5B File Offset: 0x00072D5B
		// (set) Token: 0x06001FE6 RID: 8166 RVA: 0x00074B63 File Offset: 0x00072D63
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

		// Token: 0x17000ADB RID: 2779
		// (get) Token: 0x06001FE7 RID: 8167 RVA: 0x00074B81 File Offset: 0x00072D81
		// (set) Token: 0x06001FE8 RID: 8168 RVA: 0x00074B89 File Offset: 0x00072D89
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

		// Token: 0x17000ADC RID: 2780
		// (get) Token: 0x06001FE9 RID: 8169 RVA: 0x00074BA7 File Offset: 0x00072DA7
		// (set) Token: 0x06001FEA RID: 8170 RVA: 0x00074BAF File Offset: 0x00072DAF
		[DataSourceProperty]
		public CharacterCreationOptionVM SelectedOption
		{
			get
			{
				return this._selectedOption;
			}
			set
			{
				if (value != this._selectedOption)
				{
					this._selectedOption = value;
					base.OnPropertyChangedWithValue<CharacterCreationOptionVM>(value, "SelectedOption");
					base.AnyItemSelected = this.SelectedOption != null;
				}
			}
		}

		// Token: 0x17000ADD RID: 2781
		// (get) Token: 0x06001FEB RID: 8171 RVA: 0x00074BDC File Offset: 0x00072DDC
		// (set) Token: 0x06001FEC RID: 8172 RVA: 0x00074BE4 File Offset: 0x00072DE4
		[DataSourceProperty]
		public MBBindingList<CharacterCreationOptionVM> SelectionList
		{
			get
			{
				return this._selectionList;
			}
			set
			{
				if (value != this._selectionList)
				{
					this._selectionList = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationOptionVM>>(value, "SelectionList");
				}
			}
		}

		// Token: 0x04000ED6 RID: 3798
		public Action OnOptionSelection;

		// Token: 0x04000ED7 RID: 3799
		private readonly Action _onMenuChanged;

		// Token: 0x04000ED8 RID: 3800
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000ED9 RID: 3801
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000EDA RID: 3802
		private CharacterCreationGainedPropertiesVM _gainedPropertiesController;

		// Token: 0x04000EDB RID: 3803
		private CharacterCreationOptionVM _selectedOption;

		// Token: 0x04000EDC RID: 3804
		private MBBindingList<CharacterCreationOptionVM> _selectionList;
	}
}
