using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Tutorial
{
	// Token: 0x0200000B RID: 11
	public class TutorialItemVM : ViewModel
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000072 RID: 114 RVA: 0x0000530A File Offset: 0x0000350A
		// (set) Token: 0x06000073 RID: 115 RVA: 0x00005312 File Offset: 0x00003512
		public Action<bool> SetIsActive { get; private set; }

		// Token: 0x06000074 RID: 116 RVA: 0x0000531B File Offset: 0x0000351B
		public TutorialItemVM()
		{
			this.IsEnabled = false;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x0000532C File Offset: 0x0000352C
		public void Init(string tutorialTypeId, bool requiresMouse, Action onFinishTutorial)
		{
			this.IsEnabled = false;
			this.StepCountText = "DISABLED";
			this.RequiresMouse = requiresMouse;
			this.IsEnabled = true;
			this._onFinishTutorial = onFinishTutorial;
			this._tutorialTypeId = tutorialTypeId;
			this.AreTutorialsEnabled = BannerlordConfig.EnableTutorialHints;
			this.RefreshValues();
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005378 File Offset: 0x00003578
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DisableCurrentTutorialHint = new HintViewModel(GameTexts.FindText("str_disable_current_tutorial_step", null), null);
			this.DisableAllTutorialsHint = new HintViewModel(GameTexts.FindText("str_disable_all_tutorials", null), null);
			this.TutorialsEnabledText = GameTexts.FindText("str_tutorials_enabled", null).ToString();
			this.TutorialTitleText = GameTexts.FindText("str_initial_menu_option", "Tutorial").ToString();
			this.TitleText = GameTexts.FindText("str_campaign_tutorial_title", this._tutorialTypeId).ToString();
			TextObject textObject;
			if (Input.IsControllerConnected && !Input.IsMouseActive && GameTexts.TryGetText("str_campaign_tutorial_description", out textObject, this._tutorialTypeId + "_controller"))
			{
				this.DescriptionText = textObject.ToString();
				return;
			}
			this.DescriptionText = GameTexts.FindText("str_campaign_tutorial_description", this._tutorialTypeId).ToString();
		}

		// Token: 0x06000077 RID: 119 RVA: 0x0000545E File Offset: 0x0000365E
		public void CloseTutorialPanel()
		{
			this.IsEnabled = false;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00005467 File Offset: 0x00003667
		private void ExecuteFinishTutorial()
		{
			this._onFinishTutorial();
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005474 File Offset: 0x00003674
		private void ExecuteToggleDisableAllTutorials()
		{
			this.AreTutorialsEnabled = !this.AreTutorialsEnabled;
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00005485 File Offset: 0x00003685
		// (set) Token: 0x0600007B RID: 123 RVA: 0x0000548D File Offset: 0x0000368D
		[DataSourceProperty]
		public HintViewModel DisableCurrentTutorialHint
		{
			get
			{
				return this._disableCurrentTutorialHint;
			}
			set
			{
				if (value != this._disableCurrentTutorialHint)
				{
					this._disableCurrentTutorialHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisableCurrentTutorialHint");
				}
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600007C RID: 124 RVA: 0x000054AB File Offset: 0x000036AB
		// (set) Token: 0x0600007D RID: 125 RVA: 0x000054B3 File Offset: 0x000036B3
		[DataSourceProperty]
		public bool AreTutorialsEnabled
		{
			get
			{
				return this._areTutorialsEnabled;
			}
			set
			{
				if (value != this._areTutorialsEnabled)
				{
					this._areTutorialsEnabled = value;
					base.OnPropertyChangedWithValue(value, "AreTutorialsEnabled");
					BannerlordConfig.EnableTutorialHints = value;
				}
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600007E RID: 126 RVA: 0x000054D7 File Offset: 0x000036D7
		// (set) Token: 0x0600007F RID: 127 RVA: 0x000054DF File Offset: 0x000036DF
		[DataSourceProperty]
		public string TutorialsEnabledText
		{
			get
			{
				return this._tutorialsEnabledText;
			}
			set
			{
				if (value != this._tutorialsEnabledText)
				{
					this._tutorialsEnabledText = value;
					base.OnPropertyChangedWithValue<string>(value, "TutorialsEnabledText");
				}
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000080 RID: 128 RVA: 0x00005502 File Offset: 0x00003702
		// (set) Token: 0x06000081 RID: 129 RVA: 0x0000550A File Offset: 0x0000370A
		[DataSourceProperty]
		public string TutorialTitleText
		{
			get
			{
				return this._tutorialTitleText;
			}
			set
			{
				if (value != this._tutorialTitleText)
				{
					this._tutorialTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TutorialTitleText");
				}
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000082 RID: 130 RVA: 0x0000552D File Offset: 0x0000372D
		// (set) Token: 0x06000083 RID: 131 RVA: 0x00005535 File Offset: 0x00003735
		[DataSourceProperty]
		public HintViewModel DisableAllTutorialsHint
		{
			get
			{
				return this._disableAllTutorialsHint;
			}
			set
			{
				if (value != this._disableAllTutorialsHint)
				{
					this._disableAllTutorialsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisableAllTutorialsHint");
				}
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000084 RID: 132 RVA: 0x00005553 File Offset: 0x00003753
		// (set) Token: 0x06000085 RID: 133 RVA: 0x0000555B File Offset: 0x0000375B
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000086 RID: 134 RVA: 0x0000557E File Offset: 0x0000377E
		// (set) Token: 0x06000087 RID: 135 RVA: 0x00005586 File Offset: 0x00003786
		[DataSourceProperty]
		public string StepCountText
		{
			get
			{
				return this._stepCountText;
			}
			set
			{
				if (value != this._stepCountText)
				{
					this._stepCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "StepCountText");
				}
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000088 RID: 136 RVA: 0x000055A9 File Offset: 0x000037A9
		// (set) Token: 0x06000089 RID: 137 RVA: 0x000055B1 File Offset: 0x000037B1
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600008A RID: 138 RVA: 0x000055CF File Offset: 0x000037CF
		// (set) Token: 0x0600008B RID: 139 RVA: 0x000055D7 File Offset: 0x000037D7
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

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600008C RID: 140 RVA: 0x000055FA File Offset: 0x000037FA
		// (set) Token: 0x0600008D RID: 141 RVA: 0x00005602 File Offset: 0x00003802
		[DataSourceProperty]
		public string SoundId
		{
			get
			{
				return this._soundId;
			}
			set
			{
				if (value != this._soundId)
				{
					this._soundId = value;
					base.OnPropertyChanged("SoundId");
				}
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00005624 File Offset: 0x00003824
		// (set) Token: 0x0600008F RID: 143 RVA: 0x0000562C File Offset: 0x0000382C
		[DataSourceProperty]
		public ImageIdentifierVM CenterImage
		{
			get
			{
				return this._centerImage;
			}
			set
			{
				if (value != this._centerImage)
				{
					this._centerImage = value;
					base.OnPropertyChanged("CenterImage");
				}
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000090 RID: 144 RVA: 0x00005649 File Offset: 0x00003849
		// (set) Token: 0x06000091 RID: 145 RVA: 0x00005651 File Offset: 0x00003851
		[DataSourceProperty]
		public bool RequiresMouse
		{
			get
			{
				return this._requiresMouse;
			}
			set
			{
				if (value != this._requiresMouse)
				{
					this._requiresMouse = value;
					base.OnPropertyChanged("RequiresMouse");
				}
			}
		}

		// Token: 0x04000033 RID: 51
		private const string ControllerIdentificationModifier = "_controller";

		// Token: 0x04000034 RID: 52
		private string _tutorialTypeId;

		// Token: 0x04000035 RID: 53
		private Action _onFinishTutorial;

		// Token: 0x04000037 RID: 55
		private string _titleText;

		// Token: 0x04000038 RID: 56
		private string _descriptionText;

		// Token: 0x04000039 RID: 57
		private ImageIdentifierVM _centerImage;

		// Token: 0x0400003A RID: 58
		private string _soundId;

		// Token: 0x0400003B RID: 59
		private string _stepCountText;

		// Token: 0x0400003C RID: 60
		private string _tutorialsEnabledText;

		// Token: 0x0400003D RID: 61
		private string _tutorialTitleText;

		// Token: 0x0400003E RID: 62
		private bool _isEnabled;

		// Token: 0x0400003F RID: 63
		private bool _requiresMouse;

		// Token: 0x04000040 RID: 64
		private HintViewModel _disableCurrentTutorialHint;

		// Token: 0x04000041 RID: 65
		private HintViewModel _disableAllTutorialsHint;

		// Token: 0x04000042 RID: 66
		private bool _areTutorialsEnabled;

		// Token: 0x0200006D RID: 109
		public enum ItemPlacements
		{
			// Token: 0x04000315 RID: 789
			Left,
			// Token: 0x04000316 RID: 790
			Right,
			// Token: 0x04000317 RID: 791
			Top,
			// Token: 0x04000318 RID: 792
			Bottom,
			// Token: 0x04000319 RID: 793
			TopLeft,
			// Token: 0x0400031A RID: 794
			TopRight,
			// Token: 0x0400031B RID: 795
			BottomLeft,
			// Token: 0x0400031C RID: 796
			BottomRight,
			// Token: 0x0400031D RID: 797
			Center
		}
	}
}
