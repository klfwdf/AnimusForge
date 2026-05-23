using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000144 RID: 324
	public class PerkVM : ViewModel
	{
		// Token: 0x17000A7D RID: 2685
		// (get) Token: 0x06001ED2 RID: 7890 RVA: 0x0007158B File Offset: 0x0006F78B
		private bool _hasAlternativeAndSelected
		{
			get
			{
				return this.AlternativeType != 0 && this._getIsPerkSelected(this.Perk.AlternativePerk);
			}
		}

		// Token: 0x17000A7E RID: 2686
		// (get) Token: 0x06001ED3 RID: 7891 RVA: 0x000715AD File Offset: 0x0006F7AD
		// (set) Token: 0x06001ED4 RID: 7892 RVA: 0x000715B5 File Offset: 0x0006F7B5
		public PerkVM.PerkStates CurrentState
		{
			get
			{
				return this._currentState;
			}
			private set
			{
				if (value != this._currentState)
				{
					this._currentState = value;
					this.PerkState = (int)value;
				}
			}
		}

		// Token: 0x17000A7F RID: 2687
		// (set) Token: 0x06001ED5 RID: 7893 RVA: 0x000715CE File Offset: 0x0006F7CE
		public bool IsInSelection
		{
			set
			{
				if (value != this._isInSelection)
				{
					this._isInSelection = value;
					this.RefreshState();
					if (!this._isInSelection)
					{
						this._onSelectionOver(this);
					}
				}
			}
		}

		// Token: 0x06001ED6 RID: 7894 RVA: 0x000715FC File Offset: 0x0006F7FC
		public PerkVM(PerkObject perk, bool isAvailable, PerkVM.PerkAlternativeType alternativeType, Action<PerkVM> onStartSelection, Action<PerkVM> onSelectionOver, Func<PerkObject, bool> getIsPerkSelected, Func<PerkObject, bool> getIsPreviousPerkSelected)
		{
			PerkVM <>4__this = this;
			this.AlternativeType = (int)alternativeType;
			this.Perk = perk;
			this._onStartSelection = onStartSelection;
			this._onSelectionOver = onSelectionOver;
			this._getIsPerkSelected = getIsPerkSelected;
			this._getIsPreviousPerkSelected = getIsPreviousPerkSelected;
			this._isAvailable = isAvailable;
			this.PerkId = "SPPerks\\" + perk.StringId;
			this.Level = (int)perk.RequiredSkillValue;
			this.LevelText = ((int)perk.RequiredSkillValue).ToString();
			this.Hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPerkEffectText(perk, <>4__this._getIsPerkSelected(<>4__this.Perk)));
			this._perkConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_perks");
			this.RefreshState();
		}

		// Token: 0x06001ED7 RID: 7895 RVA: 0x00071700 File Offset: 0x0006F900
		public void RefreshState()
		{
			bool flag = this._getIsPerkSelected(this.Perk);
			if (!this._isAvailable)
			{
				this.CurrentState = PerkVM.PerkStates.NotEarned;
				return;
			}
			if (this._isInSelection)
			{
				this.CurrentState = PerkVM.PerkStates.InSelection;
				return;
			}
			if (flag)
			{
				this.CurrentState = PerkVM.PerkStates.EarnedAndActive;
				return;
			}
			if (this.Perk.AlternativePerk != null && this._getIsPerkSelected(this.Perk.AlternativePerk))
			{
				this.CurrentState = PerkVM.PerkStates.EarnedAndNotActive;
				return;
			}
			if (this._getIsPreviousPerkSelected(this.Perk))
			{
				this.CurrentState = PerkVM.PerkStates.EarnedButNotSelected;
				return;
			}
			this.CurrentState = PerkVM.PerkStates.EarnedPreviousPerkNotSelected;
		}

		// Token: 0x06001ED8 RID: 7896 RVA: 0x00071799 File Offset: 0x0006F999
		public void ExecuteShowPerkConcept()
		{
			if (this._perkConceptObj != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._perkConceptObj.EncyclopediaLink);
				return;
			}
			Debug.FailedAssert("Couldn't find Perks encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\PerkVM.cs", "ExecuteShowPerkConcept", 151);
		}

		// Token: 0x06001ED9 RID: 7897 RVA: 0x000717D8 File Offset: 0x0006F9D8
		public void ExecuteStartSelection()
		{
			if (this._isAvailable && !this._getIsPerkSelected(this.Perk) && !this._hasAlternativeAndSelected && this._getIsPreviousPerkSelected(this.Perk))
			{
				this._onStartSelection(this);
			}
		}

		// Token: 0x17000A80 RID: 2688
		// (get) Token: 0x06001EDA RID: 7898 RVA: 0x00071827 File Offset: 0x0006FA27
		// (set) Token: 0x06001EDB RID: 7899 RVA: 0x0007182F File Offset: 0x0006FA2F
		[DataSourceProperty]
		public bool IsTutorialHighlightEnabled
		{
			get
			{
				return this._isTutorialHighlightEnabled;
			}
			set
			{
				if (value != this._isTutorialHighlightEnabled)
				{
					this._isTutorialHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsTutorialHighlightEnabled");
				}
			}
		}

		// Token: 0x17000A81 RID: 2689
		// (get) Token: 0x06001EDC RID: 7900 RVA: 0x0007184D File Offset: 0x0006FA4D
		// (set) Token: 0x06001EDD RID: 7901 RVA: 0x00071855 File Offset: 0x0006FA55
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

		// Token: 0x17000A82 RID: 2690
		// (get) Token: 0x06001EDE RID: 7902 RVA: 0x00071873 File Offset: 0x0006FA73
		// (set) Token: 0x06001EDF RID: 7903 RVA: 0x0007187B File Offset: 0x0006FA7B
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

		// Token: 0x17000A83 RID: 2691
		// (get) Token: 0x06001EE0 RID: 7904 RVA: 0x00071899 File Offset: 0x0006FA99
		// (set) Token: 0x06001EE1 RID: 7905 RVA: 0x000718A1 File Offset: 0x0006FAA1
		[DataSourceProperty]
		public int PerkState
		{
			get
			{
				return this._perkState;
			}
			set
			{
				if (value != this._perkState)
				{
					this._perkState = value;
					base.OnPropertyChangedWithValue(value, "PerkState");
				}
			}
		}

		// Token: 0x17000A84 RID: 2692
		// (get) Token: 0x06001EE2 RID: 7906 RVA: 0x000718BF File Offset: 0x0006FABF
		// (set) Token: 0x06001EE3 RID: 7907 RVA: 0x000718C7 File Offset: 0x0006FAC7
		[DataSourceProperty]
		public int AlternativeType
		{
			get
			{
				return this._alternativeType;
			}
			set
			{
				if (value != this._alternativeType)
				{
					this._alternativeType = value;
					base.OnPropertyChangedWithValue(value, "AlternativeType");
				}
			}
		}

		// Token: 0x17000A85 RID: 2693
		// (get) Token: 0x06001EE4 RID: 7908 RVA: 0x000718E5 File Offset: 0x0006FAE5
		// (set) Token: 0x06001EE5 RID: 7909 RVA: 0x000718ED File Offset: 0x0006FAED
		[DataSourceProperty]
		public string LevelText
		{
			get
			{
				return this._levelText;
			}
			set
			{
				if (value != this._levelText)
				{
					this._levelText = value;
					base.OnPropertyChangedWithValue<string>(value, "LevelText");
				}
			}
		}

		// Token: 0x17000A86 RID: 2694
		// (get) Token: 0x06001EE6 RID: 7910 RVA: 0x00071910 File Offset: 0x0006FB10
		// (set) Token: 0x06001EE7 RID: 7911 RVA: 0x00071918 File Offset: 0x0006FB18
		[DataSourceProperty]
		public string BackgroundImage
		{
			get
			{
				return this._backgroundImage;
			}
			set
			{
				if (value != this._backgroundImage)
				{
					this._backgroundImage = value;
					base.OnPropertyChangedWithValue<string>(value, "BackgroundImage");
				}
			}
		}

		// Token: 0x17000A87 RID: 2695
		// (get) Token: 0x06001EE8 RID: 7912 RVA: 0x0007193B File Offset: 0x0006FB3B
		// (set) Token: 0x06001EE9 RID: 7913 RVA: 0x00071943 File Offset: 0x0006FB43
		[DataSourceProperty]
		public string PerkId
		{
			get
			{
				return this._perkId;
			}
			set
			{
				if (value != this._perkId)
				{
					this._perkId = value;
					base.OnPropertyChangedWithValue<string>(value, "PerkId");
				}
			}
		}

		// Token: 0x04000E60 RID: 3680
		public readonly PerkObject Perk;

		// Token: 0x04000E61 RID: 3681
		private readonly Action<PerkVM> _onStartSelection;

		// Token: 0x04000E62 RID: 3682
		private readonly Action<PerkVM> _onSelectionOver;

		// Token: 0x04000E63 RID: 3683
		private readonly Func<PerkObject, bool> _getIsPerkSelected;

		// Token: 0x04000E64 RID: 3684
		private readonly Func<PerkObject, bool> _getIsPreviousPerkSelected;

		// Token: 0x04000E65 RID: 3685
		private readonly bool _isAvailable;

		// Token: 0x04000E66 RID: 3686
		private readonly Concept _perkConceptObj;

		// Token: 0x04000E67 RID: 3687
		private bool _isInSelection;

		// Token: 0x04000E68 RID: 3688
		private PerkVM.PerkStates _currentState = PerkVM.PerkStates.None;

		// Token: 0x04000E69 RID: 3689
		private string _levelText;

		// Token: 0x04000E6A RID: 3690
		private string _perkId;

		// Token: 0x04000E6B RID: 3691
		private string _backgroundImage;

		// Token: 0x04000E6C RID: 3692
		private BasicTooltipViewModel _hint;

		// Token: 0x04000E6D RID: 3693
		private int _level;

		// Token: 0x04000E6E RID: 3694
		private int _alternativeType;

		// Token: 0x04000E6F RID: 3695
		private int _perkState = -1;

		// Token: 0x04000E70 RID: 3696
		private bool _isTutorialHighlightEnabled;

		// Token: 0x020002CC RID: 716
		public enum PerkStates
		{
			// Token: 0x04001385 RID: 4997
			None = -1,
			// Token: 0x04001386 RID: 4998
			NotEarned,
			// Token: 0x04001387 RID: 4999
			EarnedButNotSelected,
			// Token: 0x04001388 RID: 5000
			InSelection,
			// Token: 0x04001389 RID: 5001
			EarnedAndActive,
			// Token: 0x0400138A RID: 5002
			EarnedAndNotActive,
			// Token: 0x0400138B RID: 5003
			EarnedPreviousPerkNotSelected
		}

		// Token: 0x020002CD RID: 717
		public enum PerkAlternativeType
		{
			// Token: 0x0400138D RID: 5005
			NoAlternative,
			// Token: 0x0400138E RID: 5006
			FirstAlternative,
			// Token: 0x0400138F RID: 5007
			SecondAlternative
		}
	}
}
