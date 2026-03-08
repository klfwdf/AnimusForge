using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x02000030 RID: 48
	public class UpgradeTargetVM : ViewModel
	{
		// Token: 0x060004B3 RID: 1203 RVA: 0x0001B89C File Offset: 0x00019A9C
		public UpgradeTargetVM(int upgradeIndex, CharacterObject character, CharacterCode upgradeCharacterCode, Action<int, int> onUpgraded, Action<UpgradeTargetVM> onFocused)
		{
			this._upgradeIndex = upgradeIndex;
			this._originalCharacter = character;
			this._upgradeTarget = this._originalCharacter.UpgradeTargets[upgradeIndex];
			this._onUpgraded = onUpgraded;
			this._onFocused = onFocused;
			PerkObject perkRequirement;
			Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, this._originalCharacter, this._upgradeTarget, out perkRequirement);
			this.Requirements = new UpgradeRequirementsVM();
			this.Requirements.SetItemRequirement(this._upgradeTarget.UpgradeRequiresItemFromCategory);
			this.Requirements.SetPerkRequirement(perkRequirement);
			this.TroopImage = new CharacterImageIdentifierVM(upgradeCharacterCode);
		}

		// Token: 0x060004B4 RID: 1204 RVA: 0x0001B941 File Offset: 0x00019B41
		public override void RefreshValues()
		{
			base.RefreshValues();
			UpgradeRequirementsVM requirements = this.Requirements;
			if (requirements == null)
			{
				return;
			}
			requirements.RefreshValues();
		}

		// Token: 0x060004B5 RID: 1205 RVA: 0x0001B95C File Offset: 0x00019B5C
		public void Refresh(int upgradableAmount, bool isAvailable, bool isInsufficient, bool itemRequirementsMet, bool perkRequirementsMet, string hintString, bool isMarinerTroop)
		{
			this.AvailableUpgrades = upgradableAmount;
			this.IsAvailable = isAvailable;
			this.IsInsufficient = isInsufficient;
			this.IsMarinerTroop = isMarinerTroop;
			UpgradeRequirementsVM requirements = this.Requirements;
			if (requirements != null)
			{
				requirements.SetRequirementsMet(itemRequirementsMet, perkRequirementsMet);
			}
			this._hintString = hintString;
			this.Hint = new BasicTooltipViewModel(() => this.GetHint());
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x0001B9BC File Offset: 0x00019BBC
		private string GetHint()
		{
			string stackModifierString = CampaignUIHelper.GetStackModifierString(GameTexts.FindText("str_entire_stack_shortcut_recruit_units", null), GameTexts.FindText("str_five_stack_shortcut_recruit_units", null), this.AvailableUpgrades >= 5);
			if (string.IsNullOrEmpty(stackModifierString) || this.AvailableUpgrades < 1)
			{
				return this._hintString;
			}
			return GameTexts.FindText("str_string_newline_string", null).SetTextVariable("STR1", this._hintString).SetTextVariable("STR2", stackModifierString)
				.ToString();
		}

		// Token: 0x060004B7 RID: 1207 RVA: 0x0001BA34 File Offset: 0x00019C34
		public void ExecuteUpgradeEncyclopediaLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this._upgradeTarget.EncyclopediaLink);
		}

		// Token: 0x060004B8 RID: 1208 RVA: 0x0001BA50 File Offset: 0x00019C50
		public void ExecuteUpgrade()
		{
			if (this.IsAvailable && !this.IsInsufficient)
			{
				Action<int, int> onUpgraded = this._onUpgraded;
				if (onUpgraded == null)
				{
					return;
				}
				onUpgraded(this._upgradeIndex, this.AvailableUpgrades);
			}
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x0001BA7E File Offset: 0x00019C7E
		public void ExecuteSetFocused()
		{
			if (this._upgradeTarget != null)
			{
				Action<UpgradeTargetVM> onFocused = this._onFocused;
				if (onFocused == null)
				{
					return;
				}
				onFocused(this);
			}
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x0001BA99 File Offset: 0x00019C99
		public void ExecuteSetUnfocused()
		{
			Action<UpgradeTargetVM> onFocused = this._onFocused;
			if (onFocused == null)
			{
				return;
			}
			onFocused(null);
		}

		// Token: 0x17000150 RID: 336
		// (get) Token: 0x060004BB RID: 1211 RVA: 0x0001BAAC File Offset: 0x00019CAC
		// (set) Token: 0x060004BC RID: 1212 RVA: 0x0001BAB4 File Offset: 0x00019CB4
		[DataSourceProperty]
		public InputKeyItemVM PrimaryActionInputKey
		{
			get
			{
				return this._primaryActionInputKey;
			}
			set
			{
				if (value != this._primaryActionInputKey)
				{
					this._primaryActionInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PrimaryActionInputKey");
				}
			}
		}

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x060004BD RID: 1213 RVA: 0x0001BAD2 File Offset: 0x00019CD2
		// (set) Token: 0x060004BE RID: 1214 RVA: 0x0001BADA File Offset: 0x00019CDA
		[DataSourceProperty]
		public InputKeyItemVM SecondaryActionInputKey
		{
			get
			{
				return this._secondaryActionInputKey;
			}
			set
			{
				if (value != this._secondaryActionInputKey)
				{
					this._secondaryActionInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "SecondaryActionInputKey");
				}
			}
		}

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x060004BF RID: 1215 RVA: 0x0001BAF8 File Offset: 0x00019CF8
		// (set) Token: 0x060004C0 RID: 1216 RVA: 0x0001BB00 File Offset: 0x00019D00
		[DataSourceProperty]
		public InputKeyItemVM TertiaryActionInputKey
		{
			get
			{
				return this._tertiaryActionInputKey;
			}
			set
			{
				if (value != this._tertiaryActionInputKey)
				{
					this._tertiaryActionInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "TertiaryActionInputKey");
				}
			}
		}

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x060004C1 RID: 1217 RVA: 0x0001BB1E File Offset: 0x00019D1E
		// (set) Token: 0x060004C2 RID: 1218 RVA: 0x0001BB26 File Offset: 0x00019D26
		[DataSourceProperty]
		public UpgradeRequirementsVM Requirements
		{
			get
			{
				return this._requirements;
			}
			set
			{
				if (value != this._requirements)
				{
					this._requirements = value;
					base.OnPropertyChangedWithValue<UpgradeRequirementsVM>(value, "Requirements");
				}
			}
		}

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x060004C3 RID: 1219 RVA: 0x0001BB44 File Offset: 0x00019D44
		// (set) Token: 0x060004C4 RID: 1220 RVA: 0x0001BB4C File Offset: 0x00019D4C
		[DataSourceProperty]
		public CharacterImageIdentifierVM TroopImage
		{
			get
			{
				return this._troopImage;
			}
			set
			{
				if (value != this._troopImage)
				{
					this._troopImage = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "TroopImage");
				}
			}
		}

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x060004C5 RID: 1221 RVA: 0x0001BB6A File Offset: 0x00019D6A
		// (set) Token: 0x060004C6 RID: 1222 RVA: 0x0001BB72 File Offset: 0x00019D72
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
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
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x17000156 RID: 342
		// (get) Token: 0x060004C7 RID: 1223 RVA: 0x0001BB90 File Offset: 0x00019D90
		// (set) Token: 0x060004C8 RID: 1224 RVA: 0x0001BB98 File Offset: 0x00019D98
		[DataSourceProperty]
		public int AvailableUpgrades
		{
			get
			{
				return this._availableUpgrades;
			}
			set
			{
				if (value != this._availableUpgrades)
				{
					this._availableUpgrades = value;
					base.OnPropertyChangedWithValue(value, "AvailableUpgrades");
				}
			}
		}

		// Token: 0x17000157 RID: 343
		// (get) Token: 0x060004C9 RID: 1225 RVA: 0x0001BBB6 File Offset: 0x00019DB6
		// (set) Token: 0x060004CA RID: 1226 RVA: 0x0001BBBE File Offset: 0x00019DBE
		[DataSourceProperty]
		public bool IsAvailable
		{
			get
			{
				return this._isAvailable;
			}
			set
			{
				if (value != this._isAvailable)
				{
					this._isAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsAvailable");
				}
			}
		}

		// Token: 0x17000158 RID: 344
		// (get) Token: 0x060004CB RID: 1227 RVA: 0x0001BBDC File Offset: 0x00019DDC
		// (set) Token: 0x060004CC RID: 1228 RVA: 0x0001BBE4 File Offset: 0x00019DE4
		[DataSourceProperty]
		public bool IsInsufficient
		{
			get
			{
				return this._isInsufficient;
			}
			set
			{
				if (value != this._isInsufficient)
				{
					this._isInsufficient = value;
					base.OnPropertyChangedWithValue(value, "IsInsufficient");
				}
			}
		}

		// Token: 0x17000159 RID: 345
		// (get) Token: 0x060004CD RID: 1229 RVA: 0x0001BC02 File Offset: 0x00019E02
		// (set) Token: 0x060004CE RID: 1230 RVA: 0x0001BC0A File Offset: 0x00019E0A
		[DataSourceProperty]
		public bool IsHighlighted
		{
			get
			{
				return this._isHighlighted;
			}
			set
			{
				if (value != this._isHighlighted)
				{
					this._isHighlighted = value;
					base.OnPropertyChangedWithValue(value, "IsHighlighted");
				}
			}
		}

		// Token: 0x1700015A RID: 346
		// (get) Token: 0x060004CF RID: 1231 RVA: 0x0001BC28 File Offset: 0x00019E28
		// (set) Token: 0x060004D0 RID: 1232 RVA: 0x0001BC30 File Offset: 0x00019E30
		[DataSourceProperty]
		public bool IsMarinerTroop
		{
			get
			{
				return this._isMarinerTroop;
			}
			set
			{
				if (value != this._isMarinerTroop)
				{
					this._isMarinerTroop = value;
					base.OnPropertyChangedWithValue(value, "IsMarinerTroop");
				}
			}
		}

		// Token: 0x04000204 RID: 516
		private CharacterObject _originalCharacter;

		// Token: 0x04000205 RID: 517
		private CharacterObject _upgradeTarget;

		// Token: 0x04000206 RID: 518
		private Action<int, int> _onUpgraded;

		// Token: 0x04000207 RID: 519
		private Action<UpgradeTargetVM> _onFocused;

		// Token: 0x04000208 RID: 520
		private int _upgradeIndex;

		// Token: 0x04000209 RID: 521
		private string _hintString;

		// Token: 0x0400020A RID: 522
		private InputKeyItemVM _primaryActionInputKey;

		// Token: 0x0400020B RID: 523
		private InputKeyItemVM _secondaryActionInputKey;

		// Token: 0x0400020C RID: 524
		private InputKeyItemVM _tertiaryActionInputKey;

		// Token: 0x0400020D RID: 525
		private UpgradeRequirementsVM _requirements;

		// Token: 0x0400020E RID: 526
		private CharacterImageIdentifierVM _troopImage;

		// Token: 0x0400020F RID: 527
		private BasicTooltipViewModel _hint;

		// Token: 0x04000210 RID: 528
		private int _availableUpgrades;

		// Token: 0x04000211 RID: 529
		private bool _isAvailable;

		// Token: 0x04000212 RID: 530
		private bool _isInsufficient;

		// Token: 0x04000213 RID: 531
		private bool _isHighlighted;

		// Token: 0x04000214 RID: 532
		private bool _isMarinerTroop;
	}
}
