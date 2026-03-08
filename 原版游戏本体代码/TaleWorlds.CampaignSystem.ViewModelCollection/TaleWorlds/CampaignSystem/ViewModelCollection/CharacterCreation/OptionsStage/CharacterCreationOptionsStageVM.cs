using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.OptionsStage
{
	// Token: 0x02000158 RID: 344
	public class CharacterCreationOptionsStageVM : CharacterCreationStageBaseVM
	{
		// Token: 0x0600203F RID: 8255 RVA: 0x000757D8 File Offset: 0x000739D8
		public CharacterCreationOptionsStageVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText)
			: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
		{
			base.Title = GameTexts.FindText("str_difficulty", null).ToString();
			base.Description = GameTexts.FindText("str_determine_difficulty", null).ToString();
			MBBindingList<CampaignOptionItemVM> mbbindingList = new MBBindingList<CampaignOptionItemVM>();
			List<ICampaignOptionData> characterCreationCampaignOptions = CampaignOptionsManager.GetCharacterCreationCampaignOptions();
			for (int i = 0; i < characterCreationCampaignOptions.Count; i++)
			{
				mbbindingList.Add(new CampaignOptionItemVM(characterCreationCampaignOptions[i]));
			}
			this.OptionsController = new CampaignOptionsControllerVM(mbbindingList);
			base.CanAdvance = this.CanAdvanceToNextStage();
			this.CameraControlKeys = new MBBindingList<InputKeyItemVM>();
		}

		// Token: 0x06002040 RID: 8256 RVA: 0x00075870 File Offset: 0x00073A70
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.OptionsController.RefreshValues();
		}

		// Token: 0x06002041 RID: 8257 RVA: 0x00075883 File Offset: 0x00073A83
		private void OnOptionChange(string identifier)
		{
		}

		// Token: 0x06002042 RID: 8258 RVA: 0x00075885 File Offset: 0x00073A85
		public override bool CanAdvanceToNextStage()
		{
			return true;
		}

		// Token: 0x06002043 RID: 8259 RVA: 0x00075888 File Offset: 0x00073A88
		public override void OnNextStage()
		{
			this._affirmativeAction();
		}

		// Token: 0x06002044 RID: 8260 RVA: 0x00075895 File Offset: 0x00073A95
		public override void OnPreviousStage()
		{
			this._negativeAction();
		}

		// Token: 0x06002045 RID: 8261 RVA: 0x000758A4 File Offset: 0x00073AA4
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

		// Token: 0x06002046 RID: 8262 RVA: 0x00075918 File Offset: 0x00073B18
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06002047 RID: 8263 RVA: 0x00075927 File Offset: 0x00073B27
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06002048 RID: 8264 RVA: 0x00075938 File Offset: 0x00073B38
		public void AddCameraControlInputKey(HotKey hotKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x06002049 RID: 8265 RVA: 0x0007595C File Offset: 0x00073B5C
		public void AddCameraControlInputKey(GameKey gameKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x0600204A RID: 8266 RVA: 0x00075980 File Offset: 0x00073B80
		public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x17000AFC RID: 2812
		// (get) Token: 0x0600204B RID: 8267 RVA: 0x000759AC File Offset: 0x00073BAC
		// (set) Token: 0x0600204C RID: 8268 RVA: 0x000759B4 File Offset: 0x00073BB4
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

		// Token: 0x17000AFD RID: 2813
		// (get) Token: 0x0600204D RID: 8269 RVA: 0x000759D2 File Offset: 0x00073BD2
		// (set) Token: 0x0600204E RID: 8270 RVA: 0x000759DA File Offset: 0x00073BDA
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

		// Token: 0x17000AFE RID: 2814
		// (get) Token: 0x0600204F RID: 8271 RVA: 0x000759F8 File Offset: 0x00073BF8
		// (set) Token: 0x06002050 RID: 8272 RVA: 0x00075A00 File Offset: 0x00073C00
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

		// Token: 0x17000AFF RID: 2815
		// (get) Token: 0x06002051 RID: 8273 RVA: 0x00075A1E File Offset: 0x00073C1E
		// (set) Token: 0x06002052 RID: 8274 RVA: 0x00075A26 File Offset: 0x00073C26
		[DataSourceProperty]
		public CampaignOptionsControllerVM OptionsController
		{
			get
			{
				return this._optionsController;
			}
			set
			{
				if (value != this._optionsController)
				{
					this._optionsController = value;
					base.OnPropertyChangedWithValue<CampaignOptionsControllerVM>(value, "OptionsController");
				}
			}
		}

		// Token: 0x17000B00 RID: 2816
		// (get) Token: 0x06002053 RID: 8275 RVA: 0x00075A44 File Offset: 0x00073C44
		// (set) Token: 0x06002054 RID: 8276 RVA: 0x00075A4C File Offset: 0x00073C4C
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

		// Token: 0x04000F03 RID: 3843
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000F04 RID: 3844
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000F05 RID: 3845
		private MBBindingList<InputKeyItemVM> _cameraControlKeys;

		// Token: 0x04000F06 RID: 3846
		private CampaignOptionsControllerVM _optionsController;

		// Token: 0x04000F07 RID: 3847
		private bool _characterGamepadControlsEnabled;
	}
}
