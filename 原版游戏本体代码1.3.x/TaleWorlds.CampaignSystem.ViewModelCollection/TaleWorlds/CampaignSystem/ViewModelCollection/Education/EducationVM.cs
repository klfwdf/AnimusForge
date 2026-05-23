using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F6 RID: 246
	public class EducationVM : ViewModel
	{
		// Token: 0x0600160B RID: 5643 RVA: 0x00055F10 File Offset: 0x00054110
		public EducationVM(Hero child, Action<bool> onDone, Action<EducationCampaignBehavior.EducationCharacterProperties[]> onOptionSelect, Action<List<BasicCharacterObject>, List<Equipment>> sendPossibleCharactersAndEquipment)
		{
			this._onDone = onDone;
			this._onOptionSelect = onOptionSelect;
			this._sendPossibleCharactersAndEquipment = sendPossibleCharactersAndEquipment;
			this._child = child;
			this._educationBehavior = Campaign.Current.GetCampaignBehavior<IEducationLogic>();
			int num;
			this._educationBehavior.GetStageProperties(this._child, out num);
			this._pageCount = num + 1;
			this.GainedPropertiesController = new EducationGainedPropertiesVM(this._child, this._pageCount);
			this.Options = new MBBindingList<EducationOptionVM>();
			this.Review = new EducationReviewVM(this._pageCount);
			this.CanGoBack = true;
			this.InitWithStageIndex(0);
		}

		// Token: 0x0600160C RID: 5644 RVA: 0x00055FDC File Offset: 0x000541DC
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject currentPageTitleTextObj = this._currentPageTitleTextObj;
			this.StageTitleText = ((currentPageTitleTextObj != null) ? currentPageTitleTextObj.ToString() : null) ?? "";
			TextObject currentPageDescriptionTextObj = this._currentPageDescriptionTextObj;
			this.PageDescriptionText = ((currentPageDescriptionTextObj != null) ? currentPageDescriptionTextObj.ToString() : null) ?? "";
			TextObject currentPageInstructionTextObj = this._currentPageInstructionTextObj;
			this.ChooseText = ((currentPageInstructionTextObj != null) ? currentPageInstructionTextObj.ToString() : null) ?? "";
			this.Options.ApplyActionOnAllItems(delegate(EducationOptionVM o)
			{
				o.RefreshValues();
			});
			foreach (EducationOptionVM educationOptionVM in this.Options)
			{
				if (educationOptionVM.IsSelected)
				{
					this.OptionEffectText = educationOptionVM.OptionEffect;
					this.OptionDescriptionText = educationOptionVM.OptionDescription;
				}
			}
		}

		// Token: 0x0600160D RID: 5645 RVA: 0x000560D8 File Offset: 0x000542D8
		private void InitWithStageIndex(int index)
		{
			this._latestOptionId = null;
			this.CanAdvance = false;
			this._currentPageIndex = index;
			this.OptionEffectText = "";
			this.OptionDescriptionText = "";
			this.Options.Clear();
			if (index < this._pageCount - 1)
			{
				List<BasicCharacterObject> list = new List<BasicCharacterObject>();
				List<Equipment> list2 = new List<Equipment>();
				TextObject currentPageTitleTextObj;
				TextObject currentPageDescriptionTextObj;
				TextObject currentPageInstructionTextObj;
				EducationCampaignBehavior.EducationCharacterProperties[] array;
				string[] array2;
				this._educationBehavior.GetPageProperties(this._child, this._selectedOptions.Take(index).ToList<string>(), out currentPageTitleTextObj, out currentPageDescriptionTextObj, out currentPageInstructionTextObj, out array, out array2);
				this._currentPageTitleTextObj = currentPageTitleTextObj;
				this._currentPageDescriptionTextObj = currentPageDescriptionTextObj;
				this._currentPageInstructionTextObj = currentPageInstructionTextObj;
				for (int i = 0; i < array2.Length; i++)
				{
					TextObject optionText;
					TextObject optionDescription;
					TextObject optionEffect;
					ValueTuple<CharacterAttribute, int>[] optionAttributes;
					ValueTuple<SkillObject, int>[] optionSkills;
					ValueTuple<SkillObject, int>[] optionFocusPoints;
					EducationCampaignBehavior.EducationCharacterProperties[] array3;
					this._educationBehavior.GetOptionProperties(this._child, array2[i], this._selectedOptions, out optionText, out optionDescription, out optionEffect, out optionAttributes, out optionSkills, out optionFocusPoints, out array3);
					this.Options.Add(new EducationOptionVM(new Action<object>(this.OnOptionSelect), array2[i], optionText, optionDescription, optionEffect, false, optionAttributes, optionSkills, optionFocusPoints, array3));
					foreach (EducationCampaignBehavior.EducationCharacterProperties educationCharacterProperties in array3)
					{
						if (educationCharacterProperties.Character != null && !list.Contains(educationCharacterProperties.Character))
						{
							list.Add(educationCharacterProperties.Character);
						}
						if (educationCharacterProperties.Equipment != null && !list2.Contains(educationCharacterProperties.Equipment))
						{
							list2.Add(educationCharacterProperties.Equipment);
						}
					}
				}
				this.OnlyHasOneOption = this.Options.Count == 1;
				if (this._selectedOptions.Count > index)
				{
					string item = this._selectedOptions[index];
					int num = array2.IndexOf(item);
					if (num >= 0)
					{
						Action<EducationCampaignBehavior.EducationCharacterProperties[]> onOptionSelect = this._onOptionSelect;
						if (onOptionSelect != null)
						{
							onOptionSelect(this.Options[num].CharacterProperties);
						}
						if (index == this._currentPageIndex)
						{
							this.Options[num].ExecuteAction();
							this.CanAdvance = true;
						}
					}
				}
				else
				{
					EducationCampaignBehavior.EducationCharacterProperties[] array5 = new EducationCampaignBehavior.EducationCharacterProperties[(array != null) ? array.Length : 1];
					for (int k = 0; k < ((array != null) ? array.Length : 0); k++)
					{
						array5[k] = array[k];
						if (array5[k].Character != null && !list.Contains(array5[k].Character))
						{
							list.Add(array5[k].Character);
						}
						if (array5[k].Equipment != null && !list2.Contains(array5[k].Equipment))
						{
							list2.Add(array5[k].Equipment);
						}
					}
					Action<EducationCampaignBehavior.EducationCharacterProperties[]> onOptionSelect2 = this._onOptionSelect;
					if (onOptionSelect2 != null)
					{
						onOptionSelect2(array5);
					}
				}
				if (this.OnlyHasOneOption)
				{
					this.Options[0].ExecuteAction();
				}
				this._sendPossibleCharactersAndEquipment(list, list2);
			}
			else
			{
				this._currentPageTitleTextObj = new TextObject("{=Ck9HT8fQ}Summary", null);
				this._currentPageInstructionTextObj = null;
				this._currentPageDescriptionTextObj = null;
				this.OnlyHasOneOption = false;
				this.CanAdvance = true;
			}
			TextObject currentPageTitleTextObj2 = this._currentPageTitleTextObj;
			this.StageTitleText = ((currentPageTitleTextObj2 != null) ? currentPageTitleTextObj2.ToString() : null) ?? "";
			TextObject currentPageInstructionTextObj2 = this._currentPageInstructionTextObj;
			this.ChooseText = ((currentPageInstructionTextObj2 != null) ? currentPageInstructionTextObj2.ToString() : null) ?? "";
			TextObject currentPageDescriptionTextObj2 = this._currentPageDescriptionTextObj;
			this.PageDescriptionText = ((currentPageDescriptionTextObj2 != null) ? currentPageDescriptionTextObj2.ToString() : null) ?? "";
			if (this._currentPageIndex == 0)
			{
				this.NextText = this._nextPageTextObj.ToString();
				this.PreviousText = GameTexts.FindText("str_exit", null).ToString();
			}
			else if (this._currentPageIndex == this._pageCount - 1)
			{
				this.NextText = GameTexts.FindText("str_done", null).ToString();
				this.PreviousText = this._previousPageTextObj.ToString();
			}
			else
			{
				this.NextText = this._nextPageTextObj.ToString();
				this.PreviousText = this._previousPageTextObj.ToString();
			}
			this.UpdateGainedProperties();
			this.Review.SetCurrentPage(this._currentPageIndex);
		}

		// Token: 0x0600160E RID: 5646 RVA: 0x0005650C File Offset: 0x0005470C
		private void OnOptionSelect(object optionIdAsObj)
		{
			if (optionIdAsObj != this._latestOptionId)
			{
				string optionId = (string)optionIdAsObj;
				EducationOptionVM educationOptionVM = this.Options.FirstOrDefault((EducationOptionVM o) => (string)o.Identifier == optionId);
				this.Options.ApplyActionOnAllItems(delegate(EducationOptionVM o)
				{
					o.IsSelected = false;
				});
				educationOptionVM.IsSelected = true;
				string actionText = educationOptionVM.ActionText;
				if (this._currentPageIndex == this._selectedOptions.Count)
				{
					this._selectedOptions.Add(optionId);
				}
				else if (this._currentPageIndex < this._selectedOptions.Count)
				{
					this._selectedOptions[this._currentPageIndex] = optionId;
				}
				else
				{
					Debug.FailedAssert("Skipped a stage for education!!!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Education\\EducationVM.cs", "OnOptionSelect", 210);
				}
				this.OptionEffectText = educationOptionVM.OptionEffect;
				this.OptionDescriptionText = educationOptionVM.OptionDescription;
				Action<EducationCampaignBehavior.EducationCharacterProperties[]> onOptionSelect = this._onOptionSelect;
				if (onOptionSelect != null)
				{
					onOptionSelect(educationOptionVM.CharacterProperties);
				}
				this.UpdateGainedProperties();
				this.CanAdvance = true;
				this._latestOptionId = optionIdAsObj;
				this.Review.SetGainForStage(this._currentPageIndex, this.OptionEffectText);
			}
		}

		// Token: 0x0600160F RID: 5647 RVA: 0x0005664C File Offset: 0x0005484C
		private void UpdateGainedProperties()
		{
			this.GainedPropertiesController.UpdateWithSelections(this._selectedOptions, this._currentPageIndex);
		}

		// Token: 0x06001610 RID: 5648 RVA: 0x00056668 File Offset: 0x00054868
		public void ExecuteNextStage()
		{
			if (this._currentPageIndex + 1 < this._pageCount)
			{
				this.InitWithStageIndex(this._currentPageIndex + 1);
				return;
			}
			this._educationBehavior.Finalize(this._child, this._selectedOptions);
			Action<bool> onDone = this._onDone;
			if (onDone == null)
			{
				return;
			}
			onDone(false);
		}

		// Token: 0x06001611 RID: 5649 RVA: 0x000566BC File Offset: 0x000548BC
		public void ExecutePreviousStage()
		{
			if (this._currentPageIndex > 0)
			{
				this.InitWithStageIndex(this._currentPageIndex - 1);
				return;
			}
			if (this._currentPageIndex == 0)
			{
				Action<bool> onDone = this._onDone;
				if (onDone == null)
				{
					return;
				}
				onDone(true);
			}
		}

		// Token: 0x06001612 RID: 5650 RVA: 0x000566EF File Offset: 0x000548EF
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

		// Token: 0x06001613 RID: 5651 RVA: 0x00056718 File Offset: 0x00054918
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001614 RID: 5652 RVA: 0x00056727 File Offset: 0x00054927
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700074C RID: 1868
		// (get) Token: 0x06001615 RID: 5653 RVA: 0x00056736 File Offset: 0x00054936
		// (set) Token: 0x06001616 RID: 5654 RVA: 0x0005673E File Offset: 0x0005493E
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

		// Token: 0x1700074D RID: 1869
		// (get) Token: 0x06001617 RID: 5655 RVA: 0x0005675C File Offset: 0x0005495C
		// (set) Token: 0x06001618 RID: 5656 RVA: 0x00056764 File Offset: 0x00054964
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

		// Token: 0x1700074E RID: 1870
		// (get) Token: 0x06001619 RID: 5657 RVA: 0x00056782 File Offset: 0x00054982
		// (set) Token: 0x0600161A RID: 5658 RVA: 0x0005678A File Offset: 0x0005498A
		[DataSourceProperty]
		public string StageTitleText
		{
			get
			{
				return this._stageTitleText;
			}
			set
			{
				if (value != this._stageTitleText)
				{
					this._stageTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "StageTitleText");
				}
			}
		}

		// Token: 0x1700074F RID: 1871
		// (get) Token: 0x0600161B RID: 5659 RVA: 0x000567AD File Offset: 0x000549AD
		// (set) Token: 0x0600161C RID: 5660 RVA: 0x000567B5 File Offset: 0x000549B5
		[DataSourceProperty]
		public string ChooseText
		{
			get
			{
				return this._chooseText;
			}
			set
			{
				if (value != this._chooseText)
				{
					this._chooseText = value;
					base.OnPropertyChangedWithValue<string>(value, "ChooseText");
				}
			}
		}

		// Token: 0x17000750 RID: 1872
		// (get) Token: 0x0600161D RID: 5661 RVA: 0x000567D8 File Offset: 0x000549D8
		// (set) Token: 0x0600161E RID: 5662 RVA: 0x000567E0 File Offset: 0x000549E0
		[DataSourceProperty]
		public string PageDescriptionText
		{
			get
			{
				return this._pageDescriptionText;
			}
			set
			{
				if (value != this._pageDescriptionText)
				{
					this._pageDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "PageDescriptionText");
				}
			}
		}

		// Token: 0x17000751 RID: 1873
		// (get) Token: 0x0600161F RID: 5663 RVA: 0x00056803 File Offset: 0x00054A03
		// (set) Token: 0x06001620 RID: 5664 RVA: 0x0005680B File Offset: 0x00054A0B
		[DataSourceProperty]
		public string OptionEffectText
		{
			get
			{
				return this._optionEffectText;
			}
			set
			{
				if (value != this._optionEffectText)
				{
					this._optionEffectText = value;
					base.OnPropertyChangedWithValue<string>(value, "OptionEffectText");
				}
			}
		}

		// Token: 0x17000752 RID: 1874
		// (get) Token: 0x06001621 RID: 5665 RVA: 0x0005682E File Offset: 0x00054A2E
		// (set) Token: 0x06001622 RID: 5666 RVA: 0x00056836 File Offset: 0x00054A36
		[DataSourceProperty]
		public string OptionDescriptionText
		{
			get
			{
				return this._optionDescriptionText;
			}
			set
			{
				if (value != this._optionDescriptionText)
				{
					this._optionDescriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "OptionDescriptionText");
				}
			}
		}

		// Token: 0x17000753 RID: 1875
		// (get) Token: 0x06001623 RID: 5667 RVA: 0x00056859 File Offset: 0x00054A59
		// (set) Token: 0x06001624 RID: 5668 RVA: 0x00056861 File Offset: 0x00054A61
		[DataSourceProperty]
		public string NextText
		{
			get
			{
				return this._nextText;
			}
			set
			{
				if (value != this._nextText)
				{
					this._nextText = value;
					base.OnPropertyChangedWithValue<string>(value, "NextText");
				}
			}
		}

		// Token: 0x17000754 RID: 1876
		// (get) Token: 0x06001625 RID: 5669 RVA: 0x00056884 File Offset: 0x00054A84
		// (set) Token: 0x06001626 RID: 5670 RVA: 0x0005688C File Offset: 0x00054A8C
		[DataSourceProperty]
		public string PreviousText
		{
			get
			{
				return this._previousText;
			}
			set
			{
				if (value != this._previousText)
				{
					this._previousText = value;
					base.OnPropertyChangedWithValue<string>(value, "PreviousText");
				}
			}
		}

		// Token: 0x17000755 RID: 1877
		// (get) Token: 0x06001627 RID: 5671 RVA: 0x000568AF File Offset: 0x00054AAF
		// (set) Token: 0x06001628 RID: 5672 RVA: 0x000568B7 File Offset: 0x00054AB7
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

		// Token: 0x17000756 RID: 1878
		// (get) Token: 0x06001629 RID: 5673 RVA: 0x000568D5 File Offset: 0x00054AD5
		// (set) Token: 0x0600162A RID: 5674 RVA: 0x000568DD File Offset: 0x00054ADD
		[DataSourceProperty]
		public bool CanGoBack
		{
			get
			{
				return this._canGoBack;
			}
			set
			{
				if (value != this._canGoBack)
				{
					this._canGoBack = value;
					base.OnPropertyChangedWithValue(value, "CanGoBack");
				}
			}
		}

		// Token: 0x17000757 RID: 1879
		// (get) Token: 0x0600162B RID: 5675 RVA: 0x000568FB File Offset: 0x00054AFB
		// (set) Token: 0x0600162C RID: 5676 RVA: 0x00056903 File Offset: 0x00054B03
		[DataSourceProperty]
		public bool OnlyHasOneOption
		{
			get
			{
				return this._onlyHasOneOption;
			}
			set
			{
				if (value != this._onlyHasOneOption)
				{
					this._onlyHasOneOption = value;
					base.OnPropertyChangedWithValue(value, "OnlyHasOneOption");
				}
			}
		}

		// Token: 0x17000758 RID: 1880
		// (get) Token: 0x0600162D RID: 5677 RVA: 0x00056921 File Offset: 0x00054B21
		// (set) Token: 0x0600162E RID: 5678 RVA: 0x00056929 File Offset: 0x00054B29
		[DataSourceProperty]
		public MBBindingList<EducationOptionVM> Options
		{
			get
			{
				return this._options;
			}
			set
			{
				if (value != this._options)
				{
					this._options = value;
					base.OnPropertyChangedWithValue<MBBindingList<EducationOptionVM>>(value, "Options");
				}
			}
		}

		// Token: 0x17000759 RID: 1881
		// (get) Token: 0x0600162F RID: 5679 RVA: 0x00056947 File Offset: 0x00054B47
		// (set) Token: 0x06001630 RID: 5680 RVA: 0x0005694F File Offset: 0x00054B4F
		[DataSourceProperty]
		public EducationGainedPropertiesVM GainedPropertiesController
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
					base.OnPropertyChangedWithValue<EducationGainedPropertiesVM>(value, "GainedPropertiesController");
				}
			}
		}

		// Token: 0x1700075A RID: 1882
		// (get) Token: 0x06001631 RID: 5681 RVA: 0x0005696D File Offset: 0x00054B6D
		// (set) Token: 0x06001632 RID: 5682 RVA: 0x00056975 File Offset: 0x00054B75
		[DataSourceProperty]
		public EducationReviewVM Review
		{
			get
			{
				return this._review;
			}
			set
			{
				if (value != this._review)
				{
					this._review = value;
					base.OnPropertyChangedWithValue<EducationReviewVM>(value, "Review");
				}
			}
		}

		// Token: 0x04000A09 RID: 2569
		private readonly Action<bool> _onDone;

		// Token: 0x04000A0A RID: 2570
		private readonly Action<EducationCampaignBehavior.EducationCharacterProperties[]> _onOptionSelect;

		// Token: 0x04000A0B RID: 2571
		private readonly Action<List<BasicCharacterObject>, List<Equipment>> _sendPossibleCharactersAndEquipment;

		// Token: 0x04000A0C RID: 2572
		private readonly IEducationLogic _educationBehavior;

		// Token: 0x04000A0D RID: 2573
		private readonly Hero _child;

		// Token: 0x04000A0E RID: 2574
		private readonly TextObject _nextPageTextObj = new TextObject("{=Rvr1bcu8}Next", null);

		// Token: 0x04000A0F RID: 2575
		private readonly TextObject _previousPageTextObj = new TextObject("{=WXAaWZVf}Previous", null);

		// Token: 0x04000A10 RID: 2576
		private readonly int _pageCount;

		// Token: 0x04000A11 RID: 2577
		private readonly List<string> _selectedOptions = new List<string>();

		// Token: 0x04000A12 RID: 2578
		private TextObject _currentPageTitleTextObj;

		// Token: 0x04000A13 RID: 2579
		private TextObject _currentPageDescriptionTextObj;

		// Token: 0x04000A14 RID: 2580
		private TextObject _currentPageInstructionTextObj;

		// Token: 0x04000A15 RID: 2581
		private object _latestOptionId;

		// Token: 0x04000A16 RID: 2582
		private int _currentPageIndex;

		// Token: 0x04000A17 RID: 2583
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000A18 RID: 2584
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000A19 RID: 2585
		private string _stageTitleText;

		// Token: 0x04000A1A RID: 2586
		private string _chooseText;

		// Token: 0x04000A1B RID: 2587
		private string _pageDescriptionText;

		// Token: 0x04000A1C RID: 2588
		private string _optionEffectText;

		// Token: 0x04000A1D RID: 2589
		private string _optionDescriptionText;

		// Token: 0x04000A1E RID: 2590
		private string _nextText;

		// Token: 0x04000A1F RID: 2591
		private string _previousText;

		// Token: 0x04000A20 RID: 2592
		private bool _canAdvance;

		// Token: 0x04000A21 RID: 2593
		private bool _canGoBack;

		// Token: 0x04000A22 RID: 2594
		private bool _onlyHasOneOption;

		// Token: 0x04000A23 RID: 2595
		private MBBindingList<EducationOptionVM> _options;

		// Token: 0x04000A24 RID: 2596
		private EducationGainedPropertiesVM _gainedPropertiesController;

		// Token: 0x04000A25 RID: 2597
		private EducationReviewVM _review;
	}
}
