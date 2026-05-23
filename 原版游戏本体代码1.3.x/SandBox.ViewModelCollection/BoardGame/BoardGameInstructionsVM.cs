using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.BoardGame
{
	// Token: 0x0200005E RID: 94
	public class BoardGameInstructionsVM : ViewModel
	{
		// Token: 0x060005A9 RID: 1449 RVA: 0x00014EFC File Offset: 0x000130FC
		public BoardGameInstructionsVM(CultureObject.BoardGameType boardGameType)
		{
			this._boardGameType = boardGameType;
			this.InstructionList = new MBBindingList<BoardGameInstructionVM>();
			for (int i = 0; i < this.GetNumberOfInstructions(this._boardGameType); i++)
			{
				this.InstructionList.Add(new BoardGameInstructionVM(this._boardGameType, i));
			}
			this._currentInstructionIndex = 0;
			if (this.InstructionList.Count > 0)
			{
				this.InstructionList[0].IsEnabled = true;
			}
			this.RefreshValues();
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x00014F7C File Offset: 0x0001317C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.InstructionsText = GameTexts.FindText("str_how_to_play", null).ToString();
			this.PreviousText = GameTexts.FindText("str_previous", null).ToString();
			this.NextText = GameTexts.FindText("str_next", null).ToString();
			this.InstructionList.ApplyActionOnAllItems(delegate(BoardGameInstructionVM x)
			{
				x.RefreshValues();
			});
			if (this._currentInstructionIndex >= 0 && this._currentInstructionIndex < this.InstructionList.Count)
			{
				TextObject textObject = new TextObject("{=hUSmlhNh}{CURRENT_PAGE}/{TOTAL_PAGES}", null);
				textObject.SetTextVariable("CURRENT_PAGE", (this._currentInstructionIndex + 1).ToString());
				textObject.SetTextVariable("TOTAL_PAGES", this.InstructionList.Count.ToString());
				this.CurrentPageText = textObject.ToString();
				this.IsPreviousButtonEnabled = this._currentInstructionIndex != 0;
				this.IsNextButtonEnabled = this._currentInstructionIndex < this.InstructionList.Count - 1;
			}
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x00015098 File Offset: 0x00013298
		public void ExecuteShowPrevious()
		{
			if (this._currentInstructionIndex > 0 && this._currentInstructionIndex < this.InstructionList.Count)
			{
				this.InstructionList[this._currentInstructionIndex].IsEnabled = false;
				this._currentInstructionIndex--;
				this.InstructionList[this._currentInstructionIndex].IsEnabled = true;
				this.RefreshValues();
			}
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x00015104 File Offset: 0x00013304
		public void ExecuteShowNext()
		{
			if (this._currentInstructionIndex >= 0 && this._currentInstructionIndex < this.InstructionList.Count - 1)
			{
				this.InstructionList[this._currentInstructionIndex].IsEnabled = false;
				this._currentInstructionIndex++;
				this.InstructionList[this._currentInstructionIndex].IsEnabled = true;
				this.RefreshValues();
			}
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x00015171 File Offset: 0x00013371
		private int GetNumberOfInstructions(CultureObject.BoardGameType game)
		{
			switch (game)
			{
			case CultureObject.BoardGameType.Seega:
				return 4;
			case CultureObject.BoardGameType.Puluc:
				return 5;
			case CultureObject.BoardGameType.Konane:
				return 3;
			case CultureObject.BoardGameType.MuTorere:
				return 2;
			case CultureObject.BoardGameType.Tablut:
				return 4;
			case CultureObject.BoardGameType.BaghChal:
				return 4;
			default:
				return 0;
			}
		}

		// Token: 0x170001AE RID: 430
		// (get) Token: 0x060005AE RID: 1454 RVA: 0x000151A0 File Offset: 0x000133A0
		// (set) Token: 0x060005AF RID: 1455 RVA: 0x000151A8 File Offset: 0x000133A8
		[DataSourceProperty]
		public bool IsPreviousButtonEnabled
		{
			get
			{
				return this._isPreviousButtonEnabled;
			}
			set
			{
				if (value != this._isPreviousButtonEnabled)
				{
					this._isPreviousButtonEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsPreviousButtonEnabled");
				}
			}
		}

		// Token: 0x170001AF RID: 431
		// (get) Token: 0x060005B0 RID: 1456 RVA: 0x000151C6 File Offset: 0x000133C6
		// (set) Token: 0x060005B1 RID: 1457 RVA: 0x000151CE File Offset: 0x000133CE
		[DataSourceProperty]
		public bool IsNextButtonEnabled
		{
			get
			{
				return this._isNextButtonEnabled;
			}
			set
			{
				if (value != this._isNextButtonEnabled)
				{
					this._isNextButtonEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsNextButtonEnabled");
				}
			}
		}

		// Token: 0x170001B0 RID: 432
		// (get) Token: 0x060005B2 RID: 1458 RVA: 0x000151EC File Offset: 0x000133EC
		// (set) Token: 0x060005B3 RID: 1459 RVA: 0x000151F4 File Offset: 0x000133F4
		[DataSourceProperty]
		public string InstructionsText
		{
			get
			{
				return this._instructionsText;
			}
			set
			{
				if (value != this._instructionsText)
				{
					this._instructionsText = value;
					base.OnPropertyChangedWithValue<string>(value, "InstructionsText");
				}
			}
		}

		// Token: 0x170001B1 RID: 433
		// (get) Token: 0x060005B4 RID: 1460 RVA: 0x00015217 File Offset: 0x00013417
		// (set) Token: 0x060005B5 RID: 1461 RVA: 0x0001521F File Offset: 0x0001341F
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

		// Token: 0x170001B2 RID: 434
		// (get) Token: 0x060005B6 RID: 1462 RVA: 0x00015242 File Offset: 0x00013442
		// (set) Token: 0x060005B7 RID: 1463 RVA: 0x0001524A File Offset: 0x0001344A
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

		// Token: 0x170001B3 RID: 435
		// (get) Token: 0x060005B8 RID: 1464 RVA: 0x0001526D File Offset: 0x0001346D
		// (set) Token: 0x060005B9 RID: 1465 RVA: 0x00015275 File Offset: 0x00013475
		[DataSourceProperty]
		public string CurrentPageText
		{
			get
			{
				return this._currentPageText;
			}
			set
			{
				if (value != this._currentPageText)
				{
					this._currentPageText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentPageText");
				}
			}
		}

		// Token: 0x170001B4 RID: 436
		// (get) Token: 0x060005BA RID: 1466 RVA: 0x00015298 File Offset: 0x00013498
		// (set) Token: 0x060005BB RID: 1467 RVA: 0x000152A0 File Offset: 0x000134A0
		[DataSourceProperty]
		public MBBindingList<BoardGameInstructionVM> InstructionList
		{
			get
			{
				return this._instructionList;
			}
			set
			{
				if (value != this._instructionList)
				{
					this._instructionList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BoardGameInstructionVM>>(value, "InstructionList");
				}
			}
		}

		// Token: 0x040002CD RID: 717
		private readonly CultureObject.BoardGameType _boardGameType;

		// Token: 0x040002CE RID: 718
		private int _currentInstructionIndex;

		// Token: 0x040002CF RID: 719
		private bool _isPreviousButtonEnabled;

		// Token: 0x040002D0 RID: 720
		private bool _isNextButtonEnabled;

		// Token: 0x040002D1 RID: 721
		private string _instructionsText;

		// Token: 0x040002D2 RID: 722
		private string _previousText;

		// Token: 0x040002D3 RID: 723
		private string _nextText;

		// Token: 0x040002D4 RID: 724
		private string _currentPageText;

		// Token: 0x040002D5 RID: 725
		private MBBindingList<BoardGameInstructionVM> _instructionList;
	}
}
