using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Incidents
{
	// Token: 0x0200004C RID: 76
	public class MapIncidentOptionVM : ViewModel
	{
		// Token: 0x060004A7 RID: 1191 RVA: 0x000122EA File Offset: 0x000104EA
		public MapIncidentOptionVM(TextObject description, List<TextObject> hints, int index, Action<MapIncidentOptionVM> onSelected, Action<MapIncidentOptionVM> onFocused)
		{
			this.Index = index;
			this._descriptionText = description;
			this._hints = hints.ToList<TextObject>();
			this._onSelected = onSelected;
			this._onFocused = onFocused;
		}

		// Token: 0x060004A8 RID: 1192 RVA: 0x0001231C File Offset: 0x0001051C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Description = this._descriptionText.ToString();
			this.Hint = CampaignUIHelper.MergeTextObjectsWithNewline(this._hints);
		}

		// Token: 0x060004A9 RID: 1193 RVA: 0x00012346 File Offset: 0x00010546
		public override void OnFinalize()
		{
			base.OnFinalize();
		}

		// Token: 0x060004AA RID: 1194 RVA: 0x0001234E File Offset: 0x0001054E
		public void ExecuteSelect()
		{
			this._onSelected(this);
		}

		// Token: 0x060004AB RID: 1195 RVA: 0x0001235C File Offset: 0x0001055C
		public void ExecuteFocus()
		{
			this._onFocused(this);
		}

		// Token: 0x060004AC RID: 1196 RVA: 0x0001236A File Offset: 0x0001056A
		public void ExecuteUnfocus()
		{
			this._onFocused(null);
		}

		// Token: 0x1700015B RID: 347
		// (get) Token: 0x060004AD RID: 1197 RVA: 0x00012378 File Offset: 0x00010578
		// (set) Token: 0x060004AE RID: 1198 RVA: 0x00012380 File Offset: 0x00010580
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x060004AF RID: 1199 RVA: 0x0001239E File Offset: 0x0001059E
		// (set) Token: 0x060004B0 RID: 1200 RVA: 0x000123A6 File Offset: 0x000105A6
		[DataSourceProperty]
		public bool IsFocused
		{
			get
			{
				return this._isFocused;
			}
			set
			{
				if (value != this._isFocused)
				{
					this._isFocused = value;
					base.OnPropertyChangedWithValue(value, "IsFocused");
				}
			}
		}

		// Token: 0x1700015D RID: 349
		// (get) Token: 0x060004B1 RID: 1201 RVA: 0x000123C4 File Offset: 0x000105C4
		// (set) Token: 0x060004B2 RID: 1202 RVA: 0x000123CC File Offset: 0x000105CC
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

		// Token: 0x1700015E RID: 350
		// (get) Token: 0x060004B3 RID: 1203 RVA: 0x000123EF File Offset: 0x000105EF
		// (set) Token: 0x060004B4 RID: 1204 RVA: 0x000123F7 File Offset: 0x000105F7
		[DataSourceProperty]
		public string Hint
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
					base.OnPropertyChangedWithValue<string>(value, "Hint");
				}
			}
		}

		// Token: 0x0400024C RID: 588
		public readonly int Index;

		// Token: 0x0400024D RID: 589
		private readonly TextObject _descriptionText;

		// Token: 0x0400024E RID: 590
		private readonly List<TextObject> _hints;

		// Token: 0x0400024F RID: 591
		private readonly Action<MapIncidentOptionVM> _onSelected;

		// Token: 0x04000250 RID: 592
		private readonly Action<MapIncidentOptionVM> _onFocused;

		// Token: 0x04000251 RID: 593
		private bool _isSelected;

		// Token: 0x04000252 RID: 594
		private bool _isFocused;

		// Token: 0x04000253 RID: 595
		private string _description;

		// Token: 0x04000254 RID: 596
		private string _hint;
	}
}
