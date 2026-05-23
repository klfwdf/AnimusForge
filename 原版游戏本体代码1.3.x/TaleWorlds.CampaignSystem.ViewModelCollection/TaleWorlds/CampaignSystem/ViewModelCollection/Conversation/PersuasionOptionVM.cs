using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation
{
	// Token: 0x02000119 RID: 281
	public class PersuasionOptionVM : ViewModel
	{
		// Token: 0x170008A4 RID: 2212
		// (get) Token: 0x060019C0 RID: 6592 RVA: 0x0006178C File Offset: 0x0005F98C
		private ConversationSentenceOption _option
		{
			get
			{
				return this._manager.CurOptions[this._index];
			}
		}

		// Token: 0x060019C1 RID: 6593 RVA: 0x000617A4 File Offset: 0x0005F9A4
		public PersuasionOptionVM(ConversationManager manager, int index, Action onReadyToContinue)
		{
			this._index = index;
			this._manager = manager;
			this._onReadyToContinue = onReadyToContinue;
			if (ConversationManager.GetPersuasionIsActive() && this._option.HasPersuasion)
			{
				float num;
				float num2;
				float num3;
				float num4;
				this._manager.GetPersuasionChances(this._option, out num, out num2, out num3, out num4);
				this.CritFailChance = (int)(num3 * 100f);
				this.FailChance = (int)(num4 * 100f);
				this.SuccessChance = (int)(num * 100f);
				this.CritSuccessChance = (int)(num2 * 100f);
				this._args = this._option.PersuationOptionArgs;
			}
			this.RefreshValues();
		}

		// Token: 0x060019C2 RID: 6594 RVA: 0x00061850 File Offset: 0x0005FA50
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (ConversationManager.GetPersuasionIsActive() && this._option.HasPersuasion)
			{
				GameTexts.SetVariable("NUMBER", this.CritFailChance);
				this.CritFailChanceText = GameTexts.FindText("str_NUMBER_percent", null).ToString();
				GameTexts.SetVariable("NUMBER", this.FailChance);
				this.FailChanceText = GameTexts.FindText("str_NUMBER_percent", null).ToString();
				GameTexts.SetVariable("NUMBER", this.SuccessChance);
				this.SuccessChanceText = GameTexts.FindText("str_NUMBER_percent", null).ToString();
				GameTexts.SetVariable("NUMBER", this.CritSuccessChance);
				this.CritSuccessChanceText = GameTexts.FindText("str_NUMBER_percent", null).ToString();
				this.CritFailHint = new BasicTooltipViewModel(delegate()
				{
					GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_critical_fail", null));
					GameTexts.SetVariable("NUMBER", this.CritFailChance);
					GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent", null));
					return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
				});
				this.FailHint = new BasicTooltipViewModel(delegate()
				{
					GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_fail", null));
					GameTexts.SetVariable("NUMBER", this.FailChance);
					GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent", null));
					return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
				});
				this.SuccessHint = new BasicTooltipViewModel(delegate()
				{
					GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_success", null));
					GameTexts.SetVariable("NUMBER", this.SuccessChance);
					GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent", null));
					return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
				});
				this.CritSuccessHint = new BasicTooltipViewModel(delegate()
				{
					GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_critical_success", null));
					GameTexts.SetVariable("NUMBER", this.CritSuccessChance);
					GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent", null));
					return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
				});
				this.ProgressingOptionHint = new HintViewModel(GameTexts.FindText("str_persuasion_progressing_hint", null), null);
				this.BlockingOptionHint = new HintViewModel(GameTexts.FindText("str_persuasion_blocking_hint", null), null);
				this.IsABlockingOption = this._args.CanBlockOtherOption;
				this.IsAProgressingOption = this._args.CanMoveToTheNextReservation;
			}
		}

		// Token: 0x060019C3 RID: 6595 RVA: 0x000619C1 File Offset: 0x0005FBC1
		internal void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> result)
		{
			this.IsPersuasionResultReady = true;
			if (result.Item1 == this._args)
			{
				this.PersuasionResultIndex = (int)result.Item2;
			}
		}

		// Token: 0x060019C4 RID: 6596 RVA: 0x000619E4 File Offset: 0x0005FBE4
		public string GetPersuasionAdditionalText()
		{
			string text = null;
			if (this._args != null)
			{
				if (this._args.SkillUsed != null)
				{
					text = ((Hero.MainHero.GetSkillValue(this._args.SkillUsed) <= 50) ? "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>" : "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>").Replace("{TEXT}", this._args.SkillUsed.Name.ToString());
				}
				if (this._args.TraitUsed != null && !this._args.TraitUsed.IsHidden)
				{
					int traitLevel = Hero.MainHero.GetTraitLevel(this._args.TraitUsed);
					string text2;
					if (traitLevel == 0)
					{
						text2 = "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>";
					}
					else
					{
						text2 = (((traitLevel > 0 && this._args.TraitEffect == TraitEffect.Positive) || (traitLevel < 0 && this._args.TraitEffect == TraitEffect.Negative)) ? "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>" : "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>");
					}
					text2 = text2.Replace("{TEXT}", this._args.TraitUsed.Name.ToString());
					if (text != null)
					{
						GameTexts.SetVariable("LEFT", text);
						GameTexts.SetVariable("RIGHT", text2);
						text = GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
					}
					else
					{
						text = text2;
					}
				}
				if (this._args.TraitCorrelation != null)
				{
					foreach (Tuple<TraitObject, int> tuple in this._args.TraitCorrelation)
					{
						if (tuple.Item2 != 0 && this._args.TraitUsed != tuple.Item1 && !tuple.Item1.IsHidden)
						{
							int traitLevel2 = Hero.MainHero.GetTraitLevel(tuple.Item1);
							string text3;
							if (traitLevel2 == 0)
							{
								text3 = "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>";
							}
							else
							{
								text3 = ((traitLevel2 * tuple.Item2 > 0) ? "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>" : "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>");
							}
							text3 = text3.Replace("{TEXT}", tuple.Item1.Name.ToString());
							if (text != null)
							{
								GameTexts.SetVariable("LEFT", text);
								GameTexts.SetVariable("RIGHT", text3);
								text = GameTexts.FindText("str_LEFT_comma_RIGHT", null).ToString();
							}
							else
							{
								text = text3;
							}
						}
					}
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				GameTexts.SetVariable("STR", text);
				return GameTexts.FindText("str_STR_in_parentheses", null).ToString();
			}
			return string.Empty;
		}

		// Token: 0x060019C5 RID: 6597 RVA: 0x00061C45 File Offset: 0x0005FE45
		public void ExecuteReadyToContinue()
		{
			Action onReadyToContinue = this._onReadyToContinue;
			if (onReadyToContinue == null)
			{
				return;
			}
			onReadyToContinue.DynamicInvokeWithLog(Array.Empty<object>());
		}

		// Token: 0x170008A5 RID: 2213
		// (get) Token: 0x060019C6 RID: 6598 RVA: 0x00061C5D File Offset: 0x0005FE5D
		// (set) Token: 0x060019C7 RID: 6599 RVA: 0x00061C65 File Offset: 0x0005FE65
		[DataSourceProperty]
		public bool IsPersuasionResultReady
		{
			get
			{
				return this._isPersuasionResultReady;
			}
			set
			{
				if (this._isPersuasionResultReady != value)
				{
					this._isPersuasionResultReady = value;
					base.OnPropertyChangedWithValue(value, "IsPersuasionResultReady");
				}
			}
		}

		// Token: 0x170008A6 RID: 2214
		// (get) Token: 0x060019C8 RID: 6600 RVA: 0x00061C83 File Offset: 0x0005FE83
		// (set) Token: 0x060019C9 RID: 6601 RVA: 0x00061C8B File Offset: 0x0005FE8B
		[DataSourceProperty]
		public bool IsABlockingOption
		{
			get
			{
				return this._isABlockingOption;
			}
			set
			{
				if (this._isABlockingOption != value)
				{
					this._isABlockingOption = value;
					base.OnPropertyChangedWithValue(value, "IsABlockingOption");
				}
			}
		}

		// Token: 0x170008A7 RID: 2215
		// (get) Token: 0x060019CA RID: 6602 RVA: 0x00061CA9 File Offset: 0x0005FEA9
		// (set) Token: 0x060019CB RID: 6603 RVA: 0x00061CB1 File Offset: 0x0005FEB1
		[DataSourceProperty]
		public bool IsAProgressingOption
		{
			get
			{
				return this._isAProgressingOption;
			}
			set
			{
				if (this._isAProgressingOption != value)
				{
					this._isAProgressingOption = value;
					base.OnPropertyChangedWithValue(value, "IsAProgressingOption");
				}
			}
		}

		// Token: 0x170008A8 RID: 2216
		// (get) Token: 0x060019CC RID: 6604 RVA: 0x00061CCF File Offset: 0x0005FECF
		// (set) Token: 0x060019CD RID: 6605 RVA: 0x00061CD7 File Offset: 0x0005FED7
		[DataSourceProperty]
		public int SuccessChance
		{
			get
			{
				return this._successChance;
			}
			set
			{
				if (this._successChance != value)
				{
					this._successChance = value;
					base.OnPropertyChangedWithValue(value, "SuccessChance");
				}
			}
		}

		// Token: 0x170008A9 RID: 2217
		// (get) Token: 0x060019CE RID: 6606 RVA: 0x00061CF5 File Offset: 0x0005FEF5
		// (set) Token: 0x060019CF RID: 6607 RVA: 0x00061CFD File Offset: 0x0005FEFD
		[DataSourceProperty]
		public int PersuasionResultIndex
		{
			get
			{
				return this._persuasionResultIndex;
			}
			set
			{
				if (this._persuasionResultIndex != value)
				{
					this._persuasionResultIndex = value;
					base.OnPropertyChangedWithValue(value, "PersuasionResultIndex");
				}
			}
		}

		// Token: 0x170008AA RID: 2218
		// (get) Token: 0x060019D0 RID: 6608 RVA: 0x00061D1B File Offset: 0x0005FF1B
		// (set) Token: 0x060019D1 RID: 6609 RVA: 0x00061D23 File Offset: 0x0005FF23
		[DataSourceProperty]
		public int FailChance
		{
			get
			{
				return this._failChance;
			}
			set
			{
				if (this._failChance != value)
				{
					this._failChance = value;
					base.OnPropertyChangedWithValue(value, "FailChance");
				}
			}
		}

		// Token: 0x170008AB RID: 2219
		// (get) Token: 0x060019D2 RID: 6610 RVA: 0x00061D41 File Offset: 0x0005FF41
		// (set) Token: 0x060019D3 RID: 6611 RVA: 0x00061D49 File Offset: 0x0005FF49
		[DataSourceProperty]
		public int CritSuccessChance
		{
			get
			{
				return this._critSuccessChance;
			}
			set
			{
				if (this._critSuccessChance != value)
				{
					this._critSuccessChance = value;
					base.OnPropertyChangedWithValue(value, "CritSuccessChance");
				}
			}
		}

		// Token: 0x170008AC RID: 2220
		// (get) Token: 0x060019D4 RID: 6612 RVA: 0x00061D67 File Offset: 0x0005FF67
		// (set) Token: 0x060019D5 RID: 6613 RVA: 0x00061D6F File Offset: 0x0005FF6F
		[DataSourceProperty]
		public int CritFailChance
		{
			get
			{
				return this._critFailChance;
			}
			set
			{
				if (this._critFailChance != value)
				{
					this._critFailChance = value;
					base.OnPropertyChangedWithValue(value, "CritFailChance");
				}
			}
		}

		// Token: 0x170008AD RID: 2221
		// (get) Token: 0x060019D6 RID: 6614 RVA: 0x00061D8D File Offset: 0x0005FF8D
		// (set) Token: 0x060019D7 RID: 6615 RVA: 0x00061D95 File Offset: 0x0005FF95
		[DataSourceProperty]
		public string FailChanceText
		{
			get
			{
				return this._failChanceText;
			}
			set
			{
				if (this._failChanceText != value)
				{
					this._failChanceText = value;
					base.OnPropertyChangedWithValue<string>(value, "FailChanceText");
				}
			}
		}

		// Token: 0x170008AE RID: 2222
		// (get) Token: 0x060019D8 RID: 6616 RVA: 0x00061DB8 File Offset: 0x0005FFB8
		// (set) Token: 0x060019D9 RID: 6617 RVA: 0x00061DC0 File Offset: 0x0005FFC0
		[DataSourceProperty]
		public string CritFailChanceText
		{
			get
			{
				return this._critFailChanceText;
			}
			set
			{
				if (this._critFailChanceText != value)
				{
					this._critFailChanceText = value;
					base.OnPropertyChangedWithValue<string>(value, "CritFailChanceText");
				}
			}
		}

		// Token: 0x170008AF RID: 2223
		// (get) Token: 0x060019DA RID: 6618 RVA: 0x00061DE3 File Offset: 0x0005FFE3
		// (set) Token: 0x060019DB RID: 6619 RVA: 0x00061DEB File Offset: 0x0005FFEB
		[DataSourceProperty]
		public string SuccessChanceText
		{
			get
			{
				return this._successChanceText;
			}
			set
			{
				if (this._successChanceText != value)
				{
					this._successChanceText = value;
					base.OnPropertyChangedWithValue<string>(value, "SuccessChanceText");
				}
			}
		}

		// Token: 0x170008B0 RID: 2224
		// (get) Token: 0x060019DC RID: 6620 RVA: 0x00061E0E File Offset: 0x0006000E
		// (set) Token: 0x060019DD RID: 6621 RVA: 0x00061E16 File Offset: 0x00060016
		[DataSourceProperty]
		public string CritSuccessChanceText
		{
			get
			{
				return this._critSuccessChanceText;
			}
			set
			{
				if (this._critSuccessChanceText != value)
				{
					this._critSuccessChanceText = value;
					base.OnPropertyChangedWithValue<string>(value, "CritSuccessChanceText");
				}
			}
		}

		// Token: 0x170008B1 RID: 2225
		// (get) Token: 0x060019DE RID: 6622 RVA: 0x00061E39 File Offset: 0x00060039
		// (set) Token: 0x060019DF RID: 6623 RVA: 0x00061E41 File Offset: 0x00060041
		[DataSourceProperty]
		public BasicTooltipViewModel CritFailHint
		{
			get
			{
				return this._critFailHint;
			}
			set
			{
				if (this._critFailHint != value)
				{
					this._critFailHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CritFailHint");
				}
			}
		}

		// Token: 0x170008B2 RID: 2226
		// (get) Token: 0x060019E0 RID: 6624 RVA: 0x00061E5F File Offset: 0x0006005F
		// (set) Token: 0x060019E1 RID: 6625 RVA: 0x00061E67 File Offset: 0x00060067
		[DataSourceProperty]
		public BasicTooltipViewModel FailHint
		{
			get
			{
				return this._failHint;
			}
			set
			{
				if (this._failHint != value)
				{
					this._failHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "FailHint");
				}
			}
		}

		// Token: 0x170008B3 RID: 2227
		// (get) Token: 0x060019E2 RID: 6626 RVA: 0x00061E85 File Offset: 0x00060085
		// (set) Token: 0x060019E3 RID: 6627 RVA: 0x00061E8D File Offset: 0x0006008D
		[DataSourceProperty]
		public BasicTooltipViewModel SuccessHint
		{
			get
			{
				return this._successHint;
			}
			set
			{
				if (this._successHint != value)
				{
					this._successHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SuccessHint");
				}
			}
		}

		// Token: 0x170008B4 RID: 2228
		// (get) Token: 0x060019E4 RID: 6628 RVA: 0x00061EAB File Offset: 0x000600AB
		// (set) Token: 0x060019E5 RID: 6629 RVA: 0x00061EB3 File Offset: 0x000600B3
		[DataSourceProperty]
		public BasicTooltipViewModel CritSuccessHint
		{
			get
			{
				return this._critSuccessHint;
			}
			set
			{
				if (this._critSuccessHint != value)
				{
					this._critSuccessHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CritSuccessHint");
				}
			}
		}

		// Token: 0x170008B5 RID: 2229
		// (get) Token: 0x060019E6 RID: 6630 RVA: 0x00061ED1 File Offset: 0x000600D1
		// (set) Token: 0x060019E7 RID: 6631 RVA: 0x00061ED9 File Offset: 0x000600D9
		[DataSourceProperty]
		public HintViewModel BlockingOptionHint
		{
			get
			{
				return this._blockingOptionHint;
			}
			set
			{
				if (this._blockingOptionHint != value)
				{
					this._blockingOptionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "BlockingOptionHint");
				}
			}
		}

		// Token: 0x170008B6 RID: 2230
		// (get) Token: 0x060019E8 RID: 6632 RVA: 0x00061EF7 File Offset: 0x000600F7
		// (set) Token: 0x060019E9 RID: 6633 RVA: 0x00061EFF File Offset: 0x000600FF
		[DataSourceProperty]
		public HintViewModel ProgressingOptionHint
		{
			get
			{
				return this._progressingOptionHint;
			}
			set
			{
				if (this._progressingOptionHint != value)
				{
					this._progressingOptionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ProgressingOptionHint");
				}
			}
		}

		// Token: 0x04000BD6 RID: 3030
		private const int _minSkillValueForPositive = 50;

		// Token: 0x04000BD7 RID: 3031
		private readonly ConversationManager _manager;

		// Token: 0x04000BD8 RID: 3032
		private readonly PersuasionOptionArgs _args;

		// Token: 0x04000BD9 RID: 3033
		private readonly Action _onReadyToContinue;

		// Token: 0x04000BDA RID: 3034
		private readonly int _index;

		// Token: 0x04000BDB RID: 3035
		private int _critFailChance;

		// Token: 0x04000BDC RID: 3036
		private int _failChance;

		// Token: 0x04000BDD RID: 3037
		private int _successChance;

		// Token: 0x04000BDE RID: 3038
		private int _critSuccessChance;

		// Token: 0x04000BDF RID: 3039
		private bool _isPersuasionResultReady;

		// Token: 0x04000BE0 RID: 3040
		private int _persuasionResultIndex = -1;

		// Token: 0x04000BE1 RID: 3041
		private bool _isABlockingOption;

		// Token: 0x04000BE2 RID: 3042
		private bool _isAProgressingOption;

		// Token: 0x04000BE3 RID: 3043
		private string _critFailChanceText;

		// Token: 0x04000BE4 RID: 3044
		private string _failChanceText;

		// Token: 0x04000BE5 RID: 3045
		private string _successChanceText;

		// Token: 0x04000BE6 RID: 3046
		private string _critSuccessChanceText;

		// Token: 0x04000BE7 RID: 3047
		private BasicTooltipViewModel _critFailHint;

		// Token: 0x04000BE8 RID: 3048
		private BasicTooltipViewModel _failHint;

		// Token: 0x04000BE9 RID: 3049
		private BasicTooltipViewModel _successHint;

		// Token: 0x04000BEA RID: 3050
		private BasicTooltipViewModel _critSuccessHint;

		// Token: 0x04000BEB RID: 3051
		private HintViewModel _progressingOptionHint;

		// Token: 0x04000BEC RID: 3052
		private HintViewModel _blockingOptionHint;
	}
}
