using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.BoardGame
{
	// Token: 0x0200005F RID: 95
	public class BoardGameInstructionVM : ViewModel
	{
		// Token: 0x060005BC RID: 1468 RVA: 0x000152BE File Offset: 0x000134BE
		public BoardGameInstructionVM(CultureObject.BoardGameType game, int instructionIndex)
		{
			this._game = game;
			this._instructionIndex = instructionIndex;
			this.GameType = this._game.ToString();
			this.RefreshValues();
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x000152F4 File Offset: 0x000134F4
		public override void RefreshValues()
		{
			base.RefreshValues();
			GameTexts.SetVariable("newline", "\n");
			this.TitleText = GameTexts.FindText("str_board_game_title", this._game.ToString() + "_" + this._instructionIndex).ToString();
			this.DescriptionText = GameTexts.FindText("str_board_game_instruction", this._game.ToString() + "_" + this._instructionIndex).ToString();
		}

		// Token: 0x170001B5 RID: 437
		// (get) Token: 0x060005BE RID: 1470 RVA: 0x0001538C File Offset: 0x0001358C
		// (set) Token: 0x060005BF RID: 1471 RVA: 0x00015394 File Offset: 0x00013594
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

		// Token: 0x170001B6 RID: 438
		// (get) Token: 0x060005C0 RID: 1472 RVA: 0x000153B2 File Offset: 0x000135B2
		// (set) Token: 0x060005C1 RID: 1473 RVA: 0x000153BA File Offset: 0x000135BA
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

		// Token: 0x170001B7 RID: 439
		// (get) Token: 0x060005C2 RID: 1474 RVA: 0x000153DD File Offset: 0x000135DD
		// (set) Token: 0x060005C3 RID: 1475 RVA: 0x000153E5 File Offset: 0x000135E5
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

		// Token: 0x170001B8 RID: 440
		// (get) Token: 0x060005C4 RID: 1476 RVA: 0x00015408 File Offset: 0x00013608
		// (set) Token: 0x060005C5 RID: 1477 RVA: 0x00015410 File Offset: 0x00013610
		[DataSourceProperty]
		public string GameType
		{
			get
			{
				return this._gameType;
			}
			set
			{
				if (value != this._gameType)
				{
					this._gameType = value;
					base.OnPropertyChangedWithValue<string>(value, "GameType");
				}
			}
		}

		// Token: 0x040002D6 RID: 726
		private readonly CultureObject.BoardGameType _game;

		// Token: 0x040002D7 RID: 727
		private readonly int _instructionIndex;

		// Token: 0x040002D8 RID: 728
		private bool _isEnabled;

		// Token: 0x040002D9 RID: 729
		private string _titleText;

		// Token: 0x040002DA RID: 730
		private string _descriptionText;

		// Token: 0x040002DB RID: 731
		private string _gameType;
	}
}
