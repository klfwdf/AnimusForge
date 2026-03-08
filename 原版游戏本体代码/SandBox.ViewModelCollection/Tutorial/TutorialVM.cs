using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Tutorial
{
	// Token: 0x0200000C RID: 12
	public class TutorialVM : ViewModel
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000092 RID: 146 RVA: 0x0000566E File Offset: 0x0000386E
		// (set) Token: 0x06000093 RID: 147 RVA: 0x00005675 File Offset: 0x00003875
		public static TutorialVM Instance { get; set; }

		// Token: 0x06000094 RID: 148 RVA: 0x00005680 File Offset: 0x00003880
		public TutorialVM(Action onTutorialDisabled)
		{
			TutorialVM.Instance = this;
			this._onTutorialDisabled = onTutorialDisabled;
			this.LeftItem = new TutorialItemVM();
			this.RightItem = new TutorialItemVM();
			this.BottomItem = new TutorialItemVM();
			this.TopItem = new TutorialItemVM();
			this.LeftBottomItem = new TutorialItemVM();
			this.LeftTopItem = new TutorialItemVM();
			this.RightBottomItem = new TutorialItemVM();
			this.RightTopItem = new TutorialItemVM();
			this.CenterItem = new TutorialItemVM();
			GameTexts.SetVariable("newline", "\n");
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00005714 File Offset: 0x00003914
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.LeftItem.RefreshValues();
			this.RightItem.RefreshValues();
			this.BottomItem.RefreshValues();
			this.TopItem.RefreshValues();
			this.LeftBottomItem.RefreshValues();
			this.LeftTopItem.RefreshValues();
			this.RightBottomItem.RefreshValues();
			this.RightTopItem.RefreshValues();
			this.CenterItem.RefreshValues();
		}

		// Token: 0x06000096 RID: 150 RVA: 0x0000578C File Offset: 0x0000398C
		public void SetCurrentTutorial(TutorialItemVM.ItemPlacements placement, string tutorialTypeId, bool requiresMouse)
		{
			if (this._currentTutorialItem != null)
			{
				TutorialItemVM currentTutorialItem = this._currentTutorialItem;
				if (currentTutorialItem != null)
				{
					currentTutorialItem.CloseTutorialPanel();
				}
				this._currentTutorialItem = null;
			}
			TutorialItemVM item = this.GetItem(placement);
			if (!item.IsEnabled)
			{
				this._currentTutorialItem = item;
				this._currentTutorialItem.Init(tutorialTypeId, requiresMouse, new Action(this.FinalizeTutorial));
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x000057E9 File Offset: 0x000039E9
		private void ResetCurrentTutorial()
		{
			this._currentTutorialItem.CloseTutorialPanel();
			this._currentTutorialItem = null;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00005800 File Offset: 0x00003A00
		private TutorialItemVM GetItem(TutorialItemVM.ItemPlacements placement)
		{
			switch (placement)
			{
			case TutorialItemVM.ItemPlacements.Left:
				return this.LeftItem;
			case TutorialItemVM.ItemPlacements.Right:
				return this.RightItem;
			case TutorialItemVM.ItemPlacements.Top:
				return this.TopItem;
			case TutorialItemVM.ItemPlacements.Bottom:
				return this.BottomItem;
			case TutorialItemVM.ItemPlacements.TopLeft:
				return this.LeftTopItem;
			case TutorialItemVM.ItemPlacements.TopRight:
				return this.RightTopItem;
			case TutorialItemVM.ItemPlacements.BottomLeft:
				return this.LeftBottomItem;
			case TutorialItemVM.ItemPlacements.BottomRight:
				return this.RightBottomItem;
			case TutorialItemVM.ItemPlacements.Center:
				return this.CenterItem;
			default:
				return null;
			}
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00005879 File Offset: 0x00003A79
		public void Tick(float dt)
		{
		}

		// Token: 0x0600009A RID: 154 RVA: 0x0000587B File Offset: 0x00003A7B
		public void CloseTutorialStep(bool finalizeAllSteps = false)
		{
			TutorialItemVM currentTutorialItem = this._currentTutorialItem;
			if (currentTutorialItem != null)
			{
				currentTutorialItem.CloseTutorialPanel();
			}
			this._currentTutorialItem = null;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00005895 File Offset: 0x00003A95
		public void FinalizeTutorial()
		{
			this._onTutorialDisabled();
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600009C RID: 156 RVA: 0x000058A2 File Offset: 0x00003AA2
		// (set) Token: 0x0600009D RID: 157 RVA: 0x000058AA File Offset: 0x00003AAA
		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return this._isVisible;
			}
			set
			{
				if (value != this._isVisible)
				{
					this._isVisible = value;
					base.OnPropertyChangedWithValue(value, "IsVisible");
				}
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600009E RID: 158 RVA: 0x000058C8 File Offset: 0x00003AC8
		// (set) Token: 0x0600009F RID: 159 RVA: 0x000058D0 File Offset: 0x00003AD0
		[DataSourceProperty]
		public TutorialItemVM LeftItem
		{
			get
			{
				return this._leftItem;
			}
			set
			{
				if (value != this._leftItem)
				{
					this._leftItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "LeftItem");
				}
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x000058EE File Offset: 0x00003AEE
		// (set) Token: 0x060000A1 RID: 161 RVA: 0x000058F6 File Offset: 0x00003AF6
		[DataSourceProperty]
		public TutorialItemVM RightItem
		{
			get
			{
				return this._rightItem;
			}
			set
			{
				if (value != this._rightItem)
				{
					this._rightItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "RightItem");
				}
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000A2 RID: 162 RVA: 0x00005914 File Offset: 0x00003B14
		// (set) Token: 0x060000A3 RID: 163 RVA: 0x0000591C File Offset: 0x00003B1C
		[DataSourceProperty]
		public TutorialItemVM BottomItem
		{
			get
			{
				return this._bottomItem;
			}
			set
			{
				if (value != this._bottomItem)
				{
					this._bottomItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "BottomItem");
				}
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000A4 RID: 164 RVA: 0x0000593A File Offset: 0x00003B3A
		// (set) Token: 0x060000A5 RID: 165 RVA: 0x00005942 File Offset: 0x00003B42
		[DataSourceProperty]
		public TutorialItemVM TopItem
		{
			get
			{
				return this._topItem;
			}
			set
			{
				if (value != this._topItem)
				{
					this._topItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "TopItem");
				}
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x00005960 File Offset: 0x00003B60
		// (set) Token: 0x060000A7 RID: 167 RVA: 0x00005968 File Offset: 0x00003B68
		[DataSourceProperty]
		public TutorialItemVM LeftBottomItem
		{
			get
			{
				return this._leftBottomItem;
			}
			set
			{
				if (value != this._leftBottomItem)
				{
					this._leftBottomItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "LeftBottomItem");
				}
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000A8 RID: 168 RVA: 0x00005986 File Offset: 0x00003B86
		// (set) Token: 0x060000A9 RID: 169 RVA: 0x0000598E File Offset: 0x00003B8E
		[DataSourceProperty]
		public TutorialItemVM LeftTopItem
		{
			get
			{
				return this._leftTopItem;
			}
			set
			{
				if (value != this._leftTopItem)
				{
					this._leftTopItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "LeftTopItem");
				}
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000AA RID: 170 RVA: 0x000059AC File Offset: 0x00003BAC
		// (set) Token: 0x060000AB RID: 171 RVA: 0x000059B4 File Offset: 0x00003BB4
		[DataSourceProperty]
		public TutorialItemVM RightBottomItem
		{
			get
			{
				return this._rightBottomItem;
			}
			set
			{
				if (value != this._rightBottomItem)
				{
					this._rightBottomItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "RightBottomItem");
				}
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000AC RID: 172 RVA: 0x000059D2 File Offset: 0x00003BD2
		// (set) Token: 0x060000AD RID: 173 RVA: 0x000059DA File Offset: 0x00003BDA
		[DataSourceProperty]
		public TutorialItemVM RightTopItem
		{
			get
			{
				return this._rightTopItem;
			}
			set
			{
				if (value != this._rightTopItem)
				{
					this._rightTopItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "RightTopItem");
				}
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000AE RID: 174 RVA: 0x000059F8 File Offset: 0x00003BF8
		// (set) Token: 0x060000AF RID: 175 RVA: 0x00005A00 File Offset: 0x00003C00
		[DataSourceProperty]
		public TutorialItemVM CenterItem
		{
			get
			{
				return this._centerItem;
			}
			set
			{
				if (value != this._centerItem)
				{
					this._centerItem = value;
					base.OnPropertyChangedWithValue<TutorialItemVM>(value, "CenterItem");
				}
			}
		}

		// Token: 0x04000044 RID: 68
		private const float TutorialDelayInSeconds = 0f;

		// Token: 0x04000045 RID: 69
		private TutorialItemVM _currentTutorialItem;

		// Token: 0x04000046 RID: 70
		private Action _onTutorialDisabled;

		// Token: 0x04000047 RID: 71
		private bool _isVisible;

		// Token: 0x04000048 RID: 72
		private TutorialItemVM _leftItem;

		// Token: 0x04000049 RID: 73
		private TutorialItemVM _rightItem;

		// Token: 0x0400004A RID: 74
		private TutorialItemVM _bottomItem;

		// Token: 0x0400004B RID: 75
		private TutorialItemVM _topItem;

		// Token: 0x0400004C RID: 76
		private TutorialItemVM _leftBottomItem;

		// Token: 0x0400004D RID: 77
		private TutorialItemVM _leftTopItem;

		// Token: 0x0400004E RID: 78
		private TutorialItemVM _rightBottomItem;

		// Token: 0x0400004F RID: 79
		private TutorialItemVM _rightTopItem;

		// Token: 0x04000050 RID: 80
		private TutorialItemVM _centerItem;
	}
}
