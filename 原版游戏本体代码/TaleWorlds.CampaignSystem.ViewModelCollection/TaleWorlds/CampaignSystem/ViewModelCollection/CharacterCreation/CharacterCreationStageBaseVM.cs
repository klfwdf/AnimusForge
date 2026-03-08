using System;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000157 RID: 343
	public abstract class CharacterCreationStageBaseVM : ViewModel
	{
		// Token: 0x06002027 RID: 8231 RVA: 0x000755A4 File Offset: 0x000737A4
		protected CharacterCreationStageBaseVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText)
		{
			this.CharacterCreationManager = characterCreationManager;
			this._affirmativeAction = affirmativeAction;
			this._negativeAction = negativeAction;
			this._affirmativeActionText = affirmativeActionText;
			this._negativeActionText = negativeActionText;
			TextObject affirmativeActionText2 = this._affirmativeActionText;
			this.NextStageText = ((affirmativeActionText2 != null) ? affirmativeActionText2.ToString() : null);
			TextObject negativeActionText2 = this._negativeActionText;
			this.PreviousStageText = ((negativeActionText2 != null) ? negativeActionText2.ToString() : null);
		}

		// Token: 0x06002028 RID: 8232
		public abstract void OnNextStage();

		// Token: 0x06002029 RID: 8233
		public abstract void OnPreviousStage();

		// Token: 0x0600202A RID: 8234
		public abstract bool CanAdvanceToNextStage();

		// Token: 0x17000AF2 RID: 2802
		// (get) Token: 0x0600202B RID: 8235 RVA: 0x00075642 File Offset: 0x00073842
		// (set) Token: 0x0600202C RID: 8236 RVA: 0x0007564A File Offset: 0x0007384A
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x17000AF3 RID: 2803
		// (get) Token: 0x0600202D RID: 8237 RVA: 0x0007566D File Offset: 0x0007386D
		// (set) Token: 0x0600202E RID: 8238 RVA: 0x00075675 File Offset: 0x00073875
		[DataSourceProperty]
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (value != this._description)
				{
					this._description = value;
					base.OnPropertyChangedWithValue<string>(value, "Description");
				}
			}
		}

		// Token: 0x17000AF4 RID: 2804
		// (get) Token: 0x0600202F RID: 8239 RVA: 0x00075698 File Offset: 0x00073898
		// (set) Token: 0x06002030 RID: 8240 RVA: 0x000756A0 File Offset: 0x000738A0
		[DataSourceProperty]
		public string SelectionText
		{
			get
			{
				return this._selectionText;
			}
			set
			{
				if (value != this._selectionText)
				{
					this._selectionText = value;
					base.OnPropertyChangedWithValue<string>(value, "SelectionText");
				}
			}
		}

		// Token: 0x17000AF5 RID: 2805
		// (get) Token: 0x06002031 RID: 8241 RVA: 0x000756C3 File Offset: 0x000738C3
		// (set) Token: 0x06002032 RID: 8242 RVA: 0x000756CB File Offset: 0x000738CB
		[DataSourceProperty]
		public string NextStageText
		{
			get
			{
				return this._nextStageText;
			}
			set
			{
				if (value != this._nextStageText)
				{
					this._nextStageText = value;
					base.OnPropertyChangedWithValue<string>(value, "NextStageText");
				}
			}
		}

		// Token: 0x17000AF6 RID: 2806
		// (get) Token: 0x06002033 RID: 8243 RVA: 0x000756EE File Offset: 0x000738EE
		// (set) Token: 0x06002034 RID: 8244 RVA: 0x000756F6 File Offset: 0x000738F6
		[DataSourceProperty]
		public string PreviousStageText
		{
			get
			{
				return this._previousStageText;
			}
			set
			{
				if (value != this._previousStageText)
				{
					this._previousStageText = value;
					base.OnPropertyChangedWithValue<string>(value, "PreviousStageText");
				}
			}
		}

		// Token: 0x17000AF7 RID: 2807
		// (get) Token: 0x06002035 RID: 8245 RVA: 0x00075719 File Offset: 0x00073919
		// (set) Token: 0x06002036 RID: 8246 RVA: 0x00075721 File Offset: 0x00073921
		[DataSourceProperty]
		public int TotalStageCount
		{
			get
			{
				return this._totalStageCount;
			}
			set
			{
				if (value != this._totalStageCount)
				{
					this._totalStageCount = value;
					base.OnPropertyChangedWithValue(value, "TotalStageCount");
				}
			}
		}

		// Token: 0x17000AF8 RID: 2808
		// (get) Token: 0x06002037 RID: 8247 RVA: 0x0007573F File Offset: 0x0007393F
		// (set) Token: 0x06002038 RID: 8248 RVA: 0x00075747 File Offset: 0x00073947
		[DataSourceProperty]
		public int FurthestIndex
		{
			get
			{
				return this._furthestIndex;
			}
			set
			{
				if (value != this._furthestIndex)
				{
					this._furthestIndex = value;
					base.OnPropertyChangedWithValue(value, "FurthestIndex");
				}
			}
		}

		// Token: 0x17000AF9 RID: 2809
		// (get) Token: 0x06002039 RID: 8249 RVA: 0x00075765 File Offset: 0x00073965
		// (set) Token: 0x0600203A RID: 8250 RVA: 0x0007576D File Offset: 0x0007396D
		[DataSourceProperty]
		public int CurrentStageIndex
		{
			get
			{
				return this._currentStageIndex;
			}
			set
			{
				if (value != this._currentStageIndex)
				{
					this._currentStageIndex = value;
					base.OnPropertyChangedWithValue(value, "CurrentStageIndex");
				}
			}
		}

		// Token: 0x17000AFA RID: 2810
		// (get) Token: 0x0600203B RID: 8251 RVA: 0x0007578B File Offset: 0x0007398B
		// (set) Token: 0x0600203C RID: 8252 RVA: 0x00075793 File Offset: 0x00073993
		[DataSourceProperty]
		public bool AnyItemSelected
		{
			get
			{
				return this._anyItemSelected;
			}
			set
			{
				if (value != this._anyItemSelected)
				{
					this._anyItemSelected = value;
					base.OnPropertyChangedWithValue(value, "AnyItemSelected");
				}
			}
		}

		// Token: 0x17000AFB RID: 2811
		// (get) Token: 0x0600203D RID: 8253 RVA: 0x000757B1 File Offset: 0x000739B1
		// (set) Token: 0x0600203E RID: 8254 RVA: 0x000757B9 File Offset: 0x000739B9
		[DataSourceProperty]
		public bool CanAdvance
		{
			get
			{
				return this._canAdvance;
			}
			set
			{
				if (value != this._canAdvance)
				{
					this._canAdvance = value;
					base.OnPropertyChangedWithValue(value, "CanAdvance");
				}
			}
		}

		// Token: 0x04000EF4 RID: 3828
		protected readonly CharacterCreationManager CharacterCreationManager;

		// Token: 0x04000EF5 RID: 3829
		protected readonly Action _affirmativeAction;

		// Token: 0x04000EF6 RID: 3830
		protected readonly Action _negativeAction;

		// Token: 0x04000EF7 RID: 3831
		protected readonly TextObject _affirmativeActionText;

		// Token: 0x04000EF8 RID: 3832
		protected readonly TextObject _negativeActionText;

		// Token: 0x04000EF9 RID: 3833
		private string _title = "";

		// Token: 0x04000EFA RID: 3834
		private string _description = "";

		// Token: 0x04000EFB RID: 3835
		private string _selectionText = "";

		// Token: 0x04000EFC RID: 3836
		private string _nextStageText;

		// Token: 0x04000EFD RID: 3837
		private string _previousStageText;

		// Token: 0x04000EFE RID: 3838
		private int _totalStageCount = -1;

		// Token: 0x04000EFF RID: 3839
		private int _currentStageIndex = -1;

		// Token: 0x04000F00 RID: 3840
		private int _furthestIndex = -1;

		// Token: 0x04000F01 RID: 3841
		private bool _anyItemSelected;

		// Token: 0x04000F02 RID: 3842
		private bool _canAdvance;
	}
}
